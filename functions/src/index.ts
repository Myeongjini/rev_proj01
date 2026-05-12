import * as admin from "firebase-admin";
import {setGlobalOptions} from "firebase-functions/v2";
import {HttpsError, onCall} from "firebase-functions/v2/https";

export {rollGacha} from "./gacha";
export {enhanceItem} from "./enhancement";
export {
  spendCurrency,
  grantCurrency,
  grantDeveloperCurrency,
  claimMissionReward,
  claimAttendanceReward,
  claimDungeonReward,
  claimOfflineReward,
  claimEnemyReward,
  migrateWallet,
} from "./currency";

admin.initializeApp();
setGlobalOptions({region: "asia-northeast3", maxInstances: 10});

export const getServerInfo = onCall(async (request) => {
  if (!request.auth) {
    throw new HttpsError("unauthenticated", "Login required.");
  }

  return {
    serverTime: Date.now(),
    version: "1.0.0",
    callerUid: request.auth.uid,
  };
});
