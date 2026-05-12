# Wizard Grower — Tasks v12-1 (Bundle 12.1: Async Server Economy/Gacha Runtime Stabilization)

> Patch bundle for **Tasks_v12.md**. 동일한 작업 규칙·status 정책을 그대로 준수.
> **Do NOT edit prior Tasks*.md from this track.** Read-only access only.
> Planner/Reviewer (Claude) 가 본 문서 본문/Status/Appendix E 의 단독 편집자.
> Implementer 는 본 문서 Task 사양에 따라 코드 수정 후 Appendix D 에 한 줄 추가.

---

## 0. 배경 — 무엇을 고치는가

Tasks_v12.md Appendix D 의 **Planner Handoff — Unity Freeze Risk After Ads/Reward Flow (2026-05-11)** 항목에서 implementer 가 보고한 동기 대기 패턴이 이 패치 bundle 의 단일 핵심 이슈.

**증상:** 사용자 보고 "광고 기능 도입 후 Unity 가 멈추는 것 같다". 광고 자체가 아니라, 광고/오프라인/미션/출석/던전/적처치/가챠 보상 모두 `CurrencyWallet.AddGold/AddGems`/`TrySpend*` 또는 `GachaService.TryPull*` 를 거치고, 그 경로가 Cloud Functions Task 결과를 **Unity main thread 에서 동기 대기**(`.GetAwaiter().GetResult()`)하기 때문에 Firebase dispatcher continuation 과의 deadlock 또는 장시간 frame stall 가능성이 있다.

**확정된 위험 지점 (Tasks_v12.md L431–435 인용):**
- `Assets/Scripts/Economy/CurrencyWallet.cs`
  - `ServerCurrencyAuthority.GrantAsync(...).GetAwaiter().GetResult()` (현 L132)
  - `ServerCurrencyAuthority.SpendAsync(...).GetAwaiter().GetResult()` (현 L159)
- `Assets/Scripts/Weapons/GachaService.cs`
  - `PullFromServerAsync(count).GetAwaiter().GetResult()` (현 L222)

**부수 위험:** Firebase Emulator 미실행 / 네트워크 끊김 / Functions 미배포 상황에서 main thread 블록이 무기한 지속되는 시나리오. 모바일 빌드/Editor 모두 영향.

---

## 1. Task Dependency Graph

```
Bundle 12.1
B12.1-A → B12.1-B → B12.1-C → B12.1-D → Bundle 12.1 Release Gate

B12.1-A: CurrencyWallet/ICurrencyAuthority Async API + Sync API deprecation
B12.1-B: GachaService Async Pull Pipeline + UI handoff
B12.1-C: Reward/Spend Call-Site Migration + UI Pending Guards
B12.1-D: Timeout/Cancellation + Startup Popup Safety + Graceful Offline UX
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 12.1 | B12.1-A | CurrencyWallet Async API | 🟡 IN REVIEW | v12 AP ✅ |
| 12.1 | B12.1-B | GachaService Async Pull Pipeline | 🟡 IN REVIEW | B12.1-A ✅ |
| 12.1 | B12.1-C | Reward/Spend Call-Site Migration + UI Pending Guards | 🟡 IN REVIEW | B12.1-A ✅ + B12.1-B ✅ |
| 12.1 | B12.1-D | Timeout/Cancellation + Startup Popup Safety | 🟡 IN REVIEW | B12.1-C ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 12.1 — Async Server Economy/Gacha Runtime Stabilization

**Goal:** Unity main thread 에서 Cloud Functions 호출을 동기 대기하지 않도록 `CurrencyWallet` / `GachaService` / 모든 보상·구매 호출부를 async 또는 coroutine 기반으로 전환. 서버 미연결/지연 상황에서도 Editor/PlayMode/Live build 가 멈추지 않도록 한다.

### Bundle 12.1 Release Regression Tests
1. `rg -n "\.GetAwaiter\(\)\.GetResult\(\)|\.Wait\(\b" Assets/Scripts` → Firebase / Cloud Functions / Firestore 런타임 경로 **0건** (테스트 코드 / 마이그레이션 부트스트랩 외).
2. Firebase Emulator 미실행 상태에서 PlayMode 진입 → Editor 응답 유지, Console 에 graceful warning 1~2건만, 게임 루프 정상.
3. Emulator 실행 상태에서 다음 모든 path 정상:
   - 가챠 1×/10×/30× → 결과 모달
   - 오프라인 보상 정상 / 광고 2× 보상 정상
   - 미션 클리어 보상 / 출석 보상 / GoldDungeon 보상 / EXPDungeon 보상 / 적 처치 골드
   - 업그레이드 골드 차감
4. 서버 응답 지연(emulator 인위적 5~10초 지연) 동안:
   - 해당 버튼 비활성화 + loading 표시
   - 중복 클릭으로 인한 잔액/인벤토리 중복 변경 0건
   - 응답 도착 시 UI 정상 복구
5. 서버 응답 실패 (Emulator 강제 종료) 시:
   - 사용자 메시지 `"서버 연결이 필요합니다"` / `"서버 연결이 지연되고 있습니다"` 노출
   - 클라이언트 로컬 잔액 임의 변경 없음
   - 버튼 재활성화 후 재시도 가능
6. `GameStartupPopupQueue.RunAsync()` 실행 중 modal 미닫음 상태에서도 Editor input/main loop 동작 (popup 외 영역 입력 차단은 의도된 modal blocker 만).
7. Unity Console 0 error / 0 warning (기존 MCP client-disconnect noise 는 pre-existing 으로 분리 기록).
8. `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors. `npm --prefix functions run lint` + `run build` PASS.
9. (Cross-feature) Bundle 5/6/7/8/9/10/11/13 전 기능 회귀 — 통화 / 가챠 / 오프라인 / 던전 / 인벤토리 / 채팅 / Auth / Save 모두 정상.

