# Wizard Grower — Tasks v8 (Bundle 8: Gacha Polish + Skills + Achievements + Attendance)

> Follow-up work track to `Tasks_v7.md`.
> Different agents, different bundle, different file.
> **Do NOT edit `Tasks.md` / `Tasks_v6.md` / `Tasks_v7.md` from this track.** Read-only access only.
>
> The **Planner / Reviewer (Claude)** is the sole editor of this document.
> Implementers read this document, modify the code, and append to Appendix D.

---

## 0. Common Work Rules

### 0.1 Basic Rules
1. **Only one task at a time.** State the task ID (e.g. `Task W`) at start and end.
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
Bundle 8 is sequential. Do not start the next task until the previous task is `✅ DONE`.

**Gate pass conditions:**
1. All tasks W~AA are `✅ DONE`
2. Unity Console has 0 errors / 0 warnings
3. Bundle 8 release regression tests pass
4. Save migration from v3 to v4 preserves existing v7 progress (weapons, gems, summon level, equipped weapon)

### 0.3 Auto-Progression Restrictions (Implementation Agent)
- Do not start another Bundle 8 task on your own.
- On signature/spec conflict, **do not decide unilaterally** — record in Appendix D and wait for the reviewer.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason.
- Do not introduce new major systems or files not listed in this document unless required for compilation; log exceptions and await reviewer approval.

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>`.
- This repo uses GitHub Desktop with origin set. **Agents must not push** — push is the user's responsibility.
- At task start, run `git status` to confirm working tree state. Note any uncommitted changes in Appendix D.
- Per task = per commit. `git revert` is the rollback tool.

### 0.5 Cross-Track Coordination ⚠️
Bundle 8 builds on the v7 baseline (Tasks Q~V).

| Risk | Mitigation |
|---|---|
| `Tasks_v7.md` Task V status is `🟡 IN REVIEW` at Bundle 8 start | Treat the v7 codebase as baseline. Bundle 8 must NOT modify the meta-tasks defined in v7 (MainUI01Bar, GachaProbabilityPopup, etc.) except where explicitly listed. If a v7 file needs structural change, log it in Appendix D and await reviewer. |
| Save schema changes overlap | Task X / Z / AA all touch `SaveData`. Tasks land sequentially (W→X→Y→Z→AA); each task that bumps schema must include migration-from-v3 logic. **Final saveVersion after Bundle 8 = 4.** |
| MainUI01 nav slot reuse | v7 created `추가예정4` / `추가예정5` reserved slots. Bundle 8 Task Y replaces `추가예정4` with `스킬`. `추가예정5` remains reserved. |
| Achievement / Attendance entry points | Per user spec, these go BELOW the Auto-toggle button (not in MainUI01). Do not add them as MainUI01 nav buttons. |

---

## 1. Task Dependency Graph

```
Bundle 8
W → X → Y → Z → AA → Bundle 8 Release Gate

W:  Gacha 30-pull + result modal fix + pricing update
X:  Active skill system (3 skills + ScriptableObject + runtime cast pipeline)
Y:  Skill bar HUD + 메인UI01 스킬 탭 + slot equip flow
Z:  Achievements (daily / repeat missions)
AA: Daily attendance (10-day cycle)
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 8 | W | Gacha 30-Pull + 9/10 Visibility Fix + Pricing Update | 🟡 IN REVIEW | v7 baseline |
| 8 | X | Active Skill System (3 Skills + Cast Pipeline) | 🔴 TODO | v7 baseline |
| 8 | Y | Skill Bar HUD + 메인UI01 스킬 탭 + Equip Flow | 🔴 TODO | X ✅ |
| 8 | Z | Achievements (Daily + Repeat Missions) | 🔴 TODO | v7 baseline |
| 8 | AA | Daily Attendance (10-Day Cycle) | 🔴 TODO | Z ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 8 — Gacha Polish + Skills + Achievements + Attendance

**Goal:** wrap up the v7 weapon/gacha polish issues and add three new player-progression systems:
- Active skills (3 skills, equippable, auto-cast in Auto mode)
- Daily / repeat achievement missions
- Daily attendance check-in (10-day cycle, server-time based)

