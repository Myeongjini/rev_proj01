import * as admin from "firebase-admin";
import {FieldValue} from "firebase-admin/firestore";
import {HttpsError, onCall} from "firebase-functions/v2/https";
import definition from "../data/gachaDefinitions.json";
import {spendCurrencyInternal} from "./internal/grantCurrencyInternal";
import {weightedRandom} from "./utils/weightedRandom";

type UpperGrade = "Common" | "Normal" | "Advanced" | "Epic" | "Unique";
type LowerGrade = "Beginner" | "Intermediate" | "Upper" | "Supreme";

interface WeaponEntry {
  weaponId: string;
  upperGrade: UpperGrade;
  lowerGrade: LowerGrade;
}

interface SummonLevelEntry {
  level: number;
  pullsToNextLevel: number;
  maxUpperGrade: UpperGrade;
  upperGradeWeights: Array<{upperGrade: UpperGrade; weight: number}>;
}

interface GachaDefinitionJson {
  id: string;
  costs: Record<string, number>;
  weapons: WeaponEntry[];
  summonLevels: SummonLevelEntry[];
}

const gachaDefinition = definition as GachaDefinitionJson;
const upperOrder: UpperGrade[] = ["Common", "Normal", "Advanced", "Epic", "Unique"];
const lowerGrades: LowerGrade[] = ["Beginner", "Intermediate", "Upper", "Supreme"];

export const rollGacha = onCall({region: "asia-northeast3"}, async (request) => {
  const uid = request.auth?.uid;
  if (!uid) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  const count = Number(request.data?.count ?? 0);
  const gachaId = String(request.data?.gachaId ?? "standard");
  if (![1, 10, 30].includes(count) || gachaId !== gachaDefinition.id) {
    throw new HttpsError("invalid-argument", "Invalid gacha request.");
  }

  const cost = gachaDefinition.costs[String(count)] ?? 0;
  const db = admin.firestore();
  const userRef = db.collection("users").doc(uid);
  const summonRef = userRef.collection("gacha").doc(gachaId);

  return db.runTransaction(async (tx) => {
    const summonSnapshot = await tx.get(summonRef);
    let summonLevel = Math.max(1, Number(summonSnapshot.get("summonLevel") ?? 1));
    let pullsInLevel = Math.max(0, Number(summonSnapshot.get("summonPullsInLevel") ?? 0));

    const spendResult = await spendCurrencyInternal(tx, uid, "gem", cost, `gacha_${gachaId}_${count}`, "rollGacha");

    const pulls: WeaponEntry[] = [];
    for (let i = 0; i < count; i++) {
      const level = getLevel(summonLevel);
      const upperGrade = rollUpperGrade(level);
      const lowerGrade = weightedRandom(lowerGrades.map((grade) => ({value: grade, weight: 1})));
      const weapon = getWeapon(upperGrade, lowerGrade) ?? getFallbackWeapon(level);
      pulls.push(weapon);
      tx.set(userRef.collection("inventory").doc("weapons").collection("items").doc(weapon.weaponId), {
        weaponId: weapon.weaponId,
        count: FieldValue.increment(1),
        updatedAtMs: Date.now(),
      }, {merge: true});
      pullsInLevel++;
      const advanced = advanceLevel(summonLevel, pullsInLevel);
      summonLevel = advanced.level;
      pullsInLevel = advanced.pullsInLevel;
    }

    tx.set(summonRef, {
      summonLevel,
      summonPullsInLevel: pullsInLevel,
      updatedAtMs: Date.now(),
    }, {merge: true});

    return {
      pulls,
      newGemBalance: spendResult.balanceAfter,
      newSummonLevel: summonLevel,
      newSummonPullsInLevel: pullsInLevel,
    };
  });
});

function getLevel(level: number): SummonLevelEntry {
  const exact = gachaDefinition.summonLevels.find((entry) => entry.level === level);
  if (exact) {
    return exact;
  }

  const sorted = [...gachaDefinition.summonLevels].sort((a, b) => a.level - b.level);
  return sorted.find((entry) => entry.level > level) ?? sorted[sorted.length - 1];
}

function rollUpperGrade(level: SummonLevelEntry): UpperGrade {
  const maxIndex = upperOrder.indexOf(level.maxUpperGrade);
  const entries = level.upperGradeWeights
    .filter((entry) => upperOrder.indexOf(entry.upperGrade) <= maxIndex)
    .map((entry) => ({value: entry.upperGrade, weight: entry.weight}));
  return weightedRandom(entries);
}

function getWeapon(upperGrade: UpperGrade, lowerGrade: LowerGrade): WeaponEntry | undefined {
  return gachaDefinition.weapons.find((weapon) =>
    weapon.upperGrade === upperGrade && weapon.lowerGrade === lowerGrade
  );
}

function getFallbackWeapon(level: SummonLevelEntry): WeaponEntry {
  const maxIndex = upperOrder.indexOf(level.maxUpperGrade);
  const candidates = gachaDefinition.weapons.filter((weapon) =>
    upperOrder.indexOf(weapon.upperGrade) <= maxIndex
  );
  return candidates[Math.floor(Math.random() * candidates.length)] ?? gachaDefinition.weapons[0];
}

function advanceLevel(level: number, pullsInLevel: number): {level: number; pullsInLevel: number} {
  let current = getLevel(level);
  let currentLevel = current.level;
  let currentPulls = pullsInLevel;
  while (current.pullsToNextLevel > 0 && currentPulls >= current.pullsToNextLevel) {
    currentPulls -= current.pullsToNextLevel;
    const next = gachaDefinition.summonLevels
      .filter((entry) => entry.level > currentLevel)
      .sort((a, b) => a.level - b.level)[0];
    if (!next) {
      return {level: currentLevel, pullsInLevel: 0};
    }
    current = next;
    currentLevel = next.level;
  }
  return {level: currentLevel, pullsInLevel: currentPulls};
}
