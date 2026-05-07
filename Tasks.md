# Wizard Grower — Task Document (Tasks.md)

> This document is the work specification for the **Implementation Agent (other AI)**.
> The **Planner / Reviewer (Claude)** is the sole editor of this document;
> implementers read this document and modify the code.
> If the document and the actual code disagree, **the document is the source of truth**.

---

## 0. Common Work Rules

### 0.1 Basic Rules
1. **Only one task at a time.** State the task ID (e.g. `Task A`) at start and end.
2. Before starting: read the relevant task section thoroughly. Never modify any **"Do Not Touch"** area.
3. During work: Unity project path is `/Users/kmj/rev_proj01`. Namespace is `WizardGrower.*`.
4. On completion:
   - Verify Unity Console: **0 errors / 0 warnings**
   - Enter PlayMode and pass all **regression tests** (specified at the bottom of each task)
   - Update the task's Status line to `🟡 IN REVIEW`
   - Append a row to **Appendix D — Combined Work Log** with the task ID, date, and what you did. Do NOT add a per-task work log section.
5. After code review, the reviewer:
   - Updates Status to `✅ DONE` or back to `🔴 TODO`
   - Appends a row to **Appendix E — Combined Review Log** with the task ID, date, and findings
6. The build helper menu `Wizard Grower → Build Prototype Scene` **overwrites the scene** — do not run without explicit permission.
7. When adding/removing files, ensure `.meta` files are paired (Unity Editor handles this automatically).

### 0.2 Bundle Gate Rules ⭐
Tasks are grouped into 5 **Bundles**. Until the last task of a bundle reaches ✅ DONE, **no task from the next bundle may start**.

**Gate pass conditions (verified by reviewer):**
1. All tasks in the bundle are ✅ DONE
2. Unity Console has 0 errors / 0 warnings
3. The Bundle's combined regression tests pass (specified at the start of each bundle)
4. The next bundle's first-task "preconditions" match the current code base (reviewer re-verifies)

> **Within a single bundle**, tasks may flow continuously as long as task-level dependencies are respected. The gate only applies between bundles.

### 0.3 Auto-Progression Restrictions (Implementation Agent)
- Do not start any task from a different bundle on your own.
- On signature/spec conflict, **do not decide unilaterally** — record the issue in the Combined Work Log and wait for the reviewer's response.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason instead.
- Do not introduce new systems or new files not listed in this document (exceptions must be logged in the Combined Work Log + await reviewer approval).

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>` (e.g. `Task B done: manual fire mana cost removed, auto-mode guard added`).
- This repo is already initialized via GitHub Desktop with origin set. **Agents must not push** — pushing is the user's responsibility via GitHub Desktop GUI.
- At task start, run `git status` to confirm a clean working tree. Note any uncommitted changes in the Combined Work Log.
- If issues are found in review, `git revert` enables rollback, so maintain the one-task = one-commit principle.

---

## 1. Task Dependency Graph

```
[Bundle 1] A ✅ → B ✅ ──┐
                          ├─→ [Bundle 3] F ✅
[Bundle 2] C ✅ → D ✅ → E ✅ ─┤
                          └─→ [Bundle 4] G0 ✅ → G ✅
                                              │
                  H (after user prework ✅) ───┴─→ [Bundle 5] I
