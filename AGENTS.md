# Agent Instructions

This project is a Unity 2D prototype for an idle/clicker growth game titled
"마법사 키우기" (working title). Before implementing, modifying, or generating
assets, read this file and keep the prototype scope in mind.

## Primary Goal

Create a playable Unity 2D prototype, not a finished commercial game.

The prototype must clearly demonstrate this core loop:

1. The wizard attacks monsters automatically.
2. The player clicks or taps to fire stronger manual magic projectiles.
3. Monsters take damage and show floating damage numbers.
4. Defeated monsters grant gold.
5. Gold is spent on upgrades.
6. Upgrades increase combat power.
7. Higher stages spawn stronger monsters.
8. Every few stages, a timed boss encounter appears.

Prioritize a clean, maintainable foundation over large amounts of content.

## Design Pillars

- Genre: 2D idle/clicker, light RPG growth, simple stage combat.
- Main action: automatic magic attacks plus click/touch attacks.
- Control complexity: very low. Mouse click and mobile touch should be the
  main interaction.
- Strategic layer: upgrades, mana use, critical hits, and boss preparation.
- Uncertainty: critical chance and future weapon or skill rolls.
- Feedback: damage numbers, DPS display, mana bar, monster HP bar, boss timer,
  stage progress, and gold changes.

## Prototype Scope

Implement only the minimum playable prototype.

Required:

- One main scene named `MainScene`.
- Player wizard object.
- Normal enemy and boss enemy.
- Automatic attack.
- Manual click/touch attack.
- One active skill button.
- Mana system with max mana, current mana, regeneration, skill cost, and
  cooldown.
- Projectile movement and damage application.
- Floating damage text.
- Critical hit calculation and critical feedback.
- Gold wallet.
- Basic attack, mana, and critical upgrades.
- Stage progression.
- Boss stage every fixed interval, such as every 5 stages.
- Boss time limit, such as 20 seconds.
- Basic UI for stage, gold, DPS, mana, HP, boss timer, upgrades, and skill.
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

## Gameplay Rules

Use these defaults unless there is a good reason to adjust them:

- Base attack damage: 10.
- Auto attack interval: 1 second.
- Manual click attack multiplier: 2x.
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

## Scene Layout

Target a horizontal 16:9 layout.

- Place the player wizard on the left side or lower-left side.
- Place the enemy on the right side or center-right.
- Projectiles should travel from the wizard toward the enemy.
- Damage text should appear near the enemy and float upward.
- Monster HP bar should appear above the enemy.
- Top UI should include stage, gold, DPS, and boss timer.
- Bottom UI should include mana, upgrade buttons, and active skill button.

## Generated 2D Art Direction

Do not rely on external downloaded assets. Generate simple prototype sprites
inside the Unity project.

Store generated art under:

- `Assets/Art/Generated/`

Required sprite concepts:

- Wizard: cute fantasy wizard, small body, large robe, pointed hat, blue or
  purple robe, small staff, clear silhouette.
- Auto projectile: small glowing blue or cyan magic orb.
- Manual projectile: larger and brighter than the auto projectile.
- Active skill projectile: large magical orb, lightning, starburst, or arcane
  blast using purple, gold, or cyan accents.
- Normal enemy: simple green slime or small creature with two eyes.
- Boss enemy: larger red/dark monster with horns, crown, or other distinct
  boss features.
- Background: simple fantasy forest or training ground, low visual noise.
- UI icons: gold, mana, attack upgrade, critical upgrade, active skill.

Sprites may be generated through Unity Texture2D code, Unity MCP texture tools,
or other local generated PNG workflows. Placeholder shapes are acceptable only
if the concept remains recognizable.

## Recommended Project Structure

Use this structure where practical:

```text
Assets/
  Scripts/
    Core/
      GameManager.cs
      GameContext.cs
    Combat/
      IDamageable.cs
      DamageInfo.cs
      CombatCalculator.cs
      Projectile.cs
      ProjectileFactory.cs
      AutoAttackController.cs
      ClickAttackController.cs
      ActiveSkillController.cs
    Player/
      PlayerStats.cs
      PlayerMana.cs
      PlayerWizard.cs
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
clear ownership over creating unnecessary files.

## Architecture Requirements

Follow object-oriented principles and keep responsibilities separated.

- `IDamageable` should represent objects that can receive damage.
- `DamageInfo` should carry damage amount, critical state, and source/type data.
- `CombatCalculator` should calculate base damage, click damage, skill damage,
  and critical hits.
- `AutoAttackController` should handle automatic attack timing.
- `ClickAttackController` should handle mouse/touch input.
- `ActiveSkillController` should handle skill cooldown, mana cost, and skill
  firing.
- `ProjectileFactory` should create and configure projectiles.
- `Projectile` should move toward a target and apply damage safely.
- `EnemyBase` should contain shared enemy health, damage, death, and reward
  behavior.
- `NormalEnemy` and `BossEnemy` should extend or configure enemy behavior.
- `StageManager` should own stage progression.
- `BossStageController` should own boss timer and success/failure flow.
- `CurrencyWallet` should own gold changes.
- `UpgradeSystem` should own upgrade purchases and stat modifications.
- UI classes should display state and forward button events. They should not
  calculate core game logic.

Avoid putting all gameplay logic in one large `MonoBehaviour`.
Use pure C# classes or serializable data classes where they make the code
clearer. Use `ScriptableObject` for upgrade, skill, stage, or stat definitions
when it helps maintainability.

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

Button clicks must not accidentally trigger world click attacks. When handling
mouse or touch input, account for UI pointer checks such as
`EventSystem.current.IsPointerOverGameObject()`.

## Reliability Requirements

- The Unity Console must have no compile errors.
- Projectiles must handle missing or dead targets without null reference
  exceptions.
- Boss timer must reset correctly between boss attempts.
- Mana and cooldown checks must prevent invalid skill casts.
- Upgrade buttons must handle insufficient gold cleanly.
- UI must remain readable at the target 16:9 layout.

## Unity MCP Workflow

When using Unity MCP, actually create the project artifacts instead of only
describing them.

Expected workflow:

1. Inspect the current Unity project.
2. Create folders.
3. Create C# scripts.
4. Generate sprites or textures.
5. Create prefabs.
6. Build `MainScene`.
7. Add and wire UI.
8. Connect components and serialized references.
9. Refresh/compile.
10. Check the Unity Console.
11. Fix compile/runtime errors.
12. Report created files, scene name, and playable flow.

## Final Report Expectations

After implementation, report:

- Main scene path.
- Important scripts created.
- Important prefabs and generated sprites.
- How to run the prototype.
- What gameplay loop currently works.
- Any known limitations or follow-up work.