---

## Task B12.1-A — CurrencyWallet Async API

**Status:** 🟡 IN REVIEW
**Depends On:** v12 Task AP ✅

### 🎯 Goal
`ICurrencyAuthority` 와 `CurrencyWallet` 의 모든 server-call path 를 async-only API 로 노출하고, Unity main thread 에서 `.GetAwaiter().GetResult()` 동기 대기를 제거한다. 기존 sync API 는 local-cache 전용으로 축소하거나 obsolete 처리.

### ✅ Definition of Done
- [ ] `Assets/Scripts/Economy/ICurrencyAuthority.cs`
  - 기존 `Task<CurrencyAuthorityResult> GrantAsync(...)` / `SpendAsync(...)` 시그니처 유지.
  - 새로 `bool IsServerAuthoritative` 외에 `bool IsBusy` (선택) — 동시 호출 제한용.
- [ ] `Assets/Scripts/Economy/CurrencyWallet.cs`
  - 신규 async API:
    - `Task<bool> TrySpendGoldAsync(int amount, string reason = "spend_gold", CancellationToken ct = default)`
    - `Task<bool> TrySpendGemsAsync(int amount, string reason = "spend_gem", CancellationToken ct = default)`
    - `Task<bool> AddGoldAsync(int amount, string reason = "reward", string source = "gameplay", CancellationToken ct = default)`
    - `Task<bool> AddGemsAsync(int amount, string reason = "reward", string source = "gameplay", CancellationToken ct = default)`
  - **내부 구현**: server-authority 모드일 때 `await authority.SpendAsync(...)` / `await authority.GrantAsync(...)` 직접 await. **`.GetAwaiter().GetResult()` 사용 금지**.
  - local-authority 모드(=CloudFunctions 미연결 또는 명시적 offline 모드) 에서는 즉시 local mutation 후 `Task.FromResult(true)`.
  - 기존 sync `TrySpendGold(int, string)` / `TrySpendGems(int, string)` / `AddGold(int, string, string)` / `AddGems(int, string, string)` 는:
    - server-authority 모드에서는 **즉시 실패 + `Debug.LogWarning("Use async API in server-authority mode")` + 반환 `false`/no-op**. 절대 동기 대기 금지.
    - local-authority 모드에서는 기존처럼 즉시 local mutation.
    - `[Obsolete("Use TrySpendGoldAsync/AddGoldAsync — sync API is no-op in server-authority mode", error: false)]` 어트리뷰트 부착 — 호출부 마이그레이션 완료 후 B12.1-C에서 제거 검토.
- [ ] 동시 호출 보호: 같은 wallet 인스턴스에서 spend/grant 가 동시에 in-flight 일 때 두 번째 호출은 첫 번째 완료 대기 (단순 lock 또는 `SemaphoreSlim(1,1)`). pending 중에는 listener 가 도착해도 race 없도록 `SetGold`/`SetGems` 호출 직전 confirm.
- [ ] `StartServerWalletListener` / `StopServerWalletListener` 는 그대로 유지. 단 listener 콜백이 in-flight grant/spend 의 응답과 충돌하지 않도록, in-flight 동안에는 `pendingMutation = true` 플래그로 listener 결과를 일시 보류 (또는 마지막에 listener 가 최종값으로 덮어쓰는 정책).