```

**Order:** Bundle 1 ✅ → Bundle 2 ✅ → Bundle 3 ✅ → Bundle 4 ✅ → Bundle 5 (H, I)

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|--------|----|----|--------|------|
| 1 | A | PlayerStats stat field expansion | ✅ DONE | — |
| 1 | B | Combat consistency + fire-rate stat + bug fixes | ✅ DONE | A |
| 2 | C | ChapterDefinition / StageDefinition data model | ✅ DONE | Bundle 1 gate |
| 2 | D | StageManager flow refactor (Field ↔ BossRoom) | ✅ DONE | C |
| 2 | E | Boss-entry button + HUD chapter/stage label | ✅ DONE | D |
| 3 | F | Upgrade drawer UI (bottom toggle, 2-column scroll) | ✅ DONE | Bundle 2 gate ✅ |
| 4 | G0 | Pre-save structural cleanup | ✅ DONE | Bundle 3 gate ✅ |
| 4 | G | SaveData model + local save | ✅ DONE | G0 ✅ |
| 5 | H | Server login + user identity (Anonymous + Google; Apple skipped) | 🔴 TODO | Bundle 4 gate ✅ + user prework ✅ |
| 5 | I | Firestore server-canonical game state | 🔴 TODO | H |

> **Bundle 1, 2, 3 & 4 gates passed (2026-05-07).** Bundle 5 user prework complete (Apple skipped). Task H may begin.

Status legend:
- 🔴 TODO: not started
- 🟢 IN PROGRESS: implementation in progress (set by implementer)
- 🟡 IN REVIEW: implementation complete, awaiting review (set by implementer)
- ✅ DONE: review passed (set only by reviewer)
- ⚠️ BLOCKED: blocked (record reason)

---

# Bundle 1 — Stats + Combat Consistency ✅

**Goal:** Tighten the data model and remove core combat bugs. UI untouched.

### Bundle 1 Combined Regression Tests
1. Auto-attack works correctly (Auto ON, 1-second interval)
2. Toggling Auto OFF stops auto-attack immediately
3. Fire button → fires without consuming mana
4. PlayerStats 9 fields + EnemyBase armor exposed in Inspector
5. armorPen vs armor damage calculation correct (minimum 1 guaranteed)

---

## Task A — PlayerStats Stat Field Expansion

**Status:** ✅ DONE (2026-05-06)

**Outcome:** Replaced `PlayerStats` with 9 independent fields (`autoAttackDamage` / `manualAttackDamage` / `autoAttackInterval` / `manualAttackInterval` / `criticalChance` / `criticalMultiplier` / `armorPenetration` / `maxHealth` / `currentHealth`) plus `combatPower` cache. Added `AddXxx` mutators, `Heal` / `TakeHealth`, and a new `HealthChanged` event alongside the existing `Changed`. Removed legacy `AttackDamage` / `ManualAttackMultiplier` / `AddAttack`. `EnemyBase` gained `armor` field; `TakeDamage` now applies `effectiveArmor = max(0, armor - armorPen)` with a minimum-1 damage guarantee. Migrated `CombatCalculator` (now passes `ArmorPenetration` through `DamageInfo`), `PlayerStatProfile`, `UpgradeSystem`, `EnemyScalingService`. Reviewer-approved single-line exception at `HUDController.cs:112` (`AttackDamage` → `AutoAttackDamage`).

**Key API surface for downstream tasks:**
- `PlayerStats.{AutoAttackDamage, ManualAttackDamage, AutoAttackInterval, ManualAttackInterval, CriticalChance, CriticalMultiplier, ArmorPenetration, MaxHealth, CurrentHealth, CombatPower}`
- `PlayerStats.Add*`, `Heal(float)`, `TakeHealth(float)`; events `Changed`, `HealthChanged`
- `DamageInfo` constructor accepts trailing `armorPenetration` parameter
- `EnemyBase.Initialize(float health, int reward, float armor = 0f)`

---

## Task B — Combat Consistency + Fire-Rate Stat + Bug Fixes

**Status:** ✅ DONE (2026-05-06)

**Outcome:** Two bug fixes + one tuning decision:
1. Added `(!AutoModeEnabled || IsManualMoving)` guard to `AutoAttackController.Update()` and `TryFireNow()` — Auto OFF now correctly stops auto-fire.
2. Removed mana cost from manual fire: deleted `manualManaCost` and `PlayerMana mana` fields from `ClickAttackController`; `Initialize` signature dropped the mana arg; `GameManager` updated.
3. **User-approved tuning:** the `ManualAttackInterval` cooldown was also removed during review — Fire button fires instantly per tap. `lastFireTime` field eliminated. The `ManualAttackInterval` stat itself is retained on `PlayerStats` for forward compatibility (no longer enforced by the controller).

**Side effect (introduced during Task D):** Targeting in `AutoAttackController` / `ClickAttackController` / `ActiveSkillController` later migrated from `enemySpawner.CurrentEnemy` to `enemySpawner.GetNearestEnemy(...)` as part of multi-monster support.

---

# Bundle 2 — Chapter / Stage System ✅

**Goal:** Replace "boss every 5 kills" with "8 stages per chapter + boss-room challenge". Proceed in order: data → flow → UI.

### Bundle 2 Combined Regression Tests
1. Killing field monsters → auto-respawn, stage does not advance
2. Click boss-entry button → boss appears, 20-second timer starts
3. Boss cleared → auto-transition to next stage's field, HUD label updates
4. Boss timeout → return to field, no penalty
5. Stage 8 boss cleared → next chapter's stage 1 (or "All Cleared" if last chapter)
6. HUD label format: `음산한 숲 1-3`

---

## Task C — ChapterDefinition / StageDefinition Data Model

**Status:** ✅ DONE (2026-05-06)

**Outcome:** Replaced legacy `StageDefinition` (Serializable struct) with a ScriptableObject. Selected **Option A** in the spec: legacy struct moved to `LegacyStageBalance.cs`, leaving `StageDefinition.cs` as the new ScriptableObject. Added `ChapterDefinition` and `ChapterDatabase` ScriptableObjects. Created chapter "음산한 숲" with 8 stages (`Assets/Data/Chapters/Chapter01_GloomyForest.asset`, `Stages/Stage01~08.asset`, `ChapterDatabase.asset`). Per-stage balance follows `fieldHP = 50 * 1.25^(n-1)`, `fieldReward = round(10 * 1.18^(n-1))`, `bossHP = fieldHP * 8`, `bossReward = fieldReward * 10`, `bossTimeLimit = 20s`, all armors 0.

**Key API:** `ChapterDefinition.{chapterNumber, displayName, stages}`, `StageDefinition.{stageNumber, fieldMonster*, boss*}`, `ChapterDatabase.GetChapter(int)`.

---

## Task D — StageManager Flow Refactor (Field ↔ BossRoom)

**Status:** ✅ DONE (2026-05-06)

**Outcome:** Field/BossRoom state machine in `StageManager`:
- `EnterBossRoom()` (public) / `ReturnToField()` (private). New `StageMode` enum.
- Field mode: kill → gold + delayed respawn via `RespawnFieldEnemyAfterDelay` coroutine, guarded by `fieldSpawnVersion` so mode changes cancel in-flight respawns.
- Boss mode: 20s timer; on success → `AdvanceToNextStage()` → next stage (or next chapter, or "All Cleared" feedback if next chapter is missing in DB); on failure → `ReturnToField()`.
- `EnemySpawner` extended for multi-enemy: `activeEnemies` list, `SpawnNormal` / `SpawnNormalGroup` / `SpawnNormalReplacement` / `ClearEnemies` / `GetNearestEnemy(Vector3)`. All targeting (Auto/Click/Skill controllers) migrated to `GetNearestEnemy`.
- `EnemyBase.Initialize` takes armor; spawner forwards from `StageDefinition`.
- HUDController's old `StageChanged` subscription/handler removed (re-wired in Task E).
- `GameContext` adds `ChapterDatabase` field; `GameManager` uses new Initialize signature.

**User-requested additions during review (in scope per logs):**
- New `EnemyWanderController` — field monsters roam freely.
- New `EnemyHealthBarView` — per-monster world-space HP bar; auto-attached in `Spawn()`.
- Per-kill replacement spawn (was: respawn-all-on-empty).
- Map expansion to x±12 / y±7 (PlayerMovementController bounds, EnemySpawner spawn/wander bounds).
- `MobileCameraFitter` extended with follow target wired to PlayerWizard.

**Key API:** `StageManager.{Mode, CurrentChapter, CurrentStage, CurrentChapterNumber, CurrentStageNumber, CanEnterBoss, EnterBossRoom(), StateChanged, BossEntryAvailabilityChanged, Feedback, LoadProgress(int, int)}`. `EnemySpawner.GetNearestEnemy(Vector3)` and `ActiveEnemies`.

---

## Task E — Boss-Entry Button + HUD Chapter / Stage Display

**Status:** ✅ DONE (2026-05-06)

**Outcome:** HUDController re-subscribed to `StageManager.StateChanged` / `BossEntryAvailabilityChanged`. New serialized fields `bossEntryButton` + `bossEntryButtonLabel`. Stage label format `{chapter.displayName} {chapter.chapterNumber}-{stage.stageNumber}[ BOSS]`. Boss-entry button is interactable only in Field mode; click → `StageManager.EnterBossRoom()`.

**User-requested polish during review:**
- Korean fonts (`AppleGothic_TMP`, `KoreanFallback_TMP`) added under `Assets/Fonts/` and assigned to HUD TMP texts.
- BossEntryButton repositioned to bottom-right (anchor 1,0 / pos -24,124 / size 180x54) to avoid overlap with `ActiveSkillButton`.
- HUD `HealthBarView` deactivated to avoid conflict with per-monster `EnemyHealthBarView` (later cleaned up entirely in Task G0).

---

# Bundle 3 — Upgrade Drawer UI ✅

**Goal:** Move upgrades into a bottom drawer; provide upgrade entries for every new stat.

### Bundle 3 Combined Regression Tests
1. Toggle button visible at the bottom; panel starts collapsed
2. Click toggle → panel slides up; 2-button-per-row grid; vertical scroll works
3. All upgrade entries clickable, gold deducted, stat changes
4. Click toggle again → panel slides down
5. Upgrade effects reflect immediately in PlayMode combat

---

## Task F — Upgrade Drawer UI (bottom toggle, 2-column scroll)

**Status:** ✅ DONE (2026-05-06)

**Outcome:** `UpgradeType` enum replaced with 9 stat-specific values (`AutoDamage` / `ManualDamage` / `AutoFireRate` / `ManualFireRate` / `CriticalChance` / `CriticalMultiplier` / `ArmorPenetration` / `MaxHealth` / `Mana`). `UpgradeSystem.EnsureDefaults` populates entries with Korean displayNames; `Apply` switch maps each to the right `PlayerStats.AddXxx` / `PlayerMana.IncreaseMax` call. `HasCurrentDefaultSet()` defensive check prevents accidental clearing. New `UpgradeDrawerView` (toggle slide animation; `▼ 강화 닫기` / `▲ 강화 열기` labels) + `UpgradeDrawerGridFitter` (responsive 2-column grid). HUDController switched to dynamic prefab instantiation under a ScrollRect Content container. New prefab `Assets/Prefabs/UI/UpgradeButton.prefab`.

**User-requested visual polish during review (commit 95d022b):**
- Regenerated Wizard / Slime / Boss / TopDownBackground sprites (chibi style).
- Wizard Idle/Run animation assets + `Wizard.controller`.
- New `WizardAnimationController` — sets Animator `Moving` bool from position-delta threshold.
- New `Editor/VisualAssetUpdater` (451 lines) — sprite regeneration utility.
- DamageText runtime font size enlarged.
- All scene/drawer TMP text assigned to `AppleGothic_TMP`.

> Task G0 later removed the `manual_speed` runtime entry, leaving 8 default upgrades. The `UpgradeType.ManualFireRate` enum value, `PlayerStats.AddManualFireRate` method, and `ManualAttackInterval` field are kept for forward compatibility.

---

# Bundle 4 — Local Save ✅

**Goal:** Persist game progress to disk. Cleanup pass first, then save layer.

### Bundle 4 Combined Regression Tests
1. PlayMode → gold 100 + 1 upgrade → exit → re-enter → all restored
2. save.json is human-readable
3. saveVersion field present
4. Delete save.json then re-enter → fresh game starts correctly
5. Chapter / stage progression restored
6. Boss attack reduces the player's HP bar visibly

---

## Task G0 — Pre-Save Structural Cleanup

**Status:** ✅ DONE (2026-05-07)

**Outcome:** Resolved the 6 risks flagged in the 2026-05-07 structural review:
1. **HP unification** — duplicate `currentHealth` / `maxHealth` removed from `PlayerWizard`. `TakeBossHit(int)` now forwards to `stats.TakeHealth(amount)`. Single source of truth = `PlayerStats`.
2. **Dead `manual_speed` upgrade removed** — `EnsureDefaults` now adds 8 entries (auto_dmg / manual_dmg / auto_speed / crit_chance / crit_mult / armor_pen / max_hp / mana). `HasCurrentDefaultSet` updated to the 8-entry order. Forward-compat artifacts (enum value, stat field, mutator method) retained.
3. **`PlayerHealthBarView` added** — subscribes to `PlayerStats.HealthChanged`, renders fill via `RectTransform.anchorMax.x` + optional `HP n / m` label, properly unsubscribes in `OnDestroy`. Wired into HUDController / MainScene / HUD prefab. Boss-attack PlayMode evidence: HP 100 → 76, fill anchor (0.76, 1.00).
4. **Firestore mapper spec** — applied to Task I §I-4 (`SaveDataDocument` POCO + `SaveDataMapper`). No code work in G0.
5. **`userId` first-login conflict spec** — applied to Task I reconciliation rule #5 + §I-8 (`SaveConflictPanel`). No code work in G0.
6. **Vestigial cleanup** — `EnemySpawner.CurrentEnemy` removed; `HUDController.healthBar` field + the dead `SetActive(false)` call removed; `Assets/Scripts/UI/HealthBarView.cs` deleted; `PrototypeBuilder` migrated to `playerHealthBar` wiring.

---

## Task G — SaveData Model + Local Save

**Status:** ✅ DONE (2026-05-07)

**Outcome:** Local save layer under `Assets/Scripts/Save/`:
- **`SaveData.cs`** (`[Serializable]` for JsonUtility) with `PlayerStatsSnapshot` and `UpgradeLevelEntry`. Defaults: `saveVersion=1`, `userId="local"`, `currentChapter=1`, `currentStage=1`. Snapshot has sensible non-zero defaults so a fresh game produces a usable starting state.
- **`SaveService.cs`** — `TryLoad` / `Save` (pretty-printed JSON to `Application.persistentDataPath/save.json`) / `Reset` / `MigrateIfNeeded` (defensive guards). **Bonus method `SetCurrentData(SaveData)` added preemptively** for Task I's `OverwriteFromServer` pattern. `FilePath` exposed as a property.
- **`SaveBinder.cs`** — `ApplyToGame` / `CaptureFromGame` / `RegisterAutoSaveTriggers` / `SaveNow`. Unified 1-second debounce on `GoldChanged` / `UpgradePurchased` / `StateChanged`; `Update()` uses `Time.unscaledTime` so debounce works during pause.
- **Adapter methods (added to existing classes — reviewer-approved as I/O adapters):**
  - `PlayerStats.ApplySnapshot(snapshot)` — defensive clamps (interval ≥ 0.05, critChance 0~1, critMult ≥ 1, armor non-negative, max ≥ 1). Note: `currentHealth ≤ 0 ? maxHealth : ...` resurrects 0-HP saves to full max — acceptable for prototype, revisit if "permadeath" is added.
  - `PlayerStats.CaptureSnapshot()` — pure capture of all 9 fields.
  - `CurrencyWallet.SetGold(int)`.
  - `StageManager.LoadProgress(int chapter, int stage)`.
  - `UpgradeSystem.CaptureLevels()` / `LoadLevels(List<UpgradeLevelEntry>)`.
- **`GameManager.Awake` lifecycle:** `TryLoad → Initialize systems → ApplyToGame → RegisterAutoSaveTriggers`. `OnApplicationPause(true)` and `OnApplicationQuit` both call `SaveBinder.SaveNow`.
- **`GameContext`** adds `SaveService` + `SaveBinder` fields.

---

# Bundle 5 — Server Login + Server-Canonical User Database

**Goal:** Each user logs into the server via Google / Apple / anonymous, gets a unique identity registered, and has their game state persisted in a **server-side DB as the source of truth**. The local `save.json` (Task G) becomes only an offline cache. Two devices sharing the same account share the same progress.

**Architecture (canonical model):**
```
   Game state change
         │
         ▼
   ┌───────────────┐  push (debounced)   ┌──────────────────┐
   │  Local cache  │ ───────────────────▶│  Firestore       │  ← canonical
   │  save.json    │                     │  users/{uid}     │
   │  (offline OK) │ ◀───────────────── │  + profile doc    │
   └───────────────┘    pull / restore   └──────────────────┘
                              │
                              ▼
                    Other device with same UID
                    → restored automatically on login
