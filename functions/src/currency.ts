import * as admin from "firebase-admin";
import {HttpsError, onCall} from "firebase-functions/v2/https";
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