### 📂 Files to Modify
- `Assets/Scripts/Economy/CurrencyWallet.cs`
- `Assets/Scripts/Economy/ICurrencyAuthority.cs` (선택: IsBusy 추가)
- `Assets/Scripts/Economy/ServerCurrencyAuthority.cs` — 변경 불필요 (이미 async). 단 main thread context 안전을 위해 `ConfigureAwait(false)` 사용 금지 (Unity API 호출이 continuation 에 있을 수 있으므로 main thread 유지가 안전).

### 📂 Files to Add
- (없음 — 기존 클래스 확장)

### 🚫 Do Not Touch
- `Assets/Scripts/Cloud/CloudFunctionsClient.cs` 내부 (B12.1-D 에서 timeout/cancellation 추가 예정).
- `functions/src/**` (서버 시그니처 변경 없음).
- `Assets/Scripts/Save/CloudSyncService.cs::ReconcileWalletAsync` — 이미 async 이므로 변경 불필요.

### 🧪 Validation
1. Compile clean.
2. `rg -n "\.GetAwaiter\(\)\.GetResult\(\)|\.Wait\(\b" Assets/Scripts/Economy` → 0건.
3. Editor PlayMode + Emulator 정상 → `await wallet.AddGoldAsync(100, "test", "test")` true 반환, gold +100, transactions 컬렉션 doc 추가.
4. Emulator 종료 후 동일 호출 → `false` 반환, gold 변동 없음, `Debug.LogWarning` 1회.
5. sync `TrySpendGold(100)` 호출 (server-authority 모드) → false + warning, gold 변동 없음.
6. 동시 호출 2회 (`AddGoldAsync` × 2) → 두 번째가 첫 번째 완료까지 await, 최종 잔액 +200, transactions doc 2건.
7. Listener 가 in-flight 중 다른 값으로 갱신 → race 없이 최종 일관된 상태.

### Implementation note
**SemaphoreSlim 사용 예시:**
```csharp
private readonly SemaphoreSlim authorityLock = new SemaphoreSlim(1, 1);

public async Task<bool> AddGoldAsync(int amount, string reason, string source, CancellationToken ct = default)
{
    int gained = Mathf.Max(0, amount);
    if (gained <= 0) return true;

    await authorityLock.WaitAsync(ct);
    try
    {
        if (authority == null || !authority.IsServerAuthoritative)
        {
            SetGold(gold + gained);
            GoldGained?.Invoke(gained);
            return true;
        }
        CurrencyAuthorityResult result = await authority.GrantAsync("gold", gained, reason, source);
        if (!result.Success) return false;
        SetGold(result.BalanceAfter >= 0 ? result.BalanceAfter : gold + gained);
        GoldGained?.Invoke(gained);
        return true;
    }
    finally { authorityLock.Release(); }
}
```

**기존 sync API 처리:** Obsolete 어트리뷰트로 컴파일러 경고 → 호출부 전수 마이그레이션 B12.1-C 에서 처리.

---

## Task B12.1-B — GachaService Async Pull Pipeline

**Status:** 🟡 IN REVIEW
**Depends On:** B12.1-A ✅

### 🎯 Goal
`GachaService.TrySinglePull` / `TryTenPull` / `TryThirtyPull` 의 server 호출 경로를 async 로 전환. `PullFromServerAsync(count).GetAwaiter().GetResult()` 제거. 기존 sync API 는 obsolete + local fallback 전용으로 축소.

### ✅ Definition of Done
- [ ] `Assets/Scripts/Weapons/GachaService.cs`
  - 신규 async API:
    - `Task<GachaPullResult> TrySinglePullAsync(CancellationToken ct = default)` — 결과 객체 `{ bool Success, WeaponDefinition Pulled, string FailureMessage }`
    - `Task<GachaPullResult> TryTenPullAsync(...)` — `Pulled` 대신 `IReadOnlyList<WeaponDefinition> PulledList`
    - `Task<GachaPullResult> TryThirtyPullAsync(...)` — 동
  - 또는 단일 `Task<GachaPullResult> TryPullAsync(int count, CancellationToken ct)` + 1/10/30 wrapper.
  - 내부: `cloudFunctions != null && cloudFunctions.IsReady` 이면 `await PullFromServerAsync(count)` 결과 사용. 실패 시 `GachaPullResult.Fail("서버 뽑기에 실패했습니다.")`. **fallback 분기는 Editor + simulation 명시 플래그일 때만 실행** (B12.1-D 가 simulation 토글을 명시화).
  - 기존 sync `TrySinglePull(out)` / `TryTenPull(out)` / `TryThirtyPull(out)` / `PullThirty()` 는 obsolete 처리:
    - server-authority 모드에서는 **즉시 false + warning, out 결과 비움**.
    - Editor + simulation 플래그 켜진 경우만 기존 local roll 경로 실행.
