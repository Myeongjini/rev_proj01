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
   - **Do not modify any other part of this document.** Only the Status line and the Work Log are editable by the implementer.
5. After code review, the reviewer updates Status to `✅ DONE` or back to `🔴 TODO`.
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
- On signature/spec conflict, **do not decide unilaterally** — record the issue in the work log and wait for the reviewer's response.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason instead.
- Do not introduce new systems or new files not listed in this document (exceptions must be logged in the work log and await reviewer approval).

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>` (e.g. `Task B done: manual fire mana cost removed, auto-mode guard added`)
- This repo is already initialized via GitHub Desktop with origin set. **Agents must not push** — pushing is the user's responsibility via GitHub Desktop GUI.
- At task start, run `git status` to confirm a clean working tree. Note any uncommitted changes in the work log.
- If issues are found in review, `git revert` enables rollback, so maintain the one-task = one-commit principle.

---

## 1. Task Dependency Graph

```
[Bundle 1] A ✅ → B ──┐
                       ├─→ [Bundle 3] F
[Bundle 2] C → D → E ──┤
                       └─→ [Bundle 4] G
                                       │
                  H (after user prework) ─┴─→ [Bundle 5] I
```

**Order:** Bundle 1 → Bundle 2 → Bundle 3 → Bundle 4 → (user prework) → Bundle 5

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|--------|----|----|--------|------|
| 1 | A | PlayerStats stat field expansion | ✅ DONE | — |
| 1 | B | Combat consistency + fire-rate stat + bug fixes | ✅ DONE | A |
| 2 | C | ChapterDefinition / StageDefinition data model | ✅ DONE | Bundle 1 gate |
| 2 | D | StageManager flow refactor (Field ↔ BossRoom) | ✅ DONE | C |
| 2 | E | Boss-entry button + HUD chapter/stage label | ✅ DONE | D |
| 3 | F | Upgrade drawer UI (bottom toggle, 2-column scroll) | 🟡 IN REVIEW | Bundle 2 gate ✅ |
| 4 | G | SaveData model + local save | 🔴 TODO | Bundle 3 gate |
| 5 | H | Firebase Auth (anonymous / Google / Apple) | 🔴 TODO | Bundle 4 gate + user prework |
| 5 | I | Firestore cloud sync | 🔴 TODO | H |

> **Bundle 1 & 2 gates passed (2026-05-06).** Bundle 3 / Task F may begin.

Status legend:
- 🔴 TODO: not started
- 🟢 IN PROGRESS: implementation in progress (set by implementer)
- 🟡 IN REVIEW: implementation complete, awaiting review (set by implementer)
- ✅ DONE: review passed (set only by reviewer)
- ⚠️ BLOCKED: blocked (record reason)

---

# Bundle 1 — Stats + Combat Consistency

**Goal:** Tighten the data model and remove core combat bugs. UI untouched.

### Bundle 1 Combined Regression Tests
1. Auto-attack works correctly (Auto ON, 1-second interval)
2. Toggling Auto OFF stops auto-attack immediately
3. Fire button → fires without consuming mana, with `manualAttackInterval` cooldown
4. PlayerStats 9 fields + EnemyBase armor exposed in Inspector
5. armorPen vs armor damage calculation correct (minimum 1 guaranteed)

---

## Task A — PlayerStats Stat Field Expansion

**Status:** ✅ DONE
**Depends On:** none

### 🎯 Goal
Add 7 new stat fields to `PlayerStats` and split auto / manual attack damage into **independent** fields. Add an `armor` field to `EnemyBase` so `armorPenetration` becomes meaningful.

> This task is strictly **data model + compatibility**. Do not add upgrade buttons, change UI, or implement enemy-to-player damage logic.

### ✅ Definition of Done
- [x] Unity Console: 0 errors, 0 warnings
- [x] PlayMode regression tests (5 below) pass
- [x] All 9 new `PlayerStats` fields exposed in Inspector
- [x] `EnemyBase` Inspector exposes `armor`
- [x] All call sites (`UpgradeSystem`, `AutoAttackController`, `ClickAttackController`, `ActiveSkillController`, `EnemyScalingService`, `CombatCalculator`) use the new API

### 📂 Files Changed

#### A-1. `Assets/Scripts/Player/PlayerStats.cs` (modify)
Fields (full replacement):
```csharp
[SerializeField] private float autoAttackDamage   = 10f;
[SerializeField] private float manualAttackDamage = 20f;
[SerializeField] private float autoAttackInterval = 1f;       // shots/sec = 1/interval
[SerializeField] private float manualAttackInterval = 0.3f;
[SerializeField, Range(0f,1f)] private float criticalChance     = 0.1f;
[SerializeField] private float criticalMultiplier = 2f;
[SerializeField] private float armorPenetration   = 0f;        // flat
[SerializeField] private float maxHealth          = 100f;
[SerializeField] private float currentHealth      = 100f;
[SerializeField] private float combatPower        = 10f;       // display cache
```

Public properties (PascalCase): `AutoAttackDamage`, `ManualAttackDamage`, `AutoAttackInterval`, `ManualAttackInterval`, `CriticalChance`, `CriticalMultiplier`, `ArmorPenetration`, `MaxHealth`, `CurrentHealth`, `CombatPower`.

Add methods:
- `AddAutoDamage(float)` / `AddManualDamage(float)`
- `AddAutoFireRate(float)` / `AddManualFireRate(float)` — reduces interval, clamped to a minimum of 0.05f
- `AddCriticalChance(float)` (clamped 0~1)
- `AddCriticalMultiplier(float)`
- `AddArmorPenetration(float)`
- `AddMaxHealth(float)` — increases currentHealth by the same amount (clamped to max)
- `Heal(float)` (clamp to max), `TakeHealth(float)` (floor at 0)

Events:
- Keep existing `event Action Changed` — fired on any stat change
- New `event Action HealthChanged` — HP-only (for UI separation)

CombatPower recalculation:
```csharp
combatPower = autoAttackDamage * (1f + criticalChance * (criticalMultiplier - 1f));
```

**Compatibility:** Delete the existing `AttackDamage` property, `ManualAttackMultiplier`, and `AddAttack`. Migrate all call sites to the new names.

#### A-2. `Assets/Scripts/Combat/DamageInfo.cs` (modify)
- Add field `readonly float ArmorPenetration`
- Extend constructor signature:
```csharp
public DamageInfo(float amount, bool isCritical, DamageType type, GameObject source, float armorPenetration = 0f)
```

#### A-3. `Assets/Scripts/Combat/CombatCalculator.cs` (modify)
- `Auto(source)` → `stats.AutoAttackDamage`
- `Manual(source)` → `stats.ManualAttackDamage` (**remove** the multiplier multiplication)
- `Skill(source, mult)` → `stats.AutoAttackDamage * mult`
- All `Build()` calls now pass `stats.ArmorPenetration`

#### A-4. `Assets/Scripts/Enemies/EnemyBase.cs` (modify)
- Field: `[SerializeField] private float armor = 0f;` + `public float Armor => armor;`
- Signature: `public virtual void Initialize(float health, int reward, float armor = 0f)`
- Update inside `TakeDamage`:
```csharp
float effectiveArmor = Mathf.Max(0f, armor - info.ArmorPenetration);
float dealt          = Mathf.Max(1f, info.Amount - effectiveArmor);  // minimum 1 guaranteed
currentHealth = Mathf.Max(0f, currentHealth - dealt);
```

#### A-5. `Assets/Scripts/Data/PlayerStatProfile.cs` (modify)
Mirror new fields: `autoAttackDamage`, `manualAttackDamage`, `manualAttackInterval`, `armorPenetration`, `maxHealth`. **Delete** the existing `baseAttack` field and migrate call sites to `autoAttackDamage`.

#### A-6. `Assets/Scripts/Upgrades/UpgradeSystem.cs` (call-site migration)
- `stats.AddAttack(x)` → `stats.AddAutoDamage(x)`
- New stat upgrades will be added in **Task F** — for this task, only the existing 3 upgrade types must compile and work.

#### A-7. `Assets/Scripts/Combat/AutoAttackController.cs` (verify)
Continue using `stats.AutoAttackInterval` (name unchanged).

#### A-8. `Assets/Scripts/Combat/ClickAttackController.cs` (verify)
- Damage must go through `CombatCalculator.Manual()`. Do not reference stats directly.
- If an input cooldown is needed, use `stats.ManualAttackInterval`.

#### A-9. `Assets/Scripts/Enemies/EnemyScalingService.cs` (call-site fix)
On `EnemyBase.Initialize()` calls, pass armor `0f` (scaling is handled in Task D).

#### A-10. `Assets/Scripts/UI/HUDController.cs` (one-line modification — exception granted)
**Approved by reviewer (2026-05-06):** UI widgets are normally off-limits, but this file is the **only UI call site** that references `wizard.Stats.AttackDamage` for display. To keep compilation working when `AttackDamage` is removed, replace exactly the line below.

- Location: `Assets/Scripts/UI/HUDController.cs:112`
- Before: `attackLabel.text = $"ATK {wizard.Stats.AttackDamage:0}  CP {wizard.Stats.CombatPower:0}";`
- After:  `attackLabel.text = $"ATK {wizard.Stats.AutoAttackDamage:0}  CP {wizard.Stats.CombatPower:0}";`

> **Do not modify any other line of this file.**

### 🚫 Do Not Touch
- `StageManager`, `BossStageController`, `EnemySpawner` flow logic
- All lines of `HUDController` **except the one specified in §A-10**
- All other widgets in `Assets/Scripts/UI/*`
- `GameManager`, `GameContext` dependency-injection structure
- Scenes, prefabs
- Enemy → player damage logic
- New upgrade buttons (Task F)

### 🧪 Validation
1. Unity Console clean (0 errors / 0 warnings)
2. Enter PlayMode → wizard auto-attacks slime → gold +10
3. Click Fire button → manual damage equals exactly `manualAttackDamage` (excluding crits)
4. PlayerWizard Inspector exposes all 9 new fields
5. With `EnemyBase.armor=5`:
   - autoAttackDamage=10, armorPen=0 → damage = 5
   - autoAttackDamage=10, armorPen=3 → damage = 8
   - autoAttackDamage=10, armorPen=10 → damage = 10
   - Minimum of 1 in all cases

### 📝 Work Log (implementer)
- 2026-05-06 start: Began Task A. After confirming `Tasks.md` §A-10, decided to modify only the approved line 112 of `HUDController.cs`.
- 2026-05-06 end: Migrated PlayerStats / DamageInfo / CombatCalculator / EnemyBase / PlayerStatProfile / UpgradeSystem / EnemySpawner / HUDController:112. Script validation: 0 errors. Verified manual damage 20, armor=5 calculation (5/8/10) directly. Natural auto-attack +10 gold observation skipped due to a Unity MCP PlayMode session issue (`is_changing=true` stuck).

### 🔍 Review Notes (reviewer)
- 2026-05-06: Code verification complete. PlayerStats has 9 fields + all Add/Heal/TakeHealth methods + HealthChanged event implemented as specified. CombatCalculator uses the new API and forwards ArmorPenetration. EnemyBase armor + effectiveArmor + minimum-1 logic correct. Only line 112 of HUDController changed (other lines unchanged). No leftover legacy symbols (AttackDamage / ManualAttackMultiplier / AddAttack). **Marked ✅ DONE.** Live auto-attack verification will be confirmed alongside the Bundle 1 combined regression.

---

## Task B — Combat Consistency + Fire-Rate Stat + Bug Fixes

**Status:** ✅ DONE
**Depends On:** A ✅

### 🎯 Goal
1. **Bug fix #1:** Remove mana cost from manual attack (Fire button) — manual attacks are free per design.
2. **Bug fix #2:** `AutoAttackController` ignores Auto OFF state and keeps firing — fix.
3. Apply `ManualAttackInterval`-based cooldown (rate limit) to manual attacks.

### ✅ Definition of Done
- [ ] Unity Console: 0 errors, 0 warnings
- [ ] PlayMode regression tests (5 below) pass
- [ ] No auto-fire while Auto OFF
- [ ] Fire button consumes no mana
- [ ] Rapid Fire-button taps are rate-limited by `ManualAttackInterval`

### 📂 Files Changed

#### B-1. `Assets/Scripts/Combat/AutoAttackController.cs`
- Add a guard in `Update()`:
```csharp
if (movement != null && (!movement.AutoModeEnabled || movement.IsManualMoving))
    return;
```
- Apply the same guard in `TryFireNow()`:
```csharp
if (movement != null && (!movement.AutoModeEnabled || movement.IsManualMoving))
    return false;
```

> Note: keep the existing `movement.IsManualMoving` check. Only **add** `AutoModeEnabled`.

#### B-2. `Assets/Scripts/Combat/ClickAttackController.cs`
- Remove fields:
  - `[SerializeField] private float manualManaCost = 5f;`
  - `[SerializeField] private PlayerMana mana;`
- Remove `PlayerMana mana` parameter from `Initialize`.
- Add new field: `private float lastFireTime = -999f;`
- Replace `TryFireManual()`:
```csharp
public bool TryFireManual()
{
    TryRepairCalculator();
    if (wizard == null || projectileFactory == null || calculator == null) return false;

    float interval = wizard.Stats.ManualAttackInterval;
    if (Time.time - lastFireTime < interval) return false;

    IDamageable target = enemySpawner != null ? enemySpawner.CurrentEnemy : null;
    if (target == null || !target.IsAlive) return false;

    lastFireTime = Time.time;
    projectileFactory.FireManual(wizard.CastPoint.position, target, calculator.Manual(wizard.gameObject));
    return true;
}
```

#### B-3. `Assets/Scripts/Core/GameManager.cs`
- Remove the `mana` argument from the `context.ClickAttack.Initialize(...)` call:
  - Before: `context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator);`
  - After:  `context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, calculator);`

### 🚫 Do Not Touch
- `PlayerStats`, `CombatCalculator` internal logic
- UI (`HUDController`, etc.) — no changes
- `StageManager`, `BossStageController`, `EnemySpawner`
- Scenes, prefabs
- `ActiveSkillController` (out of scope)

### 🧪 Validation
1. Unity Console clean
2. Enter PlayMode → Auto Toggle OFF → wait 5s → enemy HP unchanged. No auto-fire even while joystick is held.
3. Auto Toggle ON → auto-attack resumes at 1-second cadence
4. Single Fire button click → mana bar unchanged, enemy HP drops by exactly `manualAttackDamage` (excluding crits)
5. Rapid 5x Fire taps → first shot fires immediately, subsequent shots fire only at `manualAttackInterval` intervals

### 📝 Work Log (implementer)
- 2026-05-06 start: Began Bundle 1 Task B. Plan: add Auto OFF guard, remove mana cost from Fire button, apply manualAttackInterval cooldown.
- 2026-05-06 end: Added `AutoModeEnabled` guard to AutoAttackController; removed `PlayerMana` / `manualManaCost` and applied `ManualAttackInterval` cooldown in ClickAttackController; migrated GameManager init signature. Script validation: 0 errors / 0 warnings. PlayMode direct verification: Auto OFF → fire returns false + HP unchanged; Auto ON → fire returns true; Fire button → mana stays 100→100; rapid taps → first shot fires only, subsequent shots fire after interval. Console shows MCP client handler shutdown logs (Exception type) but no game-code errors/warnings.
- 2026-05-06 review fix: Per user request, removed the `ManualAttackInterval` cooldown from the manual fire button. Verified rapid 3-tap fires all instantly (projectileDelta=3).

### 🔍 Review Notes (reviewer)
- 2026-05-06: Code verification complete.
  - **AutoAttackController**: `Update()` and `TryFireNow()` both apply the `(!AutoModeEnabled || IsManualMoving)` guard. Spec match.
  - **ClickAttackController**: `mana` / `manualManaCost` fields and the mana arg in `Initialize` all removed. `TryRepairCalculator()` internal protection correct.
  - **GameManager**: `mana` argument removed from the `ClickAttack.Initialize` call.
  - **User-requested change (cooldown removed)**: As noted in the work log, `lastFireTime` field and the interval guard were both removed. `TryFireManual` fires immediately on every call. **Reviewer-approved** — DoD validation #5 (cooldown-spaced rapid fire) is retired per user decision.
  - **Side effect**: In Task D, target acquisition was migrated from `enemySpawner.CurrentEnemy` to `enemySpawner.GetNearestEnemy(...)` (part of multi-monster support). Task B's spec used `CurrentEnemy`, but the natural migration to D's multi-monster model is sensible.
  - **Conclusion: ✅ DONE.**

---

# Bundle 2 — Chapter / Stage System

**Goal:** Replace "boss every 5 kills" with "8 stages per chapter + boss-room challenge". Proceed in order: data → flow → UI.

### Bundle 2 Combined Regression Tests
1. Killing field monsters → auto-respawn, stage does not advance
2. Click boss-entry button → boss appears, 20-second timer starts
3. Boss cleared → auto-transition to next stage's field, HUD label updates
4. Boss timeout → return to field, no penalty
5. Stage 8 boss cleared → transition to next chapter's stage 1 (or "All Cleared" if last chapter)
6. HUD label format: `음산한 숲 1-3`

---

## Task C — ChapterDefinition / StageDefinition Data Model

**Status:** ✅ DONE
**Depends On:** Bundle 1 gate

### 🎯 Goal
Define ScriptableObject-based chapter/stage data models and create the asset set for the first chapter "음산한 숲" (Gloomy Forest). Flow changes (D) operate on this data.

> This task is **data definition only**. Do not modify flow code such as `StageManager`.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] New asset creation menus exposed (`Assets > Create > Wizard Grower > Chapter / Stage / Chapter Database`)
- [ ] One "음산한 숲" chapter asset + 8 stage assets + one ChapterDatabase created
- [ ] The previous `StageDefinition` (Serializable struct) replaced by the new ScriptableObject

### 📂 Files Changed

#### C-1. `Assets/Scripts/Stages/StageDefinition.cs` (full replacement)
Rewrite the existing `StageDefinition` (Serializable class) as a ScriptableObject.

> **Compatibility decision:** The existing class is referenced by `StageManager`'s `[SerializeField] private StageDefinition definition` field. Switching to a ScriptableObject can break compilation. Options:
> - Option A (recommended): Move the existing Serializable definition to a new file `Assets/Scripts/Stages/LegacyStageBalance.cs` (rename the class to `LegacyStageBalance`); leave only the ScriptableObject in `StageDefinition.cs`. Temporarily make `StageManager`'s existing field point to `LegacyStageBalance` to keep compilation working (cleaned up in Task D).
> - Option B: Lightly edit the relevant part of `StageManager` here in Task C, but the "no flow code changes" constraint makes Option A safer.
>
> **Document the chosen option in the work log.**

New `StageDefinition.cs`:
```csharp
using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int stageNumber;          // 1~8
        public string displayLabel;      // e.g. "음산한 숲 1-3"

        [Header("Field Monster")]
        public float fieldMonsterHealth = 50f;
        public float fieldMonsterArmor  = 0f;
        public int   fieldMonsterReward = 10;
        public float fieldRespawnDelay  = 0.5f;

        [Header("Boss")]
        public float bossHealth      = 400f;
        public float bossArmor       = 5f;
        public int   bossReward      = 100;
        public float bossTimeLimit   = 20f;

        [Header("Optional Visuals")]
        public Sprite normalEnemyOverride;
        public Sprite bossEnemyOverride;
    }
}
```

#### C-2. `Assets/Scripts/Stages/ChapterDefinition.cs` (new)
```csharp
using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Chapter Definition", fileName = "Chapter")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int chapterNumber;
        public string displayName;       // e.g. "음산한 숲"
        public string themeDescription;

        [Header("Stages (8 recommended)")]
        public StageDefinition[] stages = new StageDefinition[8];

        [Header("Theme Visuals")]
        public Sprite backgroundSprite;
        public Color  ambientTint = Color.white;
    }
}
```

#### C-3. `Assets/Scripts/Stages/ChapterDatabase.cs` (new)
```csharp
using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Chapter Database", fileName = "ChapterDatabase")]
    public class ChapterDatabase : ScriptableObject
    {
        public ChapterDefinition[] chapters;

        public ChapterDefinition GetChapter(int chapterNumber)
        {
            foreach (var c in chapters)
                if (c != null && c.chapterNumber == chapterNumber) return c;
            return null;
        }
    }
}
```

#### C-4. Asset creation (Unity Editor work)
Path: `Assets/Data/Chapters/` (new folder)

**Create:**
1. `Chapter01_GloomyForest.asset` — chapterNumber=1, displayName="음산한 숲"
2. `Stages/Stage01_GloomyForest_1.asset` ~ `Stage08_GloomyForest_8.asset`
3. `ChapterDatabase.asset` — chapters[0] = Chapter01

**Per-stage balance values:**

| stage | fieldHP | fieldReward | bossHP | bossReward |
|-------|---------|-------------|--------|------------|
| 1 | 50 | 10 | 400 | 100 |
| 2 | 63 | 12 | 500 | 120 |
| 3 | 78 | 14 | 624 | 145 |
| 4 | 98 | 17 | 780 | 175 |
| 5 | 122 | 20 | 976 | 210 |
| 6 | 153 | 23 | 1220 | 250 |
| 7 | 191 | 28 | 1525 | 300 |
| 8 | 239 | 33 | 1906 | 360 |

(Formulas: `fieldHP = 50 * 1.25^(n-1)`, `fieldReward = round(10 * 1.18^(n-1))`, `bossHP = fieldHP * 8`, `bossReward = fieldReward * 10`)

bossTimeLimit = 20s for all. fieldMonsterArmor / bossArmor = 0 for all.

### 🚫 Do Not Touch
- Flow logic in `StageManager`, `BossStageController`, `EnemySpawner`, `EnemyScalingService` (handled in Task D)
- Scenes, prefabs

### 🧪 Validation
1. Compilation clean
2. Right-click in Project window → `Create > Wizard Grower > Chapter / Stage / Chapter Database` menus appear
3. `Assets/Data/Chapters/` contains 1 chapter + 8 stages + 1 database asset
4. ChapterDatabase Inspector: expanding chapters[0] shows all 8 stages serialized correctly

### 📝 Work Log (implementer)
- 2026-05-06 start: Began Bundle 2 Task C. Selected Option A: split the existing Serializable StageDefinition into LegacyStageBalance and minimally update StageManager's field to reference LegacyStageBalance for compile compatibility.
- 2026-05-06 end: Replaced StageDefinition with ScriptableObject and added ChapterDefinition / ChapterDatabase / LegacyStageBalance. Created Chapter01_GloomyForest, Stage01~08, and ChapterDatabase under Assets/Data/Chapters/, with DB stages[8] verified serialized. Script validation: 0 errors / 0 warnings. Console shows MCP client handler shutdown logs (Exception type) but no game-code errors/warnings.

### 🔍 Review Notes (reviewer)
- 2026-05-06: Code + asset verification complete.
  - **StageDefinition.cs**: ScriptableObject + `[CreateAssetMenu]` menu correct. All 9 fields (stageNumber/displayLabel/fieldMonster*/boss*/optional Visuals) match spec.
  - **ChapterDefinition.cs / ChapterDatabase.cs**: Newly created, spec-compliant. `GetChapter(chapterNumber)` helper correct.
  - **LegacyStageBalance.cs**: Option A taken — existing Serializable class moved out. Documented in work log.
  - **Assets**: `Assets/Data/Chapters/Chapter01_GloomyForest.asset`, `Stages/Stage01~08`, `ChapterDatabase.asset` all present. Directory structure correct.
  - **Conclusion: ✅ DONE.** (Actual balance-value serialization will be confirmed alongside the Task D PlayMode validation.)

