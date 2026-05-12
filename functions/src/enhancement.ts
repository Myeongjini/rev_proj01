import * as admin from "firebase-admin";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import {spendCurrencyInternal} from "./internal/grantCurrencyInternal";

const MAX_LEVEL = 10;
const BASE_COST = 100;
const COST_GROWTH = 1.5;

export const enhanceItem = onCall({region: "asia-northeast3"}, async (request) => {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  const slotKind = normalizeSlotKind(request.data?.slotKind);
  const itemId = String(request.data?.itemId ?? "").trim();
  const currentLevel = clampLevel(Math.floor(Number(request.data?.currentLevel ?? 0)));
  if (!itemId) {
    throw new HttpsError("invalid-argument", "itemId is required.");
  }
  if (currentLevel >= MAX_LEVEL) {
    throw new HttpsError("failed-precondition", "Enhancement cap reached.");
  }

  const cost = getCost(currentLevel);
  try {
    return await admin.firestore().runTransaction(async (tx) => {
      const spend = await spendCurrencyInternal(
        tx,
        uid,
        "enhancement_stone",
        cost,
        `enhance_${slotKind}_${itemId}_${currentLevel}`,
        "enhanceItem"
      );
      return {
        slotKind,
        itemId,
        previousLevel: currentLevel,
        nextLevel: currentLevel + 1,
        cost,
        balanceAfter: spend.balanceAfter,
      };
    });
  } catch (error) {
    if (error instanceof Error && error.message === "insufficient-currency") {
      throw new HttpsError("failed-precondition", "Insufficient enhancement stone.");
    }
    throw error;
  }
});

function normalizeSlotKind(value: unknown): string {
  const slotKind = String(value ?? "weapon").toLowerCase();
  if (slotKind === "weapon" || slotKind === "armor" || slotKind === "accessory") {
    return slotKind;
  }
  throw new HttpsError("invalid-argument", "Invalid slotKind.");
}

function getCost(currentLevel: number): number {
  return Math.max(1, Math.round(BASE_COST * Math.pow(COST_GROWTH, clampLevel(currentLevel))));
}

function clampLevel(level: number): number {
  return Math.min(MAX_LEVEL, Math.max(0, Math.floor(level)));
}