- [ ] `Assets/Scripts/Weapons/GachaPullResult.cs` 신규 — `{ Success, PulledList, FailureMessage }`. `WeaponDefinition` 단일 결과는 list 의 첫 번째.
- [ ] `GachaService.TryPull` 내부 fallback 분기에 빌드 가드 (`#if UNITY_EDITOR` + `useSimulationFallback` 플래그). Release build 에서는 절대 진입 불가.
- [ ] UI 측 변경:
  - `Assets/Scripts/UI/GachaPanel.cs` — 버튼 onClick 핸들러를 `async void GachaPanel.OnClickSingle()` 등으로 변경 + pending state. (UI 호출부는 B12.1-C 와 부분 중복 가능, GachaPanel 만 본 Task 범위.)
  - 버튼 클릭 시 `_pullInFlight = true; button.interactable = false;` → `await service.TrySinglePullAsync(...)` → finally 에서 reset.
  - 결과 모달 호출: 기존 `GachaResultPanel.Show(results)` 그대로.

### 📂 Files to Add
- `Assets/Scripts/Weapons/GachaPullResult.cs`

### 📂 Files to Modify
- `Assets/Scripts/Weapons/GachaService.cs`
- `Assets/Scripts/UI/GachaPanel.cs`

### 🚫 Do Not Touch
- `functions/src/gacha.ts` (서버 시그니처 변경 없음).
- `Assets/Scripts/UI/GachaResultPanel.cs` 내부 (rendering only).
- `Assets/Scripts/Weapons/GachaDefinition.cs` / `WeaponInventory.cs` / `WeaponFusionService.cs`.

### 🧪 Validation
1. Compile clean.
2. `rg "GetAwaiter\(\)\.GetResult\(\)" Assets/Scripts/Weapons` → 0건.
3. Emulator + Auth → 1× 가챠 버튼 클릭 → 버튼 즉시 비활성 → 결과 도착 후 결과 모달 표시 → 버튼 재활성.
4. Emulator 인위 지연 5초 → 버튼 비활성 5초 유지 → 결과 정상.
5. Emulator 응답 실패 → 결과 모달 미표시 + 토스트/라벨 `"서버 뽑기에 실패했습니다."` + 버튼 재활성.
6. Emulator 미실행 + Editor build + simulation 플래그 OFF → 버튼 클릭 시 `"서버 연결이 필요합니다"` 노출, local roll 미실행, gold 변동 없음.
7. Emulator 미실행 + Editor + simulation 플래그 ON → 기존 local roll 동작 (개발 편의).
8. Release build (simulation 플래그 빌드 가드로 제거됨) → 서버 외 경로 불가능.
9. 동시 클릭 (1× 버튼 × 2) → 한 번만 호출됨 + 잔액 변동 1회.

### Implementation note
**GachaPullResult 구조:**
```csharp
public readonly struct GachaPullResult
{
    public readonly bool Success;
    public readonly IReadOnlyList<WeaponDefinition> PulledList;
    public readonly string FailureMessage;

    public WeaponDefinition First => (PulledList != null && PulledList.Count > 0) ? PulledList[0] : null;
    public static GachaPullResult Ok(IReadOnlyList<WeaponDefinition> list) => new(true, list, null);
    public static GachaPullResult Fail(string msg) => new(false, Array.Empty<WeaponDefinition>(), msg);
    // ...
}
```

**Simulation 플래그:** `[SerializeField] private bool useSimulationFallback;` (기본 false). Editor 에서만 true 로 설정 가능, Release build 에서는 `#if UNITY_EDITOR` 로 보호.

---

## Task B12.1-C — Reward/Spend Call-Site Migration + UI Pending Guards

**Status:** 🟡 IN REVIEW
**Depends On:** B12.1-A ✅ + B12.1-B ✅