---

## Task D — StageManager Flow Refactor (Field ↔ BossRoom)

**Status:** ✅ DONE
**Depends On:** C

### 🎯 Goal
- **Field mode:** Same-stage normal monsters respawn indefinitely. Killing them grants gold. **No automatic stage progression.**
- **BossRoom mode:** Player enters via boss-entry button (Task E). 1 boss, 20-second limit.
  - Cleared → next stage's field (next chapter once stage 8 clears)
  - Timeout / failure → return to Field mode, no penalty

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] 6 regression tests pass
- [ ] Killing field monsters does not advance the stage
- [ ] Boss clear → auto-transition to next stage's field
- [ ] Boss failure → return to field, boss-entry button is re-enabled
- [ ] ChapterDatabase asset is injected via GameContext

### 📂 Files Changed

#### D-1. `Assets/Scripts/Stages/StageMode.cs` (new)
```csharp
namespace WizardGrower.Stages
{
    public enum StageMode { Field, BossRoom }
}
```

#### D-2. `Assets/Scripts/Stages/StageManager.cs` (full rewrite)
**Responsibilities:**
- Track current chapter / stage / mode
- Mode transitions: `EnterBossRoom()` (public, externally callable), `ReturnToField()` (private)
- Field mode: respawn normal monsters automatically (delay = `fieldRespawnDelay`)
- BossRoom mode: boss kill → advance stage; failure → return to field

