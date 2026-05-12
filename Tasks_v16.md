# Wizard Grower — Tasks v16 (Bundle 16: Combat Power Ranking)

> Follow-up work track to `Tasks_v15.md`. **Do NOT edit prior Tasks*.md.**

---

## 0. Common Work Rules

(Tasks_v9~v15 의 §0 규칙을 그대로 준수.)

### 0.5 Cross-Track Coordination ⚠️

| Risk | Mitigation |
|---|---|
| 신규 Firestore 컬렉션 + Composite Index 필요 | Task BE에서 firestore.indexes.json 추가. 사용자 prework: `firebase deploy --only firestore:indexes` 한 번 실행 |
| Bundle 16은 SaveData 필드 추가 거의 없음 | saveVersion 유지 (v11). Bundle 17에서 Weekly/Main/Achievement 추가 시 bump |
| 클라이언트가 자기 점수를 Firestore 직접 write — 변조 위험 | Security Rules로 1차 차단 (`auth.uid == resource.id`), Cloud Function periodic validation 작업은 Bundle 27 admin 단계로 이연. Bundle 16은 Security Rules 만 |
| ranking refresh 호출 빈도 — Firestore 비용 | TOP 100 fetch 는 사용자가 RankingPanel 열 때만. 자동 refresh 주기 30초 (패널 활성 중). Throttling 으로 비용 통제 |
| 시즌 리셋 — Bundle 16에서 도입 vs 이연 | Bundle 16은 ever-running 단일 ranking만. 시즌 리셋은 Bundle 26 Event Bundle 에서 별도 구현 |
| Bundle 12 Cloud Functions 권위 통화 — Bundle 16 ranking write 도 서버 권위 vs 클라이언트 직접 | **하이브리드**: 클라이언트가 자기 점수 write (Firestore Security Rules 검증). 권위 정밀 검증은 Bundle 27 (admin) 시점에 Cloud Function batch job 추가 |

---

## 1. Task Dependency Graph

