import * as admin from "firebase-admin";
import {CallableRequest, HttpsError, onCall} from "firebase-functions/v2/https";
import {
  CurrencyKind,
  grantCurrencyInternal,
  spendCurrencyInternal,
} from "./internal/grantCurrencyInternal";

interface CurrencyPayload {
  kind?: CurrencyKind;
  amount?: number;
  reason?: string;
  source?: string;
}

export const spendCurrency = onCall({region: "asia-northeast3"}, async (request) => {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  const payload = normalizePayload(request.data);
  try {
    return await admin.firestore().runTransaction(async (tx) => {
      return spendCurrencyInternal(
        tx,
        uid,
        payload.kind,
        payload.amount,
        payload.reason,
        "client"
      );
    });
  } catch (error) {
    if (error instanceof Error && error.message === "insufficient-currency") {
      throw new HttpsError("failed-precondition", "Insufficient currency.");
    }
    throw error;
  }
});

export const grantCurrency = onCall({region: "asia-northeast3"}, async (request) => {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  if (request.auth?.token?.serverInternal !== true) {
    throw new HttpsError(
      "permission-denied",
      "Client grants are not allowed."
    );
  }

  const payload = normalizePayload(request.data);
  return admin.firestore().runTransaction(async (tx) => {
    return grantCurrencyInternal(
      tx,
      uid,
      payload.kind,
      payload.amount,
      payload.reason,
      payload.source
    );
  });
});

export const claimMissionReward = onCall({region: "asia-northeast3"}, async (request) => {
  return grantReward(request, "mission", `mission_${String(request.data?.missionId ?? "unknown")}`);
});

export const claimAttendanceReward = onCall({region: "asia-northeast3"}, async (request) => {
  const dayIndex = Math.max(0, Math.floor(Number(request.data?.dayIndex ?? 0)));
  return grantReward(request, "attendance", `attendance_day${dayIndex}`);
});

export const claimDungeonReward = onCall({region: "asia-northeast3"}, async (request) => {
  const dungeonType = String(request.data?.dungeonType ?? "gold");
  return grantReward(request, "dungeon", dungeonType === "exp" ? "exp_dungeon" : "gold_dungeon");
});

export const claimOfflineReward = onCall({region: "asia-northeast3"}, async (request) => {
  return grantReward(request, "offline", "offline_reward");
});

export const claimEnemyReward = onCall({region: "asia-northeast3"}, async (request) => {
  const incoming = normalizePayload(request.data);
  const source = incoming.source === "boss_reward" ? "boss_reward" : "enemy_reward";
  return grantReward(request, source, incoming.reason || "enemy_reward");
});

export const migrateWallet = onCall({region: "asia-northeast3"}, async (request) => {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  const seedGold = Math.max(0, Math.floor(Number(request.data?.gold ?? 0)));
  const seedGem = Math.max(0, Math.floor(Number(request.data?.gem ?? 0)));
  return admin.firestore().runTransaction(async (tx) => {
    const db = admin.firestore();
    const walletRef = db.collection("users").doc(uid).collection("wallet").doc("main");
    const snapshot = await tx.get(walletRef);
    if (snapshot.exists) {
      return {
        gold: Number(snapshot.get("gold") ?? 0),
        gem: Number(snapshot.get("gem") ?? 0),
      };
    }

    let gold = 0;
    let gem = 0;
    if (seedGold > 0) {
      gold = (await grantCurrencyInternal(tx, uid, "gold", seedGold, "wallet_migration_v8", "migration")).balanceAfter;
    }
    if (seedGem > 0) {
      gem = (await grantCurrencyInternal(tx, uid, "gem", seedGem, "wallet_migration_v8", "migration")).balanceAfter;
    }
    if (seedGold <= 0 && seedGem <= 0) {
      tx.set(walletRef, {
        gold: 0,
        gem: 0,
        lastUpdatedMs: Date.now(),
      }, {merge: true});
    }
    return {gold, gem};
  });
});

async function grantReward(
  request: CallableRequest,
  forcedSource: string,
  forcedReason: string
): Promise<{balanceAfter: number}> {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  const incoming = normalizePayload(request.data);
  return admin.firestore().runTransaction(async (tx) => {
    return grantCurrencyInternal(
      tx,
      uid,
      incoming.kind,
      incoming.amount,
      forcedReason,
      forcedSource
    );
  });
}

function normalizePayload(data: CurrencyPayload): Required<CurrencyPayload> {
  const kind = data.kind === "gem" ? "gem" : "gold";
  const amount = Math.max(0, Math.floor(Number(data.amount ?? 0)));
  if (amount <= 0) {
    throw new HttpsError("invalid-argument", "Amount must be positive.");
  }

  return {
    kind,
    amount,
    reason: String(data.reason ?? "unknown"),
    source: String(data.source ?? "client"),
  };
}