**Skeleton:**
```csharp
public class StageManager : MonoBehaviour
{
    [SerializeField] private ChapterDatabase chapterDatabase;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private CurrencyWallet wallet;
    [SerializeField] private BossStageController bossStageController;
    [SerializeField] private PlayerProgression progression;

    private int currentChapter = 1;
    private int currentStageNumber = 1;
    private StageMode mode = StageMode.Field;

    public event Action<ChapterDefinition, StageDefinition, StageMode> StateChanged;
    public event Action<string> Feedback;
    public event Action<bool> BossEntryAvailabilityChanged;

    public ChapterDefinition CurrentChapter { get; private set; }
    public StageDefinition CurrentStage { get; private set; }
    public StageMode Mode => mode;
    public bool CanEnterBoss => mode == StageMode.Field && CurrentStage != null;

    public void Initialize(ChapterDatabase db, EnemySpawner spawner, CurrencyWallet wallet,
                           BossStageController bossStageController, PlayerProgression progression);
    public bool EnterBossRoom();
    private void OnEnemyKilled(EnemyBase enemy);
    private void OnBossFailed();
    private void AdvanceToNextStage();
    private void SpawnFieldEnemy();
    private void SpawnBossEnemy();
    private void RaiseStateChanged();
}
```

**Core behavior:**
- After `Initialize`: currentChapter=1, currentStageNumber=1, mode=Field → SpawnFieldEnemy
- `OnEnemyKilled(field)`: add gold → `Invoke(SpawnFieldEnemy, fieldRespawnDelay)`
- `OnEnemyKilled(boss)`: add gold → `bossStageController.StopTimer()` → `AdvanceToNextStage()`
- `EnterBossRoom()`: mode=BossRoom → clear current enemy → SpawnBossEnemy → `bossStageController.StartTimer(stage.bossTimeLimit)` → raise events
- `AdvanceToNextStage()`:
  - currentStageNumber++
  - If > 8: currentChapter++, currentStageNumber=1
  - If next chapter is missing in DB: `Feedback("All Cleared")` + retain current stage
  - mode=Field, SpawnFieldEnemy