### Bundle 8 Release Regression Tests
1. Gacha 1×/10×/30× buttons all visible; cost shows `100 / 1,000 / 3,000` gems
2. 30-pull deducts exactly 3,000 gems and produces exactly 30 weapons in the result modal
3. 10-pull result modal shows all 10 results without any cut off (Item #1 fix verified)
4. Skill bar (1 row × 5 slots) renders below mana bar; equipped skills show icon, empty slots show placeholder
5. 3 active skills exist as ScriptableObjects: `메테오`, `콜드빔`, `돌진`. Each has unique mana cost / cooldown / damage / VFX
6. Auto mode ON + idle + sufficient mana → equipped skills auto-cast in slot order, respecting per-skill cooldown + mana cost
7. Achievement button (small) sits below Auto-toggle. Click opens achievement panel as bottom slide-up popup (메인UI01 sub-panel rules)
8. Achievement panel has `일일미션 / 반복미션` sub-tabs; only one tab's missions visible at a time
9. Daily missions reset on Firestore server timestamp KST 00:00 boundary
10. Repeat mission counter increments linearly: clearing `몬스터 100` next target becomes `몬스터 200` then `300`, persisted permanently
11. Attendance button (small) sits below Achievement button. Click opens attendance panel as bottom slide-up popup
12. Attendance panel shows 2 rows × 5 columns (10 days). Buttons unlock by accumulated check-in count
13. After Day 10 reward claimed, next-day check-in cycles back to Day 1 (re-collectable rewards)
14. saveVersion bumped to 4; v3 saves load cleanly with default skill/achievement/attendance fields filled
15. (Cross-feature regression) v7 features still work: weapon equip, synthesize-all, summon level/probability popup, MainUI01Bar tabs

---

## Task W — Gacha 30-Pull + Result Modal Fix + Pricing Update

**Status:** 🟡 IN REVIEW
**Depends On:** v7 baseline

### 🎯 Goal
1. Investigate and fix the 10-pull modal showing only 9 results
2. Add 30-pull button alongside existing 1× / 10×
3. Update gacha pricing to flat 100 gems per pull (1×=100, 10×=1,000, 30×=3,000)

### ✅ Definition of Done

**Item 1 — 10-pull visibility fix**
- [ ] Root cause identified and documented in Appendix D (most likely: `GachaResultPanel` grid layout cell count, ScrollRect content size, or layout group mismatch)
- [ ] 10-pull modal shows all 10 result cards
- [ ] Same layout works for 30-pull (next item) without horizontal/vertical clipping

**Item 2 — 30-pull**
- [ ] `GachaPanel` shows three buttons: `1회 100`, `10회 1,000`, `30회 3,000`
- [ ] 30-pull button calls `GachaService.PullThirty()` (new method) → consumes 3,000 gems → returns 30 results
- [ ] If gems insufficient → button disabled with feedback (same pattern as 1×/10×)
- [ ] Result modal handles 30 cards (consider scrollable container OR 6×5 grid)
- [ ] Summon level progress increments by 30 (level-up may chain mid-batch; resolve in single pass)

**Item 3 — Pricing**
- [ ] `GachaDefinition.costSingle = 100`, `costTen = 1000`, new `costThirty = 3000`
- [ ] HUD gem balance reflects new costs
- [ ] First-launch initial gem grant remains `300` (covers 3 pulls now; no Bundle 8 change to grant)
- [ ] All UI labels updated to new prices

### 📂 Files to Add
- (None expected; all changes are additive to existing v7 weapon/gacha files)

### 📂 Files to Modify
- `Assets/Scripts/Weapons/GachaService.cs`
  ```csharp
  public IReadOnlyList<WeaponDefinition> PullThirty();   // new
  // PullOnce / PullTen unchanged in shape
  ```
- `Assets/Scripts/Weapons/GachaDefinition.cs`
  ```csharp
  public int costSingle = 100;   // was 30
  public int costTen = 1000;     // was 270
  public int costThirty = 3000;  // new
  ```
- `Assets/Scripts/UI/GachaPanel.cs`
  - Add 30-pull button + label `30회 3,000`
  - Update existing labels to `1회 100`, `10회 1,000`
- `Assets/Scripts/UI/GachaResultPanel.cs`
  - Investigate root cause of 9/10 visibility bug; fix layout
  - Support 30-card rendering. **Recommended:** if current layout is 5×2 grid, change to vertical-scroll list or 5×6 grid for 30 mode. Keep one shared layout that adapts to result count.
- `Assets/Prefabs/UI/GachaResultPanel.prefab` (if layout changes require prefab edit)
- `Assets/Data/Gacha/Standard.asset` — update cost fields

### 🚫 Do Not Touch
- Summon level rolling logic (Task U / V owns this)
- Pity field handling (Task V kept field-but-no-logic; preserve that)
- WeaponInventory / fusion (v7 Task S/T)
- Probability popup (v7 Task V)

### 🧪 Validation
1. Compile clean
2. PlayMode → open gacha panel → 3 buttons visible with correct prices
3. With `gems = 100` → 1× pull succeeds, gems → 0
4. With `gems = 1000` → 10× pull succeeds; modal shows 10 cards (Item 1 fix verified)
5. With `gems = 3000` → 30× pull succeeds; modal shows 30 cards
6. With `gems = 999` → 10× button disabled with `젬이 부족합니다` feedback
7. 30-pull from Lv1 with summonPullsInLevel=0 → after pull summonPullsInLevel = 30 (or rolled into Lv2 if threshold 20 crossed)
8. Save + restart → gem count and summon progress preserved
9. (Cross-feature) Probability popup still shows correct percentages after pricing change

### Implementation note
For Item 1 root cause: most likely `GachaResultPanel` uses a `GridLayoutGroup` with `constraintCount = 5` and visible cell area only fits 9 (similar bug to Task V Item 1). Verify by counting `Instantiate` calls vs visible cards. If grid container `RectTransform` height is fixed at ~9 cell heights, expand the container or wrap in `ScrollRect`.

---

## Task X — Active Skill System (3 Skills + Cast Pipeline)

**Status:** 🔴 TODO
**Depends On:** v7 baseline

### 🎯 Goal
Replace the single `ActiveSkillController` with a multi-skill framework. Define 3 ScriptableObject skills with distinct mechanics. Skills are equippable into a 5-slot bar (UI in Task Y).

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SkillDefinition` ScriptableObject with metadata + cast strategy
- [ ] `SkillDatabase` lists all available skills; supports `GetById` / `OrderedSkills`
- [ ] 3 seed skills authored: `meteor`, `cold_beam`, `charge` with planner-proposed values (see seed table)
- [ ] `SkillRuntime` per-skill cooldown + mana cost gate
- [ ] `SkillCastOrchestrator` — Auto mode iterates equipped skills in slot order, casts first ready skill that has mana
- [ ] VFX rendered via Unity Particle System per skill
- [ ] save migration v3 → v4: add `equippedSkillIds: List<string>` (fixed length 5, empty strings for unequipped) and `ownedSkillIds: List<string>`. Existing players: all 3 seed skills auto-granted as `owned`, slot 0 = `meteor`, slots 1-4 = empty by default
- [ ] `saveVersion = 4`

### 📂 Files to Add
- `Assets/Scripts/Skills/SkillId.cs` — string-id constants
- `Assets/Scripts/Skills/SkillDefinition.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Skill Definition")]
  public class SkillDefinition : ScriptableObject
  {
      public string skillId;            // "meteor", "cold_beam", "charge"
      public string displayName;        // 한글 표시명 ("메테오")
      public Sprite icon;
      public float manaCost;
      public float cooldownSeconds;
      public float damageCoefficient;   // multiplied by player's AttackDamage
      public float aoeRadius;           // 0 = single target
      public SkillTargetingMode targeting;  // NearestEnemy, SelfAround, etc.
      public ParticleSystem castVfxPrefab;
      public ParticleSystem impactVfxPrefab;
      [TextArea] public string flavorText;
  }

  public enum SkillTargetingMode
  {
      NearestEnemy,
      DashToNearestEnemy,
      SelfRadius
  }
  ```
- `Assets/Scripts/Skills/SkillDatabase.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Skill Database")]
  public class SkillDatabase : ScriptableObject
  {
      public SkillDefinition[] skills;
      public SkillDefinition GetById(string id);
      public IReadOnlyList<SkillDefinition> OrderedSkills { get; }   // sorted by skillId or array order
  }
  ```
- `Assets/Scripts/Skills/SkillRuntime.cs` — per-skill state holder
  ```csharp
  public class SkillRuntime
  {
      public SkillDefinition Definition { get; }
      public float CooldownRemaining { get; }
      public bool IsReady(PlayerMana mana);
      public void TryCast(PlayerWizard wizard, EnemySpawner spawner, ProjectileFactory factory, CombatCalculator calculator, PlayerMana mana);
      public event Action<float> CooldownChanged;   // fires per frame; UI subscribes
  }
  ```
- `Assets/Scripts/Skills/SkillCastOrchestrator.cs`
  ```csharp
  public class SkillCastOrchestrator : MonoBehaviour
  {
      // Holds 5 SkillRuntime slots (some may be null = empty).
      public void Initialize(SkillDatabase database, PlayerWizard wizard, ...);
      public void EquipSkill(int slotIndex, string skillId);  // -1 / empty to clear
      public string GetEquippedSkillId(int slotIndex);
      public SkillRuntime GetSlot(int slotIndex);
      public event Action<int, SkillDefinition> SlotChanged;

      // Auto-cast loop: called every Update by GameManager when AutoMode is ON and player is idle.
      public void TickAutoCast(PlayerMana mana);

      // Manual cast: skill bar button click invokes by slot index.
      public bool TryManualCast(int slotIndex);
  }
  ```
- `Assets/Data/Skills/Meteor.asset` (Seed values below)
- `Assets/Data/Skills/ColdBeam.asset`
- `Assets/Data/Skills/Charge.asset`
- `Assets/Data/Skills/SkillDatabase.asset`
- `Assets/VFX/Skills/Meteor_*.prefab` (Particle System prefabs — castVfx + impactVfx)
- `Assets/VFX/Skills/ColdBeam_*.prefab`
- `Assets/VFX/Skills/Charge_*.prefab`

### Seed Skill Values (Planner Proposed)

| Skill | manaCost | cooldown | damageCoef | aoeRadius | targeting | VFX hint |
|---|--:|--:|--:|--:|---|---|
| `meteor` (메테오) | 60 | 12s | 12.0 | 3.0 | NearestEnemy | Falling rock + impact crater (red/orange particle, 0.6s lifetime) |
| `cold_beam` (콜드빔) | 30 | 6s | 5.0 | 0 (single) | NearestEnemy | Blue ice stream, line shape, 0.4s |
| `charge` (돌진) | 25 | 5s | 6.0 | 0.5 (contact AOE on impact) | DashToNearestEnemy | Motion blur trail (white/cyan) + impact burst |

> Damage coefficients multiply `PlayerStats.AttackDamage`. With base atk 10, meteor = 120 dmg, cold_beam = 50, charge = 60. Tunable post-launch.
> The legacy `ActiveSkillController` (v6) was 8x — `meteor` (12x) is the new strongest skill, `cold_beam` and `charge` are weaker but cheaper / more frequent.

### 📂 Files to Modify
- `Assets/Scripts/Combat/ActiveSkillController.cs` — **deprecate** (mark `[Obsolete]` or remove if unused after wiring `SkillCastOrchestrator`). v6 single-skill button is replaced by skill bar in Task Y.
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  public int saveVersion = 4;
  public List<string> ownedSkillIds = new List<string>();
  public List<string> equippedSkillSlots = new List<string>(new string[5]);  // length 5, empty string = unequipped
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — mirror new fields
- `Assets/Scripts/Save/SaveService.cs` — v3 → v4 migration: grant all 3 seed skills as owned, slot 0 = `meteor`, slots 1~4 empty
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply skill state
- `Assets/Scripts/Core/GameContext.cs` — register `SkillDatabase` + `SkillCastOrchestrator` references
- `Assets/Scripts/Core/GameManager.cs` — initialize orchestrator after PlayerStats / Mana / EnemySpawner ready; in Update, when AutoMode is ON and joystick released, call `orchestrator.TickAutoCast(mana)`

### 🚫 Do Not Touch
- Existing auto-attack / manual-fire combat (they still fire from `AttackDamage` × coefficient as Task Q established)
- Weapon system (v7 Q~T)
- MainUI01Bar (Task V)
- Achievement / Attendance flows (Tasks Z / AA)

### 🧪 Validation
1. Compile clean
2. Fresh save → owned skill count = 3, slot 0 = meteor, slots 1~4 empty
3. PlayMode → with mana ≥ 60 + Auto ON + idle → meteor casts at nearest enemy, particle plays, mana → 0~? (depends on regen)
4. Cooldown 12s elapses without re-cast for meteor; other slots empty so no cascades
5. Equip cold_beam to slot 1 via `orchestrator.EquipSkill(1, "cold_beam")` → both meteor and cold_beam alternate in cast order respecting cooldowns and mana
6. Equip charge to slot 2 → wizard physically dashes to target on cast (transform.position lerp over 0.2s)
7. Save + restart → equipped slots restored exactly
8. Migration: load v3 save (no skill fields) → all 3 seeds auto-granted, slot 0 = meteor

### Implementation note
**Charge dash mechanic:** simplest implementation — on cast, lerp `wizard.transform.position` toward target over 0.2s; on arrival, deal damage + spawn impact VFX + return control. Disable joystick input during the lerp window.

**VFX prefab hierarchy:** each skill uses 2 prefabs — `castVfxPrefab` (spawned at wizard for meteor's "falling" indicator, or at wizard for charge trail) and `impactVfxPrefab` (spawned at impact location). Both should auto-destroy after their `ParticleSystem.main.duration`. Use `ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear)` + `Destroy(gameObject, mainDuration)`.

**Auto-cast tick policy:** to avoid all 3 skills firing on the same frame, `TickAutoCast` casts at most one skill per call. Iterates slot 0→4, casts the first that's ready + has mana, then returns. Next frame → next ready slot.

---

## Task Y — Skill Bar HUD + 메인UI01 스킬 탭 + Equip Flow

**Status:** 🔴 TODO
**Depends On:** X ✅

### 🎯 Goal
Surface the skill system in two UI places:
1. **Skill bar** (1×5 slots) anchored below mana bar — read-only display of equipped skills, cooldown indicator, manual-cast tap
2. **메인UI01 스킬 탭** — replace `추가예정4` slot. Inside the tab: skill list + equip flow ("스킬 장착" button → choose target slot 1~5)

### ✅ Definition of Done

**Skill Bar (HUD)**
- [ ] `SkillBarView` anchored beneath mana bar, 1 row × 5 columns, fixed cell size
- [ ] Each slot shows skill icon (or placeholder for empty), cooldown radial-fill overlay, mana-insufficient gray-out
- [ ] Tap a ready slot → manual cast (works regardless of Auto state)
- [ ] Tap an empty slot → no-op (no error)
- [ ] Updates in real time as `SkillCastOrchestrator.SlotChanged` and per-skill `CooldownChanged` events fire

**메인UI01 스킬 탭**
- [ ] `MainUI01Bar.reserved4Button` is replaced/repurposed as `스킬` button (label `스킬`, enabled, normal color)
- [ ] Click → `SkillTabPanel` opens as bottom slide-up popup (same animation as 강화/무기)
- [ ] Panel lists all owned skills (currently 3) as scrollable cards
- [ ] Each card: icon, displayName, manaCost, cooldown, damage, flavor text, **장착 버튼**
- [ ] Click 장착 → `SkillSlotPicker` modal opens within the skill tab showing 5 slot buttons with current contents
- [ ] Click a slot button in the picker → assigns skill to that slot, dismisses picker, refreshes card list (skill now shows "슬롯 N 장착됨")
- [ ] Each card also has an **해제 버튼** to remove the skill from its current slot (no-op if not equipped)
- [ ] All UI follows v7 메인UI01 sub-tab UI rules (internal X close button, same-tab re-click closes, mutual exclusion with other sub-panels)

### 📂 Files to Add
- `Assets/Scripts/UI/SkillBarView.cs`
- `Assets/Scripts/UI/SkillBarSlotView.cs` — single slot display with cooldown radial
- `Assets/Scripts/UI/SkillTabPanel.cs` — bottom slide-up popup, lists owned skills
- `Assets/Scripts/UI/SkillCardView.cs` — single card in the list, hosts 장착/해제 buttons
- `Assets/Scripts/UI/SkillSlotPicker.cs` — modal to choose slot 1~5
- `Assets/Prefabs/UI/SkillBar.prefab`
- `Assets/Prefabs/UI/SkillBarSlot.prefab`
- `Assets/Prefabs/UI/SkillTabPanel.prefab`
- `Assets/Prefabs/UI/SkillCard.prefab`
- `Assets/Prefabs/UI/SkillSlotPicker.prefab`

### 📂 Files to Modify
- `Assets/Scripts/UI/MainUI01Bar.cs` (v7 Task V) — change `reserved4Button` to `skillButton` (rename serialized field; or add new field while leaving reserved4 obsolete with `[Obsolete]` marker)
- `Assets/Scripts/UI/MainUI01Coordinator.cs` (v7 Task V) — add Skill tab to mutual-exclusion set
- `Assets/Scripts/UI/HUDController.cs` — wire `SkillBarView` reference + initialize with `SkillCastOrchestrator`
- `Assets/Scenes/MainScene.unity` — add SkillBar + SkillTabPanel GameObjects, wire serialized references

### 🚫 Do Not Touch
- `SkillDefinition` / `SkillDatabase` / `SkillCastOrchestrator` (Task X) — Task Y only consumes them
- Mana bar / health bar / chat / etc.
- Achievement / Attendance flows (Tasks Z / AA)

### 🧪 Validation
1. Compile clean
2. PlayMode → skill bar visible below mana bar; slot 0 shows meteor icon; slots 1~4 show empty placeholder
3. Click meteor slot → manual cast, particle plays, cooldown radial animates
4. Open 메인UI01 → `스킬` button visible (replaced reserved4); click → skill tab slides up
5. Skill tab shows 3 cards: meteor (slot 0 장착됨), cold_beam (장착 가능), charge (장착 가능)
6. Click cold_beam 장착 → slot picker shows 5 slots → click slot 1 → cold_beam now in slot 1, picker closes, skill bar slot 1 updates
7. Click cold_beam 해제 → slot 1 empty again
8. Re-equip cold_beam to slot 0 → meteor moves out, slot 0 = cold_beam (resolve overwrite by clearing meteor's slot first; meteor card shows "장착 가능" again)
9. X button on skill tab closes; clicking 스킬 button again on MainUI01Bar also closes (toggle)
10. Save + restart → equipped slots and skill bar both restored

### Implementation note
**Slot overwrite policy:** when a player equips a skill into a slot that's already occupied, the previous skill is automatically unequipped (its slot becomes empty until that skill is re-equipped elsewhere). This keeps the UX simple: "장착 = 새 슬롯에 배치, 다른 자리에 있던 같은 스킬은 자동 이동".

**Cooldown radial:** use a simple `Image.fillAmount = 1 - (cooldownRemaining / cooldown)` with `Image.Type = Filled`, `FillMethod = Radial360`, `FillOrigin = Top`. Update from `SkillRuntime.CooldownChanged` event.

---

## Task Z — Achievements (Daily + Repeat Missions)

**Status:** 🔴 TODO
**Depends On:** v7 baseline

### 🎯 Goal
Add an achievement system with 2 sub-tabs:
- **일일미션** — completable once per day, resets at server-time KST 00:00
- **반복미션** — repeatable with linearly-scaling counters (e.g. monsters 100 → 200 → 300 → ...)

### ✅ Definition of Done

**Entry point**
- [ ] Small `업적` button anchored just below the existing `Auto` toggle button (NOT in MainUI01)
- [ ] Click → `AchievementPanel` opens as bottom slide-up popup (same UI rules as MainUI01 sub-panels — internal X, mutual exclusion managed by a new lightweight controller since this isn't part of MainUI01)

**Panel structure**
- [ ] Two sub-tab buttons at top: `일일미션 / 반복미션` — only one active at a time
- [ ] Active sub-tab's missions render in the body. Inactive sub-tab's missions are not visible.
- [ ] Each mission row: `<설명> [<진행>/<목표>]` text + Slider + 보상 버튼
- [ ] 보상 버튼: gray + disabled until mission complete; turns blue + enabled on completion; click → grants reward + auto-removes mission from list (daily) or advances to next tier (repeat)

**Daily missions**
- [ ] 3 seed daily missions (planner-proposed):
  - `kill_100_monsters_daily` — `몬스터 100마리 처치` → gem 50
  - `clear_boss_once_daily` — `보스 1회 클리어` → gem 100
  - `gacha_once_daily` — `가챠 1회 사용` → gem 30
- [ ] Each daily mission tracks `claimed: bool` + `progress: int` + `lastResetUtcMs: long`
- [ ] Reset logic: at app start AND on cloud-sync resume, if `Firestore serverTimestamp` (KST) is on a later day than `lastResetUtcMs`, reset all daily missions

**Repeat missions**
- [ ] 3 seed repeat missions (planner-proposed):
  - `kill_monsters_repeat` — `몬스터 N마리 누적 처치` → gem 30 each tier; **delta = 100** (next target = current + 100)
  - `earn_gold_repeat` — `골드 N 누적 획득` → gem 30 each; delta = 10000
  - `synthesize_weapon_repeat` — `무기 N회 합성` → gem 50 each; delta = 1
- [ ] Each repeat mission tracks `currentTargetN: int` + `runningCounter: int`
- [ ] On reward claim: `runningCounter` does NOT reset; `currentTargetN += delta`. Counter is permanent.

**Persistence**
- [ ] saveVersion bump (still v4; Task X already bumped — Task Z adds fields under v4)
- [ ] Save fields:
  ```csharp
  public List<DailyMissionState> dailyMissions = new();
  public List<RepeatMissionState> repeatMissions = new();
  ```
- [ ] Cloud sync round-trips both lists
- [ ] Migration from saves before Task Z: empty lists → seed all default missions on first load post-update

### 📂 Files to Add
- `Assets/Scripts/Missions/MissionDefinition.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Mission Definition")]
  public class MissionDefinition : ScriptableObject
  {
      public string missionId;
      public string descriptionKo;        // "몬스터 {0}마리 처치"
      public MissionType type;            // Daily / Repeat
      public MissionTracker tracker;      // KillMonsters / ClearBoss / EarnGold / GachaPull / SynthesizeWeapon
      public int initialTargetCount;
      public int repeatDelta;             // 0 for daily; >0 for repeat
      public RewardKind rewardKind;       // Gem
      public int rewardAmount;
  }

  public enum MissionType { Daily, Repeat }
  public enum MissionTracker { KillMonsters, ClearBoss, EarnGold, GachaPull, SynthesizeWeapon }
  public enum RewardKind { Gem }
  ```
- `Assets/Scripts/Missions/MissionDatabase.cs`
- `Assets/Scripts/Missions/MissionService.cs` — owns runtime state, subscribes to gameplay events (EnemyKilled, GoldChanged, BossCleared, GachaPull, FusionCompleted), increments mission progress, fires `MissionCompleted` event
- `Assets/Scripts/Missions/DailyMissionState.cs` (Serializable POCO)
- `Assets/Scripts/Missions/RepeatMissionState.cs` (Serializable POCO)
- `Assets/Scripts/Missions/MissionResetService.cs` — pulls Firestore serverTimestamp, computes KST date boundary, fires `DailyResetTriggered` if a new day has begun
- `Assets/Scripts/UI/AchievementButton.cs` — small HUD button below Auto toggle
- `Assets/Scripts/UI/AchievementPanel.cs`
- `Assets/Scripts/UI/MissionRowView.cs` (one row per mission)
- `Assets/Scripts/UI/AchievementSubTabBar.cs` (Daily / Repeat toggle)
- `Assets/Prefabs/UI/AchievementPanel.prefab`
- `Assets/Prefabs/UI/MissionRow.prefab`
- `Assets/Data/Missions/Daily_Kill100Monsters.asset`
- `Assets/Data/Missions/Daily_ClearBoss.asset`
- `Assets/Data/Missions/Daily_Gacha1.asset`
- `Assets/Data/Missions/Repeat_KillMonsters.asset`
- `Assets/Data/Missions/Repeat_EarnGold.asset`
- `Assets/Data/Missions/Repeat_SynthesizeWeapon.asset`
- `Assets/Data/Missions/MissionDatabase.asset`

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — add `dailyMissions` + `repeatMissions` lists
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — mirror
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply
- `Assets/Scripts/Core/GameContext.cs` + `GameManager.cs` — register MissionService, MissionResetService, AchievementPanel; wire gameplay event subscriptions
- `Assets/Scripts/UI/HUDController.cs` — anchor `AchievementButton` below Auto toggle; bind click to open AchievementPanel
- `Assets/Scripts/Economy/CurrencyWallet.cs` — ensure `AddGems(int)` is callable from MissionService (already public per v6)

### 🚫 Do Not Touch
- MainUI01Bar (v7) — Achievement is NOT a MainUI01 sub-tab
- Skill / Gacha / Weapon systems
- Auth / Presence / Chat

### 🧪 Validation
1. Compile clean
2. Fresh save → AchievementPanel shows 3 daily missions (progress 0) + 3 repeat missions (initial targets)
3. Kill 1 monster → both `kill_100_monsters_daily` and `kill_monsters_repeat` progress increment
4. Kill 100 monsters → daily mission claim button turns blue; click → gem +50, daily mission disappears from list
5. Kill 100 more → repeat mission claim button turns blue; click → gem +30, target advances to 200, runningCounter remains 200 (NOT reset)
6. Continue to 200, 300, 400 — counter never resets, target keeps advancing by +100
7. Save + restart → all mission states preserved, including running counter
8. Daily reset: simulate by setting `lastResetUtcMs` to 2 days ago in save → restart → daily missions reset, repeat missions untouched
9. Cross-feature: gacha pull → `gacha_once_daily` increments; boss clear → `clear_boss_once_daily` increments
10. (Cross-feature regression) MainUI01 / weapon / skill tabs still work after AchievementPanel addition

### Implementation note
**Reset detection:** `MissionResetService.OnAppStart` reads Firestore `serverTimestamp()` (one request). Compare `lastResetUtcMs` (saved) with current server epoch ms, both converted to KST date (`DateTime.UtcNow + 9h`). If date differs → reset all daily missions. The Firestore call cost is negligible (1 read per app start).

**Anti-cheat:** because daily reset uses Firestore server time (not device clock), changing device date does not trigger a reset. Offline play falls back to last known server timestamp + elapsed `Time.realtimeSinceStartup` — accept up to 1 hour drift; longer offline triggers a "재접속 필요" prompt before any daily reset. This is good-enough for a prototype.

**Mission tracker subscription:** `MissionService.Initialize` subscribes to:
- `EnemySpawner.EnemyKilled` → KillMonsters trackers
- `BossStageController.BossCleared` (or equivalent) → ClearBoss trackers
- `CurrencyWallet.GoldGained` (must add this event if missing — it's small) → EarnGold trackers
- `GachaService.PullCompleted` → GachaPull trackers
- `WeaponFusionService.FusionCompleted` → SynthesizeWeapon trackers

---

## Task AA — Daily Attendance (10-Day Cycle)

**Status:** 🔴 TODO
**Depends On:** Z ✅

### 🎯 Goal
Add a daily attendance check-in system. Player checks in once per server-time day; after 10 check-ins, cycle resets to Day 1 next attendance.

### ✅ Definition of Done

**Entry point**
- [ ] Small `출석` button anchored just below the `업적` button from Task Z
- [ ] Click → `AttendancePanel` opens as bottom slide-up popup (same UI rules as Achievement)

**Panel structure**
- [ ] 2 rows × 5 columns (10 day cells)
- [ ] Each cell: day index label (1~10) + reward icon/label (gem 100 by default)
- [ ] Cell visual states:
  - Already-claimed days: dim + checkmark
  - Today's claimable day: highlighted + tappable
  - Future days: locked (grayed)
- [ ] Tap today's cell → grants reward + marks claimed + advances `currentDayIndex` (1→2, ..., 10 → cycle to 1)
- [ ] Only ONE check-in per server-day (Firestore serverTimestamp-based, same logic as Task Z reset)

**Reward configuration**
- [ ] Default each day's reward = `gem 100` (all 10 days same)
- [ ] Designer can edit per-day reward via `AttendanceConfig.asset` Inspector (rewardKind + rewardAmount per day index)
- [ ] Future-extensible: different RewardKind options would slot in cleanly (gold, weapon, etc.) — **but Bundle 8 ships gem-only**

**Cycle behavior**
- [ ] After Day 10 claim, on the next server-day, attendance resets to Day 1 (all 10 cells become claimable-future-locked, with Day 1 highlighted as today's)
- [ ] No "30-day megabox" — Bundle 8 ships pure 10-day cycle

**Persistence**
- [ ] Save fields:
  ```csharp
  public AttendanceState attendance = new();   // {currentDayIndex, lastClaimedUtcMs, totalCheckIns}
  ```
- [ ] Cloud sync round-trips state
- [ ] Migration from saves before Task AA: state defaults to {currentDayIndex=1, lastClaimedUtcMs=0, totalCheckIns=0}

### 📂 Files to Add
- `Assets/Scripts/Attendance/AttendanceConfig.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Attendance Config")]
  public class AttendanceConfig : ScriptableObject
  {
      public AttendanceDayReward[] dayRewards;   // length 10
  }

  [Serializable]
  public struct AttendanceDayReward
  {
      public RewardKind kind;       // reuse Mission's RewardKind enum
      public int amount;
  }
  ```
- `Assets/Scripts/Attendance/AttendanceState.cs` (Serializable POCO)
- `Assets/Scripts/Attendance/AttendanceService.cs`
  ```csharp
  public class AttendanceService
  {
      public bool CanClaimToday();   // false if already claimed this server-day
      public AttendanceDayReward GetTodayReward();
      public bool TryClaimToday();   // returns true on success; advances currentDayIndex
      public event Action<int, AttendanceDayReward> Claimed;   // (dayClaimed, reward)
      public AttendanceState State { get; }
  }
  ```
- `Assets/Scripts/UI/AttendanceButton.cs` — small HUD button below `업적`
- `Assets/Scripts/UI/AttendancePanel.cs`
- `Assets/Scripts/UI/AttendanceDayCellView.cs`
- `Assets/Prefabs/UI/AttendancePanel.prefab`
- `Assets/Prefabs/UI/AttendanceDayCell.prefab`
- `Assets/Data/Attendance/StandardAttendanceConfig.asset` (10 cells × gem 100)

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — add `attendance` field
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs`
- `Assets/Scripts/Save/SaveBinder.cs`
- `Assets/Scripts/Core/GameContext.cs` + `GameManager.cs` — register AttendanceService, AttendancePanel; reuse `MissionResetService` (Task Z) for server-time queries (do not duplicate Firestore reads)
- `Assets/Scripts/UI/HUDController.cs` — anchor `AttendanceButton` below `AchievementButton`; bind click

