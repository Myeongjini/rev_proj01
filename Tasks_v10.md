# Wizard Grower — Tasks v10 (Bundle 10: Gold Dungeon)

> Follow-up work track to `Tasks_v9.md`.
> Different agents, different bundle, different file.
> **Do NOT edit prior Tasks*.md from this track.** Read-only access only.
>
> The **Planner / Reviewer (Claude)** is the sole editor of this document.
> Implementers read this document, modify the code, and append to Appendix D.
>
> 장기 로드맵 (Bundle 9~30) 은 `References.md` §4.1 참조. 본 문서는 Bundle 10 (Gold Dungeon) 만 다룸.

---

## 0. Common Work Rules

### 0.1 Basic Rules
1. **Only one task at a time.** State the task ID (e.g. `Task AF`) at start and end.
2. Before starting: read the relevant task section thoroughly. Never modify any **"Do Not Touch"** area.
3. During work: Unity project path is `/Users/kmj/rev_proj01`. Namespace is `WizardGrower.*`.
4. On completion:
   - Verify Unity Console: **0 errors / 0 warnings**
   - Enter PlayMode and pass all **regression tests** for the task
   - Update the task's Status line to `🟡 IN REVIEW`
   - Append a row to **Appendix D — Combined Work Log**
5. After code review, the reviewer:
   - Updates Status to `✅ DONE` or back to `🔴 TODO`
   - Appends a row to **Appendix E — Combined Review Log**
6. The build helper menu `Wizard Grower → Build Prototype Scene` **overwrites the scene** — do not run without explicit permission.
7. When adding/removing files, ensure `.meta` files are paired (Unity Editor handles this automatically).

### 0.2 Bundle Gate Rules ⭐
Bundle 10 is sequential. Do not start the next task until the previous task is `✅ DONE`.

**Gate pass conditions:**
1. All tasks AF~AI are `✅ DONE`
2. Unity Console has 0 errors / 0 warnings
3. Bundle 10 release regression tests pass
4. Save migration from v5 to v6 preserves existing v9 progress (오프라인 보상 상태, 출석, 미션, 스킬 등)

### 0.3 Auto-Progression Restrictions (Implementation Agent)
- Do not start another Bundle 10 task on your own.
- On signature/spec conflict, **do not decide unilaterally** — record in Appendix D and wait for the reviewer.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason.
- Do not introduce new major systems or files not listed in this document unless required for compilation; log exceptions and await reviewer approval.

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>`.
- This repo uses GitHub Desktop with origin set. **Agents must not push** — push is the user's responsibility.
- At task start, run `git status` to confirm working tree state. Note any uncommitted changes in Appendix D.
- Per task = per commit. `git revert` is the rollback tool.

### 0.5 Cross-Track Coordination ⚠️
Bundle 10 builds on the v9 baseline.

| Risk | Mitigation |
|---|---|
| Scene 전환 — MainScene → GoldDungeonScene → MainScene | LoginScene → MainScene 패턴(Bundle 5/6) 재사용. `SceneManager.LoadSceneAsync` + 인증 상태 보존을 위한 `AuthBootstrapHolder` (Bundle 6 J) 그대로 사용 |
| Build Settings index 변경 | 신규 GoldDungeonScene 추가는 build index 2 (LoginScene=0, MainScene=1, GoldDungeonScene=2). 추후 Bundle 11 EXP Dungeon = index 3, 그 외 Scene은 후속 Bundle |
| Daily entry counter — `MissionResetService` (Bundle 8 Z) 의 KST 00:00 리셋 재사용 | 새 Firestore serverTimestamp 호출 추가 금지. `MissionResetService.GetServerNowMsAsync()` 또는 동치 사용 |
| Sweep 기능이 던전 진행을 건너뛰어도 보상은 정상 — 골드 무결성 | Bundle 12 (Cloud Functions 권위) 이전 단계라 클라이언트 권위 유지. Sweep 보상도 클라이언트 계산. Bundle 12에서 권위 이전 시 함께 마이그레이션 |
| 광고 시청 2배 — Bundle 9의 `AdSimulationService` 재사용 | 같은 `IRewardedAdProvider` 인터페이스. Bundle 23 실제 SDK 통합 시 함께 교체 |

---

## 1. Task Dependency Graph

```
Bundle 10
AF → AG → AH → AI → Bundle 10 Release Gate