```

**Storage choice:** Firebase Auth + Cloud Firestore. Firestore is the de-facto standard for indie idle/clicker games — generous free tier, built-in offline persistence, server-side security rules per document. Alternatives (PlayFab, Supabase, custom backend) are out of scope unless the user changes the decision before Task H starts.

### ⚠️ User Prework — Status (2026-05-07)

**Firebase setup (before Task H):**
- [x] Firebase Console → project created
- [x] iOS / Android apps registered → `GoogleService-Info.plist` + `google-services.json` placed in `Assets/`
- [x] Authentication providers enabled — **Anonymous + Google only**
- [ ] ~~Apple Developer + Sign in with Apple~~ — **SKIPPED** by user (no Apple Developer membership). Apple-specific code paths are out of scope for the current iteration; see "Configured Values" below.
- [x] Google Cloud Console → OAuth Web Client ID issued
- [x] Bundle ID / Package Name confirmed

**Firestore setup (before Task I):**
- [x] Firestore database created
- [x] Security Rules applied (recursive subcollection variant — required for `users/{uid}/profile/main`):
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId} {
      allow read, write: if request.auth != null
                         && request.auth.uid == userId;
      match /{document=**} {
        allow read, write: if request.auth != null
                           && request.auth.uid == userId;
      }
    }
  }
}
```
> The recursive `match /{document=**}` block is **necessary** — without it the `users/{uid}/profile/main` subcollection writes would be rejected. The earlier non-recursive snippet in this document was insufficient and has been replaced.

### Configured Values (use these constants in Task H)

