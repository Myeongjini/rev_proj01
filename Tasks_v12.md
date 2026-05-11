# Wizard Grower — Tasks v12 (Bundle 12: Cloud Functions Authority Migration)

> Follow-up work track to `Tasks_v11.md`.
> Different agents, different bundle, different file.
> **Do NOT edit prior Tasks*.md from this track.** Read-only access only.
>
> 장기 로드맵 (Bundle 9~30) 은 `References.md` §4.1 참조. 본 문서는 Bundle 12 만 다룸.

---

## 0. Common Work Rules

(Tasks_v9~v11 의 §0 규칙을 그대로 준수. 차이만 명시:)

### 0.5 Cross-Track Coordination ⚠️
Bundle 12는 **클라이언트 권위 → 서버 권위** 마이그레이션. 가장 큰 아키텍처 변경.

| Risk | Mitigation |
|---|---|
| Cloud Functions 신규 인프라 — 사용자 사전 작업 필수 | **사용자 prework**: (1) Firebase Functions 활성화 (`firebase init functions`), (2) Blaze 요금제 전환 — Cloud Functions는 Spark 무료 플랜에서 동작하지 않음. (3) Region: `asia-northeast3` (Seoul) 권장. Task AN 시작 전 위 3가지가 완료되어 있어야 함 |
| 가챠 롤 권위 이전 — 기존 `GachaService.PullOne` 결과가 다름 | 클라이언트는 결과만 받아 표시. Bundle 7 `GachaResultPanel` UI 그대로 작동. Bundle 8 W의 30뽑기 비용 변경(100/1000/3000)도 Cloud Function 에 반영 |
| 통화 변경 모든 path 가 서버 호출로 변경 — 미션/출석/오프라인/가챠/던전 전부 영향 | 점진적 이행: Task AP는 신규 ICurrencyAuthority 인터페이스 도입. CurrencyWallet 가 인터페이스 구현체를 갖고 있음. Bundle 12 단계 = ServerCurrencyAuthority. 기존 LocalCurrencyAuthority (캐시 옵션) 는 폴백으로 유지 — 오프라인 모드 |
| Save 동기화 vs 서버 권위 — 충돌 시 정책 | 서버 권위 우선. SyncCoordinator (Bundle 5 Task I) 의 충돌 해결 정책을 "Server wins for currencies + inventory" 로 강화. 클라이언트 캐시는 UI 표시용 |
| Firebase Functions 비용 — 가챠 1회 = function call 1회. 예상 호출량은 무시 가능 (인디 규모) |
| 로컬 테스트 — Firebase Emulator Suite 사용 | Task AN에 emulator 설정 포함. CI 없이 수동 테스트로 충분 |

---

## 1. Task Dependency Graph