### 🚫 Do Not Touch
- MainUI01Bar (v7) — Attendance is NOT a MainUI01 sub-tab
- Mission system internals (Task Z) except shared reset/server-time service
- Skill / Gacha / Weapon

### 🧪 Validation
1. Compile clean
2. Fresh save → AttendancePanel shows Day 1 highlighted (claimable), Days 2~10 locked
3. Click Day 1 → gem +100, Day 1 marked claimed (dim + check), Day 2 NOT claimable today (must wait next server-day)
4. Simulate next server-day (advance `lastClaimedUtcMs` by -25h) → Day 2 becomes claimable, Day 1 stays claimed
5. Continue through Day 10 → after Day 10 claim, next server-day → cycle reset (all cleared, Day 1 claimable again)
6. Save + restart → attendance state preserved
7. AttendanceConfig edit: change Day 5 to gem 200 in inspector → restart → Day 5 cell shows `gem 200` reward
8. Cross-feature: clicking 출석 button does not affect achievement panel (and vice-versa); only one slide-up panel can be open at a time among the small-icon stack (Auto/Chat/Achievement/Attendance)

### Implementation note
**Mutual exclusion among small-icon panels:** introduce a lightweight `SecondaryPanelCoordinator` that tracks which of `{Achievement, Attendance, Chat}` is open. Opening one closes the others. This is independent of MainUI01Coordinator (which manages 강화/무기/소환/스킬).

