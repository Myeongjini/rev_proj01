# Agent Instructions (Claude Code)

This project is a Unity 2D prototype for a top-down idle RPG titled
"마법사 키우기" (working title). Before implementing, modifying, or generating
assets, read this file and keep the prototype scope in mind.

This file is the latest design reference. If anything here conflicts with
older documents (including the previous `AGENTS.md`), this file wins.

## Primary Goal

Create a playable Unity 2D **top-down** idle RPG prototype, not a finished
commercial game.

The prototype must clearly demonstrate this core loop:

1. The wizard moves around a top-down field.
2. While idle (joystick released) and Auto mode is on, the wizard automatically
   moves toward the nearest monster and fires magic projectiles.
3. The player can take direct control at any time using a virtual joystick
   (touch/click anywhere on the field). While the player is moving the wizard
   manually, auto-move and auto-attack are disabled.
4. The player can press a dedicated **Fire button** on the HUD to fire a
   stronger manual magic projectile. Manual fire works whether moving or
   stopped.
5. Monsters take damage and show floating damage numbers.
6. Defeated monsters grant gold.
7. Gold is spent on upgrades.
8. Upgrades increase combat power.
9. Higher stages spawn stronger monsters.
10. Every few stages, a timed boss encounter appears.

Prioritize a clean, maintainable foundation over large amounts of content.

## Design Pillars

- Genre: 2D top-down idle RPG, light clicker, simple stage combat.
- Camera: top-down, orthographic.
- Main actions:
  - Virtual joystick movement.
  - Automatic movement + automatic attack while idle and Auto is on.
  - Manual Fire button for extra projectiles.
  - Active skill button.
  - Auto toggle button.
- Control complexity: very low. Touch/mouse only — joystick + a few buttons.
- Strategic layer: upgrades, mana use, critical hits, and boss preparation.
- Uncertainty: critical chance and future weapon or skill rolls.
- Feedback: damage numbers, DPS display, mana bar, monster HP bar, boss timer,
  stage progress, gold changes, and clear Auto on/off state.

## Prototype Scope

Implement only the minimum playable prototype.

Required:

- One main scene named `MainScene`.
- Player wizard object with top-down movement.
- Virtual joystick input (floating joystick: anywhere on the field becomes
  the joystick origin when pressed).
- Auto mode controller with a small on/off toggle button on the HUD.
- Auto behavior (only when joystick is released AND Auto is on):
  automatic move toward nearest enemy, then automatic attack within range.
- Manual Fire button on the HUD that fires an extra projectile per press
  (or short-cooldown repeat fire while held).
- Active skill button.
- Mana system with max mana, current mana, regeneration, skill cost, and
  cooldown.
- Normal enemy and boss enemy.
- Projectile movement and damage application.
- Floating damage text.
- Critical hit calculation and critical feedback.
- Gold wallet.
- Basic attack, mana, and critical upgrades.
- Stage progression.
- Boss stage every fixed interval, such as every 5 stages.
- Boss time limit, such as 20 seconds.
- Basic UI for stage, gold, DPS, mana, HP, boss timer, upgrades, skill,
  Fire button, joystick, and Auto toggle.
- Generated prototype 2D sprites matching the fantasy wizard concept.

Do not implement:

- Save/load.
- Online ranking.
- Monetization.
- Large equipment systems.
- Large content tables.
- Complex AI.
- Polished production art.

Leave extension points for those systems, but do not build them yet.

## Input and Auto Mode Rules

These rules are the most important behavioral change versus older prototypes.

- The old "click anywhere to fire an extra projectile" rule is **removed**.
  Tapping empty field space now belongs to the joystick, not to attacking.
- Joystick:
  - Pressing anywhere on the field (not on a UI button) starts a floating
    virtual joystick at that point.
  - Drag direction and magnitude move the wizard.
  - Releasing the joystick returns the wizard to idle state.
  - Inputs that begin over a UI element (Fire button, skill button, Auto
    toggle, upgrade buttons, etc.) must NOT be treated as joystick input.
    Use `EventSystem.current.IsPointerOverGameObject()` (and the touch
    overload on mobile) to filter.
- Auto toggle button:
  - Small button on the HUD, with a clear ON/OFF visual state.
  - Default: ON.
  - When OFF, the wizard does nothing on its own while idle.
- Combined behavior:
  - Auto move + auto attack run only when **Auto is ON** AND the joystick
    is **not** being held.
  - While the joystick is held, auto move and auto attack are suspended,
    even if Auto is ON.
  - Manual Fire button and active skill button always work, regardless of
    Auto state or joystick state (as long as their own cost/cooldown allow).

## Gameplay Rules

Use these defaults unless there is a good reason to adjust them:

- Base attack damage: 10.
- Auto attack interval: 1 second.
- Manual fire (Fire button) damage multiplier: 2x.
- Critical chance: 10%.
- Critical multiplier: 2x.
- Max mana: 100.
- Mana regeneration: 5 per second.
- Active skill mana cost: 40.
- Active skill cooldown: 8 seconds.
- Active skill damage: 8x attack damage.
- Normal enemy base HP: 50.
- Enemy HP scaling per stage: 1.25x.
- Boss HP: 8x the normal enemy HP for that stage.
- Boss time limit: 20 seconds.
- Boss interval: every 5 stages.
- Normal enemy base reward: 10 gold.
- Boss reward: 10x normal reward.
- Player move speed: ~4 units/second (tunable in inspector).
- Auto-attack range: ~5 units (tunable in inspector).