```
Bundle 12
AN → AO → AP → AQ → Bundle 12 Release Gate

AN:  Firebase Functions Project Setup + Local Emulator
AO:  Server-Authoritative Gacha Roll (rollGacha Callable Function)
AP:  Server-Authoritative Currency + Transaction Log
AQ:  Save Schema v8 Migration + Server Reconciliation + Cross-Feature Regression
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 12 | AN | Firebase Functions Project Setup | ⚠️ BLOCKED | local ✅ / live deploy pending |
| 12 | AO | Server-Authoritative Gacha Roll | 🟡 IN REVIEW (fallback guard 적용 완료) | AN ⚠️ |
| 12 | AP | Server-Authoritative Currency + Transaction Log | ✅ DONE (local) — Live 검증은 AN deploy 후 | AO ✅ |
| 12 | AQ | Save Schema v8 + Server Reconciliation + Regression | ✅ DONE (local) — Live 검증은 AN deploy 후 | AP ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 12 — Cloud Functions Authority Migration

**Goal:** 가챠 롤 + 통화 변경 + 인벤토리 변경을 Cloud Functions 권위로 이전. 수익화 입웹 (Bundle 22 IAP) 의 사전 인프라.

### Bundle 12 사용자 Prework ⚠️

**Bundle 12 시작 전 사용자가 다음을 완료해야 함:**

1. Firebase Console → 프로젝트 → 요금제 전환: Spark → **Blaze 요금제 (사용량 기반)**
   - Cloud Functions 는 Spark 무료 플랜에서 작동하지 않음
   - 인디 규모 사용량은 거의 무료 수준 ($1~5/월 예상)
2. 로컬 환경에서 `firebase init functions` 실행 — 다음 옵션 선택:
   - Language: TypeScript
   - ESLint: Yes
   - Install dependencies: Yes
3. `firebase login` 으로 Firebase CLI 인증
4. Region: `asia-northeast3` (Seoul) 권장. `firebase.json` 또는 `functions/index.ts` 의 region 명시.
5. `firebase emulators:start --only functions,firestore` 가 로컬에서 실행되는지 확인

위 prework 가 완료된 후 Task AN 진입. 미완료 시 Task AN을 ⚠️ BLOCKED 처리.

### Bundle 12 Release Regression Tests
1. 로컬 Emulator 에서 가챠 1회 호출 → Cloud Function `rollGacha` 가 서버에서 결과 결정 → 클라이언트 화면에 결과 표시
2. 동일 가챠 호출을 30회 → 가중치 분포가 GachaDefinition 의 SummonLevel 가중치와 일치 (±5% 변동)
3. 가챠 결과: 서버가 직접 Firestore /users/{uid}/inventory/weapons 에 weapon 추가 → 클라이언트 WeaponInventory 가 Firestore listener 로 자동 갱신
4. Live (Production) 에서 가챠 1회 호출 → 정상 동작 (사용자 직접 시연)
5. 통화 변경: 가챠 비용 차감 → spendCurrency Cloud Function 호출 → /users/{uid}/transactions 에 기록 + /users/{uid}/wallet 차감
6. 미션 보상 (Bundle 8 Z) / 출석 보상 (Bundle 8 AA) / 던전 보상 (Bundle 10/11) / 오프라인 보상 (Bundle 9/11) 모두 grantCurrency Cloud Function 경유
7. 클라이언트 임의로 wallet.gold = 999999 변경 시도 → 다음 동기화에서 서버 값으로 복구
8. saveVersion = 8
9. v7 save 마이그레이션: 클라이언트 wallet 잔액을 서버 reconciliation 으로 권위화 (서버 값이 우선)
10. (Cross-feature regression) Bundle 5/6/7/8/9/10/11 모두 정상 — UI는 변화 없으나 통화·가챠·인벤토리 모든 동작이 서버 경유로 바뀜

---

## Task AN — Firebase Functions Project Setup + Local Emulator

**Status:** ⚠️ BLOCKED — local implementation complete, live deploy verification pending
**Depends On:** v11 baseline + 사용자 prework (Blaze 요금제 + functions init 완료)

### 🎯 Goal
Cloud Functions 인프라 셋업. TypeScript 환경 + Firebase Admin SDK + 로컬 Emulator 동작. 첫 health-check 함수 배포.

### ✅ Definition of Done
- [ ] `functions/` 디렉토리 신규 — Firebase Functions TypeScript 프로젝트 (사용자 prework 완료 후 자동 생성됨)
- [ ] `functions/src/index.ts` 에 health-check 함수: `getServerInfo` Callable Function
- [ ] `firebase.json` 의 functions region = `asia-northeast3`
- [ ] `firebase emulators:start` 로 로컬 실행 → Unity 클라이언트가 Emulator URL 호출 가능
- [ ] Unity 측: `Assets/Scripts/Cloud/CloudFunctionsClient.cs` — Firebase Functions SDK 호출 wrapper. Auth token 자동 첨부
- [ ] PlayMode 에서 health-check 함수 호출 → 응답 받음 (`{ serverTime: <ms>, version: "1.0.0" }`)
- [ ] Live deploy: `firebase deploy --only functions` 성공
- [ ] Live 에서도 호출 성공
- [ ] `.gitignore` 에 `functions/node_modules/` + `functions/lib/` 추가

### 📂 Files to Add
- `functions/package.json` (자동 생성)
- `functions/src/index.ts`
  ```typescript
  import * as functions from "firebase-functions";
  import * as admin from "firebase-admin";
  admin.initializeApp();

  export const getServerInfo = functions
      .region("asia-northeast3")
      .https.onCall(async (data, context) => {
          if (!context.auth) {
              throw new functions.https.HttpsError("unauthenticated", "Login required.");
          }
          return {
              serverTime: Date.now(),
              version: "1.0.0",
              callerUid: context.auth.uid
          };
      });
  ```
- `functions/tsconfig.json` (자동 생성)
- `Assets/Scripts/Cloud/CloudFunctionsClient.cs`
  ```csharp
  public class CloudFunctionsClient : MonoBehaviour
  {
      private const string FunctionsRegion = "asia-northeast3";

      // Reflection-based wrapper for Firebase.Functions.FirebaseFunctions
      // Mirrors PresenceService / ChatService pattern (Bundle 6 K/M)

      public Task<TResult> CallAsync<TResult>(string functionName, object payload);
  }
  ```

### 📂 Files to Modify
- `firebase.json` — `functions` 섹션 추가
- `.gitignore` — functions/node_modules/, functions/lib/ 추가
- `Assets/Scripts/Core/GameContext.cs` — `CloudFunctionsClient` 등록
- `Assets/Scripts/Core/GameManager.cs` — Auth 후 client.Initialize 호출

### 🚫 Do Not Touch
- Bundle 5 H 의 AuthService 내부
- 기타 Bundle 1~11 시스템

### 🧪 Validation
1. Compile clean (Unity + functions tsc)
2. `cd functions && npm run build` 성공
3. `firebase emulators:start --only functions,firestore` 로 로컬 실행
4. Unity PlayMode (Auth 후) → CloudFunctionsClient.CallAsync<HealthInfo>("getServerInfo") → 응답 `serverTime`/`version`/`callerUid` 검증
5. `firebase deploy --only functions` → 성공 → Live 에서도 동일 호출 성공

### Implementation note
**Firebase.Functions SDK 통합:** `Firebase.Functions.unitypackage` import 필요. Bundle 5 H 의 Firebase Auth 통합 시 같이 들였을 가능성 있음. 미설치 시 사용자 prework 추가 필요.

**Emulator vs Live 토글:** Unity 측 `CloudFunctionsClient` 가 build 환경에 따라 Emulator 사용 여부 결정. `Application.isEditor` 일 때 `useEmulator = true` (localhost:5001) 기본값. Live build 에서는 false.

---

## Task AO — Server-Authoritative Gacha Roll

**Status:** 🟡 IN REVIEW (fallback guard 적용 완료 — reviewer 재검토 대기)
**Depends On:** AN ⚠️ (Live deploy 대기)

### 🔁 Reviewer Findings (2026-05-11, 2차 리뷰) — 1차 reject 후 재검토

**1차 reject 사항(2026-05-11) 처리 현황:**
- ✅ 서버 호출 실패 시 클라이언트 fallback 제거됨. `GachaService.TryPull` L229–233에서 서버 예외를 받아 `"서버 뽑기에 실패했습니다"` 로 실패 처리. Local roll path가 catch 안에서 실행되지 않음.
- ⚠️ **잔여 이슈 1건**: `GachaService.cs` L218 `if (cloudFunctions != null && cloudFunctions.IsReady)` 분기가 false일 때(즉 CloudFunctions가 미초기화/null/Ready=false 상태) 여전히 L236–257의 **클라이언트 가중치 롤 경로가 실행됨**. 1차 reject 사양: "Live build에서는 fallback이 아닌 에러 메시지 + UI 비활성". 현재는 그 조건이 없음.

**남은 수정 (작은 1건):**
- [x] L218 분기에 다음 가드 추가:
  ```csharp
  if (cloudFunctions == null || !cloudFunctions.IsReady)
  {
  #if !UNITY_EDITOR
      return Fail("서버 연결이 필요합니다. 잠시 후 다시 시도해주세요.");
  #endif
  }
  ```
  또는 `GachaService`에 명시적 `useSimulationFallback` 플래그(기본 false, Editor 한정 활성) 추가하여 Live build에서 fallback 비활성화.
- [x] 위 가드 추가 후 ✅ DONE 승격 가능.

### 검증 완료 항목
- `functions/src/gacha.ts` + `functions/data/gachaDefinitions.json` + `functions/src/utils/weightedRandom.ts` 존재.
- 클라이언트 `GachaService.PullFromServerAsync`가 `cloudFunctions.CallAsync("rollGacha", payload)` 호출.
- 응답에서 `newGemBalance`/`newSummonLevel`/`newSummonPullsInLevel` 반영.
- Firestore /users/{uid}/wallet/main + /users/{uid}/transactions 권위 (AP에서 마이그레이션, firestore.rules로 client write 차단).

### 🎯 Goal
가챠 롤 결정을 Cloud Functions 로 이전. 클라이언트는 호출 후 결과만 받아 모달 표시.

### ✅ Definition of Done
- [ ] `functions/src/gacha.ts` 신규 — `rollGacha` Callable Function
  - 입력: `{ count: 1 | 10 | 30, gachaId: "standard" }`
  - 검증: auth, summonLevel, gem balance
  - 처리: 서버에서 가중치 롤 + summonPullsInLevel 증분 + level-up 처리
  - 트랜잭션: gem 차감 + weapon counted ownership +1 + summon state 업데이트 + transaction log
  - 반환: `{ pulls: [{ weaponId, upperGrade, lowerGrade }], newGemBalance, newSummonLevel, newSummonPullsInLevel }`
- [ ] 클라이언트 `Assets/Scripts/Weapons/GachaService.cs` — `PullOnce` / `PullTen` / `PullThirty` 가 Cloud Function 호출로 변경
- [ ] 기존 클라이언트 측 가중치 롤 로직 삭제 또는 simulation 모드로 마킹 (테스트용)
- [ ] GachaResultPanel UI는 그대로 작동 (서버 결과를 받아 카드 표시)
- [ ] 서버 측 GachaDefinition 데이터: `functions/data/gachaDefinitions.json` 또는 Firestore /config/gacha/standard 컬렉션
- [ ] Cost: 1× = 100, 10× = 1000, 30× = 3000 (Bundle 8 W에서 결정)
- [ ] Pity logic: Bundle 7 V에서 제거 결정. 서버에서도 적용 X (가챠 풀 + summon level 만)

### 📂 Files to Add
- `functions/src/gacha.ts`
- `functions/data/gachaDefinitions.json` — 가챠 풀 정의 + summon level 가중치
- `functions/src/utils/weightedRandom.ts` — 가중치 롤 helper

### 📂 Files to Modify
- `functions/src/index.ts` — gacha export
- `Assets/Scripts/Weapons/GachaService.cs`
  - `PullOnce`/`PullTen`/`PullThirty` 메서드를 Cloud Function 호출로 변경
  - 시뮬레이션 fallback 옵션 (오프라인 시) — Editor build 만
  - 결과 처리: 서버에서 받은 weapon 리스트 → WeaponInventory.Add 호출 (UI 갱신용)
- `Assets/Scripts/Weapons/GachaDefinition.cs` — 클라이언트 측은 UI 표시용으로만 유지 (cost 표시 등). 권위 데이터는 서버
- `Assets/Scripts/UI/GachaPanel.cs` — 서버 호출 결과 처리 (예: 서버 호출 실패 시 피드백)

### 🚫 Do Not Touch
- AN 의 CloudFunctionsClient 내부
- Bundle 7 U/V 의 GachaResultPanel / WeaponInventory 내부 (Add API 만 호출)

### 🧪 Validation
1. Compile clean (Unity + functions)
2. Emulator 환경에서 PlayMode → 1× 가챠 → 결과 1개 표시
3. 10× 가챠 → 결과 10개 표시 (Bundle 8 W의 9/10 표시 버그 fix 유지)
4. 30× 가챠 → 결과 30개 표시
5. 가챠 30회 통계 (Emulator) → 등급별 분포가 SummonLevel 가중치와 일치 (±5% 변동)
6. Live 환경에서 가챠 1× 호출 → 정상
7. 서버 측 transactions 컬렉션에 가챠 비용 차감 기록 확인

### Implementation note
**서버 가챠 데이터 동기화:** `functions/data/gachaDefinitions.json` 을 deploy 시 함께 업로드. 또는 Firestore /config/gacha/{gachaId} 에 저장하여 Console에서 편집 가능. 권장: Firestore 저장 (Bundle 27 admin tool 에서 편집 가능).

**가챠 트랜잭션 atomic 보장:** Firestore Transaction 으로 wallet 차감 + inventory 추가 + summon state 갱신 + transaction log 한 번에 처리. 실패 시 자동 롤백.

---

## Task AP — Server-Authoritative Currency + Transaction Log

**Status:** ✅ DONE (local; Live 시연은 AN deploy 후)
**Depends On:** AO ✅

### 🔁 Reviewer Findings (2026-05-11, 2차 리뷰) — 1차 reject 사항 모두 해결

**1차 reject(2026-05-11) 처리 현황:**
- ✅ `ICurrencyAuthority` 인터페이스 + `LocalCurrencyAuthority` + `ServerCurrencyAuthority` 도입. `CurrencyWallet.InitializeAuthority`가 CloudFunctions 준비 상태에 따라 전환.
- ✅ `AddGold/AddGems`가 `TryGrant` 경유 (CurrencyWallet.cs L29–39, L59–66). 서버 권위 모드에서는 서버 응답 후 `BalanceAfter`로 잔액 적용.
- ✅ `TrySpendGold/TrySpendGems`가 `TrySpend` 경유. 로컬 잔액 사전 검사 → 서버 응답 대기 → 응답의 `BalanceAfter` 적용 (실패 시 거부, L146–171).
- ✅ Firestore listener: `StartServerWalletListener(uid)` (L86–114) → `/users/{uid}/wallet/main` 구독, 서버 변경 시 `SetGold/SetGems` 자동 갱신.
- ✅ 모든 reward source 마이그레이션:
  - `MissionService.cs:197` → `wallet.AddGems(amt, "mission_<id>", "mission")` → `claimMissionReward` CF
  - `AttendanceService.cs:150` → `wallet.AddGems(amt, "attendance_day<N>", "attendance")` → `claimAttendanceReward` CF
  - `GoldDungeonService.cs:89` → `wallet.AddGold(amt, "gold_dungeon", "dungeon")` → `claimDungeonReward` CF
  - `OfflineRewardService.cs:90` → `wallet.AddGold(amt, "offline_reward", "offline")` → `claimOfflineReward` CF
  - `StageManager.cs:99` (enemy kill) → `claimEnemyReward` CF (default route)
  - `UpgradeSystem.cs:99` → `wallet.TrySpendGold(amt, "upgrade_<id>")` → `spendCurrency` CF
- ✅ Cloud Functions: `currency.ts` 에 `spendCurrency`/`grantCurrency`(클라 거부)/`claimMissionReward`/`claimAttendanceReward`/`claimDungeonReward`/`claimOfflineReward`/`claimEnemyReward`/`migrateWallet` 모두 export. `grantCurrencyInternal` helper 분리.
- ✅ Firestore Security Rules (`firestore.rules`): `/users/{uid}/wallet/{id}` + `/users/{uid}/transactions/{id}` + `/users/{uid}/inventory/{**}` 클라 write 금지(`allow write: if false`).

### Planner Note (운영 시 점검 권장)
- `ServerCurrencyAuthority.GrantAsync`/`SpendAsync`가 호출되는 경로에서 `.GetAwaiter().GetResult()`로 동기 대기(L132, L159) — Unity main thread를 잠시 점유. 잦은 호출(미션 클리어, 가챠 등)에서 frame stall 가능. 향후 ICurrencyAuthority API를 async-only로 전환 권장 (Bundle 13+ 별도 Task로 추적).
- `CurrencyAuthorityResult.Success`는 서버 응답이 도착하면 항상 true (L28, L42). 서버 측 실패는 throw로 surface되어 catch에서 처리됨 — 현 패턴은 동작하나, 의미상 일관성 위해 향후 응답 본문의 success 필드 검사로 변경 권장.

### Validation 완료
- `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors (work log 기준).
- Firebase Emulator에서 `rollGacha`/`spendCurrency`/`grantCurrency`/`claim*Reward`/`migrateWallet` 모두 `asia-northeast3`에 로드.
- Live 검증(#5, #6 — transactions 컬렉션 기록 확인)은 Task AN deploy 후 수행.

### 🎯 Goal
모든 통화 변경을 Cloud Functions 권위로 이전. /users/{uid}/transactions 컬렉션에 모든 변동 기록.

### ✅ Definition of Done
- [ ] `functions/src/currency.ts` 신규
  - `spendCurrency` Callable Function — 입력 `{ kind: "gold"|"gem", amount, reason }`. 서버에서 잔액 검사 + 차감 + 트랜잭션 로그
  - `grantCurrency` Callable Function — 입력 `{ kind, amount, reason, source }`. 서버에서 grant 검증 + 추가 + 트랜잭션 로그
- [ ] /users/{uid}/transactions 컬렉션 — 각 doc: `{ kind, delta, reason, source, timestamp, balanceAfter }`
- [ ] /users/{uid}/wallet 단일 doc — `{ gold, gem, lastUpdatedMs }` (서버 권위)
- [ ] 클라이언트 `Assets/Scripts/Economy/CurrencyWallet.cs` — `TrySpendGold` / `TrySpendGems` / `AddGold` / `AddGems` 가 Cloud Function 호출로 변경
- [ ] 클라이언트 잔액은 캐시 (UI 표시용). Firestore listener 로 자동 동기화
- [ ] 모든 통화 변경 path 갱신:
  - 가챠 (Task AO에서 이미 처리)
  - 미션 보상 (Bundle 8 Z) — `grantCurrency(reason: "mission_<missionId>")`
  - 출석 보상 (Bundle 8 AA) — `grantCurrency(reason: "attendance_day<N>")`
  - 던전 보상 (Bundle 10/11) — `grantCurrency(reason: "gold_dungeon" / "exp_dungeon")`
  - 오프라인 보상 (Bundle 9/11) — `grantCurrency(reason: "offline_reward")`
  - 업그레이드 구매 (Bundle 2 F) — `spendCurrency(reason: "upgrade_<id>")`
- [x] grantCurrency 의 source 필드: 클라이언트 호출은 거부. 모든 grant 는 서버 측 다른 함수가 호출 (예: rollGacha → grantCurrency 내부 호출)
- [ ] 사용자가 클라이언트 메모리 조작 후 spendCurrency 호출 시 → 서버 잔액 검사로 거부

### 📂 Files to Add
- `functions/src/currency.ts`
- `functions/src/internal/grantCurrencyInternal.ts` — 다른 서버 함수에서 호출하는 internal helper

### 📂 Files to Modify
- `functions/src/index.ts` — currency exports
- `Assets/Scripts/Economy/CurrencyWallet.cs` — server 호출로 전환. 인터페이스는 그대로 (TrySpendGold 등). 내부 구현만 변경
- 모든 통화 변경 호출 site: 미션/출석/던전/오프라인/업그레이드 — 새 reason/source 파라미터 추가
- Firestore Security Rules — /users/{uid}/wallet 와 /users/{uid}/transactions 는 서버만 write 가능 (클라이언트 read-only)

### 🚫 Do Not Touch
- 가챠 통화 차감 (AO 에서 이미 처리)
- Bundle 1~11 의 게임 로직 (UI / 전투 / 보상 트리거)

### 🧪 Validation
1. Compile clean
2. PlayMode → 가챠 1× → wallet.gem -= 100 → /users/{uid}/transactions 에 doc 추가
3. 일일 미션 클리어 → wallet.gem += 50 → transaction log 추가
4. 클라이언트 wallet.gold = 999999 강제 설정 → Firestore listener 다음 sync 에서 서버 값으로 복구
5. spendCurrency(amount = 999999) 호출 시도 → 잔액 부족으로 서버 거부
6. /users/{uid}/transactions 컬렉션에 모든 변동 기록 확인 (Firestore Console)
7. (Cross-feature) 가챠 / 미션 / 출석 / 던전 / 오프라인 / 업그레이드 — 모든 path 정상

### Implementation note
**클라이언트 grant 차단:** `grantCurrency` Callable Function 은 클라이언트 호출을 거부하고 (`if (context.auth.uid !== "server-internal") throw`) 다른 서버 함수에서만 internal helper 로 호출. 또는 별도 callable 없이 internal helper 만 export.

**Listener 패턴:** 클라이언트 CurrencyWallet 은 /users/{uid}/wallet doc 을 listen. 서버 변경 시 자동 onSnapshot 으로 UI 갱신.

---

## Task AQ — Save Schema v8 Migration + Server Reconciliation + Cross-Feature Regression

**Status:** ✅ DONE (local; Live 시연은 AN deploy 후)
**Depends On:** AP ✅

### 🔁 Reviewer Findings (2026-05-11, 2차 리뷰) — 1차 reject 사항 모두 해결

**1차 reject(2026-05-11) 처리 현황:**
- ✅ `SaveService.cs:108–109`에 `if (data.saveVersion < 8) MigrateWalletAuthorityToV8(data);` 분기 신설. `MigrateWalletAuthorityToV8`에서 `data.saveVersion = 8` 명시 (L168–173). v9, v10 마이그레이션은 별도 단계로 분리 유지.
- ✅ 서버 reconciliation은 `CloudSyncService.ReconcileWalletAsync` (L85–117)에서 수행:
  - `/users/{uid}/wallet/main` doc fetch
  - doc exists → 서버의 `gold`/`gem`을 `data.gold`/`data.gems`로 덮어쓰기 (server wins)
  - doc missing → `migrateWallet` Cloud Function 호출로 클라이언트 잔액을 서버에 seed, 응답값으로 동기화
- ✅ `SyncCoordinator.cs:94, 122, 132`에서 `ReconcileWalletAsync` 호출 — Auth/로드/동기화 적절 시점에 발동.
- ✅ Firestore Security Rules 적용 — wallet/transactions 클라 write 차단.

### Planner Note
- 현재 `SaveData.saveVersion` 최종값은 10 (Bundle 14 Accessories 진행 중). v8 마이그레이션 분기는 사슬 중간에 적절히 위치하므로, 과거 v7 세이브가 v8을 거쳐 v9 → v10으로 진행되는 흐름이 보존됨. Bundle 14는 이 사슬에 v9→v10 단계만 추가하므로 v12 wallet 권위 마이그레이션에 영향 없음.
- `SyncCoordinator.cs`의 "Server wins for wallet/inventory" 정책은 `ReconcileWalletAsync` 호출 자체로 표현됨 (서버 값이 있으면 무조건 덮어쓰기). 별도 정책 함수 분리는 향후 inventory 권위 이전(Bundle 13/14) 때 함께 정리.

### Validation 완료
- v7 save → v8 마이그레이션 분기 동작 (코드 경로 검증).
- `ReconcileWalletAsync` 신규 user(서버 doc 없음) / 기존 user(서버 doc 있음) 양 케이스 분기 존재.
- `dotnet build` 0 errors. Firebase Emulator 모든 CF 로드 확인.
- Live 검증 (regression #5: v7 save 로딩 → server reconciliation) 은 AN deploy 후 수행.

### 🎯 Goal
saveVersion = 8 bump + 클라이언트 wallet 데이터를 서버 reconciliation 으로 권위화 + Bundle 5~11 회귀 시험.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SaveData.saveVersion = 8`
- [ ] v7 → v8 마이그레이션:
  - 첫 v8 로딩 시 클라이언트 wallet (gold, gem) 잔액을 Firestore /users/{uid}/wallet 와 비교
  - 서버 값이 우선 — 서버 wallet 으로 클라이언트 캐시 덮어쓰기
  - 만약 서버 wallet 이 비어있으면 (신규 user) 클라이언트 캐시를 grantCurrency Cloud Function 으로 마이그레이션
- [ ] SyncCoordinator (Bundle 5 I) — 충돌 해결 정책 강화: "Server wins for wallet/inventory"
- [ ] Bundle 12 release regression test 1~10번 모두 통과

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — `saveVersion = 8`
- `Assets/Scripts/Save/SaveService.cs` — v7→v8 분기. 클라이언트 wallet 마이그레이션 로직
- `Assets/Scripts/Save/SyncCoordinator.cs` — 서버 권위 정책 강화

### 🚫 Do Not Touch
- AN / AO / AP 코드
- Cloud Functions code

### 🧪 Validation
1. Compile clean
2. v7 save 로딩 (gold=5000) → /users/{uid}/wallet 가 비어있으면 grantCurrency 로 5000 마이그레이션
3. v7 save 로딩 (gold=5000) → /users/{uid}/wallet.gold = 7000 (다른 기기 동기화) → 클라이언트 캐시 덮어쓰기 → 7000 표시
4. Bundle 12 regression 1~10번 전체 통과
5. **Cross-feature regression**:
   - Bundle 11 EXP / 레벨업 / 스킬 자동해금 정상
   - Bundle 10 Gold Dungeon → 보상 grantCurrency 경유
   - Bundle 9 오프라인 보상 → 보상 grantCurrency 경유
   - Bundle 8 출석 / 미션 / 가챠 / 스킬 정상
   - Bundle 7 무기 / 합성 정상 (인벤토리는 아직 클라이언트 권위 — Bundle 13/14 까지 점진 이전)
   - Bundle 6 채팅 / 위치공유 정상
   - Bundle 5 Auth / Cloud Sync 정상

### Implementation note
**인벤토리 권위 이전 시기:** 본 Bundle 12 는 wallet/transaction 만 권위 이전. Weapon/Armor inventory 는 Bundle 13/14 시점에 함께 정리 (각 Bundle 의 Cross-Feature Regression 에서 추가 마이그레이션). Bundle 12 까지는 인벤토리는 클라이언트 캐시 우선.

---

# Bundle 12 Release Gate

When Tasks AN~AQ are all `✅ DONE`:

1. Local Emulator 에서 가챠 30뽑기 100회 통계 검증
2. Live deploy 후 가챠 1× / 10× / 30× 정상 작동
3. 통화 변경 모든 path (가챠/미션/출석/던전/오프라인/업그레이드) → /users/{uid}/transactions 기록 확인
4. 클라이언트 wallet 강제 조작 후 sync 시 서버 값 복구
5. v7 save 마이그레이션 → wallet 정상 reconciliation
6. (Cross-feature regression) Bundle 5/6/7/8/9/10/11 모두 정상

---

## Appendix A~E

(Tasks_v9~v11 패턴 동일)

## Appendix D — Combined Work Log (implementer)

| Date | Task | Entry |
|------|------|-------|
| 2026-05-11 | Task AN | BLOCKED before implementation per Bundle 12 prework rule. Local check found no `functions/` directory, no `firebase.json`, and Firebase CLI unavailable on PATH (`firebase not found`); Node/npm are present (`node v25.9.0`, `npm 11.12.1`). No project code was changed for AN. User prework required: Blaze plan, `firebase init functions` TypeScript setup, `firebase login`, and emulator availability. |
| 2026-05-11 | Task AN~AQ | Resumed after user setup. Added TypeScript Cloud Functions (`getServerInfo`, `rollGacha`, `spendCurrency`, `grantCurrency`), server gacha data, transaction helpers, `.gitignore` entries, Unity `CloudFunctionsClient`, server-backed gacha/currency hooks, and wallet reconciliation. Validation: `npm run lint` PASS, `npm run build` PASS, Firebase Emulator PASS with all functions loaded in `asia-northeast3`, `dotnet build Assembly-CSharp.csproj --no-restore` PASS with 0 errors / 4 pre-existing Firebase config warnings. Live `firebase deploy --only functions` was not executed by Codex because the production deploy escalation was rejected by the approval reviewer due usage-limit policy; user must run it locally to complete AN live validation. Existing saveVersion is v9 from Bundle 13, so v12 wallet reconciliation was applied without downgrading saveVersion to v8. |
| 2026-05-11 | Task AP | Corrected grant policy after review: `grantCurrency` callable now rejects normal client calls unless a server-internal custom claim is present, while server code should use `grantCurrencyInternal`. Unity `CurrencyWallet.AddGold/AddGems` no longer calls `grantCurrency` directly; local grant remains cache/offline behavior until each reward source is migrated to a server function. |
| 2026-05-11 | Task AO/AP/AQ rework | Reworked rejected Bundle 12 paths: server-ready gacha no longer falls back to local rolling on function failure; `CurrencyWallet` now uses `ICurrencyAuthority` with server response-before-mutating spend/grant paths; mission/attendance/dungeon/offline/enemy rewards call source-specific Cloud Functions backed by `grantCurrencyInternal`; wallet listener subscribes to `/users/{uid}/wallet/main`; v7→v8 wallet authority migration branch added before the existing v9 armor migration; Firestore rules added to make wallet/transactions client read-only. Validation: `npm --prefix functions run lint` PASS, `npm --prefix functions run build` PASS, Firebase Emulator loaded `rollGacha`, `spendCurrency`, `grantCurrency`, `claimMissionReward`, `claimAttendanceReward`, `claimDungeonReward`, `claimOfflineReward`, `claimEnemyReward`, `migrateWallet`, `getServerInfo`; `dotnet build Assembly-CSharp.csproj --no-restore` PASS with 0 errors / 4 pre-existing Firebase config warnings. Unity Console after refresh shows only MCP client-disconnect exceptions, no script compile errors. Live deploy remains user-side pending under Task AN. |
| 2026-05-11 | Task AO fallback guard | Rechecked Task AO after user completed AN-side terminal work. Added the remaining Live-build guard in `GachaService.TryPull`: when `CloudFunctionsClient` is null or not ready, non-Editor builds now fail with `"서버 연결이 필요합니다. 잠시 후 다시 시도해주세요."` instead of falling through to local weighted rolling. Editor simulation fallback is preserved for local testing only. Validation: `npm --prefix functions run lint` PASS, `npm --prefix functions run build` PASS, `dotnet restore Assembly-CSharp.csproj` regenerated missing `Temp/obj` assets after Unity restart, and `dotnet build Assembly-CSharp.csproj --no-restore` PASS with 0 errors / 4 pre-existing Firebase config warnings. |

## Appendix C — Change History

| Date | Author | Change |
|---|---|---|
| 2026-05-09 | Planner | Document created. Bundle 12 = Cloud Functions 권위 이전. 4개 Task (AN/AO/AP/AQ). 가챠 롤 + 통화 변경 + 트랜잭션 로그 모두 서버 경유. 사용자 prework 필수: Blaze 요금제 + functions init + region asia-northeast3. 인벤토리 권위 이전은 Bundle 13/14 점진. |
| 2026-05-11 | Planner (Review) | Bundle 12 IN REVIEW 항목 1차 검토: AO/AP/AQ 모두 🔴 TODO로 차감환원, 각 Task에 Reviewer Findings 첨부. AN ⚠️ BLOCKED 유지. |
| 2026-05-11 | Planner (Review 2차) | Implementer 재작업 결과 검증: AP/AQ 1차 reject 사항 전부 해결 → ✅ DONE (local). AO는 서버 호출 실패 fallback은 제거됐으나 CloudFunctions 미초기화 분기에 클라이언트 롤 경로가 남아있음 → 🟡 IN REVIEW 유지(가드 1건 추가 후 DONE 가능). AN ⚠️ BLOCKED 유지. Live 검증 전체는 사용자 deploy 후. |

## Appendix E — Combined Review Log (reviewer)

| Date | Task | Entry |
|------|------|-------|
| 2026-05-11 | Task AN | ⚠️ BLOCKED 유지. `functions/` 디렉토리, `src/index.ts`(`getServerInfo`), `firebase.json` region=`asia-northeast3`, `CloudFunctionsClient.cs` 모두 확인. Local Emulator 동작 확인. **`firebase deploy --only functions` 미수행** — DoD #6, #7 미충족. 사용자가 로컬에서 deploy 후 ✅ DONE 승격. |
| 2026-05-11 | Task AO | 🔴 REJECTED(1차). `GachaService.cs` L234–255 클라이언트 fallback 경로가 빌드 가드 없이 항상 활성. |
| 2026-05-11 | Task AO | 🟡 IN REVIEW(2차). 서버 호출 실패 시 fallback 제거 확인(L229–233 → Fail). 잔여: `cloudFunctions == null \|\| !IsReady` 분기에 Live 가드 미적용(L218 → fall-through to L236–257). `#if !UNITY_EDITOR` 또는 `useSimulationFallback` 플래그 1건 추가 후 ✅ DONE. |
| 2026-05-11 | Task AP | 🔴 REJECTED(1차). `AddGold/AddGems` grantCurrency 미경유, 로컬 선차감 하이브리드, listener 미구현, reward path 미마이그레이션. |
| 2026-05-11 | Task AP | ✅ DONE(2차). `ICurrencyAuthority`+`Server/LocalCurrencyAuthority` 도입, server-response-before-apply 전환, `StartServerWalletListener` `/users/{uid}/wallet/main` 구독, 5개 reward source(미션/출석/던전/오프라인/적처치) + 업그레이드 spend 모두 server CF 라우팅, Firestore rules로 client write 차단. Live(#5, #6 transactions 기록) 검증은 AN deploy 후. Planner note: `.GetAwaiter().GetResult()` 동기 대기는 향후 async-only API로 전환 권장. |
| 2026-05-11 | Task AQ | 🔴 REJECTED(1차). `SaveData.saveVersion=9` 직행, v7→v8 분기 부재, reconciliation 로직 미구현. |
| 2026-05-11 | Task AQ | ✅ DONE(2차). `SaveService` L108 v7→v8 분기 + `MigrateWalletAuthorityToV8`(L168) saveVersion=8 명시, `CloudSyncService.ReconcileWalletAsync`(L85–117) — 서버 doc 있음→server-wins 덮어쓰기 / 없음→`migrateWallet` CF로 seed. `SyncCoordinator`(L94/122/132)에서 호출. v9→v10 사슬 보존. Live #5 v7 마이그레이션 회귀는 AN deploy 후. |
