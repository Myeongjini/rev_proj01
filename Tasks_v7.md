# Wizard Grower — Tasks v7 (Bundle 7: Weapon Grade Grid + Fusion + Summon Levels + Combat Power)

> Follow-up work track to `Tasks_v6.md`.
> Different agents, different bundle, different file.
> **Do NOT edit `Tasks.md` or `Tasks_v6.md` from this track.** If you need to read them, do so read-only.
>
> The **Planner / Reviewer (Claude)** is the sole editor of this document.
> Implementers read this document, modify the code, and append to Appendix D.

---

## 0. Common Work Rules

### 0.1 Basic Rules
1. **Only one task at a time.** State the task ID (e.g. `Task Q`) at start and end.
2. Before starting: read the relevant task section thoroughly. Never modify any **"Do Not Touch"** area.
3. During work: Unity project path is `/Users/kmj/rev_proj01`. Namespace is `WizardGrower.*`.
4. On completion:
   - Verify Unity Console: **0 errors / 0 warnings**
   - Enter PlayMode and pass all **regression tests** for the task
   - Update the task's Status line to `🟡 IN REVIEW`
   - Append a row to **Appendix D — Combined Work Log** with the task ID, date, and what you did
5. After code review, the reviewer:
   - Updates Status to `✅ DONE` or back to `🔴 TODO`
   - Appends a row to **Appendix E — Combined Review Log**
6. The build helper menu `Wizard Grower → Build Prototype Scene` **overwrites the scene** — do not run without explicit permission.
7. When adding/removing files, ensure `.meta` files are paired (Unity Editor handles this automatically).

### 0.2 Bundle Gate Rules ⭐
Bundle 7 is sequential. Do not start the next task until the previous task is `✅ DONE`.

**Gate pass conditions:**
1. All tasks Q~U are `✅ DONE`
2. Unity Console has 0 errors / 0 warnings
3. Bundle 7 release regression tests pass
4. Save migration from v2 to v3 preserves existing v6 progress

### 0.3 Auto-Progression Restrictions (Implementation Agent)
- Do not start another Bundle 7 task on your own.
- On signature/spec conflict, **do not decide unilaterally** — record in Appendix D and wait for the reviewer.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason.
- Do not introduce new major systems or files not listed in this document unless required for compilation; log exceptions and await reviewer approval.

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>`.
- This repo uses GitHub Desktop with origin set. **Agents must not push** — push is the user's responsibility.
- At task start, run `git status` to confirm working tree state. Note any uncommitted changes in Appendix D.
- Per task = per commit. `git revert` is the rollback tool.

### 0.5 Cross-Track Coordination ⚠️
Bundle 7 builds on the current v6 weapon/gacha implementation.

| Risk | Mitigation |
|---|---|
| v6 weapon/gacha implementation IS committed (Task N `b011581`, O `bdce55e`, P `0261300`) but `Tasks_v6.md` status board was never synced past `🟡 IN REVIEW` | Treat the current codebase as the v6 baseline. Bundle 7 refactors v6 weapon/gacha code in place. **Do not edit `Tasks_v6.md`** — the stale status board is a docs-only issue, out of scope for this track. |
| Save schema changes overlap local save and Firestore mapper files | Task S owns the `saveVersion = 3` migration and mapper changes. Earlier tasks must avoid save schema edits except where explicitly listed. |
| Stat unification touches many combat/UI files | Task Q must finish and be reviewed before any weapon stat task starts. |
| Weapon inventory currently stores unique weapon IDs only | Task S replaces it with counted ownership. Task T and U depend on that API. |

---

## 1. Task Dependency Graph

```
Bundle 7
Q → R → S → T → U → V → Bundle 7 Release Gate

Q: Stat unification + combat power
R: Weapon upper/lower grade model + 20 seed weapons
S: Counted inventory + 4-column weapon grid
T: Synthesize-all fusion
U: Summon level gacha refactor
V: MainUI01 navigation + summon policy hotfix
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 7 | Q | Stat Unification + Combat Power Feedback | 🟡 IN REVIEW | v6 baseline |
| 7 | R | Weapon Grade Model + 20 Seed Weapon Table | 🟡 IN REVIEW | Q ✅ |
| 7 | S | Counted Weapon Inventory + 4-Column Weapon Window | 🟡 IN REVIEW | R ✅ |
| 7 | T | Synthesize-All Weapon Fusion | 🟡 IN REVIEW | S ✅ |
| 7 | U | Summon Level Gacha Refactor | 🟡 IN REVIEW | T ✅ |
| 7 | V | MainUI01 Navigation + Summon Policy Hotfix | 🟡 IN REVIEW | U 🟡 |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 7 — Weapon Grade Grid + Fusion + Summon Levels + Combat Power

**Goal:** convert the v6 weapon/gacha feature into a scalable idle-RPG weapon ladder:
- one unified attack stat
- player combat power with temporary increase feedback
- 5 upper grades × 4 lower grades
- counted duplicate ownership
- synthesize-all weapon fusion
- summon levels that unlock higher upper grades

### Bundle 7 Release Regression Tests
1. Fresh save starts with unified `AttackDamage = 10`, starter weapon count `1`, and no auto/manual damage split visible in UI.
2. Auto attack deals `AttackDamage × 1.0`; manual click deals `AttackDamage × 2.0`; active skill deals `AttackDamage × 8.0`.
3. Buying an attack upgrade or equipping a stronger weapon updates combat power and shows `Power: <current> +<delta>` for about 1 second.
4. Weapon grid has 4 columns and vertical scroll. Each row contains exactly one upper grade, ordered `Common → Normal → Advanced → Epic → Unique`.
5. Every seeded upper grade has exactly 4 weapons in lower grade order: `Beginner → Intermediate → Upper → Supreme`.
6. Seed weapon attack bonuses increase by grade; crossing to the next upper grade gives a larger attack jump than any lower-grade step inside the previous row.
7. Clicking a weapon slot shows that weapon's detail panel at the bottom of the weapon window and enables the equip button only when equipping is valid.
8. Counted inventory persists duplicates. Pulling the same weapon three times results in count `3`, not one unique ID.
9. Synthesize-all converts `3` exact copies into the next ladder weapon, including `Common Supreme x3 → Normal Beginner x1`.
10. Gacha level 1 never rolls above `Advanced`; level 2 can roll `Epic`; level 3 can roll `Unique`.
11. Within any rolled upper grade, lower grades are chosen at equal 25% probability.
12. Save/restart preserves weapon counts, equipped weapon, fusion results, summon level, pulls-in-level, gems, and pity counter.

---

## Task Q — Stat Unification + Combat Power Feedback

**Status:** 🟡 IN REVIEW
**Depends On:** v6 baseline

### 🎯 Goal
Replace separate auto/manual attack damage concepts with one `AttackDamage` stat and add combat power feedback when player power increases.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `PlayerStats` exposes `AttackDamage` as the single damage stat
- [ ] Auto/manual/skill damage use coefficients, not separate stat fields
- [ ] Upgrade UI and HUD labels use consistent stat names (`Attack`, `Attack Speed`, `Critical Chance`, etc.)
- [ ] Combat power recalculates after stat changes, weapon recompute, mana stat changes, upgrade purchase, and save load
- [ ] A temporary popup displays `전투력: <current>  (+<delta>)` (Korean) for about 1 second when combat power increases
- [ ] Existing save data loads without losing progress