| Key | Value |
|-----|-------|
| Bundle ID / Package Name | `com.kmj.rev_proj01` |
| Google OAuth **Web Client ID** | `926991346146-tpigpmfefp9v0judel8gt7p8lkriuhag.apps.googleusercontent.com` |
| Active providers | Anonymous, Google |
| Inactive providers | Apple (skipped) |

> **Do not commit these as plain literals in version-controlled scripts.** Place them in `Assets/Resources/AuthConfig.asset` (a ScriptableObject) or `Assets/StreamingAssets/auth_config.json` so they can be swapped per build. The Web Client ID is not a secret per se (it's exposed to Android clients anyway), but treating it as configuration data keeps environments separable.

> **All prework complete except Apple. Task H may begin.**

### Bundle 5 Combined Regression Tests
1. First launch → anonymous login → UID issued → `users/{uid}` profile doc + state doc auto-created in Firestore
2. Google login → UID linked → existing anonymous progress migrated to the linked account
3. Delete `save.json` → re-login with same account → state restored from Firestore (server is canonical)
4. PlayMode while offline → game runs normally on local cache → online recovery → automatic push to server
5. Two devices progressing simultaneously → the later-saved write wins (newer-wins by `updatedAtUnixMs`)
6. New user nickname registration on first link to Google → reflected in Firestore profile doc

> Apple login validation is **not in scope** for this iteration (provider skipped). Re-add when Apple Developer membership is acquired.

---

## Task H — Server Login + User Identity Registration

**Status:** 🔴 TODO
**Depends On:** Bundle 4 gate + Bundle 5 user prework complete

### 🎯 Goal
- Integrate Firebase Unity SDK (Auth module)
- Auto anonymous login on game start so that every player has a server identity from frame 1
- Allow upgrading the anonymous account to a permanent **Google** account via "linking" (Apple is skipped this iteration)
- On the first server contact, create a **user profile document** at `users/{uid}/profile/main` (separate from the game state document) — this is where unique user info such as nickname, account type, and registration timestamp lives
- On link to Google, prompt the user once for a display nickname (or autofill from the OAuth profile) and write it to the profile document

### Server-side data layout
```
users/{uid}                    ← top-level document (game state, see Task I)
users/{uid}/profile/main       ← subcollection document for user identity (this task)
    displayName: string
    accountType: "anonymous" | "google"      // "apple" reserved for future iteration
    createdAtUnixMs: number
    lastLoginAtUnixMs: number
    locale: string             (optional)
```

> Storing the profile in a subcollection (rather than top-level fields on `users/{uid}`) keeps identity metadata logically separated from rapidly-mutating game state, simplifies Firestore security rules, and lets profile reads/writes happen on different cadences than save sync.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] First launch → anonymous UID issued, `users/{uid}/profile/main` document auto-created with `accountType="anonymous"`
- [ ] Click Google login → account picker → UID linked, profile document updated to `accountType="google"`, `displayName` populated
- [ ] Nickname registration UI appears on first link to Google (or first launch if user opts out of anonymous)
- [ ] `lastLoginAtUnixMs` updated on every successful login
- [ ] Web Client ID and Bundle ID values come from `AuthConfig` (not hardcoded literals)
- [ ] 4 regression tests pass (device or simulator required)

### 📂 Files Changed

#### H-1. Package adoption
- Firebase Unity SDK 12.x or higher (Auth module + Firestore module)
- Google Sign In Unity Plugin (iOS / Android)
- ~~Apple Sign In Unity Plugin~~ — **not needed this iteration** (Apple skipped)

> Exact package names / versions follow the Firebase Console's Unity guide. The Firestore module is needed in Task H because we write the profile document immediately after login.

#### H-2. `Assets/Resources/AuthConfig.asset` (new ScriptableObject)
A `ScriptableObject` carrying the configuration values listed under "Configured Values" in the Bundle 5 header. Defining it as an asset (not a hardcoded literal) keeps the values out of source code and lets the build pipeline swap them per environment later.
```csharp
[CreateAssetMenu(menuName = "Wizard Grower/Auth Config", fileName = "AuthConfig")]
public class AuthConfig : ScriptableObject
{
    public string bundleId;            // "com.kmj.rev_proj01"
    public string googleWebClientId;   // "926991346146-...apps.googleusercontent.com"
}
```
- `AuthService` loads this via `Resources.Load<AuthConfig>("AuthConfig")` during init.
- Asset is committed but should be added to `.gitignore` later if rotated frequently — for now, commit it.

#### H-3. `Assets/Scripts/Auth/AuthService.cs` (new)
```csharp
public class AuthService : MonoBehaviour
{
    public string CurrentUid { get; private set; }
    public AccountType CurrentAccountType { get; private set; }
    public event Action<string, AccountType> UserChanged;

    public Task<string> SignInAnonymouslyAsync();
    public Task<bool>   LinkWithGoogleAsync();
    public Task         SignOutAsync();
}

public enum AccountType
{
    Anonymous,
    Google,
    // Apple,   // Reserved — not implemented this iteration. See Bundle 5 prework status.
}
```
> Do not implement `LinkWithAppleAsync` in this iteration. Leaving the Apple enum value commented out (or omitted) keeps the API surface honest — re-introduce when Apple Developer membership is acquired.

#### H-4. `Assets/Scripts/Auth/UserProfile.cs` (new)
Plain serializable model for the profile document.
```csharp
[FirestoreData]
public class UserProfile
{
    [FirestoreProperty] public string displayName;
    [FirestoreProperty] public string accountType;
    [FirestoreProperty] public long   createdAtUnixMs;
    [FirestoreProperty] public long   lastLoginAtUnixMs;
    [FirestoreProperty] public string locale;
}
```

#### H-5. `Assets/Scripts/Auth/UserProfileService.cs` (new)
**Responsibility:** read / write the `users/{uid}/profile/main` document on Firestore.
```csharp
public class UserProfileService
{
    public Task<UserProfile> GetOrCreateAsync(string uid, AccountType type);
    public Task UpdateDisplayNameAsync(string uid, string displayName);
    public Task UpdateAccountTypeAsync(string uid, AccountType type);
    public Task TouchLastLoginAsync(string uid);
}
```

`GetOrCreateAsync` creates the document if missing (first contact) and updates `lastLoginAtUnixMs` if present.

#### H-6. `Assets/Scripts/UI/LoginPanel.cs` (new)
- Shown on the main screen or via an Account option
- Buttons: **Google login**, **Skip / Continue as guest**. **No Apple button this iteration.**
- Reflects login result and any error from AuthService
- After a successful link to Google, if `displayName` is empty, prompts a nickname-input field once and forwards to `UserProfileService.UpdateDisplayNameAsync`

#### H-7. `Assets/Scripts/UI/NicknameRegistrationPanel.cs` (new)
- Modal panel triggered after the first real-account link
- One TMP_InputField + Submit button
- Trims, validates length 1~20, forbids whitespace-only, then calls `UserProfileService.UpdateDisplayNameAsync`

#### H-8. `Assets/Scripts/Core/GameManager.cs` (modify)
- In `Awake()`: init Firebase → `AuthService.SignInAnonymouslyAsync()` → `UserProfileService.GetOrCreateAsync(uid, Anonymous)` → forward UID to SaveBinder
- Subscribe to `AuthService.UserChanged` to call `UserProfileService.UpdateAccountTypeAsync` when the account type changes

#### H-9. `Assets/Scripts/Core/GameContext.cs` (modify)
- Add fields: `AuthService`, `UserProfileService`, `AuthConfig`