- `OnBossFailed`: mode=Field, SpawnFieldEnemy, `Feedback("Boss Failed")`
- After every state change, raise `StateChanged` and `BossEntryAvailabilityChanged(CanEnterBoss)`

**The previous `StageChanged(int, bool, int, int)` event is retired.** HUDController is temporarily cleaned up in §D-7 and re-wired to the new event in Task E.

#### D-3. `Assets/Scripts/Enemies/EnemySpawner.cs` (modify)
- `SpawnNormal(float health, int reward, float armor)` — add armor parameter
- `SpawnBoss(float health, int reward, float armor)` — add armor parameter
- Pass armor through `Initialize(health, reward, armor)` (EnemyBase already accepts armor)

#### D-4. `Assets/Scripts/Stages/BossStageController.cs`
Verify only. No changes. `StartTimer` / `StopTimer` used as-is.

#### D-5. `Assets/Scripts/Core/GameContext.cs` (modify)
- Add field: `[field: SerializeField] public ChapterDatabase ChapterDatabase { get; private set; }`

#### D-6. `Assets/Scripts/Core/GameManager.cs` (modify)
- Update the `StageManager.Initialize(...)` call signature:
```csharp
context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
```

#### D-7. `Assets/Scripts/UI/HUDController.cs` (minimal modification — exception granted)
- **Remove** the `stageManager.StageChanged += OnStageChanged;` line
- **Remove** the `OnStageChanged(int, bool, int, int)` method
- The `stageLabel.text` update is re-wired in Task E via the new `StateChanged` event
- **At the end of this task `stageLabel` may be temporarily empty** — this is normal, Task E fills it back in.

> ⚠️ HUDController is normally a UI widget and off-limits, but **only the event subscription removal + handler removal** are allowed here to keep compilation working through the signature change. No other changes. Same kind of approval as §A-10.

#### D-8. `MainScene` (Unity Editor work)
- Assign `Assets/Data/Chapters/ChapterDatabase.asset` to the GameContext's ChapterDatabase field.

> ⚠️ Do not run `Wizard Grower → Build Prototype Scene`. Edit the scene directly.

### 🚫 Do Not Touch
- `PlayerStats`, `CombatCalculator`, `Projectile`
- Other UI widgets (HUDController limited to §D-7 scope)
- Adding the boss-entry button (Task E)
- `EnemyScalingService` — may go unused, but do not delete

### 🧪 Validation
1. Compilation clean
2. Enter PlayMode → start at chapter 1, stage 1, Field (HUD stageLabel may be empty here, filled by Task E)
3. Kill 5 normal monsters → stage does not advance, gold gained each time
4. **(Temporary verification method)** Add a `[ContextMenu("Debug Enter Boss")]` method to StageManager or expose `EnterBossRoom()` in the Inspector → boss spawns + 20s timer
5. Kill the boss → currentStageNumber=2, return to Field, new normal monster spawns
6. Re-attempt boss → timeout → return to Field, currentStageNumber=2 retained

### 📝 Work Log (implementer)
- 2026-05-06 start: Began Bundle 2 Task D. Plan: replace StageManager with ChapterDatabase-based Field/BossRoom flow; only update EnemySpawner armor params, ChapterDatabase injection, GameManager init, and remove the HUDController subscription.
- 2026-05-06 end: Added StageMode; implemented StageManager field-respawn / boss-entry / boss-clear / failure-return flow; reflected armor param in EnemySpawner SpawnNormal/SpawnBoss; added ChapterDatabase field to GameContext and assigned in MainScene; updated GameManager init signature; removed the legacy StageChanged subscription/handler in HUDController. PlayMode direct verification: initial Field 1-1 NormalEnemy, killing 5 normal monsters keeps stage 1 + gold gained, EnterBossRoom spawns boss / starts 20s timer, boss kill returns to Field stage 2, boss timeout retains stage 2 / returns to Field. No game-code errors/warnings in Console.
- 2026-05-06 review fix: Restructured the previously single-monster field into a multi-monster field. Added an active enemy list / nearest-enemy lookup / NormalEnemy 5-monster group spawn to EnemySpawner; introduced EnemyWanderController to enable free roaming for field monsters; switched auto-move / auto-attack / manual attack / active skill targeting to nearest alive enemy. On entering BossRoom, all field monsters are cleared and only one BossEnemy remains.
- 2026-05-06 review fix: Fixed the issue where field monsters only respawned after all died. Added `SpawnNormalReplacement` to EnemySpawner; added a `RespawnFieldEnemyAfterDelay` coroutine and `fieldSpawnVersion` guard to StageManager; verified per-kill respawn (7→6→7). Added `EnemyHealthBarView` so every field monster and boss has its own world-space health bar. Field monster count = 7, spawn range x -5.8~5.8 / y -3.25~3.25, minimum spacing 1.15. On entering BossRoom: 0 normal monsters, 1 BossEnemy, 1 boss health bar confirmed. No game-code errors/warnings in Console.
- 2026-05-06 review fix: Addressed the feedback that the field/map was too small. Expanded `PlayerMovementController` movement bounds to x -12~12 / y -7~7; reset `EnemySpawner` field monster count to 10 and spawn/wander ranges to x -12~12 / y -7~7. Added `followTarget` / `followOffset` / `mapCenter` / `mapSize` to `MobileCameraFitter` and wired it to follow PlayerWizard via MainScene; PlayMode verified player=(11.80, 6.80) → camera=(11.80, 6.80, -10.00), centerDelta=(0,0,0). Field monsters (10) spread over the wider range. No game-code errors/warnings in Console.