### 📂 Files to Add
- `Assets/Scripts/Player/CombatPowerService.cs`
  ```csharp
  public class CombatPowerService
  {
      public float CurrentPower { get; }
      public event Action<float, float> PowerIncreased; // current, delta
      public void Initialize(PlayerStats stats, PlayerMana mana);
      public void Recalculate(bool showIncreaseFeedback);
  }
  ```
- `Assets/Scripts/UI/CombatPowerPopupView.cs`
  ```csharp
  public class CombatPowerPopupView : MonoBehaviour
  {
      public void Bind(CombatPowerService service);
      public void Show(float currentPower, float delta);
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Player/PlayerStats.cs`
  - Replace runtime public damage fields/properties with unified `AttackDamage`
  - Keep compatibility helpers only if needed for migration, but mark them as internal/private migration paths
  - Final public API should not encourage `AutoAttackDamage` / `ManualAttackDamage`
- `Assets/Scripts/Save/SaveData.cs`
  - Add `attackDamage` to `PlayerStatsSnapshot`
  - Keep legacy `autoAttackDamage` / `manualAttackDamage` fields only as migration inputs if necessary
- `Assets/Scripts/Save/SaveService.cs`
  - Migrate old snapshots by selecting `attackDamage = autoAttackDamage` if present; fallback default `10`
- `Assets/Scripts/Combat/CombatCalculator.cs`
  - Use coefficients:
    - Auto attack coefficient: `1.0f`
    - Manual click coefficient: `2.0f`
    - Active skill coefficient: `8.0f`
- `Assets/Scripts/Combat/AutoAttackController.cs`
- `Assets/Scripts/Combat/ClickAttackController.cs`
- `Assets/Scripts/Combat/ActiveSkillController.cs`
  - Ensure the right attack type coefficient is passed/applied
- `Assets/Scripts/Upgrades/UpgradeSystem.cs`
  - Replace separate auto/manual damage upgrades with one attack upgrade
  - Keep attack speed, critical, armor penetration, max health, and mana upgrades
- `Assets/Scripts/UI/HUDController.cs`
  - Display combat power and unified attack stat
  - Wire `CombatPowerPopupView`
- `Assets/Scripts/Core/GameContext.cs`
  - Add serialized references for `CombatPowerService` owner/wiring if implemented as MonoBehaviour, or popup reference if pure service is constructed in `GameManager`
- `Assets/Scripts/Core/GameManager.cs`
  - Initialize and bind combat power after stats/mana/save/weapon systems are ready

### 🚫 Do Not Touch
- Weapon grade model (`WeaponDefinition`, `Rarity`, `WeaponDatabase`) — Task R owns this
- Counted weapon inventory / save v3 schema — Task S owns this
- Gacha level rules — Task U owns this
- Auth, presence, chat, stage/boss flow

### 🧪 Validation
1. Compile clean
2. Fresh save → HUD shows Attack `10` and combat power value
3. Auto projectile damage = `10` before crit/armor
4. Manual click damage = `20` before crit/armor
5. Active skill damage = `80` before crit/armor
6. Buy Attack upgrade → combat power increases and popup `전투력: <current> (+<delta>)` appears for about 1 second
7. Save + restart → unified attack value persists

### Implementation note
Combat power formula does not need to be final commercial balance. Use a readable deterministic formula:
```text
power = attackDamage
      * (1 + criticalChance * (criticalMultiplier - 1))
      * (1 / autoAttackInterval)
      + armorPenetration * 2
      + maxHealth * 0.1
      + maxMana * 0.05
```
Round for display only. Keep the raw float for comparison.

---

## Task R — Weapon Grade Model + 20 Seed Weapon Table

**Status:** 🟡 IN REVIEW
**Depends On:** Q ✅

### 🎯 Goal
Replace the v6 single-rarity weapon model with a two-axis grade model: extensible upper grades and fixed lower grades.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] Upper grades compile as `Common`, `Normal`, `Advanced`, `Epic`, `Unique`
- [ ] Lower grades compile as `Beginner`, `Intermediate`, `Upper`, `Supreme`
- [ ] `WeaponDefinition` contains both grade fields and Korean display labels resolve correctly
- [ ] `WeaponDatabase` can return weapons by upper grade, lower grade, ladder index, and weapon ID
- [ ] Exactly 20 seed weapon assets exist: 5 upper grades × 4 lower grades
- [ ] Existing v6 starter weapon migrates to `common_beginner_staff`
- [ ] Weapon stats use unified `attackDamage`, not auto/manual split
- [ ] Every weapon has an attack increase value on equip
- [ ] Attack increase rises as grades rise
- [ ] Moving from one upper grade's `Supreme` weapon to the next upper grade's `Beginner` weapon gives a larger attack increase than any lower-grade step inside that upper grade row

### 📂 Files to Add
- `Assets/Scripts/Weapons/WeaponUpperGrade.cs`
  ```csharp
  public enum WeaponUpperGrade
  {
      Common,
      Normal,
      Advanced,
      Epic,
      Unique
  }
  ```
- `Assets/Scripts/Weapons/WeaponLowerGrade.cs`
  ```csharp
  public enum WeaponLowerGrade
  {
      Beginner,
      Intermediate,
      Upper,
      Supreme
  }
  ```
- `Assets/Scripts/Weapons/WeaponGradeLabels.cs`
  ```csharp
  public static class WeaponGradeLabels
  {
      public static string UpperKo(WeaponUpperGrade grade);
      public static string LowerKo(WeaponLowerGrade grade);
      public static string Display(WeaponUpperGrade upper, WeaponLowerGrade lower);
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Weapons/Rarity.cs`
  - Remove usage from weapon/gacha UI, or keep as obsolete compatibility only if compilation requires it
- `Assets/Scripts/Weapons/WeaponStats.cs`
  ```csharp
  [Serializable]
  public struct WeaponStats
  {
      public float attackDamage;
      public float attackSpeedBonus;      // subtracts from autoAttackInterval, clamp >= 0.05
      public float criticalChance;
      public float criticalMultiplier;
      public float armorPenetration;
      public float maxHealth;
      public float maxMana;
  }
  ```
- `Assets/Scripts/Weapons/WeaponDefinition.cs`
  ```csharp
  public string weaponId;                 // e.g. "common_beginner_staff"
  public string displayName;              // Korean display name
  public WeaponUpperGrade upperGrade;
  public WeaponLowerGrade lowerGrade;
  public int ladderIndex;                 // computed/serialized order: upper * 4 + lower
  public Sprite icon;
  public Color tintColor = Color.white;
  public Sprite accessoryGlyph;
  public Sprite projectileSprite;
  public WeaponStats statBonuses;
  [TextArea] public string flavorText;
  ```
