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
| 12 | AN | Firebase Functions Project Setup | ⚠️ BLOCKED | v11 baseline + 사용자 prework |
| 12 | AO | Server-Authoritative Gacha Roll | 🔴 TODO | AN ✅ |
| 12 | AP | Server-Authoritative Currency + Transaction Log | 🔴 TODO | AO ✅ |
| 12 | AQ | Save Schema v8 + Server Reconciliation + Regression | 🔴 TODO | AP ✅ |

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

**Status:** ⚠️ BLOCKED
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

**Status:** 🔴 TODO
**Depends On:** AN ✅

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

**Status:** 🔴 TODO
**Depends On:** AO ✅

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
- [ ] grantCurrency 의 source 필드: 클라이언트 호출은 거부. 모든 grant 는 서버 측 다른 함수가 호출 (예: rollGacha → grantCurrency 내부 호출)
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

**Status:** 🔴 TODO
**Depends On:** AP ✅

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

| Date | Author | Change |
|---|---|---|
| 2026-05-09 | Planner | Document created. Bundle 12 = Cloud Functions 권위 이전. 4개 Task (AN/AO/AP/AQ). 가챠 롤 + 통화 변경 + 트랜잭션 로그 모두 서버 경유. 사용자 prework 필수: Blaze 요금제 + functions init + region asia-northeast3. 인벤토리 권위 이전은 Bundle 13/14 점진. |