### 🔍 Review Notes (reviewer)
- 2026-05-06: Code verification complete. Core flow + multiple user-requested additions all properly applied.
  - **StageManager.cs**: Per spec — `Initialize` / `EnterBossRoom` / `OnEnemyKilled` / `OnBossFailed` / `AdvanceToNextStage` / `ReturnToField` / `SpawnFieldEnemies` / `ResolveCurrentStage` / `RaiseStateChanged` all implemented. `RespawnFieldEnemyAfterDelay` coroutine uses `fieldSpawnVersion` guard to eliminate races on mode change. `AdvanceToNextStage`'s end-of-chapter handling (retain last stage + "All Cleared" feedback when next chapter missing) correct.
  - **EnemySpawner.cs**: Multi-enemy support added beyond spec. New `activeEnemies` list, `SpawnNormalGroup` / `SpawnNormalReplacement` / `ClearEnemies` / `GetNearestEnemy`. `CurrentEnemy` retained for backward compatibility (returns most recent alive enemy). `Spawn()` auto-attaches EnemyHealthBarView / EnemyWanderController. Spacing algorithm (24-attempt retry + minSpawnSpacing) reasonable.
  - **GameContext / GameManager**: ChapterDatabase field + Initialize signature migration correct.
  - **HUDController D-7**: Old `StageChanged` subscription/handler removed (re-subscribed to new event in Task E).
  - **🆕 User-requested additions (beyond DoD, all logged):**
    - New `EnemyWanderController.cs` — field monster free roaming
    - New `EnemyHealthBarView.cs` — per-monster world-space health bars
    - Field monsters respawn one-at-a-time (was: respawn after all dead → now 1 kill = 1 respawn)
    - Map expansion: movement bounds + spawn bounds + camera follow (PlayerMovementController, MobileCameraFitter, EnemySpawner)
    - Auto/manual/skill targeting all use nearest alive enemy
  - **Side note**: `EnemySpawner.CurrentEnemy` may have no users after D (Auto/Click/Skill all use `GetNearestEnemy`). Cleanup candidate later, but harmless as a compatibility safety net for now.
  - **Conclusion: ✅ DONE.** Bundle 2 combined regression pass is also recorded in Task E's work log.

---

## Task E — Boss-Entry Button + HUD Chapter / Stage Display

**Status:** ✅ DONE
**Depends On:** D

### 🎯 Goal
- HUD label shows the format `음산한 숲 1-3`
- Add a "보스 입장" (Enter Boss) button at the top / top-right of the screen — only enabled in Field mode
- Button click → calls `StageManager.EnterBossRoom()`

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] HUD label format: `{chapterDisplayName} {chapterNumber}-{stageNumber}` (Field), with ` BOSS` suffix in BossRoom
- [ ] Field mode: boss-entry button enabled, clickable
- [ ] BossRoom mode: button disabled (interactable=false)
- [ ] 4 regression tests pass

### 📂 Files Changed

#### E-1. `Assets/Scripts/UI/HUDController.cs` (modify)
- New serialized fields:
```csharp
[SerializeField] private Button bossEntryButton;
[SerializeField] private TMP_Text bossEntryButtonLabel;
```
- Add subscriptions inside `Initialize`:
```csharp
stageManager.StateChanged += OnStateChanged;
stageManager.BossEntryAvailabilityChanged += OnBossEntryAvailabilityChanged;
bossEntryButton.onClick.AddListener(() => stageManager.EnterBossRoom());
```
- New handlers:
```csharp
private void OnStateChanged(ChapterDefinition chapter, StageDefinition stage, StageMode mode)
{
    string suffix = mode == StageMode.BossRoom ? " BOSS" : "";
    stageLabel.text = $"{chapter.displayName} {chapter.chapterNumber}-{stage.stageNumber}{suffix}";
}
private void OnBossEntryAvailabilityChanged(bool available)
{
    if (bossEntryButton != null) bossEntryButton.interactable = available;
}
```
- The `OnStageChanged` handler temporarily removed in Task D stays permanently removed.

#### E-2. `MainScene` HUD setup (Unity Editor work)
- Add a new Button under the HUD Canvas:
  - Name: `BossEntryButton`
  - Position: top-center or top-right of the screen (near stageLabel)
  - Size: 200x60 recommended
  - Child TMP_Text: "보스 입장"
- Assign `bossEntryButton`, `bossEntryButtonLabel` in HUDController's Inspector.
- Remove any debug entry-call code/menu added during Task D.

> ⚠️ Do not run `Build Prototype Scene`. Edit the scene directly.

### 🚫 Do Not Touch
- Position/events of other HUD widgets
- StageManager internals (Task D)
- Upgrade UI (Task F)

### 🧪 Validation
1. Compilation clean
2. Enter PlayMode → label shows `음산한 숲 1-1`, boss-entry button enabled
3. Click boss entry → boss appears, label shows `음산한 숲 1-1 BOSS`, button disabled
4. Kill boss → label updates to `음산한 숲 1-2`, button re-enabled

### 📝 Work Log (implementer)
- 2026-05-06 start: Began Bundle 2 Task E. Plan: wire HUDController to StageManager.StateChanged / BossEntryAvailabilityChanged, create/assign the boss-entry button, remove the Task D debug enter-boss menu.
- 2026-05-06 end: Added `bossEntryButton` / `bossEntryButtonLabel` fields and the new StateChanged / BossEntryAvailabilityChanged handlers to HUDController; created `BossEntryButton` in the scene and assigned its fields; removed StageManager's `Debug Enter Boss` ContextMenu. To suppress Korean HUD-rendering warnings, created a TMP font asset based on macOS's AppleGothic in `Assets/Fonts` and assigned it to StageLabel and BossEntryButton's label. PlayMode verified: `음산한 숲 1-1` label + boss-entry button enabled; clicking → `음산한 숲 1-1 BOSS` + button disabled + BossEnemy; killing boss → `음산한 숲 1-2` + button re-enabled. Bundle 2 combined flow: field kills retain stage; boss failure retains stage and returns to Field; clearing 1-8 boss reaches "All Cleared" path on the last chapter. No game-code errors/warnings in Console.
- 2026-05-06 review fix: Repositioned `BossEntryButton` to bottom-right (anchor 1,0 / pos -24,124 / size 180x54) to avoid overlapping with `ActiveSkillButton`. Button label remains "보스 입장"; reconfirmed AppleGothic_TMP font assignment.
- 2026-05-06 review fix: Fixed the issue where the new `EnemyHealthBarView` and the existing single HUD `HealthBarView` were both visible. Removed the `spawner.EnemySpawned += healthBar.Bind` subscription in HUDController and disabled the HUD `HealthBarView` during `Initialize`. PlayMode confirmed HUD HealthBarView active=false, 7 EnemyHealthBarView instances, 7 alive monsters. No game-code errors/warnings in Console.