- `Assets/Scripts/Weapons/WeaponDatabase.cs`
  - Add:
    ```csharp
    public WeaponDefinition GetById(string weaponId);
    public WeaponDefinition GetByGrade(WeaponUpperGrade upper, WeaponLowerGrade lower);
    public IReadOnlyList<WeaponDefinition> GetRow(WeaponUpperGrade upper);
    public WeaponDefinition GetByLadderIndex(int ladderIndex);
    public WeaponDefinition GetNext(WeaponDefinition weapon);
    public IReadOnlyList<WeaponDefinition> OrderedWeapons { get; }
    ```
  - `OrderedWeapons` must sort by upper grade ascending, then lower grade ascending
- `Assets/Scripts/Weapons/WeaponStatComposer.cs`
  - Recompute unified stats using `WeaponStats.attackDamage`
- `Assets/Scripts/Editor/VisualAssetUpdater.cs`
  - Regenerate weapon glyph/projectile placeholders for all 20 seed weapons
- `Assets/Data/Weapons/*.asset`
  - Replace the 6 v6 seed weapons with 20 v7 seed weapons
- `Assets/Data/Weapons/WeaponDatabase.asset`
  - Reference all 20 weapons in ladder order

### Seed Weapon Ladder
Use these exact IDs and grade order:

| Ladder | ID | Korean Name | Upper | Lower |
|---:|---|---|---|---|
| 0 | `common_beginner_staff` | 일반 초급 지팡이 | Common | Beginner |
| 1 | `common_intermediate_staff` | 일반 중급 지팡이 | Common | Intermediate |
| 2 | `common_upper_staff` | 일반 상급 지팡이 | Common | Upper |
| 3 | `common_supreme_staff` | 일반 최상급 지팡이 | Common | Supreme |
| 4 | `normal_beginner_staff` | 노멀 초급 지팡이 | Normal | Beginner |
| 5 | `normal_intermediate_staff` | 노멀 중급 지팡이 | Normal | Intermediate |
| 6 | `normal_upper_staff` | 노멀 상급 지팡이 | Normal | Upper |
| 7 | `normal_supreme_staff` | 노멀 최상급 지팡이 | Normal | Supreme |
| 8 | `advanced_beginner_staff` | 고급 초급 지팡이 | Advanced | Beginner |
| 9 | `advanced_intermediate_staff` | 고급 중급 지팡이 | Advanced | Intermediate |
| 10 | `advanced_upper_staff` | 고급 상급 지팡이 | Advanced | Upper |
| 11 | `advanced_supreme_staff` | 고급 최상급 지팡이 | Advanced | Supreme |
| 12 | `epic_beginner_staff` | 에픽 초급 지팡이 | Epic | Beginner |
| 13 | `epic_intermediate_staff` | 에픽 중급 지팡이 | Epic | Intermediate |
| 14 | `epic_upper_staff` | 에픽 상급 지팡이 | Epic | Upper |
| 15 | `epic_supreme_staff` | 에픽 최상급 지팡이 | Epic | Supreme |
| 16 | `unique_beginner_staff` | 유니크 초급 지팡이 | Unique | Beginner |
| 17 | `unique_intermediate_staff` | 유니크 중급 지팡이 | Unique | Intermediate |
| 18 | `unique_upper_staff` | 유니크 상급 지팡이 | Unique | Upper |
| 19 | `unique_supreme_staff` | 유니크 최상급 지팡이 | Unique | Supreme |

### Stat Seed Rule
Use a simple readable progression. Exact numbers can be tuned later, but the attack increase rule is mandatory:
- Every weapon must have an `attackDamage` equip bonus.
- Higher ladder weapons must always have higher `attackDamage`.
- The jump from one upper grade to the next upper grade must be larger than the lower-grade step increases inside the previous upper grade row.

Use this seed table:
```text
upperBaseAttack:
  Common   = 0
  Normal   = 32
  Advanced = 80
  Epic     = 150
  Unique   = 250

lowerAttackBonus:
  Beginner     = 0
  Intermediate = 4
  Upper        = 10
  Supreme      = 18

attackDamage = upperBaseAttack[upperGrade] + lowerAttackBonus[lowerGrade]
criticalChance = 0.00 for Common/Normal, 0.02 Advanced, 0.04 Epic, 0.06 Unique
criticalMultiplier = 0.00 Common/Normal/Advanced, 0.10 Epic, 0.20 Unique
armorPenetration = floor(ladderIndex / 4)
maxHealth = ladderIndex * 3
maxMana = upperGradeIndex * 5
attackSpeedBonus = min(0.20, ladderIndex * 0.005)
```

Attack jump validation examples:
```text
Common Supreme attack bonus = 18
Normal Beginner attack bonus = 32
Upper-grade transition jump = +14, which is greater than the Common row's largest lower-grade step (+8)

Normal Supreme attack bonus = 50
Advanced Beginner attack bonus = 80
Upper-grade transition jump = +30, which is greater than the Normal row's largest lower-grade step (+8)
```

### 🚫 Do Not Touch
- Save schema and counted inventory — Task S owns these
- Fusion service — Task T owns this
- Gacha service and summon levels — Task U owns this
- Combat coefficient logic from Task Q

### 🧪 Validation
1. Compile clean
2. `WeaponDatabase.OrderedWeapons.Count == 20`
3. Every upper grade row has exactly 4 weapons
4. `GetNext(common_supreme_staff)` returns `normal_beginner_staff`
5. `GetNext(unique_supreme_staff)` returns null
6. `common_beginner_staff` has zero or near-zero bonuses and is safe as starter
7. Attack bonuses are strictly increasing by ladder order
8. For every upper-grade transition, `nextUpper.Beginner.attackDamage - currentUpper.Supreme.attackDamage` is greater than the largest lower-grade attack step inside `currentUpper`

---

## Task S — Counted Weapon Inventory + 4-Column Weapon Window

**Status:** 🟡 IN REVIEW
**Depends On:** R ✅