**Cycle reset detail:** when `currentDayIndex == 10` and player claims, set `currentDayIndex = 10` + `lastClaimedUtcMs = now`. On next server-day check-in eligibility check: if `currentDayIndex == 10` AND `lastClaimedUtcMs` is yesterday → reset to `currentDayIndex = 1` and grant Day 1 next.

---

# Bundle 8 Release Gate

When Tasks W~AA are all `✅ DONE`, run one integrated PlayMode session:

1. Delete local save and Firestore game document
2. Start from LoginScene → enter MainScene
3. **Gacha**: open 소환 → see 1회/10회/30회 buttons with `100 / 1,000 / 3,000` prices; perform 30-pull and verify all 30 cards visible
4. **Skills**: skill bar shows meteor in slot 0; auto mode + idle → meteor casts; open 메인UI01 → 스킬 → equip cold_beam to slot 1 → both skills cycle in auto-cast
5. **Achievement**: small 업적 button below Auto toggle → opens panel → `일일미션 / 반복미션` tabs visible; kill 100 monsters → both daily and repeat completion paths trigger; claim rewards
6. **Attendance**: small 출석 button below 업적 → opens panel → 10-day grid; claim Day 1; verify cycle behavior with simulated date advance
7. **Save round-trip**: quit and relaunch → all v8 state restored (skills equipped, mission progress, attendance state, gem balance)
8. **v3 → v4 migration**: load a real v3 save → new fields default-fill (3 owned skills, slot 0 = meteor, empty mission/attendance) without losing v7 weapon/gacha state
9. **Cross-feature regression**: v7 features (weapon equip, synthesize, summon level + probability popup, MainUI01Bar tabs, internal close buttons) all still work
10. **Cross-feature regression (v6)**: presence write log at 5Hz, chat send/receive, cloud sync round-trip — all still work after saveVersion bump to 4

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies:
1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — no unauthorized files modified (`git diff` to confirm)
3. **"Do Not Touch" areas unchanged**
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation matches the design intent
6. **Exactly one git commit** — message includes the task ID
7. **No regression in `Tasks.md` / `Tasks_v6.md` / `Tasks_v7.md` features** — sanity check core combat, save, login, gacha, weapon UI, MainUI01Bar still function