AF:  GoldDungeonScene + Entry Flow + MainUI01 추가예정5 슬롯 활성화
AG:  GoldDungeonService + 일일 입장 제한 + 골드 보상 계산 + 난이도 스테이지
AH:  GoldDungeon UI (입장 모달 / 결과 모달 / Sweep 모달) + 광고 2배 + Best Record
AI:  Save Schema v6 Migration + Cross-Feature Regression
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 10 | AF | GoldDungeonScene + Entry Flow + MainUI01 슬롯 | 🟡 IN REVIEW | v9 baseline |
| 10 | AG | GoldDungeonService + Daily Limit + Reward Calculation | 🟡 IN REVIEW | AF ✅ |
| 10 | AH | GoldDungeon UI + Ad 2x + Best Record | 🔴 TODO | AG ✅ |
| 10 | AI | Save Schema v6 Migration + Regression | 🔴 TODO | AH ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 10 — Gold Dungeon

**Goal:** 일반 스테이지 진행과 별개로 골드를 빠르게 농사할 수 있는 별도 Scene 던전. 일일 입장 제한 + 시간 제한 + 처치 수 기반 골드 보상 + Sweep + 광고 2배.

### Bundle 10 Release Regression Tests
1. MainUI01 의 "추가예정5" 슬롯이 "골드던전" 으로 활성화 (회색→파란색·라벨 변경)
2. "골드던전" 클릭 → 입장 모달 (잔여 입장 횟수 / 난이도 선택 / 입장 / 취소)
3. "입장" 클릭 → MainScene → GoldDungeonScene 전환 (LoginScene→MainScene 패턴 재사용)
4. GoldDungeonScene: 60초 카운트다운 + 일반 스테이지 같은 자동 전투 + 처치당 골드 누적
5. 60초 종료 → 결과 모달 (`<처치 수> / <획득 골드>` + 광고 2배 + Best Record 비교)
6. "받기" 클릭 → 골드 += N → MainScene 복귀
7. "광고 보고 2배" 클릭 → AdSim 1초 → 골드 += N×2 → MainScene 복귀
8. 일일 입장 N회 (시드값: 3회) 초과 시 입장 버튼 비활성 + "오늘 입장 횟수를 모두 사용했습니다" 피드백
9. KST 00:00 (Bundle 8 Z 의 `MissionResetService`) 경계 통과 시 일일 입장 횟수 리셋
10. Sweep 버튼 (이전 Best Record 기준 즉시 보상 받기) → AdSim 0초 또는 1초 → 골드 += BestScore
11. Sweep 도 일일 입장 1회로 카운트
12. saveVersion = 6
13. Save + restart → goldDungeonState 보존 (lastEntryDateUtcMs, todayEntryCount, bestScore)
14. (Cross-feature regression) Bundle 9 오프라인 보상 모달 / Bundle 8 출석·미션·스킬 / Bundle 7 가챠·무기 / Bundle 6 채팅·위치공유 모두 정상

---

## Task AF — GoldDungeonScene + Entry Flow + MainUI01 슬롯

**Status:** 🟡 IN REVIEW
**Depends On:** v9 baseline + Bundle 7 V (`MainUI01Bar` reserved5 슬롯)