### 🔍 Review Notes (reviewer)
- 2026-05-06: Code verification complete.
  - **HUDController.cs**: `bossEntryButton` / `bossEntryButtonLabel` fields added; `Initialize` subscribes to `StateChanged` / `BossEntryAvailabilityChanged` and wires the button's onClick to `EnterBossRoom()`. The `OnStateChanged` format `"{displayName} {chapter}-{stage}[ BOSS]"` is correct. `OnBossEntryAvailabilityChanged` toggles `interactable` and keeps the label "보스 입장".
  - **HUD HealthBarView disabled**: `Initialize` calls `healthBar.gameObject.SetActive(false)` — prevents conflict between the legacy single HP bar and the new EnemyHealthBarView. Reasonable.
  - **User-requested additions**: Korean fonts (AppleGothic_TMP / KoreanFallback_TMP) added under `Assets/Fonts/`; BossEntryButton repositioned to bottom-right (anchor 1,0 / pos -24,124 / size 180x54).
  - **Conclusion: ✅ DONE.** Bundle 2 combined regression pass — 1-1 ~ 1-8 progression + "All Cleared" path verified.

---

# Bundle 3 — Upgrade Drawer UI

**Goal:** Move upgrades into a bottom drawer. Provide upgrade entries for every new stat.

### Bundle 3 Combined Regression Tests
1. Toggle button visible at the bottom; panel starts collapsed
2. Click toggle → panel slides up; 2-button-per-row grid; vertical scroll works
3. All 9 upgrades clickable, gold deducted, stat changes
4. Click toggle again → panel slides down
5. Upgrade effects reflect immediately in PlayMode combat

---

## Task F — Upgrade Drawer UI (bottom toggle, 2-column scroll)

**Status:** 🟡 IN REVIEW
**Depends On:** Bundle 2 gate

### 🎯 Goal
1. Bottom toggle button → slides the upgrade panel up/down
2. Inside the panel: ScrollRect + GridLayoutGroup (columns=2)
3. Add `UpgradeDefinition` entries corresponding to every new stat

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] Toggle button always visible at bottom-center
- [ ] Expanded panel: 2 buttons per row, vertical scroll
- [ ] 9 upgrade entries (autoDamage, manualDamage, autoFireRate, manualFireRate, critChance, critMultiplier, armorPen, maxHealth, mana)
- [ ] 5 regression tests pass

### 📂 Files Changed

#### F-1. `Assets/Scripts/Upgrades/UpgradeDefinition.cs` (modify)
**Replace `UpgradeType` enum entirely:**
```csharp
public enum UpgradeType
{
    AutoDamage,
    ManualDamage,
    AutoFireRate,
    ManualFireRate,
    CriticalChance,
    CriticalMultiplier,
    ArmorPenetration,
    MaxHealth,
    Mana,
}
```

> **Delete** the legacy `Attack` and `Critical` enum values. Migrate UpgradeSystem switch-case call sites to the new names.

#### F-2. `Assets/Scripts/Upgrades/UpgradeSystem.cs` (modify)
- Expand `EnsureDefaults()` to 9 entries:
```csharp
upgrades.Add(new UpgradeDefinition { id="auto_dmg",     displayName="자동공격력",   type=UpgradeType.AutoDamage,         baseCost=20, value=5f });
upgrades.Add(new UpgradeDefinition { id="manual_dmg",   displayName="수동공격력",   type=UpgradeType.ManualDamage,       baseCost=30, value=8f });
upgrades.Add(new UpgradeDefinition { id="auto_speed",   displayName="자동발사속도", type=UpgradeType.AutoFireRate,       baseCost=40, value=0.05f });
upgrades.Add(new UpgradeDefinition { id="manual_speed", displayName="수동발사속도", type=UpgradeType.ManualFireRate,     baseCost=40, value=0.02f });
upgrades.Add(new UpgradeDefinition { id="crit_chance",  displayName="크리확률",     type=UpgradeType.CriticalChance,     baseCost=35, value=0.03f });
upgrades.Add(new UpgradeDefinition { id="crit_mult",    displayName="크리데미지",   type=UpgradeType.CriticalMultiplier, baseCost=50, value=0.1f });
upgrades.Add(new UpgradeDefinition { id="armor_pen",    displayName="방어관통",     type=UpgradeType.ArmorPenetration,   baseCost=45, value=1f });
upgrades.Add(new UpgradeDefinition { id="max_hp",       displayName="최대체력",     type=UpgradeType.MaxHealth,          baseCost=25, value=20f });
upgrades.Add(new UpgradeDefinition { id="mana",         displayName="마나",         type=UpgradeType.Mana,               baseCost=25, value=15f });
```
- Add new cases to the `Apply(definition)` switch:
  - `AutoDamage` → `wizard.Stats.AddAutoDamage(value)`
  - `ManualDamage` → `wizard.Stats.AddManualDamage(value)`
  - `AutoFireRate` → `wizard.Stats.AddAutoFireRate(value)`
  - `ManualFireRate` → `wizard.Stats.AddManualFireRate(value)`
  - `CriticalChance` → `wizard.Stats.AddCriticalChance(value)`
  - `CriticalMultiplier` → `wizard.Stats.AddCriticalMultiplier(value)`
  - `ArmorPenetration` → `wizard.Stats.AddArmorPenetration(value)`
  - `MaxHealth` → `wizard.Stats.AddMaxHealth(value)`
  - `Mana` → `mana.IncreaseMax(value); mana.IncreaseRegeneration(1f);` (preserve existing behavior)

#### F-3. `Assets/Scripts/UI/UpgradeDrawerView.cs` (new)
```csharp
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WizardGrower.UI
{
    public class UpgradeDrawerView : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private RectTransform panel;
        [SerializeField] private float openY  = 0f;
        [SerializeField] private float closeY = -800f;
        [SerializeField] private float animDuration = 0.25f;

        private bool isOpen;

        private void Awake()
        {
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
            ApplyImmediate(false);
        }

        public void Toggle()
        {
            isOpen = !isOpen;
            if (toggleLabel != null) toggleLabel.text = isOpen ? "▼ 강화 닫기" : "▲ 강화 열기";
            StopAllCoroutines();
            StartCoroutine(Animate(isOpen ? openY : closeY));
        }

        private IEnumerator Animate(float targetY)
        {
            float elapsed = 0f;
            Vector2 start = panel.anchoredPosition;
            Vector2 end   = new Vector2(start.x, targetY);
            while (elapsed < animDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panel.anchoredPosition = Vector2.Lerp(start, end, elapsed / animDuration);
                yield return null;
            }
            panel.anchoredPosition = end;
        }

        private void ApplyImmediate(bool open)
        {
            isOpen = open;
            if (panel != null)
                panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, open ? openY : closeY);
            if (toggleLabel != null)
                toggleLabel.text = open ? "▼ 강화 닫기" : "▲ 강화 열기";
        }
    }
}
```

#### F-4. `Assets/Scripts/UI/HUDController.cs` (modify)
- Remove the existing `[SerializeField] private UpgradeButtonView[] upgradeButtons;`
- Remove the existing `[SerializeField] private Sprite[] upgradeIcons;` (or repurpose for dynamic creation)
- Add new serialized fields:
```csharp
[SerializeField] private UpgradeDrawerView upgradeDrawer;
[SerializeField] private Transform upgradeButtonContainer;     // ScrollRect Content
[SerializeField] private UpgradeButtonView upgradeButtonPrefab;
[SerializeField] private Sprite[] upgradeIcons;                // 9 entries, order matches upgrades
```
- Rewrite `BindUpgradeButtons`: iterate `system.Upgrades` → Instantiate(prefab, container) → Bind
- `RefreshUpgradeButtons`: call Refresh() on all views in the container