### 🎯 Goal
모든 wallet 호출부를 B12.1-A 의 async API 로 마이그레이션. 보상 수령 / 구매 / 가챠 클릭 / 적 처치 골드 모든 path 에서 main thread 동기 대기 0건. UI 측 pending guard 추가 (중복 클릭 방지, loading 표시).

### ✅ Definition of Done
- [ ] 다음 파일의 wallet 호출을 async 로 마이그레이션:
  - `Assets/Scripts/Missions/MissionService.cs` (현 L197: `wallet.AddGems`) → `await wallet.AddGemsAsync(amt, $"mission_{id}", "mission")`. 호출자 (mission claim 버튼) 가 async void 또는 Task 반환 핸들러로 처리.
  - `Assets/Scripts/Attendance/AttendanceService.cs` (현 L150) → `await wallet.AddGemsAsync(...)`.
  - `Assets/Scripts/Dungeons/GoldDungeonService.cs` (현 L89) → `await wallet.AddGoldAsync(...)`.
  - `Assets/Scripts/Dungeons/` 의 EXPDungeon 측 보상 호출 (있다면) → async.
  - `Assets/Scripts/Offline/OfflineRewardService.cs` (현 L90) → `await wallet.AddGoldAsync(...)`. EXP 도 함께 처리.
  - `Assets/Scripts/Stages/StageManager.cs` (현 L99: enemy kill gold) → `await wallet.AddGoldAsync(amt, reason, source)`. enemy kill path 가 빈번하므로 fire-and-forget 형태 (`_ = wallet.AddGoldAsync(...)`) + 실패 시 graceful warning. **단 동시 호출 race 는 B12.1-A 의 SemaphoreSlim 이 처리**.
  - `Assets/Scripts/Upgrades/UpgradeSystem.cs` (현 L99) → `await wallet.TrySpendGoldAsync(...)`. 업그레이드 버튼 UI 가 pending 동안 비활성.
  - `Assets/Scripts/Weapons/GachaService.cs` 의 local fallback `TrySpendGems(cost)` 호출 → 그대로 두되 (simulation 분기 안), production 경로는 서버가 차감.
- [ ] UI pending guard:
  - `MissionRowView` / `AchievementPanel` claim 버튼: 클릭 시 `button.interactable = false` → await → finally 재활성.
  - `AttendanceDayCellView` claim 버튼: 동일.
  - `GoldDungeonResultModal` / `EXPDungeonResultModal` claim 버튼: 동일.
  - `OfflineRewardModal` claim/ad 버튼: 동일.
  - `UpgradeButtonView` (업그레이드 버튼): 동일 + 비용 부족 시 별도 처리 유지.
  - `GachaPanel` (B12.1-B 에서 처리됨, 본 Task 에서는 회귀만 확인).
- [ ] enemy kill gold 처리:
  - StageManager 에서 fire-and-forget. UI 영향 없음. 그러나 실패 시 console 에 graceful warning + 로컬 캐시 변경 없음 (서버 응답 기준).
- [ ] 모든 sync `AddGold/AddGems/TrySpendGold/TrySpendGems` 호출 사이트 0건이 되도록 grep 확인. (Obsolete 경고가 컴파일 시 0건이어야 함.)

### 📂 Files to Modify
- `Assets/Scripts/Missions/MissionService.cs`
- `Assets/Scripts/Attendance/AttendanceService.cs`
- `Assets/Scripts/Dungeons/GoldDungeonService.cs`
- (필요 시) `Assets/Scripts/Dungeons/EXPDungeonService.cs`
- `Assets/Scripts/Offline/OfflineRewardService.cs`
- `Assets/Scripts/Stages/StageManager.cs`
- `Assets/Scripts/Upgrades/UpgradeSystem.cs`
- `Assets/Scripts/UI/MissionRowView.cs`
- `Assets/Scripts/UI/AttendanceDayCellView.cs`
- `Assets/Scripts/UI/GoldDungeonResultModal.cs`
- `Assets/Scripts/UI/EXPDungeonResultModal.cs`
- `Assets/Scripts/UI/OfflineRewardModal.cs`
- `Assets/Scripts/UI/UpgradeButtonView.cs` (또는 해당 클래스명)
- `Assets/Scripts/UI/GachaPanel.cs` — B12.1-B 와 중복 시 회귀만

### 📂 Files to Add
- (없음 — 기존 호출부 마이그레이션)

### 🚫 Do Not Touch
- `CurrencyWallet` / `ICurrencyAuthority` / `ServerCurrencyAuthority` 내부 (B12.1-A 가 owner).
- `GachaService` 내부 (B12.1-B 가 owner).
- 보상 액수 / 계산 공식.
- `WeaponInventory` / `ArmorInventory` (인벤토리는 본 패치 범위 밖).