### 🎯 Goal
신규 Unity Scene `GoldDungeonScene.unity` 추가 + Build Settings 등록 + MainUI01Bar 의 "추가예정5" 슬롯을 "골드던전" 으로 활성화 + 입장 모달 → Scene 전환 흐름.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `Assets/Scenes/GoldDungeonScene.unity` 신규 생성 — 일반 전투 가능한 최소 환경 (Wizard + Enemy spawner + Camera + HUD partial)
- [ ] Build Settings: `[0] LoginScene, [1] MainScene, [2] GoldDungeonScene` 순서 등록
- [ ] `MainUI01Bar.reserved5Button` → `goldDungeonButton` 으로 활성화 (라벨 "골드던전", 파란색, interactable=true)
- [ ] `MainUI01Coordinator` 에 `GoldDungeon` 탭 추가 — 다른 메인UI01 탭과 mutual exclusion
- [ ] `goldDungeonButton` 클릭 → `GoldDungeonEntryPanel` 모달 (잔여 입장 횟수 + 난이도 선택 + 입장/취소)
- [ ] "입장" 버튼 → `SceneManager.LoadSceneAsync("GoldDungeonScene")` + `AuthBootstrapHolder` 패턴 재사용으로 인증 상태 보존
- [ ] GoldDungeonScene 진입 후 자동 전투 시작 (60초 카운트다운 시작)
- [ ] 60초 종료 또는 "포기" 버튼 → MainScene 복귀
- [ ] `AuthBootstrapHolder` 가 GoldDungeonScene 에서도 살아있어 재진입 시 인증 재초기화 없음

### 📂 Files to Add
- `Assets/Scenes/GoldDungeonScene.unity` — 신규 Scene
- `Assets/Scripts/Dungeons/GoldDungeonBootstrap.cs` — Scene 진입 시 자동 전투 시작 + 60초 타이머 + 결과 콜백
  ```csharp
  public class GoldDungeonBootstrap : MonoBehaviour
  {
      [SerializeField] private float durationSeconds = 60f;
      [SerializeField] private PlayerWizard wizard;
      [SerializeField] private EnemySpawner spawner;
      [SerializeField] private CountdownTimerView timer;

      public event Action<GoldDungeonResult> Completed;

      private async void Start();   // 자동 전투 시작 + 타이머 시작
      public void Abandon();        // "포기" 버튼 콜백
  }

  [Serializable]
  public struct GoldDungeonResult
  {
      public int killCount;
      public long earnedGold;
      public int difficulty;
  }
  ```
- `Assets/Scripts/UI/GoldDungeonEntryPanel.cs` — 입장 모달
- `Assets/Scripts/UI/CountdownTimerView.cs` — Scene 안 60초 카운트다운 (HUD 같은 위치에 표시)
- `Assets/Prefabs/UI/GoldDungeonEntryPanel.prefab`
- `Assets/Prefabs/UI/CountdownTimerView.prefab`

### 📂 Files to Modify
- `Assets/Scripts/UI/MainUI01Bar.cs`
  - `reserved5Button` → `goldDungeonButton` 이름 변경 또는 새 필드 추가 + `Reserved5` enum value → `GoldDungeon` 으로 교체
  - `ConfigureReserved` 호출 대상에서 5번째 버튼 제외 (이제 활성)
  - `Wire` 호출에서 GoldDungeon 활성화
- `Assets/Scripts/UI/MainUI01Coordinator.cs`
  - `Open(NavTab.GoldDungeon)` 케이스 추가 — 단, 이는 슬라이드업 popup이 아닌 modal popup으로 처리 (`GoldDungeonEntryPanel` 인스턴스화)
  - mutual exclusion: 다른 탭 열려 있으면 닫고 GoldDungeon 모달 표시
- `Assets/Scripts/Core/GameContext.cs` — `GoldDungeonEntryPanel` 참조 추가
- `Assets/Scripts/Core/GameManager.cs` — Auth 후 `goldDungeonEntryPanel.Bind(goldDungeonService)` 호출
- `Assets/Scenes/MainScene.unity` — MainUI01Bar prefab 갱신, GoldDungeonEntryPanel 배치
- `EditorBuildSettings.asset` — GoldDungeonScene 을 build index 2 로 등록