#### H-10. `MainScene` setup
- Add LoginPanel + NicknameRegistrationPanel canvases (initially inactive; activated by the auth flow)
- Assign the `AuthConfig` asset reference to GameContext

### 🚫 Do Not Touch
- Combat, stage logic
- Other HUD widgets (StageLabel, BossEntryButton, UpgradeDrawer, etc.)
- `SaveService` core (Task G is canonical; this task only writes the **profile** doc, not the game state doc)

### 🧪 Validation
1. Compilation clean (after Firebase SDK packages imported and config files placed)
2. First launch → anonymous UID printed to Console; Firebase Console shows a new document at `users/{uid}/profile/main` with `accountType="anonymous"` and `createdAtUnixMs` populated
3. Click Google login → account picker → on success, profile doc updates to `accountType="google"`; nickname prompt appears if displayName was empty; submitted nickname is reflected in Firestore
4. Quit and re-launch → same UID, profile doc's `lastLoginAtUnixMs` increments
5. Confirm `AuthConfig.googleWebClientId` and `AuthConfig.bundleId` are read from the asset, not from any literal in source

> Implementer logs work in Appendix D. Reviewer logs findings in Appendix E.

---

## Task I — Server-Canonical Game State on Firestore

**Status:** 🔴 TODO
**Depends On:** H

### 🎯 Goal
- Persist game state to Firestore at `users/{uid}` as the **canonical source of truth**
- Local `save.json` (Task G) is demoted to an **offline cache**
- On login → pull server doc; if it exists, server wins and overwrites local. If it does not exist, the local cache is pushed up to seed the server (first-time-on-device scenario).
- Every meaningful state change (gold, upgrade purchase, stage advance, app pause/quit) is pushed to Firestore with debounce
- Conflict resolution between two simultaneously-online devices: **newer-wins by `updatedAtUnixMs`**
- Network failure tolerance: local cache continues to serve gameplay; queued writes flush on reconnect

### Server-canonical reconciliation rules
1. **On login (every launch):** pull `users/{uid}` document.
   - **If remote exists and `remote.updatedAtUnixMs > local.updatedAtUnixMs`** → overwrite local cache + emit "RestoredFromServer" feedback
   - **If remote exists and `remote.updatedAtUnixMs <= local.updatedAtUnixMs`** → push local up (this means the device played offline more recently than the server saw)
   - **If remote does not exist** → push local up (first-time bootstrap)
2. **During play:** every state-change trigger pushes to Firestore (debounced 5s). Firestore offline persistence handles brief disconnects transparently.
3. **On app pause / quit:** force-flush pending writes immediately.
4. **On account link (anonymous → Google):** Firebase Auth's `LinkWithCredentialAsync` preserves the UID, so the doc location does not change. No data migration needed.
5. **First-login conflict (`userId="local"` cache vs existing remote doc on a different UID):**
   - Scenario: user played offline first (local cache has `userId="local"` and meaningful progress), then logs into Google for the first time. The new UID may already have a remote doc from another device.
   - **If the new UID has no remote doc** → seed the server with the local cache (rewriting `local.userId = newUid` first), same as bootstrap.
   - **If the new UID has a remote doc** → present a one-time **Conflict Resolution Modal** (`Assets/Scripts/UI/SaveConflictPanel.cs`, defined in this task) showing both sides' summary (gold, current chapter/stage, last play time). User chooses one of: **Use Local**, **Use Remote**, **Cancel Login** (revert to anonymous, keep local). The chosen side becomes the new canonical state; the other is discarded. Never auto-merge — risk of cheating / silent loss.
   - The conflict modal is invoked exactly once per first-link event, never on subsequent logins (subsequent logins fall back to the standard newer-wins rule).

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] Earning gold in PlayMode → `users/{uid}` document on Firestore Console updated within 5 seconds
- [ ] Delete local `save.json` → next launch pulls full state from Firestore
- [ ] Force-quit during a write → next launch retains the last successfully pushed state
- [ ] Wi-Fi OFF → game runs normally; Wi-Fi ON → queued writes flush automatically
- [ ] Two devices logged into the same Google account → newer save wins on next sync
- [ ] 6 regression tests pass

### 📂 Files Changed

#### I-1. Package addition
- Firebase Firestore Unity module (already added in Task H if Firestore was needed for the profile doc; otherwise add here)

#### I-2. `Assets/Scripts/Save/CloudSyncService.cs` (new)
```csharp
public class CloudSyncService
{
    private FirebaseFirestore db;

    public Task              PushAsync(SaveData data);
    public Task<SaveData>    PullAsync(string uid);
    public Task              ResolveAndApply(SaveService localService, string uid);
    public Task              FlushPendingAsync();   // force-flush queued writes
}
```

`ResolveAndApply` implements the reconciliation rules above. Server is the canonical store; local is overwritten when the server is newer.

#### I-3. `Assets/Scripts/Save/SyncCoordinator.cs` (new)
**Triggers:**
- `AuthService.UserChanged` → `ResolveAndApply()` exactly once
- `wallet.GoldChanged` / `upgradeSystem.UpgradePurchased` / `stageManager.StateChanged` → debounce 5s → `PushAsync(local)`
- `OnApplicationPause(true)` → `FlushPendingAsync()`
- `OnApplicationQuit` → `FlushPendingAsync()` (best-effort, may not complete on hard kill)
- Network online recovery → flush queued pushes
- Implementer: use Firebase Firestore's built-in offline persistence (`FirestoreSettings.PersistenceEnabled = true`) so disconnected writes are queued by the SDK itself. The SyncCoordinator only adds debounce + flush on lifecycle events.

#### I-4. `Assets/Scripts/Save/SaveDataDocument.cs` + `Assets/Scripts/Save/SaveDataMapper.cs` (new — mapper pattern)
**Do not** decorate the existing `SaveData` class with `[FirestoreData]`. Mixing JsonUtility's `[Serializable]` (field-based) and Firestore's `[FirestoreData]` (property-based, requires no-arg ctor) on a single class causes silent serialization mismatches.

Create a separate Firestore POCO and a manual mapper instead:

```csharp
[FirestoreData]
public class SaveDataDocument
{
    [FirestoreProperty] public int    saveVersion       { get; set; }
    [FirestoreProperty] public string userId            { get; set; }
    [FirestoreProperty] public long   updatedAtUnixMs   { get; set; }
    [FirestoreProperty] public int    gold              { get; set; }
    [FirestoreProperty] public int    currentChapter    { get; set; }
    [FirestoreProperty] public int    currentStage      { get; set; }
    [FirestoreProperty] public PlayerStatsSnapshotDoc stats     { get; set; }
    [FirestoreProperty] public List<UpgradeLevelEntryDoc> upgrades { get; set; }
}

[FirestoreData] public class PlayerStatsSnapshotDoc { ... matching auto/manual damage etc. ... }
[FirestoreData] public class UpgradeLevelEntryDoc   { [FirestoreProperty] public string id { get; set; } [FirestoreProperty] public int level { get; set; } }

public static class SaveDataMapper
{
    public static SaveDataDocument ToDocument(SaveData data);
    public static SaveData         FromDocument(SaveDataDocument doc);
}
```

`CloudSyncService.PushAsync` / `PullAsync` operate on `SaveDataDocument`; `SaveBinder` / `SaveService` keep using `SaveData`. The mapper is the only seam between them.