### 🎯 Goal
Allow duplicate weapon ownership and rebuild the weapon window as a 4-column vertical grid where each row is one upper grade.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SaveData.saveVersion` bumped to `3`
- [ ] Existing v2 `ownedWeaponIds` migrate into counted `ownedWeapons`
- [ ] Duplicate pulls increase count instead of being ignored
- [ ] Weapon inventory panel shows all 20 weapons in 4 columns
- [ ] Each row contains one upper grade only
- [ ] Slot views show owned count (`x0`, `x1`, `x3`, etc.)
- [ ] Clicking a weapon slot selects it and displays its detail info in a dedicated bottom area of the weapon window
- [ ] Detail info includes weapon name, upper/lower grade, owned count, equipped state, attack increase, other stat bonuses, and flavor text
- [ ] Equip button is placed in the detail area and becomes active only when the selected weapon has count > 0 and is not already equipped
- [ ] Equip works only through the selected weapon detail button, not directly from the grid slot
- [ ] Save/restart preserves counts and equipped weapon

### 📂 Files to Add
- `Assets/Scripts/Weapons/OwnedWeaponEntry.cs`
  ```csharp
  [Serializable]
  public class OwnedWeaponEntry
  {
      public string weaponId;
      public int count;
  }
  ```
- `Assets/Scripts/UI/WeaponDetailView.cs`
  ```csharp
  public class WeaponDetailView : MonoBehaviour
  {
      public void Clear();
      public void Show(WeaponDefinition weapon, int ownedCount, bool equipped);
      public event Action<WeaponDefinition> EquipRequested;
  }
  ```
- `Assets/Scripts/Player/WeaponVisualController.cs` — runtime visual swap on equip
  ```csharp
  public class WeaponVisualController : MonoBehaviour
  {
      // Subscribe to WeaponInventory.EquippedChanged.
      // On change: tint the wizard's SpriteRenderer to weapon.tintColor,
      // spawn/replace the accessoryGlyph child SpriteRenderer at a fixed local offset,
      // and notify ProjectileFactory to use weapon.projectileSprite for new auto-attack projectiles.
      public void Bind(PlayerWizard wizard, WeaponInventory inventory, ProjectileFactory projectileFactory);
  }
  ```
  > v6 left this component out (verified — `Assets/Scripts/Player/` has no `WeaponVisualController.cs`). Task S DoD bullet "tint/glyph/projectile/stat bonus apply once" cannot pass without this — add it here.

### 📂 Files to Modify
- `Assets/Scripts/Weapons/WeaponInventory.cs`
  - Replace `List<string> ownedWeaponIds` with counted ownership
  - Required API:
    ```csharp
    public int GetCount(string weaponId);
    public bool Has(string weaponId);
    public void Add(string weaponId, int count = 1);
    public bool TryConsume(string weaponId, int count);
    public bool TryEquip(string weaponId);
    public IReadOnlyList<OwnedWeaponEntry> CaptureForSave();
    public void LoadFromSave(List<OwnedWeaponEntry> entries, string equippedId);
    public event Action InventoryChanged;
    public event Action<WeaponDefinition> EquippedChanged;
    public event Action<WeaponDefinition, int> WeaponCountChanged;
    ```
  - Starter fallback: if there are no valid owned entries, grant `common_beginner_staff x1`
  - If the equipped weapon count becomes 0, auto-equip the highest ladder weapon with count > 0; fallback to starter if none
- `Assets/Scripts/Save/SaveData.cs`
  - Set `saveVersion = 3`
  - Add:
    ```csharp
    public List<OwnedWeaponEntry> ownedWeapons = new List<OwnedWeaponEntry>();
    ```
  - Keep `ownedWeaponIds` only as a legacy migration field if necessary
  - Rename default equipped weapon to `common_beginner_staff`
- `Assets/Scripts/Save/SaveDataDocument.cs`
  - Mirror `ownedWeapons`, `equippedWeaponId`, `summonLevel`, and future Task U fields if mapper needs one pass
- `Assets/Scripts/Save/SaveDataMapper.cs`
  - Map counted entries to/from Firestore document
- `Assets/Scripts/Save/SaveService.cs`
  - v2 → v3 migration:
    - `wand_starter` → `common_beginner_staff`
    - any v6 weapon ID maps to the closest v7 grade below:
      - `apprentice_staff` / `crystal_wand` → `normal_beginner_staff`
      - `wizards_stave` / `flame_rod` → `advanced_beginner_staff`
      - `arcane_scepter` → `epic_beginner_staff`
    - duplicate IDs become counts
    - if the result is empty, grant `common_beginner_staff x1`
- `Assets/Scripts/Save/SaveBinder.cs`
  - Capture/apply counted weapon entries
- `Assets/Scripts/UI/WeaponInventoryPanel.cs`
  - Build grid from `WeaponDatabase.OrderedWeapons`
  - Use 4 fixed columns
  - Each visual row must contain the 4 lower grades for one upper grade
  - Sort rows by upper grade ascending
  - Add a fixed bottom detail area below the scroll view
  - Selecting a slot updates `WeaponDetailView`; do not equip directly from slot click
  - The detail area's equip button calls `WeaponInventory.TryEquip(selected.weaponId)`
- `Assets/Scripts/UI/WeaponSlotView.cs`
  - Display grade, icon, owned count, equipped state, disabled state for count 0
  - Emit a selected/clicked event to the panel, even for count-0 weapons, so users can inspect locked/unowned weapon details
- `Assets/Prefabs/UI/WeaponSlot.prefab`
  - Add count label
- `Assets/Prefabs/UI/WeaponInventoryPanel.prefab` or MainScene weapon panel hierarchy
  - Reserve bottom space for `WeaponDetailView`
  - Detail area must remain visible while the weapon grid scrolls
  - Default state before selection: show neutral text such as `Select a weapon`

### 🚫 Do Not Touch
- Weapon stat seed data from Task R unless a broken asset reference requires repair
- Fusion logic — Task T owns consume-and-create chain behavior
- Gacha roll rules — Task U owns summon levels
- Presence/chat/auth/stage flow

### 🧪 Validation
1. Compile clean
2. Delete save → PlayMode → inventory has `common_beginner_staff x1`
3. Add `common_beginner_staff` twice via debug/MCP → slot shows `x3`
4. Save JSON contains `ownedWeapons` with count `3`
5. Restart → count remains `3`
6. Weapon panel shows 5 rows × 4 columns in grade order
7. Click an unowned weapon → bottom detail area shows name/grade/stats/count `x0`; equip button disabled
8. Click an owned unequipped weapon → bottom detail area shows full stats; equip button enabled
9. Click equip in the detail area → tint/glyph/projectile/stat bonus apply once, no double stacking
10. Click currently equipped weapon → detail area shows equipped state; equip button disabled or shows already-equipped state

---

## Task T — Synthesize-All Weapon Fusion

**Status:** 🟡 IN REVIEW
**Depends On:** S ✅

### 🎯 Goal
Add one weapon synthesis button that converts every possible 3-of-kind duplicate chain into the next weapon in ladder order.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] Weapon window has a visible synthesize-all button
- [ ] `3` exact copies of one weapon convert into `1` next-ladder weapon
- [ ] Fusion chains continue until no count is >= 3
- [ ] `Common Supreme x3 → Normal Beginner x1`
- [ ] `Unique Supreme` does not synthesize further
- [ ] Equipped weapon remains valid after fusion
- [ ] Save/restart preserves fusion results

### 📂 Files to Add
- `Assets/Scripts/Weapons/WeaponFusionService.cs`
  ```csharp
  public class WeaponFusionService
  {
      public event Action<IReadOnlyList<WeaponFusionResult>> FusionCompleted;
      public bool CanFuseAny(WeaponInventory inventory, WeaponDatabase database);
      public IReadOnlyList<WeaponFusionResult> SynthesizeAll(WeaponInventory inventory, WeaponDatabase database);
  }

  [Serializable]
  public struct WeaponFusionResult
  {
      public string fromWeaponId;
      public string toWeaponId;
      public int times;
  }
  ```
- `Assets/Scripts/UI/WeaponFusionResultView.cs` — lightweight popup/list for fusion summary

### 📂 Files to Modify
- `Assets/Scripts/Weapons/WeaponInventory.cs`
  - Expose safe count mutation APIs needed by `WeaponFusionService`
  - Ensure count change events refresh UI after batch operations
- `Assets/Scripts/UI/WeaponInventoryPanel.cs`
  - Add synthesize-all button
  - Button interactable only when `CanFuseAny` is true
  - After fusion, refresh grid counts and equipped state
- `Assets/Scripts/UI/WeaponSlotView.cs`
  - Optional: visually mark slots with count >= 3
- `Assets/Scripts/Core/GameContext.cs`
  - Register `WeaponFusionService` or UI reference if needed
- `Assets/Scripts/Core/GameManager.cs`
  - Initialize/bind fusion service to inventory panel
- `Assets/Scripts/Save/SaveBinder.cs`
  - Ensure autosave/save capture happens after fusion if current save flow supports immediate save

### Fusion Algorithm
Use ladder order from `WeaponDatabase.OrderedWeapons`.

For each weapon in ascending ladder order:
1. Skip if current weapon is the final ladder weapon (`unique_supreme_staff`)
2. `times = count / 3`
3. Consume `times * 3` from current weapon
4. Add `times` to the next ladder weapon
5. Continue scanning forward so chain fusion can occur in one button press

Example:
```text
common_beginner_staff x9
→ common_intermediate_staff x3
→ common_upper_staff x1
```

### Equipped Weapon Rule
If fusion consumes all copies of the equipped weapon:
1. Equip the highest ladder weapon currently owned with count > 0
2. If none exists, grant/equip `common_beginner_staff x1`
3. Recompute player stats exactly once after the final equipped weapon is chosen

### 🚫 Do Not Touch
- Summon level / gacha roll logic — Task U owns this
- Weapon grade seed assets from Task R
- Save schema shape from Task S unless a bug blocks fusion persistence

### 🧪 Validation
1. Compile clean
2. Inventory with `common_beginner_staff x3` → synthesize → `common_intermediate_staff x1`
3. Inventory with `common_supreme_staff x3` → synthesize → `normal_beginner_staff x1`
4. Inventory with `unique_supreme_staff x3` → synthesize button ignores it and does not delete anything
5. Inventory with `common_beginner_staff x9` → one click chains to `common_upper_staff x1`
6. Equipped consumed weapon auto-equips the highest owned replacement and combat power updates
7. Save + restart → fusion counts preserved

---

## Task U — Summon Level Gacha Refactor

**Status:** 🟡 IN REVIEW
**Depends On:** T ✅

### 🎯 Goal
Refactor gacha into a summon-level system. Higher summon levels unlock higher upper grades; lower grades remain equal 25% distribution within the selected upper grade.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] First launch starts at summon level `1`, pulls-in-level `0`
- [ ] Lv1 caps upper grade at `Advanced`
- [ ] Lv2 caps upper grade at `Epic`
- [ ] Lv3 caps upper grade at `Unique`
- [ ] Pull count advances summon level using the seeded thresholds
- [ ] Lower grade is selected uniformly among 4 lower grades after upper grade is selected
- [ ] 1x pull costs 30 gems; 10x pull costs 270 gems
- [ ] 30-pull pity remains and guarantees at least the configured pity floor within currently unlocked upper grades
- [ ] Save/restart preserves `summonLevel`, `summonPullsInLevel`, `pityCounter`, and gems
- [ ] Gacha UI displays summon level, progress to next level, available max upper grade, and pity counter

### 📂 Files to Add
- `Assets/Scripts/Weapons/SummonLevelDefinition.cs`
  ```csharp
  [Serializable]
  public class SummonLevelDefinition
  {
      public int level;
      public int pullsToNextLevel; // 0 for max level
      public WeaponUpperGrade maxUpperGrade;
      public WeaponGradeWeight[] upperGradeWeights;
  }
  ```
- `Assets/Scripts/Weapons/WeaponGradeWeight.cs`
  ```csharp
  [Serializable]
  public struct WeaponGradeWeight
  {
      public WeaponUpperGrade upperGrade;
      public float weight;
  }
  ```
- `Assets/Scripts/Weapons/SummonLevelState.cs`
  ```csharp
  [Serializable]
  public class SummonLevelState
  {
      public int summonLevel = 1;
      public int summonPullsInLevel = 0;
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Weapons/GachaDefinition.cs`
  - Replace rarity weights with summon level definitions
  - Required seeded values:
    ```text
    Lv1: pullsToNextLevel 20, max Advanced, Common 70 / Normal 25 / Advanced 5
    Lv2: pullsToNextLevel 50, max Epic, Common 55 / Normal 30 / Advanced 12 / Epic 3
    Lv3: pullsToNextLevel 0,  max Unique, Common 40 / Normal 32 / Advanced 20 / Epic 7 / Unique 1
    costSingle 30, costTen 270, pityThreshold 30, pityFloor Normal
    ```
- `Assets/Scripts/Weapons/GachaService.cs`
  - Roll upper grade from current summon level weights
  - Roll lower grade uniformly:
    ```csharp
    WeaponLowerGrade lower = (WeaponLowerGrade)random.Range(0, 4);
    ```
  - Resolve weapon by `WeaponDatabase.GetByGrade(upper, lower)`
  - Increment summon progress once per pulled weapon
  - Level up immediately when threshold is met; excess pulls carry into the next level
  - Pity guarantee:
    - If pity triggers, minimum upper grade is `Normal` (so even Lv1 pity is meaningful — without this, Lv1 cap == pity floor and pity is a no-op)
    - If current max upper grade is below pity floor, clamp to current max
    - Choose among eligible upper grades using current level weights filtered to `>= floor`
- `Assets/Scripts/UI/GachaPanel.cs`
  - Show summon level (`Summon Lv. 1`)
  - Show progress (`12 / 20`) or `MAX`
  - Show max available upper grade
  - Show pity counter
- `Assets/Scripts/UI/GachaResultPanel.cs`
  - Sort results by ladder index descending or upper/lower grade descending for readability
- `Assets/Scripts/UI/RarityVisuals.cs`
  - Rename/refactor to grade visuals, or keep class name but use `WeaponUpperGrade`
- `Assets/Scripts/Save/SaveData.cs`
  - Add:
    ```csharp
    public int summonLevel = 1;
    public int summonPullsInLevel = 0;
    ```
- `Assets/Scripts/Save/SaveDataDocument.cs`
- `Assets/Scripts/Save/SaveDataMapper.cs`
- `Assets/Scripts/Save/SaveBinder.cs`
  - Capture/apply summon level fields
- `Assets/Data/Gacha/Standard.asset`
  - Configure the three seeded summon levels

### 🚫 Do Not Touch
- Weapon fusion behavior from Task T
- Counted inventory schema from Task S except adding summon fields to save capture/apply
- Combat coefficient/stat unification from Task Q
- Auth/presence/chat/stage flow

### 🧪 Validation
1. Compile clean
2. Fresh save → Gacha panel shows Summon Lv. 1 and `0 / 20`
3. Force 1000 test rolls at Lv1 with fixed random seed → no `Epic` or `Unique`
4. After 20 pulls → level becomes 2 and progress resets/carries correctly
5. At Lv2 fixed-seed test → `Epic` can appear, `Unique` cannot
6. After 50 more pulls → level becomes 3
7. At Lv3 fixed-seed test → `Unique` can appear
8. For a forced selected upper grade, lower-grade distribution across 1000 rolls is approximately even (each around 25%, allow ±7%)
9. 30-pull pity guarantees at least `Normal` if current summon level allows it
10. Save + restart → summon level/progress/gems/pity preserved

---

## Task V — MainUI01 Navigation + Summon Policy Hotfix

**Status:** 🟡 IN REVIEW
**Depends On:** U 🟡

### 🎯 Goal
Five user-requested follow-ups after Q~U landed:
1. Fix `최상급` (Supreme) lower-grade missing in the in-game weapon panel
2. Add a unified bottom-nav bar `메인UI01` (강화 / 무기 / 소환 / 추가예정4 / 추가예정5) that hosts all sub-panels
3. Each sub-panel has its own internal close button; same-button-toggle behavior is also kept (close also works by re-clicking the bottom-nav button)
4. Strip max-grade text + pity from the summon panel (keep `pityCounter` save field; only remove logic & UI)
5. Add a small probability button beside the summon-level label that opens a per-level upper-grade probability popup

### ✅ Definition of Done

**Item 1 — Supreme rendering fix**
- [ ] `WeaponInventoryPanel` consistently renders 4 lower grades per row at runtime (5×4 = 20 visible slots)
- [ ] Root cause documented in Appendix D (most likely: `WeaponInventoryPanel.prefab` `GridLayoutGroup` cell width × 4 + spacing × 3 exceeds container width — code already sets `constraintCount = 4`, so fix is in prefab cellSize/spacing/container width)

**Item 2 — 메인UI01 navigation**
- [ ] `메인UI01Bar` is anchored at the bottom of `MainScene` HUD with 5 equal-width buttons in order: `강화 / 무기 / 소환 / 추가예정4 / 추가예정5`
- [ ] `추가예정4` / `추가예정5` buttons are visible but **disabled (grayed out)** with a `준비중` Korean tooltip/label
- [ ] Clicking `강화` opens the existing slide-up upgrade drawer
- [ ] Clicking `무기` opens the weapon inventory panel **as a bottom slide-up popup** (same animation/feel as `UpgradeDrawerView`, NOT a screen-blocking modal)
- [ ] Clicking `소환` opens the gacha panel as a **full-screen overlay** (HUD obscured beneath; OK)
- [ ] Only one sub-panel may be open at a time; opening any nav button closes the previously open one

**Item 3 — Close behavior**
- [ ] Each sub-panel (`강화` / `무기` / `소환`) has an internal `X` close button
- [ ] Re-clicking the same bottom-nav button while its panel is open ALSO closes it (toggle behavior preserved)
- [ ] Clicking a different bottom-nav button closes the current panel and opens the new one in one step

**Item 4 — Summon UI cleanup + pity removal**
- [ ] `GachaPanel` no longer displays `최대 고급` (or any other "max upper grade") label
- [ ] `GachaPanel` no longer displays the pity counter
- [ ] `GachaService.PullOnce` no longer applies pity floor (just rolls weights; clamp to current `summonLevel.maxUpperGrade` only)
- [ ] `SaveData.pityCounter` field **REMAINS** on disk for backward compatibility; not read or written by gameplay
- [ ] `GachaDefinition.pityThreshold` and `pityFloor` are kept on the asset for forward-compat but unused; add a code comment noting deprecation

**Item 5 — Per-level probability popup**
- [ ] Small button (≤ 80% of summon-level TMP height) sits to the right of the summon-level label
- [ ] Click → modal popup `GachaProbabilityPopup` overlays the summon panel
- [ ] Popup lists every upper grade reachable at the current summon level with its **percentage** (normalized from `WeaponGradeWeight[]`, 0.1% precision; e.g. `Common 70.0% / Normal 25.0% / Advanced 5.0%`)
- [ ] Popup closes via (a) its own X button OR (b) clicking the dimmed area outside the popup
- [ ] If summon level changes while the popup is open, contents auto-refresh to the new level's table

### 📂 Files to Add
- `Assets/Scripts/UI/MainUI01Bar.cs` — bottom-nav controller
  ```csharp
  public class MainUI01Bar : MonoBehaviour
  {
      public enum NavTab { Upgrade, Weapon, Summon, Reserved4, Reserved5 }

      [SerializeField] private Button upgradeButton;
      [SerializeField] private Button weaponButton;
      [SerializeField] private Button summonButton;
      [SerializeField] private Button reserved4Button;   // disabled (grayed)
      [SerializeField] private Button reserved5Button;   // disabled (grayed)

      public event Action<NavTab> TabRequested;          // emit only on enabled buttons
      public void SetActiveTab(NavTab? activeTab);       // null = no tab open
  }
  ```
- `Assets/Scripts/UI/MainUI01Coordinator.cs` — drives mutual-exclusion between sub-panels
  ```csharp
  public class MainUI01Coordinator : MonoBehaviour
  {
      public void Initialize(MainUI01Bar bar, UpgradeDrawerView upgrade,
                             WeaponInventoryPanel weapon, GachaPanel summon);
      // Open(tab): closes any other open panel, opens requested.
      // Re-clicking the active tab closes it.
  }
  ```
- `Assets/Scripts/UI/GachaProbabilityPopup.cs` — per-level probability dialog
  ```csharp
  public class GachaProbabilityPopup : MonoBehaviour
  {
      [SerializeField] private Transform rowContainer;
      [SerializeField] private GachaProbabilityRowView rowPrefab;
      [SerializeField] private Button closeButton;
      [SerializeField] private Button outsideClickArea;   // full-screen invisible button beneath popup

      public void Bind(GachaService gachaService);
      public void Show(int summonLevel);
      public void Hide();
      // Auto-refresh on GachaService.SummonLevelChanged while visible.
  }
  ```
- `Assets/Scripts/UI/GachaProbabilityRowView.cs` + `Assets/Prefabs/UI/GachaProbabilityRow.prefab`
  - One row per upper grade: `<gradeKo>: 70.0%`
- `Assets/Prefabs/UI/MainUI01Bar.prefab`
- `Assets/Prefabs/UI/GachaProbabilityPopup.prefab`

### 📂 Files to Modify
- `Assets/Prefabs/UI/WeaponInventoryPanel.prefab` (Item 1) — adjust `GridLayoutGroup` `cellSize.x` and/or container width so 4 columns fit. Verify `Constraint = FixedColumnCount, ConstraintCount = 4` matches the runtime override at `WeaponInventoryPanel.cs:53-58`.
- `Assets/Scripts/UI/WeaponInventoryPanel.cs` (Items 2 + 3)
  - Add internal close button (`[SerializeField] private Button closeButton`) wired to `SetVisible(false)`
  - Convert visual show/hide to slide-up animation matching `UpgradeDrawerView` (same `LeanTween`/coroutine pattern; or factor a shared `BottomDrawerAnimator` helper if simple enough)
  - Remove direct toggle from old HUD chat-toggle-style listener; subscribe to `MainUI01Coordinator` instead
- `Assets/Scripts/UI/UpgradeDrawerView.cs` (Items 2 + 3)
  - Add internal close button if not present
  - Remove existing HUD button binding (Coordinator owns it now)
- `Assets/Scripts/UI/GachaPanel.cs` (Items 2, 3, 4, 5)
  - Convert visual presentation to full-screen overlay (anchor stretched to canvas, opaque background)
  - Add internal close button
  - Remove `maxUpperGrade` text and pity counter UI
  - Add small probability button next to summon-level label, wired to `GachaProbabilityPopup.Show(currentLevel)`
- `Assets/Scripts/UI/HUDController.cs` (Item 2)
  - Remove individual upgrade-drawer / weapon-toggle / gacha-toggle button references; replace with single `MainUI01Bar` reference
  - HUD becomes a thin host that wires `MainUI01Coordinator`
- `Assets/Scripts/Weapons/GachaService.cs` (Item 4)
  - Delete pity-trigger branch in `PullOnce`; keep weight rolling + level-cap clamping
  - Add public `SummonLevelChanged` event (so popup auto-refresh works) if not present
  - Add `IReadOnlyList<WeaponGradeWeight> GetCurrentUpperGradeWeightsNormalized()` returning percentages summing to 1.0 (for popup)
- `Assets/Scripts/Weapons/GachaDefinition.cs` (Item 4)
  - Mark `pityThreshold` / `pityFloor` with `[Obsolete]` summary comment OR add `// Deprecated in Task V — kept for SO asset compat` comment
- `Assets/Scripts/Save/SaveBinder.cs` + `Assets/Scripts/Save/SaveData.cs` + `Assets/Scripts/Save/SaveDataDocument.cs` + `Assets/Scripts/Save/SaveDataMapper.cs` (Item 4)
  - **Do NOT remove** `pityCounter` field — keep field present, just unread by gameplay. No saveVersion bump.
- `Assets/Data/Gacha/Standard.asset` (Item 4)
  - `pityThreshold = 0` (or leave; field unused)
- `Assets/Scenes/MainScene.unity` (Item 2)
  - Add `MainUI01Bar` instance + `MainUI01Coordinator` GameObject
  - Re-parent `WeaponInventoryPanel` and `GachaPanel` under canvas as appropriate (popup vs full-screen layers)
  - Wire serialized references on `HUDController` / `MainUI01Coordinator`

### 🚫 Do Not Touch
- `WeaponDefinition` / `WeaponDatabase` data (Task R) — Item 1 is layout-only
- `WeaponInventory` count semantics (Task S)
- `WeaponFusionService` (Task T)
- Combat coefficients / stat unification (Task Q)
- Auth / presence / chat / stage flow
- `pityCounter` save field shape (Item 4 keeps field as-is)

### 🧪 Validation
1. Compile clean (0 error / 0 warning)
2. PlayMode → open weapon panel → all 5 rows show **4 visible slots** including Supreme column (Item 1)
3. Bottom-nav bar visible at MainScene cold start with 5 buttons; `추가예정4`/`5` are grayed out and unclickable (Item 2)
4. Click `강화` → upgrade drawer slides up; click `강화` again → closes; open `강화` then click `무기` → drawer closes, weapon popup opens (Items 2+3)
5. Each open panel shows its internal X button; clicking X closes panel and clears bottom-nav active state (Item 3)
6. `소환` panel covers full screen; opening it does NOT keep weapon panel visible underneath (Item 2-1)
7. Gacha panel does NOT show `최대 고급` or any pity counter UI (Item 4)
8. Force 30 consecutive Lv1 pulls with all-Common roll seed → no pity-floor override; results match raw weights (Item 4)
9. Existing v3 save with `pityCounter > 0` loads cleanly; field still serializes back unchanged (Item 4)
10. Click probability button → popup shows `Common: 70.0% / Normal: 25.0% / Advanced: 5.0%` for Lv1 (Item 5)
11. With popup open, force level-up to Lv2 via debug → popup auto-refreshes to Lv2 weights (Item 5)
12. Probability popup closes by X button AND by clicking the dimmed outside area (Item 5)
13. (Cross-feature regression) HUD chat toggle / boss-entry / auto-toggle / fire button still work after MainUI01Bar refactor (Item 2 doesn't break sibling HUD widgets)

### Implementation note
- `BottomDrawerAnimator` shared helper is optional but recommended — both `UpgradeDrawerView` and `WeaponInventoryPanel` use the same slide-up motion now; centralizing it (parametrized by target `RectTransform` and duration) avoids drift.
- For Item 5 outside-click dismiss: place a transparent stretched `Button` behind the popup content (z-order: backdrop < popup); its `onClick` calls `Hide()`. This is the standard Unity UGUI pattern — no need for raycast tricks.
- For Item 4 pity-keep-field rule: `SaveDataMapper` continues to copy `pityCounter` to/from `SaveDataDocument`; only the **read sites** (`GachaService.PullOnce`, `GachaPanel` UI) stop touching it. Search-grep for `pityCounter` to confirm cleanup scope.

---

# Bundle 7 Release Gate

When Tasks Q~U are all `✅ DONE`, run one integrated PlayMode session:

1. Delete local save and Firestore game document
2. Start from LoginScene → enter MainScene
3. Confirm HUD uses unified Attack and Combat Power
4. Confirm auto/manual/skill damage coefficients with visible enemy damage numbers
5. Buy Attack upgrade → combat power popup appears for about 1 second
6. Open weapon window → 5 rows × 4 columns, ordered by upper/lower grade
7. Use debug/MCP to grant `common_supreme_staff x3` → synthesize-all → receive `normal_beginner_staff x1`
8. Open gacha → Lv1 display, 10x pull costs 270 gems, results show counted duplicates
9. Force or simulate enough pulls to reach Lv2 and Lv3 → verify caps and UI labels
10. Equip the strongest owned weapon → tint/glyph/projectile/stat/combat power update once
11. Quit and restart → weapon counts, equipped weapon, summon level/progress, gems, pity, combat power all restored
12. (Cross-feature regression) Field stage Console emits `Presence write presence/{stage}/{uid}` log entries at ~5Hz — verifies Tasks_v6.md Task K wiring still intact after Bundle 7 stat/HUD/save churn
13. (Cross-feature regression) Open chat → send a World message → it renders once (single echo path) and appears in Firebase Console under `chat/world` — verifies Tasks_v6.md Task M
14. (Cross-feature regression) After fully exercising Bundle 7 features, quit → relaunch → `SyncCoordinator` pulls remote save and applies cleanly with no UI desync — verifies Tasks.md Task I cloud sync round-trip survived `saveVersion = 3` migration

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies:
1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — no unauthorized files modified (`git diff` to confirm)
3. **"Do Not Touch" areas unchanged**
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation matches the design intent
6. **Exactly one git commit** — message includes the task ID
7. **No regression in `Tasks.md` / `Tasks_v6.md` features** — sanity check core combat, save, login, gacha, weapon UI still function

Record review results in Appendix E.

---

## Appendix B — Bundle Gate Checklist

After Task U reaches `✅ DONE`:
1. Bundle 7 release regression tests pass
2. `git log` clean — every task Q~U has exactly one implementation commit
3. Unity Console 0 errors / 0 warnings
4. Save migration v2 → v3 tested with an existing v6 save file
5. Fresh save tested
6. No unauthorized edits to `Tasks.md` or `Tasks_v6.md`

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-08 | Planner | Document created. Bundle 7 split into Q (stat unification + combat power), R (upper/lower weapon grades + 20 seed weapons), S (counted inventory + 4-column weapon window), T (synthesize-all fusion), and U (summon level gacha). User decisions resolved: new grade names, explicit summon level probability table, synthesize-all UX. |
| 2026-05-08 | Planner | Added user requests #6~7. Task R now requires attack bonuses on every weapon, monotonic attack growth, and larger attack jumps on upper-grade transitions than lower-grade steps. Task S now requires weapon-slot selection to populate a fixed bottom detail area, with equip action enabled from that detail area only when valid. |
| 2026-05-08 | Planner | **Task V detailed spec rewrite.** Replaced 11-line stub with full 5-item development plan after user reported new follow-up issues (Supreme column missing, MainUI01 navigation, internal close buttons, pity removal, per-level probability popup). Resolved ambiguities via AskUserQuestion: (1) `추가예정4/5` → disabled-grayed buttons with `준비중` label; (2) same-tab re-click → close allowed alongside internal X button (toggle preserved); (3) pity → keep `pityCounter` save field, remove only logic & UI (no saveVersion bump); (4) probability popup → percent format + X button + outside-area click dismiss + auto-refresh on summon level change. Added `MainUI01Bar`, `MainUI01Coordinator`, `GachaProbabilityPopup`, `GachaProbabilityRowView` to Files-to-Add. Item 1 root cause hypothesized as `WeaponInventoryPanel.prefab` GridLayoutGroup cell overflow (code already enforces `constraintCount = 4` at runtime) — implementer must verify and adjust prefab. |
| 2026-05-08 | Planner | **Review pass 1 of Tasks_v7.md.** Verified user-added req #6 (cross-upper Δ > within-upper Δ) is satisfied by current Stat Seed Rule: Common Supreme→Normal Beginner = +14 vs max within-upper step +8. ✓ Verified req #7 covered by `WeaponDetailView` + slot selection event + equip-from-detail flow. ✓ **Edits applied**: (1) §0.5 stale-status row rewritten — v6 N/O/P actually committed (`b011581`/`bdce55e`/`0261300`), Tasks_v6.md status board only had docs lag. (2) Task Q popup format changed `Power: ...` → Korean `전투력: <current> (+<delta>)` (DoD + validation). (3) Task S Files-to-Add: appended `Assets/Scripts/Player/WeaponVisualController.cs` — verified missing in v6 codebase, required for "tint/glyph/projectile apply once" DoD bullet. (4) Task U pity floor `Advanced` → `Normal` (Lv1 cap was Advanced making pity a no-op; new floor makes Lv1 pity meaningful). (5) Bundle 7 Release Gate appended items 12 (presence 5Hz regression), 13 (chat single-echo regression), 14 (CloudSync round-trip after v3 migration). No new tasks. |

---

## Appendix D — Combined Work Log (implementer)

> Implementers append a row here on each task transition. Format: `YYYY-MM-DD | Task X | <one-line summary>`.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-08 | Task Q | Added unified `AttackDamage` runtime stat, legacy snapshot migration from v6 auto/manual damage fields, coefficient-based auto/manual/skill damage, unified Attack/Attack Speed upgrade defaults with legacy upgrade-id migration, `CombatPowerService`, Korean combat-power popup, HUD combat power display, and MainScene popup wiring. MCP PlayMode validation passed for fresh `Attack 10 / CP 26`, auto `10`, manual `20`, skill `80`, Attack upgrade purchase, popup `전투력: 30 (+5)`, legacy save attack migration, and attack snapshot capture. Start-state note: pre-existing uncommitted `.DS_Store`, `.codex/`, and `Assets/Fonts/NanumGothicBold SDF.asset` changes were left untouched. |
| 2026-05-08 | Task R | Added upper/lower weapon grade enums and Korean grade labels, converted WeaponStats/WeaponDefinition/WeaponDatabase/WeaponStatComposer to the v7 grade ladder, regenerated weapon glyph/projectile generator support for 20 weapons, replaced the 6 v6 seed WeaponDefinition assets with 20 ladder assets, and wired WeaponDatabase in ladder order. MCP validation passed: `OrderedWeapons.Count=20`, every upper grade has 4 weapons, `GetNext(common_supreme_staff)=normal_beginner_staff`, `GetNext(unique_supreme_staff)=null`, starter attack bonus `0`, attack bonuses strictly increase, and all upper-grade transition jumps exceed lower-grade row steps. |
| 2026-05-08 | Task S | Added counted weapon ownership (`OwnedWeaponEntry`), bumped save schema to v3 with v2 weapon-id migration, updated Firestore mapper/binder, rebuilt WeaponInventory around counts, added slot count labels, fixed 4-column weapon grid, added bottom `WeaponDetailView`, and moved equip action to the detail panel. MCP PlayMode validation passed for fresh `common_beginner_staff x1`, duplicate add to `x3`, save capture count `3`, v2 migration (`wand_starter`, duplicated `apprentice_staff`, `arcane_scepter`) into counted v3 entries, 20 slots with 4 columns, unowned detail `x0` with equip disabled, owned detail with equip enabled, and `normal_beginner_staff` equip applying Attack 42 exactly once. |
| 2026-05-08 | Task T | Added `WeaponFusionService`, synthesize-all UI button, lightweight Korean fusion summary popup, batched inventory count mutation, equipped-weapon fallback validation, and MainScene/HUD/GameContext wiring. MCP PlayMode validation passed for `common_beginner_staff x9 → common_upper_staff x1`, `common_supreme_staff x3 → normal_beginner_staff x1`, `unique_supreme_staff x3` remaining unchanged, consumed equipped weapon auto-equipping the highest owned replacement, and save capture preserving synthesized counts. |
| 2026-05-08 | Task U | Replaced rarity-weight gacha with summon-level upper-grade weights, uniform lower-grade rolls, Normal pity floor, summon progress leveling/carry-over, summon state save fields, gacha UI status text, ladder-index result sorting, and seeded `Standard` summon levels. MCP validation passed for fresh `Lv1/0`, Lv1 cap Advanced, 20-pull Lv2 transition, Lv2 49/50 pity pull yielding Normal+ within Epic cap, 10x cost `270`, save mapper preserving `summonLevel`, `summonPullsInLevel`, `pityCounter`, and MainScene PlayMode initialization without game-code errors. |
| 2026-05-08 | Task V | Added `메인UI01` bottom navigation, coordinator-managed mutual exclusion, internal close buttons, bottom-drawer weapon/upgrade presentation, full-screen summon overlay, probability popup/popup rows, and removed active pity UI/logic while preserving `pityCounter` save round-trip. Item 1 root cause was layout overflow: weapon grid was forced to 4 columns in code, but the panel/prefab layout could clip the Supreme column; fixed by widening the drawer and setting WeaponInventoryPanel grid to `cellSize=(150,136)`, `spacing=(10,10)`, `constraintCount=4`. MCP checks passed through the functional paths; user manually confirmed the runtime UI looked correct and approved skipping further verification to finish. |

---

## Appendix E — Combined Review Log (reviewer)

> Reviewers append a row here after each code review. Format: `YYYY-MM-DD | Task X | <verdict + key findings>`.

| Date | Task | Entry |
|------|------|-------|