### 🚫 Do Not Touch
- LoginScene 의 인증 흐름 (Bundle 5/6)
- MainScene 의 일반 전투 시스템 (Bundle 1~4)
- `AuthBootstrapHolder` 내부 (Bundle 6 J)
- Bundle 9 의 `OfflineRewardModal` / `GameStartupPopupQueue`

### 🧪 Validation
1. Compile clean
2. PlayMode → MainScene 진입 → MainUI01 의 5번째 슬롯이 "골드던전" 라벨 + 파란색
3. 클릭 → GoldDungeonEntryPanel 모달 표시 (잔여 입장 횟수 표시 — 난이도는 일단 1단계만)
4. "입장" 클릭 → GoldDungeonScene 으로 전환 (1초 이내)
5. GoldDungeonScene 에서 wizard + enemy spawn + 자동 전투 시작
6. 60초 후 자동 종료 또는 "포기" 버튼 클릭 → MainScene 복귀
7. MainScene 복귀 후 인증 상태 그대로 (UID, 가챠/무기/스킬 모두 정상)

### Implementation note
**Scene 별 GameContext 처리:** GoldDungeonScene 도 자체 GameContext 가 필요하지만 MainScene 의 모든 시스템(가챠, 채팅 등)을 들이지는 말 것. `GoldDungeonContext` 를 별도로 두고, 필수 시스템(`PlayerWizard`, `EnemySpawner`, `ProjectileFactory`, `CombatCalculator`, `CurrencyWallet` 참조만 import 하도록 — `CurrencyWallet` 은 MainScene 에 그대로 두고 `AuthBootstrapHolder` 같은 정적 접근으로 가져옴.

**입장 모달 vs 슬라이드업 popup 차이:** 다른 메인UI01 탭(강화/무기/소환/스킬)은 같은 Scene 안의 슬라이드업 popup. GoldDungeon 은 Scene 자체 변경이라 다른 동작 — 모달 형태가 자연스러움. `MainUI01Coordinator` 가 이 차이를 내부에서 처리.

---

## Task AG — GoldDungeonService + Daily Limit + Reward Calculation

**Status:** 🟡 IN REVIEW
**Depends On:** AF ✅

### 🎯 Goal
일일 입장 횟수 제한 + 던전 내 처치당 골드 보상 + 난이도 스테이지 정의 + Best Record 저장.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `GoldDungeonService` MonoBehaviour 신규 — 일일 입장 카운터 / 보상 계산 / Best Record
- [ ] **일일 입장 N회** (시드: 3회) — `MissionResetService` 의 KST 00:00 리셋과 동일 경계
- [ ] 일일 카운터: SaveData.goldDungeonState.lastEntryDateUtcMs + todayEntryCount
  - 새 KST 일자 도래 시 todayEntryCount = 0
- [ ] 보상 계산: 처치당 골드 = current chapter 평균 stage gold × 1.5x (던전 보너스)
- [ ] **난이도 스테이지** (References 222) 시드: 5단계 (Lv1~Lv5). 각 단계는 enemy HP × 1.5^(level-1), gold reward × 1.3^(level-1)
- [ ] Lv1 던전은 무료 입장. Lv2 이상은 잠금 — Bundle 11 (Player Level) 도입 후 Player Level 로 해금. **Bundle 10 단계에서는 Lv1 만 활성, Lv2~Lv5 슬롯은 회색·"준비중" 표시**
- [ ] Best Record 저장: bestScore = max(이전 bestScore, 현재 결과 골드)
- [ ] `BeginEntryAsync(int difficulty) → bool` — 입장 가능 여부 확인 + todayEntryCount++ + lastEntryDateUtcMs 업데이트
- [ ] `CompleteEntryAsync(GoldDungeonResult result, bool watchedAd) → long` — 보상 지급 + bestScore 갱신

### 📂 Files to Add
- `Assets/Scripts/Dungeons/GoldDungeonService.cs`
  ```csharp
  public class GoldDungeonService : MonoBehaviour
  {
      [SerializeField] private int dailyEntryLimit = 3;
      [SerializeField] private GoldDungeonDifficulty[] difficulties;   // length 5

      public void Initialize(SaveService save, MissionResetService reset, CurrencyWallet wallet,
                              StageManager stageMgr, IRewardedAdProvider adProvider);

      public Task<int> GetTodayEntryCountAsync();
      public Task<bool> CanEnterTodayAsync();
      public Task<bool> BeginEntryAsync(int difficultyIndex);
      public Task<long> CompleteEntryAsync(GoldDungeonResult result, bool watchedAd);
      public long GetBestScore();
      public IReadOnlyList<GoldDungeonDifficulty> Difficulties => difficulties;

      public event Action<int> EntryCountChanged;
      public event Action<long> BestScoreChanged;
  }

  [Serializable]
  public class GoldDungeonDifficulty
  {
      public int level;                  // 1~5
      public float enemyHpMultiplier;
      public float goldRewardMultiplier;
      public int unlockPlayerLevel;      // Bundle 11 이전엔 0 (Lv1만 활성)
  }
  ```
- `Assets/Scripts/Dungeons/GoldDungeonState.cs` (Serializable POCO)
  ```csharp
  [Serializable]
  public class GoldDungeonState
  {
      public long lastEntryDateUtcMs = 0;
      public int todayEntryCount = 0;
      public long bestScore = 0;
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — `public GoldDungeonState goldDungeon = new();` 추가 (Task AI에서 saveVersion bump 처리)
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — mirror
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply
- `Assets/Scripts/Core/GameContext.cs` — `GoldDungeonService` 등록
- `Assets/Scripts/Core/GameManager.cs` — Auth + StageManager.Initialize 후 `goldDungeonService.Initialize(...)` 호출

### 🚫 Do Not Touch
- AF의 `GoldDungeonBootstrap` 내부 — 인터페이스만
- `MissionResetService` 내부 (Bundle 8 Z)
- 기타 Bundle 1~9 시스템

### 🧪 Validation
1. Compile clean
2. 처음 진입 → todayEntryCount = 0 → CanEnterTodayAsync = true
3. BeginEntryAsync(0) → todayEntryCount = 1, return true
4. 3회 BeginEntryAsync → 3회째 후 todayEntryCount = 3, CanEnterTodayAsync = false (4번째 false 반환)
5. KST 00:00 시뮬레이션 (lastEntryDateUtcMs 를 어제 23:59 로 수동 설정) → 새 일자 도래 → todayEntryCount = 0 자동 리셋
6. CompleteEntryAsync(result, false) → wallet += result.earnedGold, bestScore 갱신
7. CompleteEntryAsync(result, true) → wallet += result.earnedGold × 2
8. Difficulty Lv1 만 unlockPlayerLevel = 0 (즉시 활성), Lv2~Lv5 는 unlockPlayerLevel >= 5 (Bundle 11에서 활성화 예정)
9. Save + restart → goldDungeonState (lastEntryDateUtcMs, todayEntryCount, bestScore) 보존

### Implementation note
**Difficulty 시드값 (planner-proposed):**
```
Lv1: enemyHp×1.0, gold×1.0, unlockLevel=0
Lv2: enemyHp×1.5, gold×1.3, unlockLevel=5
Lv3: enemyHp×2.25, gold×1.69, unlockLevel=10
Lv4: enemyHp×3.375, gold×2.197, unlockLevel=15
Lv5: enemyHp×5.0625, gold×2.856, unlockLevel=20
```
Bundle 10 단계에서는 Player Level 시스템이 미구현이라 Lv2~Lv5 모두 unlockLevel 미달 상태로 표시. Bundle 11 (Player Level) 진입 후 Player Level 이 unlockLevel 이상이면 활성.

**일일 카운터 리셋 판정:** Bundle 8 Z 패턴 그대로. `MissionResetService.IsNewDay(savedDateUtcMs, currentServerNowMs)` 메서드가 존재하면 그것 사용. 없으면 KST 변환 후 date 비교 인라인.

---

## Task AH — GoldDungeon UI + Ad 2x + Best Record

**Status:** 🔴 TODO
**Depends On:** AG ✅

### 🎯 Goal
입장 모달 / 던전 내 카운트다운 / 결과 모달 / Sweep 모달 / 광고 2배 + Best Record 표시.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `GoldDungeonEntryPanel` 모달:
  - 잔여 입장 횟수 표시 (`<남은>/<총>`)
  - 난이도 슬롯 5개 (Lv1 활성, Lv2~Lv5 잠금·회색 + "Lv N 필요" 라벨)
  - "입장" 버튼 (선택 난이도 입장)
  - "Sweep" 버튼 (Best Record 즉시 받기 — bestScore > 0 일 때만 활성)
  - "취소" 버튼 (모달 닫기)
- [ ] GoldDungeonScene 내 `CountdownTimerView` — 우측 상단 60초 카운트다운
- [ ] `GoldDungeonResultModal` (MainScene 복귀 후 자동 표시):
  - "<처치 수> / <획득 골드>" 표시
  - "Best Record: <bestScore>" 표시 + 신기록 갱신 시 "🏆 신기록!" 강조
  - "받기" 버튼 → 골드 +N
  - "광고 보고 2배" 버튼 → AdSim → 골드 +N×2
- [ ] Sweep 결과: bestScore 즉시 지급 (광고 2배 옵션 포함). 일일 입장 1회로 카운트.
- [ ] 광고 시청 2배는 Bundle 9 의 `IRewardedAdProvider` 그대로 사용 (Bundle 23에서 실제 SDK 교체)

### 📂 Files to Add
- `Assets/Scripts/UI/GoldDungeonResultModal.cs`
- `Assets/Scripts/UI/GoldDungeonDifficultySlotView.cs`
- `Assets/Prefabs/UI/GoldDungeonResultModal.prefab`
- `Assets/Prefabs/UI/GoldDungeonDifficultySlot.prefab`

### 📂 Files to Modify
- `Assets/Scripts/UI/GoldDungeonEntryPanel.cs` (Task AF 에서 추가됨) — 난이도 슬롯 + Sweep 버튼 + 잔여 횟수 표시 보강
- `Assets/Scripts/Dungeons/GoldDungeonBootstrap.cs` — `Completed` 이벤트가 발생하면 결과를 SceneTransfer.PendingResult 에 저장 후 MainScene 로드 → MainScene 진입 직후 GoldDungeonResultModal 표시
- `Assets/Scripts/Core/GameStartupPopupQueue.cs` (Bundle 9 AD) — GoldDungeonResultModal 도 등록 가능하도록 확장 (오프라인 보상 모달 다음 우선순위)
- `Assets/Scripts/Core/GameManager.cs` — `GoldDungeonResultModal` Bind + popup queue 등록

### 🚫 Do Not Touch
- AF / AG 내부 로직
- Bundle 9 OfflineRewardModal 내부
- 메인UI01 / 가챠 / 무기 / 스킬

### 🧪 Validation
1. Compile clean
2. PlayMode → "골드던전" → 모달 표시 → Lv1 만 파란색, Lv2~Lv5 회색 + "Lv 5 필요" 라벨
3. "입장" 클릭 → GoldDungeonScene → 60초 카운트다운 + 자동 전투 → 결과 → MainScene → 결과 모달 자동 팝업
4. 결과 모달: 처치 수 + 획득 골드 + bestScore 비교
5. 첫 진입 시 신기록 → "🏆 신기록!" 강조
6. "받기" → 골드 += N
7. "광고 보고 2배" → AdSim → 골드 += N×2
8. Sweep 버튼 (bestScore > 0 일 때): 즉시 결과 모달 표시 (전투 없이) → bestScore 그대로 받기 / 광고 2배 옵션
9. 일일 3회 모두 사용 → 입장 모달의 "입장" 버튼 비활성, "오늘 입장 횟수를 모두 사용했습니다" 피드백
10. (Cross-feature) Bundle 9 오프라인 보상 모달이 있는 경우 먼저 표시되고, 닫은 후 GoldDungeon 결과 모달 표시 (popup queue 우선순위 정상)

### Implementation note
**Scene 간 결과 전달:** GoldDungeonScene 에서 결과를 어떻게 MainScene 로 넘기는가 — 가장 단순한 방식은 정적 클래스 `GoldDungeonSceneTransfer.PendingResult`. MainScene 진입 후 GameStartupPopupQueue 가 PendingResult 를 확인하여 모달 등록.

**광고 2배 흐름:** Bundle 9 의 `IRewardedAdProvider` 인터페이스 재사용. `GoldDungeonResultModal.OnClaimAd` 가 `await adProvider.WatchRewardedAdAsync()` → 1초 대기 → 골드 ×2.

---

## Task AI — Save Schema v6 Migration + Cross-Feature Regression

**Status:** 🔴 TODO
**Depends On:** AH ✅

### 🎯 Goal
saveVersion = 6 bump + v5 → v6 마이그레이션 + Bundle 5/6/7/8/9 회귀 시험.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SaveData.saveVersion = 6`
- [ ] v5 → v6 마이그레이션:
  - `goldDungeon` 누락이면 default-fill (lastEntryDateUtcMs = 0, todayEntryCount = 0, bestScore = 0)
- [ ] Cloud sync 라운드트립이 신규 필드 보존
- [ ] Bundle 10 release regression test 1~14번 모두 통과

### 📂 Files to Add
- (None)

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — `public int saveVersion = 6;`
- `Assets/Scripts/Save/SaveService.cs` — `MigrateIfNeeded` 에 v5→v6 분기 추가
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — Firestore mirror

### 🚫 Do Not Touch
- AF / AG / AH 코드
- v5 baseline 데이터 모델 (Bundle 9 까지의 필드들)

### 🧪 Validation
1. Compile clean
2. v5 save.json 로딩 → 자동으로 saveVersion = 6 + goldDungeon default-fill
3. v6 save.json 로딩 → 그대로 보존 (no double migration)
4. Bundle 10 regression 1~14번 전체 통과 (PlayMode 직접 시연)
5. **Cross-feature regression**:
   - Bundle 9 오프라인 보상 모달 정상 (GameStartupPopupQueue 우선순위)
   - Bundle 8 출석 / 일일 미션 자동 리셋 정상 (KST 경계 공유)
   - Bundle 8 스킬 바 / 가챠 30뽑기 / 채팅 / 무기 / 메인UI01 모두 정상
   - Bundle 6 위치공유 5Hz / 채팅 / Bundle 5 Cloud Sync 라운드트립 정상

### Implementation note
**KST 경계 공유:** Bundle 8 Z의 `MissionResetService` 가 KST 일자 판정 메서드를 publish하고 있어야 함. 만약 private 이라면 implementer 가 메서드를 internal/public 으로 승격하거나 동치 helper 를 새로 작성. 새 Firestore call 추가 금지.

---

# Bundle 10 Release Gate

When Tasks AF~AI are all `✅ DONE`, run one integrated PlayMode session:

1. Delete local save and Firestore game document
2. Start from LoginScene → MainScene
3. MainUI01 의 5번째 슬롯이 "골드던전" + 파란색
4. 클릭 → 입장 모달 → 잔여 3회 + Lv1 활성 / Lv2~Lv5 잠금
5. "입장" Lv1 클릭 → GoldDungeonScene 전환 + 60초 카운트다운 + 자동 전투
6. 60초 종료 → MainScene 복귀 → 결과 모달 자동 팝업 + 신기록 표시
7. "받기" → 골드 +N
8. 두 번째 입장 → "광고 보고 2배" → 골드 +N×2
9. 세 번째 입장 → 받지 않고 닫기 → 다음 진입 시 같은 결과 다시 표시 (idempotent — Bundle 9 패턴 재사용)
10. 4번째 입장 시도 → "오늘 입장 횟수를 모두 사용했습니다" 피드백
11. KST 00:00 경계 시뮬레이션 → 다시 3회 가능
12. Sweep 버튼 (bestScore > 0) → 즉시 결과 모달 → bestScore 받기 (일일 1회 카운트)
13. Save + restart → goldDungeonState 모두 보존
14. v5 save 마이그레이션 → 모든 v9 진척 보존 + goldDungeon default-fill
15. (Cross-feature regression) Bundle 5/6/7/8/9 모든 핵심 기능 정상

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies:
1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — no unauthorized files modified (`git diff` to confirm)
3. **"Do Not Touch" areas unchanged**
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation matches the design intent
6. **Exactly one git commit** — message includes the task ID
7. **No regression in prior bundles** — sanity check Bundle 1~9 features still function

Record review results in Appendix E.

---

## Appendix B — Bundle Gate Checklist

After Task AI reaches `✅ DONE`:
1. Bundle 10 release regression tests pass (전 15개)
2. `git log` clean — every task AF~AI has exactly one implementation commit
3. Unity Console 0 errors / 0 warnings
4. Save migration v5 → v6 tested with an existing v9 save file
5. Fresh save tested
6. No unauthorized edits to prior `Tasks*.md`

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-09 | Planner | Document created. Bundle 10 = Gold Dungeon. 4개 Task (AF/AG/AH/AI). 사용자 결정사항: (1) 별도 Scene 스위치, (2) 일일 입장 제한 (3회 시드), (3) Sweep 기능 포함, (4) 광고 시청 2배. Bundle 11 (Player Level) 이전 단계라 난이도 Lv1 만 활성, Lv2~Lv5 는 잠금. Bundle 11 진입 시 Player Level 로 해금. |

---

## Appendix D — Combined Work Log (implementer)

| Date | Task | Entry |
|------|------|-------|
| 2026-05-11 | Task AF | Added `GoldDungeonScene`, `GoldDungeonBootstrap`, `GoldDungeonEntryPanel`, `CountdownTimerView`, entry/timer prefabs, MainUI01 gold dungeon tab wiring, GameContext/GameManager/HUD runtime panel binding, and build setting index 2 registration. Validation PASS: `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors / 0 warnings; MCP asset validation confirmed GoldDungeonScene, entry/timer prefabs, and build index order LoginScene/MainScene/GoldDungeonScene. Unity Console still contains MCP stale-client/disposed-object transport logs after refresh timeouts; no Task AF compile/runtime product error was found. Start-state unrelated dirty files left unstaged: `.DS_Store`, `Assets/.DS_Store`, `Assets/Scripts/.DS_Store`, `Assets/Fonts/NanumGothicBold SDF.asset`, deleted `Tasks_BtoI_Draft.md`, `Tasks_v7.md`, `.codex/`, `References.md`, later Tasks_v11~v17/v20/v21 docs. |
| 2026-05-11 | Task AG | Added `GoldDungeonService`, `GoldDungeonState`, five seeded difficulties, save/cloud mapper mirror, SaveBinder capture/autosave trigger, GameContext/GameManager service wiring, and EntryPanel service-backed entry gating. Validation PASS: `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors / 0 warnings; MCP validation confirmed initial entry count, daily limit at 3, 4th entry rejection, KST day reset via `MissionResetService`, reward calculation, normal/ad 2x completion, best score persistence, mapper round-trip, and Lv1/Lv2~Lv5 unlock settings. Unity Console residual entries are MCP stale-client disconnect logs only. |

---

## Appendix E — Combined Review Log (reviewer)

| Date | Task | Entry |
|------|------|-------|