#### F-5. `MainScene` setup (Unity Editor work)
Inside the HUD Canvas:
1. **Bottom toggle button**: `UpgradeToggleButton` — bottom-center
2. **Drawer panel**: `UpgradeDrawerPanel` (RectTransform; slide via anchoredPosition Y)
   - Child ScrollRect (Vertical only)
   - ScrollRect.Content: GridLayoutGroup
     - Constraint = FixedColumnCount, Count = 2
     - Cell Size 320x140 (tunable)
3. Attach UpgradeDrawerView to the panel; assign Inspector fields
4. Create new prefab `Assets/Prefabs/UI/UpgradeButton.prefab`:
   - UpgradeButtonView + Button + TMP_Text + Image
   - Assign to HUDController's `upgradeButtonPrefab` field

> ⚠️ Do not run `Build Prototype Scene`.

### 🚫 Do Not Touch
- `PlayerStats`, `CombatCalculator`
- `StageManager`, boss flow
- Position of other HUD widgets

### 🧪 Validation
1. Compilation clean
2. PlayMode → toggle button visible at the bottom, panel off-screen
3. Click toggle → slides up, 2-column grid, vertical scroll works
4. Click each of the 9 upgrades → gold deducted + stat increased (verify via PlayerStats Inspector)
5. Click toggle again → slides down

### 📝 Work Log (implementer)
- 2026-05-06 start: Bundle 3 Task F started. `Tasks.md` confirms Bundle 1 & 2 gates passed and Task F may begin. Existing uncommitted `Tasks.md` translation changes, `Tasks_kr.md`, and `.DS_Store` files were present at start; treating them as user/ambient changes and avoiding unrelated edits. Implementing UpgradeType 9-stat migration, UpgradeSystem default entries/switch cases, UpgradeDrawerView, dynamic HUD upgrade button binding, UpgradeButton prefab, and MainScene drawer setup.
- 2026-05-06 end: Replaced UpgradeType with 9 stat-specific entries and migrated UpgradeSystem default definitions/apply switch. Added UpgradeDrawerView and UpgradeDrawerGridFitter. HUDController now instantiates UpgradeButtonView prefab entries dynamically under ScrollRect Content. Created `Assets/Prefabs/UI/UpgradeButton.prefab` and configured MainScene with bottom-center UpgradeToggleButton plus UpgradeDrawerPanel/ScrollRect/GridLayoutGroup. Review feedback during implementation: button cells now fill the drawer width as a responsive 2-column grid and use larger cells (PlayMode measured cell size 907x176), making vertical scrolling meaningful. Fixed UpgradeButtonView runtime listener binding so button clicks purchase upgrades. PlayMode verified: panel starts closed at y=-520, toggle opens to y=84, 9 children, 2 columns, vertical scroll enabled, all 9 upgrade clicks spend gold and update autoDamage/manualDamage/autoFireRate/manualFireRate/critChance/critMultiplier/armorPen/maxHealth/mana immediately, toggle closes back to y=-520. Console has no game-code errors/warnings.
- 2026-05-06 review fix: User requested MapleStory/Latale-inspired 2D chibi visual polish for Wizard/Slime/Boss/Background, Wizard animation, natural map/background scale after the camera-follow change, larger damage text readability, and repair of broken TMP rendering in the upgrade drawer. Added `VisualAssetUpdater` editor utility, regenerated Wizard/Slime/Boss/TopDownBackground sprites, added Wizard idle/run animation assets + movement-driven animation controller, reassigned generated sprites to prefabs/MainScene, enlarged runtime damage text sizes, and reassigned all scene/upgrade TMP text to `AppleGothic_TMP`. PlayMode verified: Wizard uses Run sprite with Animator + driver, field background uses `TopDownBackground`, spawned slime uses new `Slime`, `DamageText.prefab` font size is 38, and scene TMP non-AppleGothic count is 0.

### 🔍 Review Notes (reviewer)
- (empty)

---

# Bundle 4 — Local Save

**Goal:** Persist game progress to disk.

### Bundle 4 Combined Regression Tests
1. PlayMode → gold 100 + 1 upgrade → exit → re-enter → all restored
2. save.json is human-readable
3. saveVersion field present
4. Delete save.json then re-enter → fresh game starts correctly
5. Chapter / stage progression restored

---

## Task G — SaveData Model + Local Save

**Status:** 🔴 TODO
**Depends On:** Bundle 3 gate

### 🎯 Goal
- Serialize stats, gold, chapter/stage, upgrade levels
- Save as JSON to `Application.persistentDataPath/save.json`
- Auto-load on game start; auto-save on key events
- `saveVersion` + migration hook

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] After exit/re-enter, gold / chapter+stage / stats are restored
- [ ] save.json is human-readable JSON
- [ ] 5 regression tests pass

### 📂 Files Changed

#### G-1. `Assets/Scripts/Save/SaveData.cs` (new)
```csharp
using System;
using System.Collections.Generic;

namespace WizardGrower.Save
{
    [Serializable]
    public class SaveData
    {
        public int saveVersion = 1;
        public string userId = "local";
        public long updatedAtUnixMs;

        public int gold;
        public int currentChapter = 1;
        public int currentStage = 1;

        public PlayerStatsSnapshot stats = new PlayerStatsSnapshot();
        public List<UpgradeLevelEntry> upgrades = new List<UpgradeLevelEntry>();
    }

    [Serializable]
    public class PlayerStatsSnapshot
    {
        public float autoAttackDamage;
        public float manualAttackDamage;
        public float autoAttackInterval;
        public float manualAttackInterval;
        public float criticalChance;
        public float criticalMultiplier;
        public float armorPenetration;
        public float maxHealth;
        public float currentHealth;
    }

    [Serializable]
    public class UpgradeLevelEntry
    {
        public string id;
        public int level;
    }
}
```

#### G-2. `Assets/Scripts/Save/SaveService.cs` (new)
```csharp
using System.IO;
using UnityEngine;

namespace WizardGrower.Save
{
    public class SaveService : MonoBehaviour
    {
        private const string FileName = "save.json";
        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        public SaveData CurrentData { get; private set; } = new SaveData();

        public bool TryLoad()
        {
            if (!File.Exists(FilePath)) return false;
            string json = File.ReadAllText(FilePath);
            SaveData loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded == null) return false;
            CurrentData = MigrateIfNeeded(loaded);
            return true;
        }

        public void Save()
        {
            CurrentData.updatedAtUnixMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonUtility.ToJson(CurrentData, prettyPrint: true);
            File.WriteAllText(FilePath, json);
        }

        public void Reset()
        {
            CurrentData = new SaveData();
            if (File.Exists(FilePath)) File.Delete(FilePath);
        }

        private SaveData MigrateIfNeeded(SaveData data)
        {
            if (data.saveVersion < 1) data.saveVersion = 1;
            return data;
        }
    }
}
```

#### G-3. `Assets/Scripts/Save/SaveBinder.cs` (new)
**Responsibility:** Bidirectional mapping between SaveData and game systems (`PlayerStats`, `CurrencyWallet`, `StageManager`, `UpgradeSystem`) + auto-save trigger registration.

```csharp
public class SaveBinder : MonoBehaviour
{
    public void ApplyToGame(SaveData data, GameContext ctx);
    public SaveData CaptureFromGame(GameContext ctx);
    public void RegisterAutoSaveTriggers(GameContext ctx, SaveService service);
}
```

**Auto-save triggers:**
- `wallet.GoldChanged` (recommend a 1-second debounce)
- `upgradeSystem.UpgradePurchased`
- `stageManager.StateChanged`
- `OnApplicationPause(true)`, `OnApplicationQuit`

**Required helper methods on existing classes:**
- `PlayerStats`: `ApplySnapshot(snapshot)`, `CaptureSnapshot()` (G-6)
- `StageManager`: `LoadProgress(int chapter, int stage)`
- `UpgradeSystem`: `LoadLevels(List<UpgradeLevelEntry>)`