### 🧪 Validation
1. Compile clean. **Obsolete 경고 0건** (모든 호출부 마이그레이션 완료 신호).
2. `rg -n "wallet\.\(AddGold\|AddGems\|TrySpendGold\|TrySpendGems\)\(" Assets/Scripts` → 0건 (전부 async 변형 호출).
3. Emulator + 미션 100마리 처치 → `kill_100_monsters_daily` claim 버튼 클릭 → 버튼 즉시 비활성 → gem +50 → 버튼 사라짐 (or 재활성).
4. Emulator + 출석 Day 1 claim → 동.
5. Emulator + GoldDungeon 결과 모달 claim → 동. 광고 2× 버튼 동일.
6. Emulator + 오프라인 보상 modal claim / 광고 claim → 동.
7. Emulator + 업그레이드 클릭 → pending → 골드 차감 + 능력치 갱신. 잔액 부족 시 토스트.
8. 적 처치 100마리 연속 → 골드 누적 증가 + frame stall 없음 (fire-and-forget).
9. 모든 케이스에서 main thread 응답 유지, double-claim 으로 인한 중복 지급 0건.

### Implementation note
**async void vs Task 반환 UI 핸들러:**
Unity Button 의 `onClick.AddListener` 는 `Action` 형태이므로 `async void` 가 자연스럽다. 단 예외 propagation 이 안되므로 try/catch 명시 + finally 에서 버튼 재활성 보장:
```csharp
private async void OnClaim()
{
    if (busy) return;
    busy = true;
    button.interactable = false;
    try
    {
        bool ok = await wallet.AddGemsAsync(reward, $"mission_{def.missionId}", "mission");
        if (ok) RemoveOrAdvance();
        else ShowFailureToast();
    }
    catch (Exception ex) { Debug.LogException(ex); ShowFailureToast(); }
    finally { busy = false; button.interactable = true; }
}
```

**enemy kill fire-and-forget:**
```csharp
_ = wallet.AddGoldAsync(enemy.RewardGold, reason, source).ContinueWith(t =>
{
    if (t.IsFaulted) Debug.LogWarning($"Enemy gold grant failed: {t.Exception?.GetBaseException().Message}");
}, TaskContinuationOptions.OnlyOnFaulted);
```
Or 단순 `async void` wrapper. Race / 누락은 SemaphoreSlim 이 막아 줌.

---

## Task B12.1-D — Timeout/Cancellation + Startup Popup Safety + Graceful Offline UX

**Status:** 🟡 IN REVIEW
**Depends On:** B12.1-C ✅

### 🎯 Goal
`CloudFunctionsClient` 호출에 timeout/cancellation 도입. 사용자 메시지 표준화. `GameStartupPopupQueue` 가 main loop 를 영구 점유하지 않도록 안전화. Emulator 미실행 / Functions 미배포 상황에서도 게임 진입 가능.

### ✅ Definition of Done
- [ ] `Assets/Scripts/Cloud/CloudFunctionsClient.cs`
  - `CallAsync` 에 `CancellationToken ct = default` + 내부 `Task.WhenAny(call, Task.Delay(timeoutMs, ct))` 패턴으로 timeout(기본 8초, inspector 노출).
  - timeout 시 `TimeoutException` throw 또는 `CloudFunctionsCallResult` 실패 반환.
  - `IsReady` 가 false 인 상태에서 호출 시도 → 즉시 `Fail("Cloud Functions is not initialized")` (대기 없음).
- [ ] 사용자 메시지 헬퍼:
  - `Assets/Scripts/UI/Common/ToastView.cs` 또는 기존 알림 시스템에 다음 표준 문자열 노출:
    - `"서버 연결이 필요합니다."`
    - `"서버 연결이 지연되고 있습니다. 잠시 후 다시 시도해주세요."`
    - `"서버 보상 수령에 실패했습니다. 다시 시도해주세요."`
  - 모든 wallet 실패 / gacha 실패 / dungeon claim 실패 path 가 이 헬퍼 사용.
- [ ] `GameStartupPopupQueue.RunAsync()` 안전화:
  - 각 popup 의 `ShowAsync` 가 사용자 입력을 영구히 막지 않도록 `Task.WhenAny(popup.ShowAsync, Task.Delay(timeoutMs))` 또는 popup 자체에 close timeout 지원.
  - popup 시퀀스 중 한 popup 실패 시 전체 queue 가 멈추지 않도록 `try/catch` 로 격리 + 다음 popup 으로 진행.
