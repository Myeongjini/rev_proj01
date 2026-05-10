# Wizard Grower — Tasks v9 (Bundle 9: Offline Reward System)

> Follow-up work track to `Tasks_v8.md`.
> Different agents, different bundle, different file.
> **Do NOT edit `Tasks.md` / `Tasks_v6.md` / `Tasks_v7.md` / `Tasks_v8.md` from this track.** Read-only access only.
>
> The **Planner / Reviewer (Claude)** is the sole editor of this document.
> Implementers read this document, modify the code, and append to Appendix D.
>
> 장기 로드맵 (Bundle 10~18+) 은 `References.md` §4.1 참조. 본 문서는 Bundle 9 (오프라인 보상)만 다룸.

---

## 0. Common Work Rules

### 0.1 Basic Rules
1. **Only one task at a time.** State the task ID (e.g. `Task AB`) at start and end.
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
Bundle 9 is sequential. Do not start the next task until the previous task is `✅ DONE`.

**Gate pass conditions:**
1. All tasks AB~AE are `✅ DONE`
2. Unity Console has 0 errors / 0 warnings
3. Bundle 9 release regression tests pass
4. Save migration from v4 to v5 preserves existing v8 progress (skills, missions, attendance, weapons, gems)

### 0.3 Auto-Progression Restrictions (Implementation Agent)
- Do not start another Bundle 9 task on your own.
- On signature/spec conflict, **do not decide unilaterally** — record in Appendix D and wait for the reviewer.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason.
- Do not introduce new major systems or files not listed in this document unless required for compilation; log exceptions and await reviewer approval.

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>`.
- This repo uses GitHub Desktop with origin set. **Agents must not push** — push is the user's responsibility.
- At task start, run `git status` to confirm working tree state. Note any uncommitted changes in Appendix D.
- Per task = per commit. `git revert` is the rollback tool.

### 0.5 Cross-Track Coordination ⚠️
Bundle 9 builds on the v8 baseline (Tasks W~AA).

| Risk | Mitigation |
|---|---|
| `Tasks_v8.md` Bundle 8 일부 Task가 ✅ DONE 미동기화 상태일 가능성 | v8 코드베이스가 baseline. v8 docs 갱신은 별개 작업. Bundle 9 implementer는 v8 Tasks 문서 status가 어떻든 코드를 baseline으로 인정 |
| Save schema 변경 (v4 → v5) | Task AE가 schema bump + 마이그레이션 담당. AB~AD는 SaveData 필드 추가만 가능, 마이그레이션 로직은 AE에서 처리 |
| Server time 호출 — `MissionResetService` (Bundle 8 Z) 재사용 | **새 Firestore serverTimestamp 호출 추가 금지.** `MissionResetService.GetServerNowMsAsync()` 또는 동치 메서드를 통해 동일 캐시 활용 |
| `AttendanceService` (Bundle 8 AA) 와 동시 자동 팝업 충돌 가능 | `SecondaryPanelCoordinator` (Bundle 8 AA) 패턴 재사용. 오프라인 보상 모달은 Auto/Chat/Achievement/Attendance 중 하나로 분류되지 않는 **일회성 모달**이므로 별도 GameStartupPopupQueue 도입 — Task AD 명세 |
| 광고 SDK 미통합 (Bundle 18+) | Bundle 9는 **광고 시청 2배**를 **시뮬레이션 모드**로 구현. `AdSimulationService.WatchRewardedAd(callback)` 인터페이스만 마련하고 즉시 콜백 호출 (실제 SDK는 Bundle 18+에서 wire) |
| Player Level 미구현 (Bundle 11) | Bundle 9는 **골드만 누적**. EXP 누적은 Bundle 11에서 Player Level 도입 시 OfflineRewardService 확장 |

---

## 1. Task Dependency Graph

```
Bundle 9
AB → AC → AD → AE → Bundle 9 Release Gate