## Scene Layout

Top-down 2D field. Target either landscape 16:9 or a portrait mobile aspect
ratio — whichever the existing scene already uses.

- Camera: orthographic, looking down on the field.
- Player wizard spawns somewhere on the field; enemies spawn around the
  field at varied positions.
- Projectiles travel from the wizard toward the targeted enemy.
- Damage text appears near the enemy and floats upward.
- Monster HP bar appears above each enemy.
- Top HUD: stage, gold, DPS, boss timer.
- Side/bottom HUD: mana bar, upgrade buttons.
- Bottom-right HUD: Fire button (large, easy to press) with the active skill
  button nearby.
- Corner HUD: small Auto toggle button with a clear ON/OFF look.
- The joystick is floating: it has no fixed home position. It only renders
  while the player is pressing on the field.

## Generated 2D Art Direction

Do not rely on external downloaded assets. Generate simple prototype sprites
inside the Unity project. Sprites must read clearly from a top-down angle.

Store generated art under:

- `Assets/Art/Generated/`

Required sprite concepts:

- Wizard (top-down): cute fantasy wizard seen from above, pointed hat
  visible from the top, blue or purple robe, clear silhouette.
- Auto projectile: small glowing blue or cyan magic orb, optional trail.
- Manual projectile (Fire button): larger and brighter than the auto
  projectile so manual fire feels stronger.
- Active skill projectile: large magical orb, lightning, starburst, or
  arcane blast using purple, gold, or cyan accents. Clearly distinct from
  basic attacks.
- Normal enemy: simple green slime or small creature with two eyes.
- Boss enemy: larger red/dark monster with horns, crown, or other distinct
  boss features.
- Background: simple top-down fantasy field (grass, stone tiles), low
  visual noise so units and UI stay readable.
- UI icons: gold, mana, attack upgrade, critical upgrade, active skill,
  Fire button, Auto toggle, joystick base, joystick knob.

Sprites may be generated through Unity Texture2D code or Unity MCP texture
tools. Placeholder shapes are acceptable only if the concept remains
recognizable.

## Recommended Project Structure

The previous structure is preserved where possible. New folders/files are
added for input, joystick, manual fire, and auto-mode logic.

```text
Assets/
  Scripts/
    Core/
      GameManager.cs
      GameContext.cs
    Input/
      VirtualJoystick.cs
      InputService.cs
    Combat/
      IDamageable.cs
      DamageInfo.cs
      CombatCalculator.cs
      Projectile.cs
      ProjectileFactory.cs
      AutoAttackController.cs
      ManualFireButtonController.cs   // replaces ClickAttackController
      ActiveSkillController.cs
      TargetingService.cs
    Player/
      PlayerStats.cs
      PlayerMana.cs
      PlayerWizard.cs
      PlayerMovement.cs               // joystick-driven movement
      PlayerAutoMovement.cs           // auto move toward nearest enemy
      AutoModeController.cs           // owns Auto toggle state
      PlayerProgression.cs
    Enemies/
      EnemyBase.cs
      NormalEnemy.cs
      BossEnemy.cs
      EnemySpawner.cs
      EnemyScalingService.cs
    Stages/
      StageManager.cs
      StageDefinition.cs
      BossStageController.cs
    Economy/
      CurrencyWallet.cs
      RewardService.cs
    Upgrades/
      UpgradeDefinition.cs
      UpgradeSystem.cs
      UpgradeButtonView.cs
    UI/
      HUDController.cs
      HealthBarView.cs
      ManaBarView.cs
      DamageTextView.cs
      FloatingTextSpawner.cs
      BossTimerView.cs
      DPSView.cs
      JoystickView.cs
      FireButtonView.cs
      AutoToggleView.cs
    Data/
    Utilities/
      Timer.cs
      Pool.cs
      MathUtil.cs
  Art/
    Generated/
  Prefabs/
  Scenes/
```

This structure is a guide, not a rigid requirement. Prefer consistency and
clear ownership over creating unnecessary files. If the project already
contains files under older names (for example `ClickAttackController.cs`),
prefer renaming/refactoring over duplicating responsibilities.

## Architecture Requirements

Follow object-oriented principles and keep responsibilities separated.

- `IDamageable` represents objects that can receive damage.
- `DamageInfo` carries damage amount, critical state, and source/type data.
- `CombatCalculator` calculates base damage, manual fire damage, skill
  damage, and critical hits.
- `VirtualJoystick` / `InputService` own raw input and expose a normalized
  movement vector plus an `IsPressed` flag.
- `PlayerMovement` consumes joystick input and moves the wizard. It exposes
  whether the player is currently being controlled manually.
- `PlayerAutoMovement` moves the wizard toward the nearest enemy when
  allowed.