- [ ] Editor pre-flight:
  - Emulator 미실행 + Editor 진입 시: `CloudFunctionsClient.Initialize` 가 자동으로 `IsReady = false` 로 머무름 + Console 에 1회 안내 `"Firebase Emulator not detected — currency/gacha calls will be local-only."`.
  - 게임 entry 흐름이 무한 대기하지 않고 정상 진입. 통화/가챠 호출 시도는 graceful fail.
- [ ] (선택) Editor 메뉴 `Wizard Grower → Cloud → Force Offline Mode (Test)` — server-authority 강제 OFF 로 회귀 시나리오 빠른 확인.

### 📂 Files to Modify
- `Assets/Scripts/Cloud/CloudFunctionsClient.cs`
- `Assets/Scripts/Core/GameStartupPopupQueue.cs`
- `Assets/Scripts/UI/OfflineRewardModal.cs` (timeout 처리 회귀)
- `Assets/Scripts/UI/GoldDungeonResultModal.cs` / `EXPDungeonResultModal.cs` (필요 시)
- `Assets/Scripts/UI/Common/<Toast 또는 알림 클래스>` (신설 또는 확장)

### 📂 Files to Add
- (필요 시) `Assets/Scripts/UI/Common/ServerStatusToast.cs` — 서버 상태 메시지 헬퍼

### 🚫 Do Not Touch
- `functions/src/**` (서버 코드 변경 없음).
- `CurrencyWallet` / `GachaService` 내부 (B12.1-A/B 가 owner). 단 호출부에서 timeout 발생 시 처리 추가는 가능.
- `firestore.rules`.

### 🧪 Validation
1. Compile clean.
2. Emulator 미실행 + Editor PlayMode → Login → MainScene 진입 정상 + Console 안내 메시지 1회.
3. Emulator 실행 → 가챠 1× 호출 → timeout 미발생 (정상 응답 < 1초).
4. Emulator 응답 지연 강제 (예: `setTimeout(20s)` 삽입한 wrapper) → 8초 후 timeout → 사용자 메시지 표시 → 잔액 변동 없음 → 버튼 재활성.
5. `GameStartupPopupQueue` 첫 popup 이 의도적 예외 발생 → catch + 다음 popup 진행 → 게임 main loop 정상.
6. Force Offline Mode 메뉴 ON → 가챠 클릭 시 `"서버 연결이 필요합니다."` 노출, 잔액 변동 없음. OFF 시 정상 복귀.
7. 모바일 빌드 실기 (가능 시) → 네트워크 OFF 진입 → 게임 시작 가능, 통화 호출 시 graceful fail.

### Implementation note
**CallAsync timeout 패턴:**
```csharp
public async Task<IDictionary<string, object>> CallAsync(string functionName, object payload, CancellationToken ct = default)
{
    if (!IsReady) throw new InvalidOperationException("Cloud Functions not initialized");
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
    Task<IDictionary<string, object>> call = InvokeAsync(functionName, payload, linked.Token);
    Task delay = Task.Delay(TimeoutMs, linked.Token);
    Task winner = await Task.WhenAny(call, delay);
    if (winner == delay)
    {
        linked.Cancel();
        throw new TimeoutException($"Cloud Function '{functionName}' timed out after {TimeoutMs}ms");
    }
    return await call;
}
```

**GameStartupPopupQueue try/catch:**
각 popup 의 `RunAsync` 를 `try { await popup.ShowAsync(ct); } catch (Exception ex) { Debug.LogException(ex); }` 로 격리. queue 가 어떤 popup 실패에도 다음으로 진행하도록.

---

# Bundle 12.1 Release Gate

When Tasks B12.1-A ~ B12.1-D 모두 `✅ DONE`:

1. `rg -n "\.GetAwaiter\(\)\.GetResult\(\)|\.Wait\(\b" Assets/Scripts` → Firebase/CF 런타임 경로 0건.
2. Emulator 미실행 시 게임 진입/플레이/세이브 정상, 통화/가챠는 graceful fail.
3. Emulator 실행 시 가챠 1/10/30 + 미션/출석/던전/오프라인/적처치/업그레이드 8개 path 정상.
4. 지연/실패 시나리오에서 UI pending guard 동작, 중복 지급 0건.
5. `dotnet build` 0 errors, `npm --prefix functions run lint` + `run build` PASS.
6. (Cross-feature regression) Bundle 5/6/7/8/9/10/11/13 회귀 통과.
7. Unity Console 0 error / 0 warning (pre-existing MCP/Firebase config noise 분리 기록).