AB:  Offline Time Tracking Infrastructure (lastSeenAt + 서버 시간 elapsed 계산)
AC:  Offline Gold Reward Calculation Engine (현재 stage 기반 누적 골드)
AD:  Offline Reward Modal UI (자동 팝업 + 광고 2배 + 시뮬레이션 ad)
AE:  Save Schema v5 Migration + Cross-Feature Regression
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 9 | AB | Offline Time Tracking Infrastructure | 🟡 IN REVIEW | v8 baseline |
| 9 | AC | Offline Gold Reward Calculation | 🔴 TODO | AB ✅ |
| 9 | AD | Offline Reward Modal UI + Ad Simulation | 🔴 TODO | AC ✅ |
| 9 | AE | Save Schema v5 Migration + Regression | 🔴 TODO | AD ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 9 — Offline Reward System

**Goal:** 플레이어가 앱을 종료한 시간만큼 누적 골드 보상을 자동 계산해 재접속 시 자동 모달로 수령. 광고 시청 시 2배. 캡 12시간.

### Bundle 9 Release Regression Tests
1. 앱 정상 종료 → 재실행 → MainScene 진입 직후 OfflineRewardModal 자동 팝업 (단, 경과 시간 < 30초이면 미표시)
2. 모달은 `<경과 시간>` (시:분 형식, 예 `2시간 35분`) + `<누적 골드>` 표시
3. "받기" 버튼 클릭 → 골드 +N → 모달 닫힘 → save round-trip
4. "광고 보고 2배" 버튼 클릭 → AdSimulationService 호출 → 시뮬레이션 광고 완료 → 골드 +2N → 모달 닫힘
5. 12시간 캡: 24시간 경과 후 재접속해도 보상은 12시간 분량만 지급
6. 짧은 재접속 (< 30초) 은 모달 미표시 (정상 멀티태스킹 케이스)
7. 광고 시청 시 정확히 2배가 지급되는지 (n × 2, 부동소수 오차 없음)
8. 모달 미수령 상태에서 앱 강제종료 → 재접속 시 같은 보상 그대로 표시 (idempotent — 중복 지급 방지)
9. v4 save 로딩 시 default-fill로 lastSeenAt = 현재 시간, offlinePendingReward = 0 → 첫 보상 0
10. saveVersion = 5
11. Cloud sync round-trip: 다른 기기에서 로그인 → lastSeenAt 정확히 복원 → 지속 누적
12. (Cross-feature regression) Bundle 8 출석 / 일일 미션 자동 모달 / 채팅 / 메인UI01 모두 정상

---

## Task AB — Offline Time Tracking Infrastructure

**Status:** 🟡 IN REVIEW
**Depends On:** v8 baseline (특히 Bundle 8 Z의 `MissionResetService.GetServerNowMsAsync` 또는 동치 서버 시간 메서드)

### 🎯 Goal
앱이 종료/일시정지된 마지막 시각(`lastSeenAtUtcMs`)을 기록하고, 재실행 시 Firestore 서버 시간 기준으로 경과 초를 계산하는 기반 인프라.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `OfflineTimeTracker` MonoBehaviour 신규 — 앱 일시정지/종료/포커스 변화에 후크
- [ ] Application life-cycle hooks: `OnApplicationPause(true)` / `OnApplicationQuit()` 시 현재 서버 시간을 SaveData.lastSeenAtUtcMs에 기록 + SaveBinder.SaveNow 호출
- [ ] 재시작 시 SaveService.LoadFromDisk → SaveData.lastSeenAtUtcMs를 읽고, 서버 시간과 비교해 elapsed 초 산출
- [ ] elapsed 초가 30초 미만이면 "정상 멀티태스킹" 으로 간주하고 OfflineReward 흐름을 트리거하지 않음 (Task AD 자동 팝업 제외 조건)
- [ ] 12시간 캡 (43200초) 적용. elapsed > 캡 시 elapsed = 캡으로 클램프
- [ ] 음수 elapsed (서버 시간 역행) 시 0으로 클램프 + 경고 로그
- [ ] 신규 `IOfflineTimeProvider` 인터페이스 — Task AC 가 의존성 주입으로 사용