#### G-4. `Assets/Scripts/Core/GameManager.cs` (modify)
- At the start of `Awake()`:
```csharp
context.SaveService.TryLoad();
// ... existing Initialize calls ...
context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
context.SaveBinder.RegisterAutoSaveTriggers(context, context.SaveService);
```
- Add lifecycle callbacks:
```csharp
private void OnApplicationPause(bool paused) { if (paused) context.SaveService.Save(); }
private void OnApplicationQuit() { context.SaveService.Save(); }
```

#### G-5. `Assets/Scripts/Core/GameContext.cs` (modify)
- Add fields: `SaveService`, `SaveBinder`

#### G-6. `Assets/Scripts/Player/PlayerStats.cs` (exception modification — 2 new methods)
**Reviewer-approved (in Task G body):** Task A's "do not touch PlayerStats" was to forbid stat changes. These additions are I/O adapters (snapshot apply/extract) and do not change stat semantics, so they are allowed.

- `public void ApplySnapshot(PlayerStatsSnapshot s)` — apply fields from serialized data and emit Changed/HealthChanged events
- `public PlayerStatsSnapshot CaptureSnapshot()` — extract a snapshot of current stats

> Document this addition in the work log.

### 🚫 Do Not Touch
- Game logic itself (combat, boss, stage flow)
- UI widgets (HUDController unchanged)

### 🧪 Validation
1. Compilation clean
2. PlayMode → earn 100 gold, upgrade auto-damage once → stop → re-enter → gold and stats restored
3. Open `save.json` in a text editor, confirm human-readable JSON
4. saveVersion=1 field present
5. Delete save.json → re-enter → fresh game (currentChapter=1, currentStage=1, gold=0)

### 📝 Work Log (implementer)
- (empty)

### 🔍 Review Notes (reviewer)
- (empty)

---

# Bundle 5 — Firebase Auth + Firestore Sync

**Goal:** Per-user cloud save. Cross-device synchronization.

### ⚠️ User Prework Required Before Starting Bundle 5

**Firebase setup (before Task H):**
- [ ] Firebase Console → create project, capture Project ID
- [ ] Register iOS app in Firebase → download `GoogleService-Info.plist` → place in `Assets/`
- [ ] Register Android app in Firebase → download `google-services.json` → place in `Assets/`
- [ ] Authentication → enable Anonymous / Google / Apple providers
- [ ] Apple Developer ($99/year) → enable Sign in with Apple, issue Service ID
- [ ] Google Cloud Console → issue OAuth client IDs
- [ ] Confirm Bundle ID / Package Name (e.g. `com.kmj.wizardgrower`)

**Firestore setup (before Task I):**
- [ ] Create Firestore database (Production mode recommended)
- [ ] Set Security Rules:
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

> **Do not start H/I until all prework above is complete and the user explicitly says "proceed with Bundle 5".**

### Bundle 5 Combined Regression Tests
1. First launch → anonymous login, UID issued, Firestore document auto-created
2. Google login → UID linked, existing progress retained
3. Delete save.json → re-login with same account → restored from Firestore
4. PlayMode while offline → game runs normally → online recovery → auto-sync
5. Two devices progressing → the later-saved one wins

---

## Task H — Firebase Auth (Anonymous / Google / Apple Login)

**Status:** 🔴 TODO
**Depends On:** Bundle 4 gate + Bundle 5 user prework complete

### 🎯 Goal
- Integrate Firebase Unity SDK (Auth module)
- Auto anonymous login on game start
- Allow linking with Google / Apple
- On login success, write the UID into SaveData.userId

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] First launch issues anonymous UID and logs to console
- [ ] Click Google login → account picker → UID linked
- [ ] Click Apple login → account picker → UID linked (iOS only)
- [ ] 4 regression tests pass (device or simulator required)

### 📂 Files Changed

#### H-1. Package adoption
- Firebase Unity SDK 12.x or higher (Auth module)
- Apple Sign In Unity Plugin
- Google Sign In Unity Plugin

> Exact package names/versions follow the Firebase Console's Unity guide.

#### H-2. `Assets/Scripts/Auth/AuthService.cs` (new)
```csharp
public class AuthService : MonoBehaviour
{
    public string CurrentUid { get; private set; }
    public event Action<string> UserChanged;

    public Task<string> SignInAnonymouslyAsync();
    public Task<bool> LinkWithGoogleAsync();
    public Task<bool> LinkWithAppleAsync();
    public Task SignOutAsync();
}
```

#### H-3. `Assets/Scripts/UI/LoginPanel.cs` (new)
- Invoked from an options screen or main screen
- Buttons: Google, Apple (iOS only), Skip
- Reflect AuthService results in the UI

#### H-4. `Assets/Scripts/Core/GameManager.cs` (modify)
- In `Awake()`: initialize Firebase → `AuthService.SignInAnonymouslyAsync()` → forward UID to SaveBinder

#### H-5. `Assets/Scripts/Core/GameContext.cs` (modify)
- Add field: `AuthService`

#### H-6. `MainScene` setup
- Add LoginPanel UI (optional)

### 🚫 Do Not Touch
- Combat, stage logic
- Other UI widgets

### 🧪 Validation
1. Compilation clean (after Firebase SDK package import)
2. First launch → anonymous UID logged
3. Google login → Google credential linked to UID
4. Apple login (iOS build) → same flow

### 📝 Work Log (implementer)
- (empty)

### 🔍 Review Notes (reviewer)
- (empty)

---

## Task I — Firestore Cloud Sync

**Status:** 🔴 TODO
**Depends On:** H

### 🎯 Goal
- Sync local SaveData to a Firestore document at `users/{uid}`
- Offline-first (local is truth, sync in background)
- Conflict resolution: compare `updatedAtUnixMs` → newer wins
- On network failure, keep operating locally

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] Earning gold in PlayMode → Firestore updated within 5 seconds
- [ ] After deleting save.json, re-login → restore from Firestore
- [ ] Offline mode operates normally
- [ ] 5 regression tests pass

### 📂 Files Changed

#### I-1. Package addition
- Firebase Firestore Unity module

#### I-2. `Assets/Scripts/Save/CloudSyncService.cs` (new)
```csharp
public class CloudSyncService
{
    private FirebaseFirestore db;

    public Task PushAsync(SaveData data);
    public Task<SaveData> PullAsync(string uid);
    public Task ResolveAndApply(SaveService localService, string uid);
}
```

`ResolveAndApply` behavior:
- Pull remote from Firestore
- If no remote → push local
- If remote is newer (updatedAtUnixMs) → overwrite local + save
- Otherwise → push local

#### I-3. `Assets/Scripts/Save/SyncCoordinator.cs` (new)
**Triggers:**
- AuthService.UserChanged → run `ResolveAndApply()` once
- On SaveData change, debounce 5s then push
- `OnApplicationPause(true)` → push immediately
- Queue while offline; flush on online recovery

#### I-4. `Assets/Scripts/Save/SaveData.cs` (Firestore compatibility)
- Add `[FirestoreData]`, `[FirestoreProperty]` attributes (per field)

#### I-5. `Assets/Scripts/Core/GameManager.cs` (modify)
- Start SyncCoordinator from the AuthService login callback

### 🚫 Do Not Touch
- Combat, stage, UI logic
- SaveService core (keep it separate from CloudSyncService)

### 🧪 Validation
1. Compilation clean
2. PlayMode anonymous login → earn gold → confirm `users/{uid}` updated in Firestore Console
3. Delete save.json → same UID re-login → restored from Firestore
4. Wi-Fi OFF in PlayMode → operates normally → Wi-Fi ON → auto-sync
5. Two devices progressing simultaneously → later save wins

### 📝 Work Log (implementer)
- (empty)

### 🔍 Review Notes (reviewer)
- (empty)

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies the following against this document:

1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — verify no unauthorized files were modified (`git diff`)
3. **"Do Not Touch" areas unchanged** — listed files unchanged
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation result matches the document's intent
6. **Exactly one git commit** — commit message includes the task ID

Record review results in each task's "🔍 Review Notes" section.

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
