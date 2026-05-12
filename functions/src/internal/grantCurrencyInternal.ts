import * as admin from "firebase-admin";
import {FieldValue} from "firebase-admin/firestore";

export type CurrencyKind = "gold" | "gem" | "enhancement_stone";

export interface CurrencyMutationResult {
  balanceAfter: number;
}

export async function grantCurrencyInternal(
  tx: admin.firestore.Transaction,
  uid: string,
  kind: CurrencyKind,
  amount: number,
  reason: string,
  source: string
): Promise<CurrencyMutationResult> {
  const normalized = Math.max(0, Math.floor(amount));
  const db = admin.firestore();
  const walletRef = db.collection("users").doc(uid).collection("wallet").doc("main");
  const snapshot = await tx.get(walletRef);
  const current = snapshot.exists ? Number(snapshot.get(kind) ?? 0) : 0;
  const balanceAfter = current + normalized;
  tx.set(walletRef, {
    [kind]: balanceAfter,
    lastUpdatedMs: Date.now(),
  }, {merge: true});
  writeTransaction(tx, uid, kind, normalized, reason, source, balanceAfter);
  return {balanceAfter};
}

export async function spendCurrencyInternal(
  tx: admin.firestore.Transaction,
  uid: string,
  kind: CurrencyKind,
  amount: number,
  reason: string,
  source: string
): Promise<CurrencyMutationResult> {
  const normalized = Math.max(0, Math.floor(amount));
  const db = admin.firestore();
  const walletRef = db.collection("users").doc(uid).collection("wallet").doc("main");
  const snapshot = await tx.get(walletRef);
  const current = snapshot.exists ? Number(snapshot.get(kind) ?? 0) : 0;
  if (current < normalized) {
    throw new Error("insufficient-currency");
  }

  const balanceAfter = current - normalized;
  tx.set(walletRef, {
    [kind]: balanceAfter,
    lastUpdatedMs: Date.now(),
  }, {merge: true});
  writeTransaction(tx, uid, kind, -normalized, reason, source, balanceAfter);
  return {balanceAfter};
}

function writeTransaction(
  tx: admin.firestore.Transaction,
  uid: string,
  kind: CurrencyKind,
  delta: number,
  reason: string,
  source: string,
  balanceAfter: number
): void {
  const db = admin.firestore();
  const ref = db.collection("users").doc(uid).collection("transactions").doc();
  tx.set(ref, {
    kind,
    delta,
    reason,
    source,
    timestamp: FieldValue.serverTimestamp(),
    balanceAfter,
  });
}