- `AutoModeController` owns the Auto toggle state and decides whether
  automatic systems may run. Its rule is:
  `CanAutoAct = AutoEnabled && !PlayerMovement.IsBeingControlledByJoystick`.
- `AutoAttackController` handles automatic attack timing. It only fires
  while `AutoModeController.CanAutoAct` is true and a target is in range.
- `ManualFireButtonController` handles the Fire button on the HUD. It is
  always allowed (no Auto/joystick gating), subject only to its own
  internal cooldown if any.
- `ActiveSkillController` handles skill cooldown, mana cost, and skill
  firing. Always allowed, subject to its own cost and cooldown.
- `TargetingService` finds the nearest valid enemy for auto-move and
  auto-attack.
- `ProjectileFactory` creates and configures projectiles.
- `Projectile` moves toward a target and applies damage safely, handling
  null/dead targets without exceptions.
- `EnemyBase` contains shared enemy health, damage, death, and reward
  behavior.
- `NormalEnemy` and `BossEnemy` extend or configure enemy behavior.
- `StageManager` owns stage progression.
- `BossStageController` owns boss timer and success/failure flow.
- `CurrencyWallet` owns gold changes.
- `UpgradeSystem` owns upgrade purchases and stat modifications.
- UI classes display state and forward button events. They must not
  calculate core game logic.

Avoid putting all gameplay logic in one large `MonoBehaviour`. Use pure C#
classes or serializable data classes where they make the code clearer. Use
`ScriptableObject` for upgrade, skill, stage, or stat definitions when it
helps maintainability.

## Event Guidelines

Prefer events or explicit public methods to keep systems loosely coupled.

Useful events:

- `OnEnemyDamaged`
- `OnEnemyKilled`
- `OnGoldChanged`
- `OnStageChanged`
- `OnManaChanged`
- `OnBossTimerChanged`
- `OnDpsChanged`
- `OnUpgradePurchased`
- `OnAutoModeChanged`             // Auto toggle ON/OFF
- `OnPlayerMovingStateChanged`    // joystick held vs released

Keep event names and payloads simple.

## UI Requirements

The UI must include:

- Current stage.
- Current gold.
- Player attack power or combat power.
- DPS.
- Mana bar.
- Enemy HP bar.
- Boss timer, hidden outside boss stages.
- Upgrade buttons.
- Active skill button.
- **Fire button** for manual extra projectiles.
- **Auto toggle button**, small, with a clear ON/OFF visual state.
- Floating virtual joystick visuals (base + knob), shown only while pressed.

Button clicks must not be interpreted as joystick input or as world taps.
When handling mouse or touch input, always run UI pointer checks
(`EventSystem.current.IsPointerOverGameObject()`, plus the touch overload on
mobile) before starting joystick drag logic.

## Reliability Requirements

- The Unity Console must have no compile errors.
- Projectiles must handle missing or dead targets without null reference
  exceptions.
- Boss timer must reset correctly between boss attempts.
- Mana and cooldown checks must prevent invalid skill casts.
- Upgrade buttons must handle insufficient gold cleanly.
- UI must remain readable at the target aspect ratio.
- Auto toggle state must be respected immediately:
  - Turning Auto OFF while idle stops auto-move and auto-attack at once.
  - Pressing the joystick while Auto is ON suspends auto-move/auto-attack
    on the same frame the press starts.
  - Releasing the joystick (with Auto still ON) resumes auto behavior.
- Manual Fire button and active skill button never depend on Auto state.

## Unity MCP Workflow

When using Unity MCP, actually create or update the project artifacts
instead of only describing them. Because earlier prototype work already
exists, prefer inspecting first, then refactoring/extending — do not wipe
existing work.

Expected workflow:

1. Inspect the current Unity project (scenes, scripts, prefabs).
2. Identify where the existing project already matches this spec and
   where it does not.
3. Create or rename folders as needed.
4. Create or update C# scripts (e.g. add `VirtualJoystick`,
   `PlayerMovement`, `PlayerAutoMovement`, `AutoModeController`,
   `ManualFireButtonController`; refactor any old `ClickAttackController`).
5. Generate sprites or textures only if missing.
6. Create or update prefabs (Player, Enemies, Projectile, DamageText,
   HUD, Joystick, Fire button, Auto toggle).
7. Update `MainScene` so the new HUD elements and joystick exist.
8. Wire components and serialized references.
9. Refresh/compile.
10. Check the Unity Console.
11. Fix compile/runtime errors.
12. Report what was created vs. what was modified, plus the playable flow.

## Final Report Expectations

After implementation, report:

- Main scene path.
- Important scripts created or modified (clearly distinguish the two).
- Important prefabs and generated sprites.
- How to run the prototype.
- What gameplay loop currently works, including:
  - Joystick movement.
  - Auto-move + auto-attack while idle and Auto is ON.
  - Auto suspension while joystick is held.
  - Auto fully off when Auto toggle is OFF.
  - Manual Fire button firing extra projectiles.
  - Active skill firing with mana cost and cooldown.
  - Damage numbers, gold gain, upgrades, stage progression, boss timer.
- Any known limitations or follow-up work.