---

## Appendix A — Reviewer Checklist (Per Task)

리뷰어는 각 Task 별로 다음을 검증:
1. **DoD 100% 충족** — 체크박스 전부 PASS
2. **Files Modified 가 spec 과 일치** — 불필요한 파일 변경 0건 (`git diff` 확인)
3. **"Do Not Touch" 영역 무변경**
4. **🧪 Validation 단계 직접 실행 결과 PASS**
5. **Spec 의도 일치** — 동기 대기 제거 + UI guard + graceful UX
6. **commit 1개** (`Task B12.1-X done: <요약>`)
7. **No regression** — Bundle 5~13 핵심 기능 정상

리뷰 결과는 Appendix E 에 기록.

---

## Appendix B — Bundle Gate Checklist

Task B12.1-D `✅ DONE` 후:
1. Bundle 12.1 release regression 1~9번 모두 통과
2. `git log` 에 B12.1-A~D 각 1 commit
3. Unity Console 0 errors / 0 warnings
4. Tasks_v12.md 의 Planner Handoff 항목이 본 bundle 로 해결되었음을 Tasks_v12.md Appendix E 에 follow-up 한 줄 추가 (Planner 수행).

---

## Appendix C — Change History

| Date | Author | Change |
|---|---|---|
| 2026-05-11 | Planner | Document created. Tasks_v12.md Appendix D 의 Planner Handoff (Unity Freeze Risk After Ads/Reward Flow) 항목을 단일 핵심 이슈로 정의. 4개 Task (B12.1-A/B/C/D) 로 분해: A=CurrencyWallet async, B=GachaService async, C=호출부 마이그레이션 + UI guard, D=timeout/popup safety/offline UX. Bundle 12 의 AN/AO/AP/AQ 와 무관하게 코드 회귀만 다루는 패치 bundle. |

---

## Appendix D — Combined Work Log (implementer)

> Implementer 는 각 Task 종료 시 한 행 추가. 형식: `YYYY-MM-DD | Task B12.1-X | <one-line summary>`.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-11 | Task B12.1-A | Added async `CurrencyWallet` spend/grant APIs with `SemaphoreSlim` serialization, removed Economy-layer `.GetAwaiter().GetResult()` sync waits, and changed legacy sync wallet APIs to local-only with server-authority warnings/obsolete markers. Validation: `rg` over `Assets/Scripts/Economy` found 0 `.GetAwaiter().GetResult()`/`.Wait(` hits; `npm --prefix functions run lint` PASS; `npm --prefix functions run build` PASS; `dotnet restore Assembly-CSharp.csproj` regenerated missing `Temp/obj`; `dotnet build Assembly-CSharp.csproj --no-restore` PASS with 0 errors. Build currently reports 7 expected `CS0618` wallet call-site warnings that are owned by B12.1-C plus 4 pre-existing Chat/Presence Firebase config warnings. |
| 2026-05-12 | Task B12.1-A | Hardened `CurrencyWallet` local-authority async paths so local spend/grant mutate immediately before server semaphore wait; revalidated `Assets/Scripts` has 0 `.GetAwaiter().GetResult()`/`.Wait(` hits. |
| 2026-05-12 | Task B12.1-B | Confirmed async gacha pull pipeline, passed cancellation token into `CloudFunctionsClient.CallAsync`, removed obsolete sync local wallet spend from `GachaService.TryPull`, and kept UI pending feedback in `GachaPanel`. |
| 2026-05-12 | Task B12.1-C | Migrated mission, attendance, gold dungeon, offline reward, enemy reward, and upgrade UI spend/grant paths to async APIs; added pending guards to mission/attendance rows, dungeon/offline modals, and upgrade buttons. Validation: `rg "wallet\\.(AddGold|AddGems|TrySpendGold|TrySpendGems)\\(" Assets/Scripts` returned 0 call-site hits. |
| 2026-05-12 | Task B12.1-D | Added `CloudFunctionsClient` 8s call timeout and `GameStartupPopupQueue` popup timeout/try-catch isolation. Validation: Unity console compile check showed no script errors; `dotnet build Assembly-CSharp.csproj --no-restore` PASS with 0 errors and 4 pre-existing Chat/Presence config warnings. |

---

## Appendix E — Combined Review Log (reviewer)

| Date | Task | Entry |
|------|------|-------|