```
Bundle 16
BD → BE → BF → BG → Bundle 16 Release Gate

BD:  RankingService 신규 + combat power score push 로직
BE:  Firestore 컬렉션 + Security Rules + Composite Indexes
BF:  RankingPanel UI (전투력 TOP 100 + 자기 위치)
BG:  Cross-Feature Regression + Polish (saveVersion bump 없음)
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 16 | BD | RankingService + Combat Power Score Push | 🔴 TODO | v15 baseline |
| 16 | BE | Firestore Schema + Security Rules + Composite Indexes | 🔴 TODO | BD ✅ |
| 16 | BF | RankingPanel UI + 전투력 TOP 100 + 자기 위치 | 🔴 TODO | BE ✅ |
| 16 | BG | Cross-Feature Regression + Polish | 🔴 TODO | BF ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 16 — Combat Power Ranking

**Goal:** 오직 "전투력" 단일 요소만 대상으로 하는 단순 리더보드. PVP/길드 OUT 상태에서 동기부여 콘텐츠를 제공하되, 스테이지/레벨/던전 점수 등 다른 랭킹 요소는 Bundle 16 범위에서 제외한다. Firestore + Security Rules 로 보호하고, 권위 정밀 검증은 후속 admin Bundle 로 이연한다.

### Bundle 16 사용자 Prework ⚠️

**Bundle 16 시작 전 사용자가 다음을 완료해야 함:**

1. Bundle 12 Cloud Functions 셋업 완료 (`functions/` 디렉토리 + Blaze 요금제) — Bundle 12 prework 와 동일
2. Task BE 산출물 deploy 명령:
   - `firebase deploy --only firestore:rules` — Security Rules
   - `firebase deploy --only firestore:indexes` — Composite Indexes (생성 시간 5분~)

위 prework 가 Task BE 의 implementer 수행. 사용자는 deploy 권한만 확인.

### Bundle 16 Release Regression Tests
1. HUD 또는 메인UI01 어디엔가 "랭킹" 진입 버튼 — 위치 미정 (implementer 권장: HUD 우상단 작은 아이콘, 출석/업적 버튼과 동일 stack)
2. 클릭 → RankingPanel 슬라이드업 popup
3. 패널 제목/본문은 "전투력 랭킹" 단일 화면. 상단 category 탭 없음
4. 전투력 TOP 100 + 자기 위치 (자기보다 위 2명, 자기, 아래 2명) 별도 섹션
5. TOP 1~3 는 강조 (트로피/메달 아이콘 + 색상)
6. 자기 점수가 TOP 100 안에 있으면 "자기 위치" 섹션 생략 + 메인 리스트에서 강조
7. 30초마다 자동 새로고침 (패널 활성 중)
8. "새로고침" 버튼 — 즉시 refresh
9. 점수 push: 전투력 상승/장비 변경/스탯 변경 시 자동 호출 — `RankingService.PushMyCombatPowerScoreAsync()`
10. 점수 push 는 throttle: 같은 전투력 점수 30초 내 중복 호출 무시 (Firestore 비용 통제)
11. 다른 유저 클릭 → 단순 프로필 모달 (displayName, 전투력 점수만)
12. (Cross-feature regression) Bundle 5~15 모두 정상

---

## Task BD — RankingService + Combat Power Score Push

**Status:** 🔴 TODO
**Depends On:** v15 baseline

### 🎯 Goal
클라이언트 측 RankingService 신규. 자기 전투력 점수만 Firestore 에 push 하는 throttled API. 스테이지/레벨/던전/재화 등 다른 점수 카테고리는 Bundle 16 범위에서 구현하지 않는다.

### ✅ Definition of Done
- [ ] `RankingService` MonoBehaviour 신규
- [ ] 카테고리는 `CombatPower` 단일. `Stage`, `Level` enum/탭/API 금지
- [ ] `PushMyCombatPowerScoreAsync(score, displayName)` — Firestore `/rankings/combatPower/entries/{uid}` 에 write
- [ ] Throttle: 같은 combatPower score 30초 내 중복 호출 무시
- [ ] 자동 push 트리거:
  - `CombatPowerService.PowerChanged` 또는 동등 이벤트 → 전투력 push
  - 장비 장착/합성/강화 등으로 전투력이 재계산될 때 전투력 push
- [ ] `GetTopCombatPowerAsync(limit=100) → IReadOnlyList<RankingEntry>`
- [ ] `GetMyCombatPowerRankAsync() → MyRankInfo` — 자기 등수 + 주변 5명

### 📂 Files to Add
- `Assets/Scripts/Ranking/RankingService.cs`
  ```csharp
  public class RankingService : MonoBehaviour
  {
      public void Initialize(AuthService auth, UserProfileService profile,
                              CombatPowerService combat);

      public Task PushMyCombatPowerScoreAsync();   // CombatPowerService.CurrentPower 산출
      public Task<IReadOnlyList<RankingEntry>> GetTopCombatPowerAsync(int limit = 100);
      public Task<MyRankInfo> GetMyCombatPowerRankAsync();

      public event Action Refreshed;
  }

  [Serializable]
  public struct RankingEntry
  {
      public int rank;
      public string uid;
      public string displayName;
      public long score;
      public long lastUpdateUtcMs;
  }

  [Serializable]
  public struct MyRankInfo
  {
      public int myRank;                       // 1-based, 0 if not ranked
      public RankingEntry[] surrounding;       // 자기 위 2 + 자기 + 아래 2 (최대 5)
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Core/GameContext.cs` + `GameManager.cs` — RankingService 등록 + Initialize

### 🚫 Do Not Touch
- Bundle 12 Cloud Functions 코드
- 기타 Bundle 5~15 시스템

### 🧪 Validation
1. Compile clean
2. PlayMode → 전투력 변경 → Firestore Console 에서 `/rankings/combatPower/entries/{uid}` 자동 갱신
3. 스테이지 진행만으로 `/rankings/stage` 문서가 생성되지 않음
4. 레벨업만으로 `/rankings/level` 문서가 생성되지 않음
5. 30초 내 동일 push 시도 → Firestore write 발생 안 함 (throttle 검증)
6. `GetTopCombatPowerAsync(100)` → 자기 + 다른 가짜 유저 (테스트 데이터) 정렬 결과
7. `GetMyCombatPowerRankAsync()` → 자기 등수 + 주변

### Implementation note
**Score 환산:**
```csharp
// CombatPower score: combatPowerService.CurrentPower (round to long)
long cpScore = (long)combatPowerService.CurrentPower;
```

**Throttle 구현:**
```csharp
private long lastPushedScore;
private long lastPushMs;

public async Task PushMyCombatPowerScoreAsync() {
    long score = (long)combatPowerService.CurrentPower;
    long nowMs = (long)(Time.unscaledTime * 1000);
    if (lastPushedScore == score && nowMs - lastPushMs < 30000)
        return;   // skip duplicate
    // ... write to Firestore
    lastPushedScore = score;
    lastPushMs = nowMs;
}
```

---

## Task BE — Firestore Schema + Security Rules + Composite Indexes

**Status:** 🔴 TODO
**Depends On:** BD ✅

### 🎯 Goal
Firestore 컬렉션 구조 + Security Rules + Composite Indexes 정의·deploy.

### ✅ Definition of Done
- [ ] Firestore 컬렉션 구조 명세:
  ```
  /rankings/combatPower/entries/{uid}: { score, displayName, lastUpdateUtcMs }
  ```
- [ ] Security Rules:
  - `match /rankings/combatPower/entries/{uid}`:
    - `read: auth != null` (모든 인증 사용자)
    - `write: auth.uid == uid && score validation`
- [ ] Composite Indexes:
  - `rankings/combatPower/entries`: orderBy(score, desc), limit 100
  - `rankings/combatPower/entries`: orderBy(score, desc) + where(score >= X) — 자기 주변 순위용
- [ ] `/rankings/stage`, `/rankings/level` rules/index/schema 는 Bundle 16에서 추가하지 않음
- [ ] firestore.rules + firestore.indexes.json 갱신 — 기존 rule 보존 (presence/chat/users 등)
- [ ] deploy: `firebase deploy --only firestore:rules,firestore:indexes`

### 📂 Files to Add
- (None — 기존 firestore.rules / firestore.indexes.json 갱신)

### 📂 Files to Modify
- `firestore.rules` — /rankings 섹션 추가
  ```
  match /rankings/combatPower/entries/{uid} {
      allow read: if request.auth != null;
      allow write: if request.auth != null
                    && request.auth.uid == uid
                    && request.resource.data.score is int
                    && request.resource.data.score >= 0
                    && request.resource.data.score < 999999999
                    && request.resource.data.displayName is string
                    && request.resource.data.displayName.size() <= 30;
  }
  ```
- `firestore.indexes.json` — composite indexes 추가
  ```json
  {
    "indexes": [
      {
        "collectionGroup": "entries",
        "queryScope": "COLLECTION",
        "fields": [
          { "fieldPath": "score", "order": "DESCENDING" },
          { "fieldPath": "lastUpdateUtcMs", "order": "DESCENDING" }
        ]
      }
    ]
  }
  ```

### 🚫 Do Not Touch
- 기존 firestore.rules 의 다른 섹션 (presence/chat/users)
- Bundle 12 Cloud Functions

### 🧪 Validation
1. `firebase deploy --only firestore:rules` 성공
2. `firebase deploy --only firestore:indexes` 성공 (생성 시간 5분 가량 — 사용자 대기)
3. Firebase Console → Firestore → Indexes 탭에서 신규 indexes "Enabled" 상태 확인
4. PlayMode → `RankingService.PushMyCombatPowerScoreAsync()` → Firestore `/rankings/combatPower/entries/{uid}` 생성 정상
5. `/rankings/stage` 또는 `/rankings/level` 클라이언트 write 시도 → 거부 또는 경로 미사용 확인
6. 다른 user (테스트 계정) score 변조 시도 → 거부 (Security Rule)
7. score = -1 또는 score = 1B 시도 → 거부

### Implementation note
**Index 생성 시간:** Composite Index deploy 후 Firebase 측에서 background build 가 5~10분. 그 동안은 query 가 실패할 수 있음. Validation 4번은 deploy 후 충분 시간 후에 실행.

**Validation 5번 테스트 방법:** 실제 다른 유저 변조는 어렵지만, Firestore Console 에서 수동으로 다른 uid 의 entry 를 변경 시도 → Security Rule 검증 (Console 은 admin 권한이라 가능. 클라이언트 SDK 변조는 거부됨을 PlayMode 에서 확인).

---

## Task BF — RankingPanel UI + 전투력 TOP 100 + 자기 위치

**Status:** 🔴 TODO
**Depends On:** BE ✅

### 🎯 Goal
랭킹 패널 슬라이드업 popup + 전투력 단일 정렬 리스트 + 자기 위치 별도 섹션. 카테고리 탭은 만들지 않는다.

### ✅ Definition of Done
- [ ] HUD 우상단 (Auto/Chat/Achievement/Attendance 같은 작은 버튼 stack 내) "랭킹" 버튼 추가
- [ ] `SecondaryPanelCoordinator` (Bundle 8 AA) mutual exclusion 에 RankingPanel 추가
- [ ] `RankingPanel` 슬라이드업 popup (Bundle 8 패턴)
- [ ] 상단 category tab 없음. 패널 제목은 "전투력 랭킹"
- [ ] 전투력 TOP 100 리스트 (rank, displayName, combatPower score)
- [ ] 자기 위치가 TOP 100 안 → 메인 리스트에서 자기 행 강조 (배경색)
- [ ] 자기 위치가 TOP 100 밖 → 별도 "내 순위" 섹션 (자기 위 2 + 자기 + 아래 2)
- [ ] TOP 1~3 강조 — 트로피 아이콘 (🥇🥈🥉) + 황금/은/동 색
- [ ] 새로고침 버튼 + 30초 자동 refresh
- [ ] 다른 유저 행 클릭 → 작은 프로필 모달 (displayName, 전투력 점수)
- [ ] 빈 ranking 처리: 데이터 없으면 "아직 랭킹 데이터가 없습니다" 표시

### 📂 Files to Add
- `Assets/Scripts/UI/RankingPanel.cs`
- `Assets/Scripts/UI/RankingRowView.cs`
- `Assets/Scripts/UI/UserProfileModal.cs`
- `Assets/Prefabs/UI/RankingPanel.prefab`
- `Assets/Prefabs/UI/RankingRow.prefab`
- `Assets/Prefabs/UI/UserProfileModal.prefab`
- `Assets/Scripts/UI/RankingButton.cs` — HUD 버튼

### 📂 Files to Modify
- `Assets/Scripts/UI/HUDController.cs` — RankingButton + RankingPanel 와이어링
- `Assets/Scripts/UI/SecondaryPanelCoordinator.cs` — RankingPanel 등록
- `Assets/Scripts/Core/GameContext.cs` + `GameManager.cs` — RankingPanel Bind

### 🚫 Do Not Touch
- BD / BE 코드
- 기존 패널 (Achievement/Attendance/Chat)

### 🧪 Validation
1. Compile clean
2. PlayMode → HUD "랭킹" 버튼 → RankingPanel 슬라이드업
3. 전투력 TOP 100 리스트 + 빈 데이터 시 "아직 랭킹 데이터가 없습니다"
4. category tab 이 없고 스테이지/레벨 UI 텍스트가 노출되지 않음
5. 자기 점수가 TOP 안에 있을 때 — 자기 행 배경색 강조
6. 자기 점수가 TOP 밖 → "내 순위" 섹션 + 주변 5명
7. 다른 행 클릭 → UserProfileModal — displayName + 전투력 점수만 표시
8. 30초 후 자동 refresh (콘솔 로그로 확인)
9. "새로고침" 클릭 → 즉시 refresh
10. 다른 패널 (Achievement/Attendance/Chat) 열려있으면 자동 닫고 RankingPanel 표시 (mutual exclusion)

### Implementation note
**랭킹 데이터 정렬:** Firestore query 결과는 이미 정렬되어 옴 (composite index). 클라이언트는 단순 list 표시.

**자기 위치 산출:** 두 query 필요:
1. TOP 100: `orderBy(score, desc).limit(100)`
2. My rank surrounding: `where(score, >, myScore).orderBy(score, asc).limit(2)` (위 2명) + `where(score, <, myScore).orderBy(score, desc).limit(2)` (아래 2명) + 자기 자신

**rank 번호 계산:** TOP 100 안: 클라이언트 인덱스 + 1. TOP 100 밖: `count(score > myScore)` query → rank = count + 1. count() 는 Firestore aggregation API (2023+ 지원).

---

## Task BG — Cross-Feature Regression + Polish

**Status:** 🔴 TODO
**Depends On:** BF ✅

### 🎯 Goal
saveVersion bump 없음 (Bundle 16 은 SaveData 변경 없음). Bundle 5~15 회귀 시험만.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] Bundle 16 release regression 1~12번 모두 통과
- [ ] **Cross-feature regression**:
  - Bundle 15 강화 시스템 정상 (강화석 던전 / 강화 모달)
  - Bundle 14 장신구 / Bundle 13 방어구 / Bundle 12 Cloud Functions / Bundle 11 Player Level / Bundle 10 GoldDungeon / Bundle 9 오프라인 보상 / Bundle 8 출석·미션·스킬·가챠 / Bundle 7 무기 / Bundle 6 채팅·위치공유 / Bundle 5 Auth·Cloud Sync 모두 정상
- [ ] 랭킹 시스템 도입 후 다른 시스템에 미친 영향 없음 검증

### 📂 Files to Modify
- (None — 회귀 시험만)

### 🚫 Do Not Touch
- BD / BE / BF 코드

### 🧪 Validation
1. Compile clean
2. Bundle 16 regression 1~12번 전체 통과
3. Cross-feature regression 모두 통과

### Implementation note
**SaveData 변경 없음:** Ranking 데이터는 모두 Firestore 에 있음. 클라이언트는 캐시만. saveVersion 11 그대로 유지.

---

# Bundle 16 Release Gate

When Tasks BD~BG are all `✅ DONE`:

1. HUD 랭킹 버튼 → RankingPanel
2. 전투력 단일 화면 정상 + TOP 100 + 자기 위치
3. 점수 push throttle 정상
4. Security Rules 보호 (변조 거부)
5. (Cross-feature regression) Bundle 5~15 모두 정상

---

## Appendix A~E

(Tasks_v9~v15 패턴 동일)

| Date | Author | Change |
|---|---|---|
| 2026-05-09 | Planner | Document created. Bundle 16 = Ranking feature. 4개 Task (BD/BE/BF/BG). Firestore composite index + Security Rules. 클라이언트 직접 write (Security Rule 1차 차단), 권위 정밀 검증은 Bundle 27 admin 시점. TOP 100 + 자기 위치 주변 5명. saveVersion 미변경 (SaveData 변경 없음). 시즌 리셋은 Bundle 26 Event 에서 별도. |
| 2026-05-13 | Planner | Scope revised by user request: Bundle 16 ranking is now **combat power only**. Removed Stage/Level ranking categories, 3-tab UI, stage/level push triggers, and `/rankings/stage`/`/rankings/level` schema from the implementation plan. |

### Appendix D — Combined Work Log

| Date | Task | Summary |
|---|---|---|
| 2026-05-13 | BD | Added `RankingService` for combat-power-only Firestore ranking writes/queries, including duplicate score push throttling, TOP 100 fetch, and surrounding rank lookup. Wired service creation through `GameContext`/`GameManager` and pushed the current combat power after Firebase auth bootstrap. Validation: `dotnet build Assembly-CSharp.csproj --no-restore` passed with 4 pre-existing Chat/Presence warnings; Unity Console showed no new script compile errors after refresh; Unity PlayMode test job `bb2e5eae39624e569b67066067b04968` succeeded. |