Record review results in Appendix E.

---

## Appendix B — Bundle Gate Checklist

After Task AA reaches `✅ DONE`:
1. Bundle 8 release regression tests pass
2. `git log` clean — every task W~AA has exactly one implementation commit
3. Unity Console 0 errors / 0 warnings
4. Save migration v3 → v4 tested with an existing v7 save file
5. Fresh save tested
6. No unauthorized edits to `Tasks.md` / `Tasks_v6.md` / `Tasks_v7.md`

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-09 | Planner | Document created. Bundle 8 split into W (gacha 30-pull + 9/10 fix + pricing 100/1000/3000), X (3-skill ScriptableObject system + cast pipeline + Particle System VFX), Y (skill bar HUD + 메인UI01 스킬 탭 replacing 추가예정4 + 장착 버튼 → slot picker flow), Z (daily/repeat missions with Firestore server-time daily reset, linear repeat counters), AA (10-day attendance cycle with editable per-day rewards, server-time gated). User decisions resolved: skill slot model = equip-via-button (not drag), VFX pipeline = Particle System, daily reset = Firestore serverTimestamp KST, repeat counter = linear delta, 30-pull pricing = flat 100 gems/pull (no discount), skill seed values = planner-proposed (meteor 12x/60mp/12s, cold_beam 5x/30mp/6s, charge 6x/25mp/5s), attendance after Day 10 = cycle to Day 1. saveVersion bumps to 4 in Task X with migration. |