### 📂 Files to Add
- `Assets/Scripts/Offline/OfflineTimeTracker.cs`
  ```csharp
  public class OfflineTimeTracker : MonoBehaviour, IOfflineTimeProvider
  {
      [SerializeField] private long capSeconds = 43200;        // 12h
      [SerializeField] private long minTriggerSeconds = 30;    // 30초 미만은 무시

      public void Initialize(SaveService saveService, MissionResetService resetService);

      // life-cycle hook
      private void OnApplicationPause(bool paused);
      private void OnApplicationQuit();

      // public API for Task AC
      public Task<OfflineWindow> ResolveOfflineWindowAsync();
      public bool ShouldTriggerOfflineFlow(OfflineWindow window);
  }

  [Serializable]
  public struct OfflineWindow
  {
      public long elapsedSeconds;       // 0 ~ capSeconds (clamped)
      public long lastSeenAtUtcMs;
      public long currentServerNowMs;
      public bool isCapped;             // elapsed가 cap에 도달했는지
  }

  public interface IOfflineTimeProvider
  {
      Task<OfflineWindow> ResolveOfflineWindowAsync();
      bool ShouldTriggerOfflineFlow(OfflineWindow window);
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  // 신규 필드 (Task AE 가 v5 마이그레이션 처리하지만, 필드 자체는 AB가 추가)
  public long lastSeenAtUtcMs = 0;
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — `lastSeenAtUtcMs` mirror
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply lastSeenAtUtcMs
- `Assets/Scripts/Core/GameContext.cs` — `[field: SerializeField] public OfflineTimeTracker OfflineTime { get; private set; }` 추가
- `Assets/Scripts/Core/GameManager.cs` — Auth 후 `OfflineTime.Initialize(context.SaveService, context.MissionResetService)` 호출

### 🚫 Do Not Touch
- `MissionResetService` 내부 로직 (Bundle 8 Z) — 호출만 가능
- `AttendanceService` (Bundle 8 AA) — 분리 시스템
- 가챠 / 무기 / 스킬 / 채팅 / 인증

### 🧪 Validation
1. Compile clean
2. PlayMode → 시작 → ResolveOfflineWindowAsync → elapsedSeconds = 0 (lastSeenAtUtcMs 첫 진입은 현재 시간으로 default-fill)
3. PlayMode 종료 → SaveData.lastSeenAtUtcMs 가 종료 시 서버 시간으로 기록됨 (save.json 검사)
4. 30초 후 재진입 → elapsed = ~30초 → ShouldTriggerOfflineFlow == true
5. 10초 후 재진입 → ShouldTriggerOfflineFlow == false
6. 24시간 후 재진입 (시뮬레이션: lastSeenAtUtcMs를 25h 전으로 수동 설정) → elapsed = 43200 (cap), isCapped = true
7. 음수 elapsed (lastSeenAtUtcMs = future) → 0으로 클램프 + 콘솔 경고

### Implementation note
**Server time 호출:** Bundle 8 Z의 `MissionResetService` 가 이미 Firestore serverTimestamp 호출 + 캐싱을 구현. 재호출 시 같은 캐시 사용. 만약 `MissionResetService.GetServerNowMsAsync()` 메서드가 존재하지 않으면 — Bundle 8 Z 구현이 다른 이름을 쓸 가능성 — implementer 는 해당 서비스의 실제 public API를 확인하여 동치 호출 사용. 새 Firestore call 추가 금지.

**Application life-cycle:** Unity는 `OnApplicationPause(true)` 가 백그라운드 진입 시, `OnApplicationQuit()` 가 정상 종료 시 호출. 두 곳 모두에서 lastSeenAtUtcMs 갱신 + SaveBinder.SaveNow. 이미 Bundle 5 H의 GameManager 에 `OnApplicationPause/Quit` hook이 있으니 OfflineTimeTracker가 별도로 구독하지 말고 GameManager 에서 호출하는 패턴을 사용해도 OK.

---

## Task AC — Offline Gold Reward Calculation

**Status:** 🔴 TODO
**Depends On:** AB ✅

### 🎯 Goal
경과 시간 + 현재 stage 기반으로 누적 골드 보상을 결정하는 계산 엔진. idempotent (재진입 시 같은 결과).

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `OfflineRewardCalculator` 순수 C# 클래스 — `CalculateGold(OfflineWindow window, ChapterDefinition currentChapter, StageDefinition currentStage, PlayerStats stats) → long`
- [ ] 골드 비율 공식: 현재 stage의 `goldPerKill` × 평균 처치율 × elapsedSeconds
  - 평균 처치율 = `1 / 현재 stage 기본 자동 공격 시간` × 효율 계수 0.5 (오프라인은 50% 효율)
- [ ] `OfflineRewardService` MonoBehaviour — 인벤토리 + 지갑 + 모달 와이어링 게이트웨이
  - `Task<OfflineRewardSnapshot> ResolvePendingAsync()` — Task AB의 OfflineWindow → 누적 골드
  - `Task ClaimAsync(bool watchedAd)` — 골드 지급 + 마지막 lastSeenAtUtcMs 갱신 + save
- [ ] **Idempotent**: ResolvePendingAsync 가 클레임 전에 여러 번 호출되어도 같은 OfflineRewardSnapshot 리턴 (서버 시간이 다시 흐른 만큼은 무시)
  - 구현: SaveData.offlineRewardPending 필드 (long gold) 가 0 이상일 때는 그 값을 그대로 사용. 0이면 새로 계산하여 SaveData.offlineRewardPending 에 기록
  - ClaimAsync 가 wallet.AddGold + offlineRewardPending = 0 + lastSeenAtUtcMs 갱신
- [ ] watchedAd = true 시 골드 × 2

### 📂 Files to Add
- `Assets/Scripts/Offline/OfflineRewardCalculator.cs`
  ```csharp
  public static class OfflineRewardCalculator
  {
      public static long CalculateGold(OfflineWindow window, ChapterDefinition chapter, StageDefinition stage, PlayerStats stats);
  }
  ```
- `Assets/Scripts/Offline/OfflineRewardService.cs`
  ```csharp
  public class OfflineRewardService : MonoBehaviour
  {
      public void Initialize(IOfflineTimeProvider time, CurrencyWallet wallet, StageManager stageMgr, PlayerWizard wizard, SaveService save);

      public Task<OfflineRewardSnapshot> ResolvePendingAsync();
      public Task ClaimAsync(bool watchedAd);

      public event Action<OfflineRewardSnapshot> PendingResolved;
      public event Action<long, bool> Claimed;   // (totalGold, watchedAd)
  }

  [Serializable]
  public struct OfflineRewardSnapshot
  {
      public long elapsedSeconds;
      public bool isCapped;
      public long baseGold;
      public long maxAdMultipliedGold;   // baseGold × 2
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  public long offlineRewardPending = 0;   // > 0이면 미수령 보상 존재
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — mirror
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply
- `Assets/Scripts/Core/GameContext.cs` — `OfflineRewardService` 등록
- `Assets/Scripts/Core/GameManager.cs` — Auth 후 + StageManager.Initialize 후 `OfflineRewardService.Initialize(...)` 호출

### 🚫 Do Not Touch
- AB의 `OfflineTimeTracker` 내부 — 인터페이스 호출만
- 가챠 / 무기 / 스킬 / 미션 / 출석 / 채팅 / 인증
- `RewardService` (있다면) 내부 로직

### 🧪 Validation
1. Compile clean
2. lastSeenAtUtcMs를 1시간 전으로 수동 설정 후 PlayMode → ResolvePendingAsync → snapshot.elapsedSeconds ~ 3600 → snapshot.baseGold > 0
3. 같은 PlayMode에서 ResolvePendingAsync 두 번 호출 → 둘 다 동일한 baseGold 리턴 (idempotent)
4. ClaimAsync(false) → 지갑 골드 += baseGold, SaveData.offlineRewardPending = 0, lastSeenAtUtcMs = 현재 서버 시간
5. ClaimAsync(true) 의 경우 → 지갑 += baseGold × 2
6. 캡 12시간 시뮬레이션 (lastSeenAtUtcMs = 24h 전) → snapshot.isCapped = true, baseGold 는 12시간 분량만
7. 미수령 상태로 앱 강제종료 → 재진입 → ResolvePendingAsync → 같은 baseGold (offlineRewardPending 필드에서 읽음, 재계산 X)
8. Save + restart → offlineRewardPending 보존

### Implementation note
**골드 비율 시드값**: 현재 stage의 `goldPerKill` 이 정확히 어떤 필드인지는 implementer가 `StageDefinition` 또는 `RewardService` 코드 점검 후 결정. 적절한 필드가 없다면 `GameplayConstants.AutoAttackInterval` (1초 가정) × `EnemyHpAtStage` 처치 시 골드 = `RewardForKillAtStage` 라는 식으로 계산. 효율 0.5는 "오프라인 효율 페널티" — 일반적인 idle RPG 패턴.

**idempotent 흐름:**
```
앱 종료 (lastSeen = T0)
   ↓
재시작 (서버 시간 = T1)
   ↓
ResolvePending → SaveData.offlineRewardPending == 0 이면 신규 계산:
                    elapsed = min(T1 - T0, cap)
                    gold = formula(elapsed, stage)
                    SaveData.offlineRewardPending = gold
                    SaveBinder.SaveNow
                 SaveData.offlineRewardPending > 0 이면 그 값 그대로 리턴
   ↓
모달 노출 (Task AD)
   ↓
사용자 강제종료 (수령 안 함)
   ↓
재시작 → ResolvePending → SaveData.offlineRewardPending == X (이전 값) → 같은 X 리턴
   ↓
ClaimAsync 호출 시:
   wallet.AddGold(X × multiplier)
   SaveData.offlineRewardPending = 0
   SaveData.lastSeenAtUtcMs = T2 (현재 서버 시간)
   SaveBinder.SaveNow
```

---

## Task AD — Offline Reward Modal UI + Ad Simulation

**Status:** 🔴 TODO
**Depends On:** AC ✅

### 🎯 Goal
재접속 시 자동 팝업되는 보상 모달 + 광고 시뮬레이션 인터페이스. Bundle 8의 `SecondaryPanelCoordinator` 와 충돌하지 않는 1회성 popup queue 도입.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `OfflineRewardModal` UI — full-screen overlay (소환 풀스크린과 동일 스타일)
  - 상단: "오프라인 보상" 타이틀
  - 본문: `경과 시간: <Hh M m>` (예: `2시간 35분`) / `누적 골드: <X>`
  - 버튼: `받기` (파란색) / `광고 보고 2배 (X×2)` (오렌지색)
  - 닫기 X 버튼 (받지 않고 닫기 → 다음 진입 시 재팝업)
- [ ] `GameStartupPopupQueue` MonoBehaviour 신규 — MainScene 진입 직후 한 번씩만 실행되는 1회성 popup 시퀀스 관리. 향후 다른 시작 popup (예: 공지사항, 패스 만료) 추가 가능한 구조
- [ ] OfflineRewardModal은 GameStartupPopupQueue의 첫 번째 등록자
- [ ] OfflineTimeTracker.ShouldTriggerOfflineFlow == true 일 때만 모달 enqueue
- [ ] `AdSimulationService` MonoBehaviour 신규
  - `Task<bool> WatchRewardedAdAsync()` — 시뮬레이션 모드: 1초 후 true 반환 (광고 SDK 미통합 단계 placeholder)
  - 콘솔 로그: `"[AdSim] Rewarded ad watched (simulated)"` 출력
  - 향후 Bundle 18+ 에서 실제 SDK 와 교체 가능하도록 인터페이스 분리

### 📂 Files to Add
- `Assets/Scripts/UI/OfflineRewardModal.cs`
  ```csharp
  public class OfflineRewardModal : MonoBehaviour
  {
      [SerializeField] private CanvasGroup canvasGroup;
      [SerializeField] private TMP_Text titleLabel;
      [SerializeField] private TMP_Text elapsedLabel;
      [SerializeField] private TMP_Text goldLabel;
      [SerializeField] private Button claimButton;
      [SerializeField] private TMP_Text claimButtonLabel;
      [SerializeField] private Button claimAdButton;
      [SerializeField] private TMP_Text claimAdButtonLabel;
      [SerializeField] private Button closeButton;

      public void Bind(OfflineRewardService service, AdSimulationService ad);
      public void Show(OfflineRewardSnapshot snapshot);
      public void Hide();
      public event Action Closed;
  }
  ```
- `Assets/Scripts/Core/GameStartupPopupQueue.cs`
  ```csharp
  public class GameStartupPopupQueue : MonoBehaviour
  {
      public void Register(IGameStartupPopup popup);   // 우선순위 순서대로 호출
      public Task RunAsync();                          // 등록된 popup 순차 처리
  }

  public interface IGameStartupPopup
  {
      bool ShouldShow();
      Task ShowAsync();
  }
  ```
- `Assets/Scripts/Ads/AdSimulationService.cs`
  ```csharp
  public class AdSimulationService : MonoBehaviour, IRewardedAdProvider
  {
      public async Task<bool> WatchRewardedAdAsync()
      {
          await Task.Delay(1000);   // simulate 1s ad
          Debug.Log("[AdSim] Rewarded ad watched (simulated)");
          return true;
      }
  }

  public interface IRewardedAdProvider
  {
      Task<bool> WatchRewardedAdAsync();
  }
  ```
- `Assets/Prefabs/UI/OfflineRewardModal.prefab` — full-screen overlay, canvas sort order 100 (다른 UI 위)

### 📂 Files to Modify
- `Assets/Scripts/Core/GameManager.cs` — Auth + OfflineRewardService 초기화 직후 `await offlinePopupQueue.RunAsync()` 호출
- `Assets/Scripts/Core/GameContext.cs` — `OfflineRewardModal`, `GameStartupPopupQueue`, `AdSimulationService` 등록
- `Assets/Scenes/MainScene.unity` — OfflineRewardModal Canvas 배치 (비활성 상태 시작), GameStartupPopupQueue + AdSimulationService GameObject 추가

### 🚫 Do Not Touch
- AB / AC 내부 로직 — 인터페이스만 호출
- `SecondaryPanelCoordinator` (Bundle 8 AA) — 이건 Auto/Chat/Achievement/Attendance 토글 패널 코디. OfflineRewardModal 은 1회성 시작 popup이므로 다른 시스템
- 메인UI01 / 가챠 / 무기 / 스킬

### 🧪 Validation
1. Compile clean
2. lastSeenAtUtcMs를 1시간 전으로 수동 설정 후 PlayMode → MainScene 진입 직후 OfflineRewardModal 자동 팝업
3. 모달에 "1시간 0분" + 골드 X 표시
4. "받기" 클릭 → 지갑 += X → 모달 닫힘
5. lastSeenAtUtcMs 다시 1시간 전으로 설정 후 PlayMode 재진입 → 모달 재팝업
6. "광고 보고 2배" 클릭 → 콘솔 `[AdSim] Rewarded ad watched (simulated)` → 1초 후 지갑 += X × 2 → 모달 닫힘
7. lastSeenAtUtcMs를 5초 전으로 설정 → 진입 → 모달 미팝업 (30초 임계값 미달)
8. 받지 않고 X 닫기 → 다음 진입 시 재팝업 (offlineRewardPending 보존)

### Implementation note
**시뮬레이션 광고:** Bundle 18+ 에서 AdMob 등 실제 SDK가 들어오면 `AdSimulationService` 를 `AdMobService : IRewardedAdProvider` 로 교체. `OfflineRewardModal.Bind` 가 `IRewardedAdProvider` 인터페이스를 받으니 교체 시 코드 변경 최소.

**한글 시간 포맷:** `elapsedSeconds` → "X시간 Y분" / "X분 Y초" / "X초" 등 자연스러운 한국어. `< 60s` 케이스는 보통 모달 자체가 안 뜨므로 (30s threshold) "분" 단위 시작 가정.

**popup queue priority:** OfflineRewardModal 만 Bundle 9에서 등록. 향후 Bundle 별로 추가 등록자 (예: Pass 만료 알림) 가 늘어남. 현재 단순 List<IGameStartupPopup> 순회로 충분.

---

## Task AE — Save Schema v5 Migration + Cross-Feature Regression

**Status:** 🔴 TODO
**Depends On:** AD ✅

### 🎯 Goal
saveVersion = 5 bump + v4 → v5 마이그레이션 + Bundle 8 (skill/mission/attendance) 회귀 시험.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SaveData.saveVersion = 5`
- [ ] v4 → v5 마이그레이션:
  - `lastSeenAtUtcMs` 누락이면 = 현재 서버 시간 (즉, 첫 v5 로딩 시 오프라인 기간 = 0초)
  - `offlineRewardPending` 누락이면 = 0
- [ ] 마이그레이션 로직은 `SaveService.MigrateIfNeeded` 에 v4→v5 분기 추가
- [ ] Cloud sync (Firestore) 라운드트립이 신규 필드 모두 보존
- [ ] Bundle 9 release regression test 1~12번 모두 통과

### 📂 Files to Add
- (None)

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — `public int saveVersion = 5;`
- `Assets/Scripts/Save/SaveService.cs` — `MigrateIfNeeded` 에 v4→v5 분기 추가
  ```csharp
  if (data.saveVersion < 5)
  {
      // lastSeenAtUtcMs 가 0이면 첫 v5 로딩으로 간주 → 현재 서버 시간 시드 (Task AB initialize에서 처리)
      // offlineRewardPending 0 그대로 유지
      data.saveVersion = 5;
  }
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — Firestore mirror에 신규 필드 확인

### 🚫 Do Not Touch
- AB / AC / AD 코드 (이미 ✅ DONE 상태)
- v4 baseline 데이터 모델 (Bundle 8 까지의 필드들)

### 🧪 Validation
1. Compile clean
2. v4 save.json 로딩 → 자동으로 saveVersion = 5 + lastSeenAtUtcMs/offlineRewardPending default-fill → 첫 보상 0
3. v5 save.json 로딩 → 그대로 보존 (no double migration)
4. Bundle 9 regression 1~12번 전체 통과 (PlayMode 직접 시연)
5. **Cross-feature regression**:
   - Bundle 8 출석 (AttendanceService) 정상 동작
   - Bundle 8 일일 미션 자동 리셋 정상
   - Bundle 8 스킬 바 + 자동 시전 정상
   - Bundle 8 가챠 30뽑기 + 확률 팝업 정상
   - Bundle 7 무기 인벤토리 + 합성 + 등급 그리드 정상
   - Bundle 6 위치 공유 5Hz + 채팅 정상
   - Bundle 5 Auth + Cloud Sync 라운드트립 정상

### Implementation note
**Migration 의 안전성:** v4 → v5 마이그레이션은 default-fill 만 처리하므로 데이터 손실 없음. 하지만 마이그레이션 로직에 버그가 있으면 v8 진척이 모두 사라질 수 있음. 검증 단계에서 **반드시 v4 save 사본을 별도 보관 후 마이그레이션 실행 → 결과 비교**. 실패 시 git revert 로 롤백.

**Cloud sync 호환:** Firestore의 mapper가 새 필드를 읽지 못하면 디폴트로 채워야지 절대 실패하면 안 됨. `SaveDataDocument` 의 새 필드는 nullable / default 값 포함하여 forward-compatible 유지.

---

# Bundle 9 Release Gate

When Tasks AB~AE are all `✅ DONE`, run one integrated PlayMode session:

1. Delete local save and Firestore game document
2. Start from LoginScene → enter MainScene → 일반 플레이 (몬스터 처치, 스테이지 진행)
3. 정상 종료
4. 5초 후 재실행 → MainScene 진입 → **모달 미팝업 (30초 임계값 미달)** 확인
5. 다시 정상 종료
6. lastSeenAtUtcMs를 1시간 전으로 수동 조작 (테스트 도구 또는 save.json 직접 편집)
7. 재실행 → MainScene 진입 → **OfflineRewardModal 자동 팝업** + "1시간 0분" + 골드 N 표시
8. "받기" 클릭 → 지갑 += N → 모달 닫힘
9. 다시 lastSeenAtUtcMs를 1시간 전으로 → 재진입 → 모달 재팝업
10. "광고 보고 2배" 클릭 → 콘솔 `[AdSim] Rewarded ad watched (simulated)` → 지갑 += N × 2
11. lastSeenAtUtcMs를 25시간 전으로 → 재진입 → 모달 표시 + isCapped = true → 12시간 분량만 보상
12. 받지 않고 X 닫기 → 재진입 → 같은 보상 그대로 표시 (idempotent)
13. 받은 후 정상 플레이 → Bundle 8 출석 / 일일 미션 / 가챠 / 무기 / 채팅 / 위치공유 모두 정상
14. v4 save (Bundle 8 후 직후 백업) → v5 자동 마이그레이션 → 모든 v8 진척 보존
15. Quit + 다른 기기에서 같은 UID 로그인 → Cloud sync 라운드트립으로 lastSeenAtUtcMs 복원 → 다시 첫 기기 진입 시 모달 동작 정상

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies:
1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — no unauthorized files modified (`git diff` to confirm)
3. **"Do Not Touch" areas unchanged**
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation matches the design intent
6. **Exactly one git commit** — message includes the task ID
7. **No regression in `Tasks.md` / `Tasks_v6.md` / `Tasks_v7.md` / `Tasks_v8.md` features** — sanity check core combat, save, login, gacha, weapon UI, MainUI01Bar, skills, missions, attendance still function

Record review results in Appendix E.

---

## Appendix B — Bundle Gate Checklist

After Task AE reaches `✅ DONE`:
1. Bundle 9 release regression tests pass (전 15개)
2. `git log` clean — every task AB~AE has exactly one implementation commit
3. Unity Console 0 errors / 0 warnings
4. Save migration v4 → v5 tested with an existing v8 save file
5. Fresh save tested
6. No unauthorized edits to `Tasks.md` / `Tasks_v6.md` / `Tasks_v7.md` / `Tasks_v8.md`

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-09 | Planner | Document created. Bundle 9 = 오프라인 보상 시스템 단일 Bundle. 4개 Task (AB/AC/AD/AE). 사용자 결정사항: (1) 캡 12시간, (2) 광고 시청 시 2배 (시뮬레이션 모드 — Bundle 18+ 에서 실제 SDK), (3) 자동 모달 (로그인 후 자동 팝업), (4) saveVersion v4 → v5 bump. Bundle 9 이후 우선순위는 References.md §4.1 참조 (Bundle 10 = Gold Dungeon — 우선순위 상승). Player Level 미구현 단계라 EXP 누적 보상은 Bundle 11에서 OfflineRewardCalculator 확장. Bundle 8 baseline 의존: MissionResetService 의 서버 시간 호출 재사용 + AttendanceService 와의 popup 충돌 방지 위해 `GameStartupPopupQueue` 별도 도입. |

---

## Appendix D — Combined Work Log (implementer)

> Implementers append a row here on each task transition. Format: `YYYY-MM-DD | Task X | <one-line summary>`.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-11 | Task AB | Added `OfflineTimeTracker` + `IOfflineTimeProvider`, `OfflineWindow`, `lastSeenAtUtcMs` save/cloud mirror, GameContext/GameManager lifecycle wiring, and 30s trigger / 12h cap / negative elapsed clamp behavior. Unity batchmode validation PASS: first-run seed, 10s ignored, 31s triggers, 25h clamps to 43200s with cap flag, future timestamp clamps to 0 with expected warning, mapper round-trip, and last-seen save. Batchmode still emitted external UnityConnect timeout and pre-existing TMP font-atlas quit exception after PASS; no Task AB compile/runtime validation failure occurred. Start-state unrelated dirty files left unstaged: `.DS_Store`, `Assets/.DS_Store`, `Assets/Scripts/.DS_Store`, `Assets/Fonts/NanumGothicBold SDF.asset`, deleted `Tasks_BtoI_Draft.md`, `Tasks_v7.md`, `.codex/`, `References.md`. |

---

## Appendix E — Combined Review Log (reviewer)

> Reviewers append a row here after each code review. Format: `YYYY-MM-DD | Task X | <verdict + key findings>`.

| Date | Task | Entry |
|------|------|-------|