#### I-5. `Assets/Scripts/Save/SaveService.cs` (modify)
- After Bundle 5 lands, the local file becomes a **cache**, not a primary store. Add a new method `OverwriteFromServer(SaveData remote)` that the CloudSyncService can call when the server wins reconciliation; it should atomically replace `CurrentData` and persist the new file.

#### I-6. `Assets/Scripts/Core/GameManager.cs` (modify)
- After `AuthService.SignInAnonymouslyAsync()` succeeds, call `SyncCoordinator.Start(uid)` which kicks off `ResolveAndApply` and registers triggers
- On `AuthService.UserChanged` (e.g. Google link), call `SyncCoordinator.OnUidChanged(newUid)` — usually the same UID since linking preserves it, but be defensive

#### I-7. `Assets/Scripts/Core/GameContext.cs` (modify)
- Add fields: `CloudSyncService`, `SyncCoordinator`

#### I-8. `Assets/Scripts/UI/SaveConflictPanel.cs` (new)
Modal panel triggered by `SyncCoordinator` per reconciliation rule #5 (first-login conflict).
```csharp
public class SaveConflictPanel : MonoBehaviour
{
    public Task<ConflictChoice> ShowAsync(SaveData local, SaveDataDocument remote);
}
public enum ConflictChoice { UseLocal, UseRemote, CancelLogin }
```
- Display side-by-side summary: gold, chapter-stage, last-play timestamp.
- The resolved choice is consumed by `SyncCoordinator`:
  - `UseLocal` → mutate `local.userId = newUid` → `PushAsync` (overwrites remote)
  - `UseRemote` → `OverwriteFromServer(SaveDataMapper.FromDocument(remote))`
  - `CancelLogin` → AuthService.SignOutAsync() and stay anonymous; local cache untouched.

### 🚫 Do Not Touch
- Combat, stage, UI logic
- `SaveService` core file IO (only the new `OverwriteFromServer` method is permitted; do not change existing `Save` / `TryLoad` / `Reset` semantics)
- `UserProfileService` (Task H scope) — keep profile docs separate from game state docs

### 🧪 Validation
1. Compilation clean
2. PlayMode anonymous login → earn gold → confirm `users/{uid}` updated in Firestore Console within 5s
3. Delete `save.json` → re-launch → game state restored from Firestore
4. Wi-Fi OFF in PlayMode → upgrade something → Wi-Fi ON → confirm Firestore reflects the change within 10s
5. Two devices logged into the same Google account, both modify state → on next sync, the later `updatedAtUnixMs` wins
6. Force-quit Unity during a write → re-launch → no data loss for the last successfully pushed state

> Implementer logs work in Appendix D. Reviewer logs findings in Appendix E.

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies the following against this document:

1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — verify no unauthorized files were modified (`git diff`)
3. **"Do Not Touch" areas unchanged** — listed files unchanged
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation result matches the document's intent
6. **Exactly one git commit** — commit message includes the task ID

Record review results in Appendix E.

---

## Appendix B — Bundle Gate Checklist

After the bundle's last task reaches ✅ DONE, before starting the next bundle:

1. **Bundle's combined regression tests pass** (specified at the start of each bundle)
2. **`git log` clean** — every task in the bundle has a commit
3. **0 errors / 0 warnings**
4. **Next bundle's first-task preconditions match** — re-verify that code base changes don't violate the document's assumptions