---

## Appendix D — Combined Work Log (implementer)

> Implementers append a row here on each task transition. Format: `YYYY-MM-DD | Task X | <one-line summary>`.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-09 | Task W | Implemented 30-pull gacha pricing (`1회 100`, `10회 1,000`, `30회 3,000`), `GachaService.PullThirty()`, adaptive result-modal layout, and `Standard` cost fields. Item 1 root cause: `GachaResultPanel` had one fixed 5-column result layout (`Cards` 640x370, `cellSize=(112,166)`, spacing 12) and fixed card construction assumptions with no card-count-specific fit policy, plus delayed `Destroy()` could leave stale children during immediate re-show; fixed by using `<=10` layout `5 cols / cell=(112,166) / spacing=(12,12)`, `30` layout `6 cols / cell=(100,66) / spacing=(8,8)`, keeping container `640x370`, forcing layout rebuilds, and detaching old cards before destroy. MCP PlayMode Validation 1~9 PASS; final console had no game-code compile/runtime errors after validation, only MCP transport log noise when queried. Start-state unrelated changes noted: `.DS_Store`, `Assets/.DS_Store`, `Assets/Fonts/NanumGothicBold SDF.asset`, `.codex/`; `Tasks_v8.md` was an untracked planner document before this Task W update. |

---

## Appendix E — Combined Review Log (reviewer)

> Reviewers append a row here after each code review. Format: `YYYY-MM-DD | Task X | <verdict + key findings>`.

| Date | Task | Entry |
|------|------|-------|