Once cleared, the reviewer reports "Bundle X gate passed" to the user and proceeds to the next bundle.

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-06 | Planner | Initial document, Task A detailed, B~I outlines |
| 2026-05-06 | Planner | Added §A-10 to Task A — single-line exception for `HUDController.cs:112` |
| 2026-05-06 | Planner | Task A marked ✅ DONE (review complete) |
| 2026-05-06 | Planner | Introduced Bundle 1~5 structure, full specs ported for B~I, added bundle-gate / auto-progression / git-commit rules to §0 |
| 2026-05-06 | Planner | Reviewed and marked Task B/C/D/E ✅ DONE. Bundle 1 & 2 gates passed. User-requested additions (cooldown removal, multi-monster field, map expansion + camera follow, Korean fonts, per-monster health bars) recorded in work logs and approved. New components (`EnemyWanderController`, `EnemyHealthBarView`, `MobileCameraFitter` follow) added. |
| 2026-05-06 | Planner | Translated document to English to reduce token usage. Korean retained only for in-game display strings (chapter name "음산한 숲", button label "보스 입장", upgrade `displayName` values, drawer labels "강화 열기/닫기"). Korean backup preserved at `Tasks_kr.md`. |
| 2026-05-06 | Planner | Reviewed and marked Task F ✅ DONE. Bundle 3 gate passed. User-requested visual polish (sprite regeneration, Wizard animation, AppleGothic_TMP font reassignment, larger damage text, responsive grid fitter) all logged and approved. New components: `UpgradeDrawerView`, `UpgradeDrawerGridFitter`, `WizardAnimationController`, `Editor/VisualAssetUpdater`. |
| 2026-05-06 | Planner | Bundle 5 reframed: server DB (Firestore) is now the **canonical store**; local `save.json` is demoted to an offline cache once Bundle 5 ships. Task H expanded to include explicit user identity registration: separate `users/{uid}/profile/main` document with `displayName` / `accountType` / `createdAtUnixMs` / `lastLoginAtUnixMs`, plus a one-time nickname registration UI on first link to Google/Apple. Task I rewritten with server-canonical reconciliation rules (server wins on login if newer; local seeds server on first contact; newer-wins between online devices). User prework checklist (Firebase Console, Apple Developer, Google Cloud Console, Firestore rules) remains the gating prerequisite. |
| 2026-05-07 | Planner | Pre-Task-G structural review. Six risks flagged: (1) duplicate HP fields in `PlayerWizard` vs `PlayerStats`; (2) `manual_speed` upgrade is a sham (no effect after Task B's cooldown removal); (3) no visible player HP bar after Task E's `healthBar` deactivation; (4) Firestore + JsonUtility serialization conflict on a single class; (5) `userId="local"` → real-UID first-login conflict resolution undefined; (6) vestigial refs (`EnemySpawner.CurrentEnemy`, HUDController `healthBar`, PrototypeBuilder L136). Items #1 / #2 / #3 / #6 became **new Task G0** (pre-save cleanup, must be ✅ DONE before Task G). Items #4 / #5 applied directly to Task I spec — `SaveDataDocument` mapper pattern (§I-4) and `SaveConflictPanel` (§I-8) added; reconciliation rules grew rule #5 for first-login conflict. Removed an accidentally-duplicated old Task I body left over from the Bundle 5 reframe. |
| 2026-05-07 | Planner | Cleaned up duplicate Task I body that remained after the Bundle 5 reframe (the old offline-first version was still present after the new server-canonical version). |
| 2026-05-07 | Planner | Reviewed Task G0 → ✅ DONE. All 6 risk items (HP unification, dead manual_speed upgrade, PlayerHealthBarView, Firestore mapper spec, userId conflict spec, vestigial refs) resolved. Boss-attack visible HP bar verified in PlayMode (100 → 76). Task G may begin. |
| 2026-05-07 | Planner | Reviewed Task G → ✅ DONE. Save/load round-trip verified (gold 80, autoAttackDamage 15, stage 1-2, autoLevel 1, saveVersion 1). Fresh-game on file delete confirmed. Bundle 4 gate passed. |
| 2026-05-07 | Planner | Bundle 5 prework status updated: Anonymous + Google enabled; Apple SKIPPED (no Apple Developer membership). Bundle ID `com.kmj.rev_proj01` and Google Web Client ID recorded under "Configured Values". Firestore rules confirmed (recursive `{document=**}` variant covering subcollections). Task H scope reduced: removed Apple package / `LinkWithAppleAsync` / `accountType="apple"` / Apple regression test. Added `AuthConfig` ScriptableObject (§H-2) so Bundle ID and Web Client ID are configuration data rather than literals. `AccountType.Apple` enum value left commented out for future re-introduction. Task H may begin. |
| 2026-05-07 | Planner | Compressed completed task bodies (A / B / C / D / E / F / G0 / G) to outcome summaries to reduce token usage. Per-task work-log and review-note sections consolidated into two new appendices (D and E). Active tasks (H, I), bundle headers, regression tests, appendices, and change history are preserved verbatim. §0 updated to point implementers to Appendix D and reviewers to Appendix E. |

---

## Appendix D — Combined Work Log (implementer)

> Implementers append a row here on each task transition (start / end / review fix). Use the format `YYYY-MM-DD | Task X | <one-line summary>`. Group multi-step entries under the same task ID.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-06 | A | Start: Confirmed §A-10. Plan to modify only line 112 of `HUDController.cs`. |
| 2026-05-06 | A | End: PlayerStats / DamageInfo / CombatCalculator / EnemyBase / PlayerStatProfile / UpgradeSystem / EnemySpawner / HUDController:112 migrated. Script validation 0 errors. Manual damage 20, armor=5 calculation (5/8/10) verified directly. Natural auto-attack +10 gold observation skipped due to Unity MCP `is_changing=true` session issue. |
| 2026-05-06 | B | Start: Bundle 1 Task B begun. Plan: Auto OFF guard + Fire mana removal + manualAttackInterval cooldown. |
| 2026-05-06 | B | End: AutoModeEnabled guard added; `PlayerMana` / `manualManaCost` removed; `ManualAttackInterval` cooldown applied; GameManager init signature migrated. PlayMode verified Auto OFF → fire false + HP unchanged; Auto ON → fire true; Fire button → mana 100→100; rapid taps → first only, then interval-spaced. Console clean. |
| 2026-05-06 | B | Review fix: Per user request, removed the `ManualAttackInterval` cooldown. Rapid 3-tap fires all instantly (projectileDelta=3). |
| 2026-05-06 | C | Start: Selected Option A — split legacy Serializable `StageDefinition` into `LegacyStageBalance`, minimal StageManager field change. |
| 2026-05-06 | C | End: ScriptableObject `StageDefinition`, `ChapterDefinition`, `ChapterDatabase`, plus `LegacyStageBalance` added. `Assets/Data/Chapters/Chapter01_GloomyForest`, `Stage01~08`, `ChapterDatabase` created and DB stages[8] verified. Console clean. |
| 2026-05-06 | D | Start: Bundle 2 Task D begun. Plan: replace StageManager with ChapterDatabase-based Field/BossRoom flow + EnemySpawner armor + ChapterDatabase injection + GameManager init + HUDController subscription removal. |
| 2026-05-06 | D | End: StageMode added; StageManager full flow implemented; EnemySpawner armor; GameContext ChapterDatabase + MainScene assigned; GameManager init signature; HUDController old `StageChanged` subscription/handler removed. PlayMode: initial Field 1-1 NormalEnemy; 5 normal kills keeps stage 1 + gold; EnterBossRoom spawns boss + 20s timer; boss kill → stage 2 Field; boss timeout → stage 2 Field. Console clean. |
| 2026-05-06 | D | Review fix: Restructured single-monster field into multi-monster field. EnemySpawner active list / nearest lookup / 5-monster group spawn; new `EnemyWanderController` for free roam; Auto/Click/Skill targets switched to nearest alive enemy. Boss entry clears all field monsters → 1 BossEnemy. |
| 2026-05-06 | D | Review fix: Per-kill respawn instead of respawn-after-all-die. Added `SpawnNormalReplacement` + `RespawnFieldEnemyAfterDelay` coroutine + `fieldSpawnVersion` guard. 7→6→7 verified. New `EnemyHealthBarView` per-monster. Field count 7, spawn x±5.8 / y±3.25, min spacing 1.15. Boss enter: 0 normals + 1 BossEnemy + 1 boss bar. |
| 2026-05-06 | D | Review fix: Map expansion. PlayerMovementController bounds x±12 / y±7; EnemySpawner field count 10 with matching ranges. `MobileCameraFitter` got followTarget / followOffset / mapCenter / mapSize, wired to PlayerWizard via MainScene. PlayMode: player (11.80, 6.80) → camera (11.80, 6.80, -10), centerDelta (0,0,0). 10 monsters dispersed. Console clean. |
| 2026-05-06 | E | Start: Plan to wire HUDController to `StateChanged` / `BossEntryAvailabilityChanged`, create boss-entry button, remove Task D debug menu. |
| 2026-05-06 | E | End: HUDController fields + handlers added; BossEntryButton created and assigned; StageManager `Debug Enter Boss` ContextMenu removed. AppleGothic-based TMP font asset created in `Assets/Fonts` and assigned to StageLabel + BossEntryButton label to suppress Korean rendering warnings. PlayMode: `음산한 숲 1-1` + button enabled; boss click → `음산한 숲 1-1 BOSS` + button disabled + BossEnemy; boss kill → `음산한 숲 1-2` + button re-enabled. Bundle 2 combined: field stage retention, boss-fail Field return, 1-8 boss → All Cleared. Console clean. |
| 2026-05-06 | E | Review fix: Repositioned BossEntryButton to bottom-right (anchor 1,0 / pos -24,124 / size 180x54) to avoid `ActiveSkillButton` overlap. |
| 2026-05-06 | E | Review fix: HUD legacy `HealthBarView` and per-monster `EnemyHealthBarView` were both visible. Removed `spawner.EnemySpawned += healthBar.Bind` subscription; deactivated HUD HealthBarView in `Initialize`. PlayMode: HUD HealthBarView active=false, 7 EnemyHealthBarView, 7 alive monsters. Console clean. |
| 2026-05-06 | F | Start: Bundle 3 Task F begun after gate-1/2 confirmation. Treated uncommitted `Tasks.md` translation, `Tasks_kr.md`, `.DS_Store` as ambient. Plan: UpgradeType 9-stat migration, EnsureDefaults + Apply switch, UpgradeDrawerView, dynamic HUD button binding, UpgradeButton prefab, MainScene drawer setup. |
| 2026-05-06 | F | End: 9-stat UpgradeType + UpgradeSystem migration. UpgradeDrawerView + UpgradeDrawerGridFitter added. HUDController dynamically instantiates UpgradeButtonView prefab under ScrollRect Content. `Assets/Prefabs/UI/UpgradeButton.prefab` created; MainScene built with bottom-center toggle + UpgradeDrawerPanel/ScrollRect/GridLayoutGroup. Responsive 2-column grid (cell 907x176 in PlayMode). UpgradeButtonView runtime listener fixed so clicks purchase. PlayMode: panel y=-520 closed → toggle → y=84; 9 children, 2 columns, vertical scroll; 9 upgrade clicks deduct gold and update all 9 stat fields; toggle closes back to y=-520. Console clean. |
| 2026-05-06 | F | Review fix: Visual polish — chibi sprite regeneration (Wizard / Slime / Boss / TopDownBackground), Wizard idle/run animation + controller, larger DamageText, all TMP reassigned to `AppleGothic_TMP`. PlayMode: Wizard runs with Animator + driver, field bg = `TopDownBackground`, slime uses new `Slime`, `DamageText.prefab` size 38, scene non-AppleGothic count 0. |
| 2026-05-07 | G0 | Start: Began Task G0 only after confirming G is downstream. Existing working tree had only prior visual/TMP changes + `Tasks_kr.md`; treated as ambient. |
| 2026-05-07 | G0 | End: HP unification — `PlayerWizard.TakeBossHit(int)` forwards to `stats.TakeHealth(amount)`; duplicate fields/properties removed. Dead `manual_speed` runtime entry removed; `HasCurrentDefaultSet` updated to 8 IDs; enum + stat field + mutator retained for forward compat. New `PlayerHealthBarView` wired to HUDController + MainScene + HUD prefab; legacy `HealthBarView` script deleted after verifying 0 refs. `EnemySpawner.CurrentEnemy` removed; PrototypeBuilder switched to `playerHealthBar`. Validation: scripts compiled; 0 refs to removed symbols; PlayMode boss room → HP 100 → 76 with `PlayerHealthBarView` showing `HP 76 / 100`, fill anchor (0.76, 1.00); UpgradeSystem.Upgrades = 8 ids. Console clean. |
| 2026-05-07 | G | Start: Began Task G after G0 ✅. Scope: local save model/service/binder + adapters + GameContext/GameManager wiring + validation. Task H/I cloud work untouched. |
| 2026-05-07 | G | End: `SaveData` / `SaveService` / `SaveBinder` added under `Assets/Scripts/Save`. Wired into GameContext, GameManager, MainScene, PrototypeBuilder. Adapters: `PlayerStats.ApplySnapshot` / `CaptureSnapshot`, `CurrencyWallet.SetGold`, `StageManager.LoadProgress`, `UpgradeSystem.CaptureLevels` / `LoadLevels`. GameManager loads → initializes → applies → registers debounced auto-save; pause/quit flush via `SaveBinder.SaveNow`. Validation: with `save.json` reset, gold 100 → bought auto-damage → stage 1-2 → save; JSON pretty-printed with `saveVersion: 1`, `gold: 80`, `currentChapter: 1`, `currentStage: 2`, `autoAttackDamage: 15`, upgrade `{id:"auto_dmg", level:1}`. Stop/re-enter restored gold=80, autoAttackDamage=15, stage=1-2, autoLevel=1, saveVersion=1. Delete file → fresh game (gold=0, stage=1-1, autoAttackDamage=10, saveVersion=1). Console clean (only MCP reconnect/disposed-client logs). |

---

## Appendix E — Combined Review Log (reviewer)

> Reviewers append a row here after each code review. Use the format `YYYY-MM-DD | Task X | <verdict + key findings>`.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-06 | A | ✅ DONE. PlayerStats 9 fields + all Add/Heal/TakeHealth + HealthChanged event correct. CombatCalculator uses new API + forwards ArmorPenetration. EnemyBase armor + effectiveArmor + min-1 logic correct. HUDController:112 only line changed. 0 leftover legacy symbols (AttackDamage / ManualAttackMultiplier / AddAttack). Live auto-attack to be confirmed in Bundle 1 combined regression. |
| 2026-05-06 | B | ✅ DONE. AutoAttackController guards correct. ClickAttackController mana fields + Initialize arg removed. GameManager call updated. User-approved cooldown removal documented; DoD #5 (interval-spaced rapid fire) retired by user decision. Side migration to `GetNearestEnemy` (from Task D) noted as natural follow-on. |
| 2026-05-06 | C | ✅ DONE. ScriptableObject `StageDefinition` + `[CreateAssetMenu]` correct. `ChapterDefinition` / `ChapterDatabase` + `GetChapter` helper correct. Option A choice (LegacyStageBalance) documented in work log. Assets present and correctly nested. Live balance values to be cross-checked in Task D PlayMode. |
| 2026-05-06 | D | ✅ DONE. StageManager full flow per spec including `RespawnFieldEnemyAfterDelay` race-guarded coroutine and end-of-chapter "All Cleared" handling. EnemySpawner multi-enemy support + spacing algo (24-attempt retry + minSpawnSpacing) reasonable; `CurrentEnemy` retained for compat (later removed in G0). New `EnemyWanderController` / `EnemyHealthBarView` introduced per user-requested review fixes. Map expansion + camera-follow correct. Bundle 2 combined regression passed (logged in Task E entry). |
| 2026-05-06 | E | ✅ DONE. HUDController serialized fields + StateChanged / BossEntryAvailabilityChanged subscriptions correct. Stage label format `{displayName} {chapter}-{stage}[ BOSS]` correct. Boss button interactable toggle correct. Korean fonts assignment + button repositioning + HUD HealthBarView deactivation all logged. Bundle 2 combined: 1-1 ~ 1-8 + All Cleared verified. |
| 2026-05-06 | F | ✅ DONE. UpgradeType 9-value enum replacement + UpgradeSystem.EnsureDefaults + Apply switch all correct. `HasCurrentDefaultSet` defensive check is a sensible bonus. UpgradeDrawerView (with `RemoveListener` defense for hot reload) and responsive `UpgradeDrawerGridFitter` correct. HUDController dynamic instantiation pattern correct; `UpgradeButton.prefab` present. User-requested visual polish (sprite regen, Wizard animation, AppleGothic_TMP TMP reassignment, larger DamageText) all logged + approved. Bundle 3 gate may pass — F is the only task in the bundle. |
| 2026-05-07 | G0 | ✅ DONE. (1) HP unification — TakeBossHit forwards to `stats.TakeHealth`; PlayerWizard duplicate fields/properties gone; GameManager line 37 still compiles via forwarder. (2) `manual_speed` removed; 8 entries; `HasCurrentDefaultSet` updated; forward-compat artifacts retained. (3) PlayerHealthBarView subscribes to HealthChanged, renders via anchorMax fill, properly unsubscribes; HUDController binding correct. (4)/(5) confirmed already applied to Task I body. (6) `EnemySpawner.CurrentEnemy`, `HUDController.healthBar`, `HealthBarView.cs` removed; PrototypeBuilder migrated; 0 game-code refs remain. Bundle 4 regression #6 verified by implementer (HP 100 → 76 visible in bar). git: single commit `5aa9654`. |
| 2026-05-07 | G | ✅ DONE. SaveData three-class layout with sensible defaults. SaveService TryLoad / Save (pretty-printed) / Reset / MigrateIfNeeded defensively guards version, userId, chapter/stage, and null nested objects; `SetCurrentData` / `FilePath` exposed preemptively for Task I. SaveBinder unified 1s debounce on three signals; `Update()` uses `Time.unscaledTime` (works during pause). Adapter methods all present and reasonable; ApplySnapshot defensively clamps every field, with `currentHealth ≤ 0 ? maxHealth` resurrection note. GameManager lifecycle order correct (TryLoad → Initialize → ApplyToGame → RegisterAutoSaveTriggers); pause/quit hooks call SaveNow. GameContext has both fields with `[field: SerializeField]`. DoD 5/5 passed per work-log evidence. git: single commit `f8d4eb5`. Bundle 4 gate passed; Task H may begin pending Bundle 5 prework. |
