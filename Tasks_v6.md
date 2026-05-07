# Wizard Grower — Tasks v6 (Bundle 6: UX Polish + Multiplayer + Weapons)

> Parallel work track to `Tasks.md` (Bundles 1–5).
> Different agents, different bundles, different files.
> **Do NOT edit `Tasks.md` from this track.** If you need to read it, do so read-only.
> The full design rationale lives in `Design_v6.md` — read it before starting any task.
>
> The **Planner / Reviewer (Claude)** is the sole editor of this document.
> Implementers read this document, modify the code, and append to Appendix D.

---

## 0. Common Work Rules

### 0.1 Basic Rules
1. **Only one task at a time.** State the task ID (e.g. `Task K`) at start and end.
2. Before starting: read the relevant task section thoroughly. Never modify any **"Do Not Touch"** area.
3. During work: Unity project path is `/Users/kmj/rev_proj01`. Namespace is `WizardGrower.*`.
4. On completion:
   - Verify Unity Console: **0 errors / 0 warnings**
   - Enter PlayMode and pass all **regression tests** (specified at the bottom of each task)
   - Update the task's Status line to `🟡 IN REVIEW`
   - Append a row to **Appendix D — Combined Work Log** with the task ID, date, and what you did.
5. After code review, the reviewer:
   - Updates Status to `✅ DONE` or back to `🔴 TODO`
   - Appends a row to **Appendix E — Combined Review Log**
6. The build helper menu `Wizard Grower → Build Prototype Scene` **overwrites the scene** — do not run without explicit permission.
7. When adding/removing files, ensure `.meta` files are paired (Unity Editor handles this automatically).

### 0.2 Sub-bundle Gate Rules ⭐
Bundle 6 has three independent sub-bundles (6A / 6B / 6C). Each has its own gate. The **Bundle 6 release gate** requires all three sub-bundle gates ✅ + the integrated regression test (§ Bundle 6 Release Gate).

### 0.3 Auto-Progression Restrictions (Implementation Agent)
- Do not start any task from a different sub-bundle on your own.
- On signature/spec conflict, **do not decide unilaterally** — record in Combined Work Log and wait for the reviewer.
- Never temporarily modify "Do Not Touch" areas. If compilation breaks, mark Status `⚠️ BLOCKED` with reason.
- Do not introduce new systems or files not listed in this document (exceptions logged + reviewer approval).

### 0.4 Git Commit Rules
- **One git commit per task completion** is mandatory. Format: `Task X done: <one-line summary>`.
- This repo uses GitHub Desktop with origin set. **Agents must not push** — push is the user's responsibility.
- At task start, run `git status` to confirm clean working tree.
- Per task = per commit. `git revert` is the rollback tool.

### 0.5 Cross-Track Coordination ⚠️
The existing `Tasks.md` is in active use (Bundle 5 still has tasks I and H-VAL). Bundle 6 must coexist:

| Risk | Mitigation |
|---|---|
| Both tracks edit `SaveData.cs` | **Sub-bundle 6C waits on Task I ✅ before starting Task O.** Sub-bundle 6A and 6B do not touch SaveData. |
| Both tracks edit `GameContext.cs` / `GameManager.cs` / `HUDController.cs` | These are wiring hubs. Additions are line-additions, not line-replacements. Implementer commits in known order; reviewer coordinates merge. |
| Two `Tasks*.md` documents with two reviewers | Only Claude (planner/reviewer) edits both. Implementers never cross-edit. |

If you are an implementer assigned a Bundle 6 task and `Tasks.md` shows Task I 🟢 IN PROGRESS, **do not start Task O** until Task I lands. Tasks J / K / L / M / N can proceed regardless.

---

## 1. Task Dependency Graph

```
Sub-bundle 6A (UX Polish)
└── Task J — LoginScene + Splash + AuthBootstrapHolder

Sub-bundle 6B (Multiplayer Infrastructure — needs RTDB prework ⚠️)
├── Task K — RTDB integration + PresenceService
├── Task L — RemotePlayerView + on-stage rendering    ← depends on K
└── Task M — World + Stage Chat                       ← depends on K (RTDB SDK)

Sub-bundle 6C (Weapons & Gacha — depends on Task I ✅)
├── Task N — Weapon data model + stat composer
├── Task O — Weapon inventory + equip UI + visual swap  ← depends on N
└── Task P — Gacha service + UI + Gem currency          ← depends on N, O
```

**6A / 6B / 6C are independent of each other.** They can run in parallel. The Bundle 6 release gate requires all three sub-bundles ✅.

---

## 2. Task Status Board

| Sub-bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 6A | J | LoginScene + Splash + AuthBootstrapHolder | 🟡 IN REVIEW | Tasks.md Bundle 5 H ✅ |
| 6B | K | RTDB integration + PresenceService | 🔴 TODO | RTDB user prework ✅ |
| 6B | L | RemotePlayerView + on-stage rendering | 🔴 TODO | K |
| 6B | M | World + Stage Chat | 🔴 TODO | K |
| 6C | N | Weapon data model + stat composer | 🔴 TODO | Tasks.md Task G ✅ |
| 6C | O | Weapon inventory + equip UI + visual swap | 🔴 TODO | N + Tasks.md Task I ✅ |
| 6C | P | Gacha service + UI + Gem currency | 🔴 TODO | N, O |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Sub-bundle 6A — UX Polish

**Goal:** dedicated entry scene with splash + login. MainScene wakes with auth state already prepared.

### Sub-bundle 6A Combined Regression Tests
1. Cold start → LoginScene loads (Build index 0) → splash visible ≥ 0.8s
2. Anonymous login completes → splash fades → LoginPanel shown
3. "Continue as Guest" → MainScene loads → HUD visible with anonymous UID
4. Re-launch → same UID, splash → LoginPanel auto-skipped if user is already signed in (re-uses existing FirebaseUser)
5. Google login → nickname modal → MainScene loads → HUD visible with Google UID + nickname

---

## Task J — LoginScene + Splash + AuthBootstrapHolder

**Status:** 🟡 IN REVIEW
**Depends On:** Tasks.md Task H ✅ (`AuthService` / `UserProfileService` / `AuthConfig` already exist)

### 🎯 Goal
Convert MainScene-embedded login into a dedicated entry scene. MainScene receives auth state via a `DontDestroyOnLoad` carrier and skips re-initialization.

### ✅ Definition of Done
- [ ] Unity Console: 0 errors / 0 warnings
- [ ] Build Settings: `[0] LoginScene, [1] MainScene`
- [ ] Sub-bundle 6A regression tests 1~5 all pass
- [ ] No Firebase re-init in MainScene (verified by log; AuthService is consumed, not re-created)

### 📂 Files to Add
- `Assets/Scenes/LoginScene.unity` — new entry scene
- `Assets/Scripts/Login/LoginBootstrap.cs` — orchestrator (splash → auth → load MainScene)
  ```csharp
  public class LoginBootstrap : MonoBehaviour
  {
      [SerializeField] private SplashView splash;
      [SerializeField] private LoginPanel loginPanel;
      [SerializeField] private NicknameRegistrationPanel nicknamePanel;
      [SerializeField] private float minSplashSeconds = 0.8f;

      private async void Start() { /* init Firebase → anon login → load MainScene */ }
  }
  ```
- `Assets/Scripts/Login/AuthBootstrapHolder.cs` — DontDestroyOnLoad singleton
  ```csharp
  public class AuthBootstrapHolder : MonoBehaviour
  {
      public static AuthBootstrapHolder Instance { get; private set; }
      public AuthService Auth { get; private set; }
      public UserProfileService Profile { get; private set; }
      public AuthConfig Config { get; private set; }
      public string Uid => Auth?.CurrentUid;
  }
  ```
- `Assets/Scripts/UI/SplashView.cs` — logo + spinner + fade animation
- `Assets/Prefabs/UI/SplashCanvas.prefab`
- `Assets/Prefabs/UI/LoginCanvas.prefab` (extracted from current MainScene login UI)
- `Assets/Art/UI/Splash_Logo.png` placeholder

### 📂 Files to Modify
- `Assets/Scripts/Core/GameManager.cs` — strip `InitializeAuthenticationAsync` block; consume `AuthBootstrapHolder.Instance.{Auth, Profile}` instead. Auth + profile are guaranteed ready at MainScene wake-up.
- `Assets/Scenes/MainScene.unity` — remove the `LoginPanel` + `NicknameRegistrationPanel` GameObjects (they live in LoginScene now). Remove their references from `GameContext` if any.
- `Build Settings` (`File → Build Settings`): order `[0] LoginScene, [1] MainScene`. Save settings.

### 🚫 Do Not Touch
- `AuthService.cs`, `UserProfileService.cs`, `AuthConfig.cs` (Bundle 5 closed)
- `Firestore` integration paths
- HUD panels other than the login canvas
- Combat / stage / save logic

### 🧪 Validation
1. Open Build Settings → confirm scene order
2. Stop / Play in LoginScene → splash → login → MainScene transition smooth
3. Quit + Play → previously-signed-in FirebaseUser reused (verify `auth.CurrentUser != null` skips picker)
4. Toggle airplane mode during login → expect "network error" path with retry, not silent freeze
5. Click "Continue as Guest" → MainScene loads → `AuthBootstrapHolder.Instance.Uid` non-empty in MainScene

---

# Sub-bundle 6B — Multiplayer Infrastructure

**Goal:** real-time presence + chat over Firebase Realtime Database.

### ⚠️ User Prework (must complete before starting Task K)

- [ ] Firebase Console → **Realtime Database** → Create database (region: `asia-northeast3` recommended for KR users)
- [ ] Realtime Database → **Rules** tab → paste:
```json
{
  "rules": {
    "presence": {
      "$stage": {
        "$uid": {
          ".read": "auth != null",
          ".write": "auth != null && auth.uid === $uid"
        }
      }
    },
    "chat": {
      "world": {
        "$msg": {
          ".read": "auth != null",
          ".write": "auth != null && newData.child('uid').val() === auth.uid && newData.child('text').isString() && newData.child('text').val().length <= 200"
        }
      },
      "stage": {
        "$stage": {
          "$msg": {
            ".read": "auth != null",
            ".write": "auth != null && newData.child('uid').val() === auth.uid && newData.child('text').isString() && newData.child('text').val().length <= 200"
          }
        }
      }
    }
  }
}
```
- [ ] Firebase Unity SDK 재import → `FirebaseDatabase.unitypackage` 체크 추가
- [ ] Polyfill 충돌 정리 (Bundle 5 H 작업 때와 동일):
  - `rm -rf Assets/PlayServicesResolver Assets/PlayServicesResolver.meta` (재등장 시)
  - `rm -rf Assets/Parse Assets/Parse.meta` (재등장 시)
  - `xattr -dr com.apple.quarantine Assets/Firebase/`
- [ ] Unity 재시작 → Console 0 error 확인

> **사용자가 위 5단계를 완료하고 명시적으로 "Bundle 6B 진행 가능"이라 알리기 전까지 Task K 시작 금지.**

### Sub-bundle 6B Combined Regression Tests
1. Two Editor instances (clone the project folder into a sibling for the second instance) login as different anonymous UIDs → enter same stage → both see each other's `RemoteWizard` ghost
2. One instance moves → other instance sees position update within ~300ms (5Hz throttle + 200ms Lerp)
3. Instance A enters boss room → A's ghost disappears from B's screen (boss room is solo)
4. Force-quit instance A → instance B's screen removes A's ghost within 30s (stale entry filter)
5. World chat: A sends → B receives within 1s
6. Stage chat: A sends in chapter1-stage1 → B in chapter1-stage1 receives, but C in chapter1-stage2 does NOT
7. Spam: rapid-tap send → only 1 message per 2s actually goes through (client throttle)

---

## Task K — RTDB Integration + PresenceService

**Status:** 🔴 TODO
**Depends On:** RTDB user prework ✅

### 🎯 Goal
Wire Firebase Realtime Database into the project. Add `PresenceService` that writes own position at 5Hz and exposes a stream of remote-player positions for the current stage.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `FirebaseDatabase` namespace imports successfully
- [ ] `PresenceService.WriteOwn(x, y)` writes to `presence/{chapterId}_{stageNumber}/{uid}` and returns within 100ms
- [ ] `PresenceService.SubscribeStage(stage)` returns a stream of `RemotePresenceEvent` (added / changed / removed)
- [ ] `OnDisconnect()` is registered → manual app kill → entry removed in Firebase Console within 60s
- [ ] `PresenceCoordinator` throttles `PlayerMovementController.PositionChanged` to 5Hz (every 200ms)
- [ ] Stage transition correctly swaps presence node (old stage entry removed, new stage entry written)

### 📂 Files to Add
- `Assets/Scripts/Multiplayer/PresenceService.cs`
  ```csharp
  public class PresenceService
  {
      public Task InitializeAsync(string uid, string displayName);
      public Task WriteOwnAsync(string stage, float x, float y);
      public Task RemoveOwnAsync(string stage);
      public IDisposable SubscribeStage(string stage, Action<RemotePresenceEvent> onEvent);
  }

  public struct RemotePresenceEvent
  {
      public RemotePresenceEventType Type;  // Added / Changed / Removed
      public string Uid;
      public string DisplayName;
      public Vector2 Position;
      public long LastUpdateUnixMs;
  }
  ```
- `Assets/Scripts/Multiplayer/PresenceCoordinator.cs` — orchestrator
  ```csharp
  public class PresenceCoordinator : MonoBehaviour
  {
      [SerializeField] private float writeIntervalSeconds = 0.2f;  // 5Hz
      // subscribes to PlayerMovementController.PositionChanged
      // subscribes to StageManager.StateChanged for stage transitions
      // throttles writes via Time.unscaledTime
  }
  ```

### 📂 Files to Modify
- `Assets/Scripts/Player/PlayerMovementController.cs` — add `public event Action<Vector2> PositionChanged;` invoked at the end of `Move()`
- `Assets/Scripts/Core/GameContext.cs` — add `PresenceService` and `PresenceCoordinator` fields
- `Assets/Scripts/Core/GameManager.cs` — instantiate PresenceService after AuthBootstrapHolder is consumed; call `PresenceCoordinator.Begin()` after `StageManager.Initialize`

### 🚫 Do Not Touch
- `AuthService` / `UserProfileService` (Bundle 5 closed)
- `StageManager` core flow (only `StateChanged` subscribed)
- HUD UI

### 🧪 Validation
1. Compile clean after `FirebaseDatabase` import
2. PlayMode → log shows `presence/1_1/{uid}` write every ~200ms
3. Firebase Console → Realtime Database → see live entry under `presence/1_1`
4. Move wizard → `x`, `y` fields update in console
5. Enter boss room → entry node changes (or is cleared per design); re-enter field → entry returns
6. Quit Unity → entry disappears from console within 60s

### Implementation note
Use the same defensive reflection-based pattern as `AuthService` in case the `Firebase.Database` namespace fails to load (e.g. SDK partially imported). Throw clear runtime errors instead of compile errors. Keep the API surface (`PresenceService`) testable in isolation.

---

## Task L — RemotePlayerView + On-Stage Rendering

**Status:** 🔴 TODO
**Depends On:** K ✅

### 🎯 Goal
Render other players' positions visually as ghost wizards on the local screen. Smoothly interpolate and clean up stale entries.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] When `PresenceService` emits `Added`, a `RemoteWizard` prefab is instantiated at the reported position
- [ ] On `Changed`, the existing `RemotePlayerView` Lerps over 200ms to the new position
- [ ] On `Removed`, the GameObject is destroyed
- [ ] Stale filter: views whose `lastUpdateUnixMs > 30s` ago are forcibly cleaned up
- [ ] Sub-bundle 6B regression tests 1~4 pass

### 📂 Files to Add
- `Assets/Scripts/Multiplayer/RemotePlayerView.cs`
  ```csharp
  public class RemotePlayerView : MonoBehaviour
  {
      [SerializeField] private SpriteRenderer body;
      [SerializeField] private TMP_Text nameLabel;
      [SerializeField] private float lerpDuration = 0.2f;

      public void Initialize(string uid, string displayName, Vector2 initialPos);
      public void SetTarget(Vector2 newPos);   // smooth Lerp
      public void Touch(long unixMs);          // updates lastUpdateUnixMs
      public bool IsStale(long nowUnixMs, long thresholdMs = 30_000);
  }
  ```
- `Assets/Prefabs/RemoteWizard.prefab` — Wizard sprite at 50% alpha, no Animator, no Wander, no collider, name label child

### 📂 Files to Modify
- `Assets/Scripts/Multiplayer/PresenceCoordinator.cs` — extend with `Dictionary<string, RemotePlayerView> remotes` and per-event handlers
- `Assets/Scripts/Core/GameContext.cs` — register `RemoteWizardPrefab` reference for instantiation

### 🚫 Do Not Touch
- Local PlayerWizard / PlayerMovementController logic
- `EnemySpawner` / `EnemyHealthBarView`
- Boss flow

### 🧪 Validation
1. Compile clean
2. Two Editor instances (use **Unity Hub > project copy** or `mklink`/`ln -s` to share Assets but separate Library) — log in as different UIDs
3. Move A → B's screen shows ghost moving with ~200ms latency
4. Force-quit A → B sees ghost disappear within 30s
5. A enters boss room → B's view of A clears

### Implementation note
For the "two Editor instances" test setup: Unity does not natively support running the same project in two editors. Workarounds:
- Use **Unity ParrelSync** package (free, MIT) to clone the project for testing
- OR: build a development player to a second machine and run alongside the editor
- OR (simplest): hand-write a test entry to `presence/1_1/fake-uid-001` from the Firebase Console and verify it renders

---

## Task M — World + Stage Chat

**Status:** 🔴 TODO
**Depends On:** K (RTDB SDK already imported)

### 🎯 Goal
Two-channel chat system. Players send and receive text messages in the world channel and the current stage channel.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] HUD has a chat toggle button (bottom-left or above gem display)
- [ ] Chat panel has World / Stage tabs
- [ ] Sending a message writes to `chat/world/{pushId}` or `chat/stage/{stage}/{pushId}` with `{uid, displayName, text, ts}`
- [ ] Reading shows last 50 messages on open + auto-scroll on new
- [ ] Send throttle: client refuses second send within 2 seconds (UI shows brief feedback)
- [ ] Sub-bundle 6B regression tests 5~7 pass

### 📂 Files to Add
- `Assets/Scripts/Chat/ChatChannel.cs`
  ```csharp
  public enum ChatChannel { World, Stage }
  ```
- `Assets/Scripts/Chat/ChatService.cs`
  ```csharp
  public class ChatService
  {
      public Task InitializeAsync(string uid, string displayName);
      public Task SendAsync(ChatChannel channel, string stage, string text);
      public IDisposable SubscribeChannel(ChatChannel channel, string stage, int limit, Action<ChatMessage> onMessage);
  }

  [Serializable]
  public struct ChatMessage
  {
      public string Uid;
      public string DisplayName;
      public string Text;
      public long Ts;
  }
  ```
- `Assets/Scripts/UI/ChatPanel.cs` — tab toggle, scroll list, input field, send button
- `Assets/Scripts/UI/ChatMessageView.cs` + `Assets/Prefabs/UI/ChatMessage.prefab`

### 📂 Files to Modify
- `Assets/Scripts/UI/HUDController.cs` — add chat toggle button + ChatPanel reference; bind `chatToggleButton.onClick` to open/close
- `Assets/Scripts/Core/GameContext.cs` — register `ChatService`
- `Assets/Scripts/Core/GameManager.cs` — initialize ChatService after auth ready

### 🚫 Do Not Touch
- Combat, stage, save logic
- PresenceService / RemotePlayerView (Task L)
- Other HUD widgets' position

### 🧪 Validation
1. Compile clean
2. PlayMode → click chat toggle → panel slides up
3. Type "hello" + Enter → message appears in panel; Firebase Console shows entry under `chat/world`
4. Tab switch to Stage → typing sends to `chat/stage/1_1`
5. Rapid-fire 5 sends in 1 second → only 1 actually persists; UI shows "잠시만 기다려주세요" briefly
6. Newline / 200+ char input → send button disabled or input rejected (decide one)

### Implementation note
Use `OrderByKey().LimitToLast(50).OnChildAdded` for live tail. The push ID is time-ordered so this gives natural chronological ordering. Stage channel listener must re-subscribe when `StageManager.StateChanged` fires.

---

# Sub-bundle 6C — Weapons & Gacha

**Goal:** equippable weapons with stat bonuses + sprite swap; gacha to pull weapons by rarity using Gem currency.

> **Sub-bundle 6C waits on `Tasks.md` Task I ✅** before starting Task O. Task N can begin earlier (data-only).

### Sub-bundle 6C Combined Regression Tests
1. New game (delete `save.json`) → starts with `wand_starter` equipped → ATK reflects starter bonus (+0)
2. Equip a Rare weapon → wizard tints + glyph appears + ATK / crit chance increase visibly + projectile sprite swaps
3. Unequip & re-equip → no double-stacking (recompute correctness)
4. Save + restart → equipped weapon persists, owned list persists
5. Open Gacha → 1-pull → gem -30, inventory grows by 1, result modal shows the pulled weapon
6. 30 pulls without any Rare → 30th pull guaranteed Rare+ (pity check)
7. 10-pull → gem -270, inventory grows by 10
8. Insufficient gems → 1-pull blocked with feedback

---

## Task N — Weapon Data Model + Stat Composer

**Status:** 🔴 TODO
**Depends On:** Tasks.md Task G ✅ (saved data layer)

### 🎯 Goal
Define the weapon data model (ScriptableObjects) and the stat composition layer that applies equipped weapon bonuses on top of `PlayerStats`.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `WeaponDefinition`, `WeaponDatabase`, `Rarity`, `WeaponStats` types compile
- [ ] `WeaponStatComposer.Recompute(base, equipped)` produces correct combined stats with documented combinator rules per field
- [ ] 6 sample `WeaponDefinition` assets exist under `Assets/Data/Weapons/`
- [ ] `WeaponDatabase.asset` references all 6 weapons + `GetById` / `ByRarity` work
- [ ] `Editor/VisualAssetUpdater` extended with menu items to regenerate placeholder glyph + projectile sprites

### 📂 Files to Add
- `Assets/Scripts/Weapons/Rarity.cs`
  ```csharp
  public enum Rarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }
  ```
- `Assets/Scripts/Weapons/WeaponStats.cs`
  ```csharp
  [Serializable]
  public struct WeaponStats
  {
      public float autoAttackDamage;       // additive
      public float manualAttackDamage;     // additive
      public float autoFireRateBonus;      // SUBTRACTED from autoAttackInterval (clamp ≥ 0.05)
      public float criticalChance;         // additive, clamp 0~1 after sum
      public float criticalMultiplier;     // additive
      public float armorPenetration;       // additive
      public float maxHealth;              // additive
  }
  ```
- `Assets/Scripts/Weapons/WeaponDefinition.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Weapon Definition")]
  public class WeaponDefinition : ScriptableObject
  {
      public string weaponId;
      public string displayName;
      public Rarity rarity;
      public Sprite icon;
      public Color tintColor = Color.white;
      public Sprite accessoryGlyph;
      public Sprite projectileSprite;
      public WeaponStats statBonuses;
      [TextArea] public string flavorText;
  }
  ```
- `Assets/Scripts/Weapons/WeaponDatabase.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Weapon Database")]
  public class WeaponDatabase : ScriptableObject
  {
      public WeaponDefinition[] weapons;
      public WeaponDefinition GetById(string weaponId);
      public IEnumerable<WeaponDefinition> ByRarity(Rarity r);
  }
  ```
- `Assets/Scripts/Weapons/WeaponStatComposer.cs` — pure logic, no MonoBehaviour
  ```csharp
  public static class WeaponStatComposer
  {
      // Returns a snapshot reflecting base + equipped bonuses with documented combinators.
      public static PlayerStatsSnapshot Recompute(PlayerStatsSnapshot baseSnapshot, WeaponStats? equipped);
  }
  ```
- `Assets/Data/Weapons/Wand_Starter.asset` (Common, all bonuses 0, tint = white)
- `Assets/Data/Weapons/Apprentice_Staff.asset` (Uncommon, autoDmg+5, manualDmg+8, tint = soft cyan)
- `Assets/Data/Weapons/Crystal_Wand.asset` (Uncommon, autoDmg+3, manualDmg+3, critChance+0.05, tint = pale violet)
- `Assets/Data/Weapons/Wizards_Stave.asset` (Rare, autoDmg+12, manualDmg+18, tint = gold)
- `Assets/Data/Weapons/Flame_Rod.asset` (Rare, autoDmg+8, manualDmg+10, armorPen+3, tint = warm red)
- `Assets/Data/Weapons/Arcane_Scepter.asset` (Epic, autoDmg+25, manualDmg+40, critChance+0.1, critMult+0.3, tint = deep purple)
- `Assets/Data/Weapons/WeaponDatabase.asset`
- `Assets/Art/Generated/WeaponGlyphs/*.png` (6 placeholder glyphs — runtime-generated by VisualAssetUpdater)
- `Assets/Art/Generated/WeaponProjectiles/*.png` (6 placeholder projectile sprites)

### 📂 Files to Modify
- `Assets/Scripts/Editor/VisualAssetUpdater.cs` — add `[MenuItem("Wizard Grower/Generate Weapon Glyphs")]` and `[MenuItem("Wizard Grower/Generate Weapon Projectiles")]` that procedurally write the placeholder PNGs (small geometric shapes per weapon)

### 🚫 Do Not Touch
- `PlayerStats.cs` (Task O modifies it; not this task)
- `SaveData.cs` (Task O modifies it)
- HUD / inventory UI (Task O)
- Combat code

### 🧪 Validation
1. Compile clean
2. Project window → right-click → `Create > Wizard Grower > Weapon Definition` works
3. `WeaponDatabase.asset` Inspector shows 6 weapons populated
4. Unit-test mental check: `Recompute(base, weapon_arcane_scepter)` adds +25 to autoAttackDamage, clamps critChance to ≤ 1.0, etc.
5. `Wizard Grower → Generate Weapon Glyphs` runs without error and writes placeholder PNGs

### Implementation note
`WeaponStatComposer.Recompute` MUST document each field's combinator. Suggested doc-comment table:
```
| field                  | base op   | equipped op   | clamp        |
|------------------------|-----------|---------------|--------------|
| autoAttackDamage       | base.x    | + equipped.x  | none         |
| manualAttackDamage     | base.x    | + equipped.x  | none         |
| autoAttackInterval     | base.x    | - equipped.autoFireRateBonus | ≥ 0.05 |
| criticalChance         | base.x    | + equipped.x  | 0~1          |
| criticalMultiplier     | base.x    | + equipped.x  | ≥ 1          |
| armorPenetration       | base.x    | + equipped.x  | ≥ 0          |
| maxHealth              | base.x    | + equipped.x  | ≥ 1          |
| currentHealth          | unchanged | unchanged     | clamp ≤ max  |
| manualAttackInterval   | unchanged | unchanged     | ≥ 0.05       |
```

---

## Task O — Weapon Inventory + Equip UI + Visual Swap

**Status:** 🔴 TODO
**Depends On:** N + Tasks.md Task I ✅ (SaveData / Firestore mapper closed before bumping `saveVersion`)

### 🎯 Goal
Wire the weapon system into save/load, runtime equip changes, and UI. Equipping a weapon visibly tints the wizard, swaps the projectile, and updates ATK.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SaveData.saveVersion` bumped to 2 with migration that defaults missing fields to `wand_starter`
- [ ] First launch (delete save.json) → starter weapon auto-granted + auto-equipped
- [ ] Open inventory → 6 weapons (or just the starter, after a wipe) displayed in grid
- [ ] Click a non-equipped weapon → equip → wizard tint changes + glyph appears + projectile sprite swaps + PlayerStats updates
- [ ] Save round-trip preserves `equippedWeaponId` + `ownedWeaponIds`
- [ ] Sub-bundle 6C regression tests 1~4 pass

### 📂 Files to Add
- `Assets/Scripts/Weapons/WeaponInventory.cs` — MonoBehaviour
  ```csharp
  public class WeaponInventory : MonoBehaviour
  {
      public IReadOnlyCollection<string> OwnedWeaponIds { get; }
      public string EquippedWeaponId { get; }
      public WeaponDefinition Equipped { get; }

      public event Action<WeaponDefinition> EquippedChanged;
      public event Action<WeaponDefinition> WeaponObtained;

      public void Initialize(WeaponDatabase db);
      public void Add(string weaponId);
      public bool TryEquip(string weaponId);
      public void LoadFromSave(List<string> ownedIds, string equippedId);
      public (List<string> owned, string equipped) CaptureForSave();
  }
  ```
- `Assets/Scripts/Player/WeaponVisualController.cs` — runtime sprite tint + glyph
  ```csharp
  public class WeaponVisualController : MonoBehaviour
  {
      [SerializeField] private SpriteRenderer wizardBody;
      [SerializeField] private Vector3 glyphLocalOffset = new(0.18f, 0.05f, -0.01f);
      private GameObject glyphChild;

      public void Bind(WeaponInventory inventory);  // subscribes to EquippedChanged
      private void Apply(WeaponDefinition weapon);  // tint + glyph swap
  }
  ```
- `Assets/Scripts/UI/WeaponInventoryPanel.cs` — scrollable grid + equip click
- `Assets/Scripts/UI/WeaponSlotView.cs` + `Assets/Prefabs/UI/WeaponSlot.prefab` — single tile (icon + frame color by rarity + equipped indicator)

### 📂 Files to Modify
- `Assets/Scripts/Player/PlayerStats.cs` — split into "base + equipped delta" by adding:
  - `void RecomputeWithEquipped(WeaponStats? equipped)` (uses `WeaponStatComposer`)
  - Internally keeps a `_baseSnapshot` separate from the runtime exposed values; mutators (`AddAutoDamage` etc.) update the base; `RecomputeWithEquipped` merges into runtime view
- `Assets/Scripts/Save/SaveData.cs` — add `string equippedWeaponId` (default `"wand_starter"`), `List<string> ownedWeaponIds` (default `["wand_starter"]`)
- `Assets/Scripts/Save/SaveService.cs.MigrateIfNeeded` — handle v1 → v2: defaults the new fields if missing
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply weapon fields via `WeaponInventory.LoadFromSave / CaptureForSave`; after `ApplySnapshot`, call `RecomputeWithEquipped(equipped.statBonuses)`
- `Assets/Scripts/Save/SaveDataDocument.cs` (Task I file — add `equippedWeaponId` + `ownedWeaponIds` for Firestore round-trip; coordinate with active Tasks.md track)
- `Assets/Scripts/Combat/ProjectileFactory.cs` — accept a `Sprite` override or read from `WeaponInventory.Equipped.projectileSprite` for `FireAuto`. Manual / Skill keep current sprites.
- `Assets/Scripts/Core/GameManager.cs` — instantiate `WeaponInventory` + `WeaponVisualController`, bind in correct order (after SaveBinder.ApplyToGame so base stats are loaded, then equip applies bonus)
- `Assets/Scripts/Core/GameContext.cs` — register `WeaponInventory`, `WeaponDatabase` reference
- `Assets/Scripts/UI/HUDController.cs` — weapon-inventory toggle button

### 🚫 Do Not Touch
- `WeaponDefinition` / `WeaponDatabase` / `WeaponStatComposer` (Task N closed)
- Stage / boss / chat / presence
- AuthService / UserProfileService

### 🧪 Validation
1. Compile clean
2. Delete `save.json` → PlayMode → wizard appears with starter tint (white = no change), starter glyph (or none) → ATK = base value
3. Open inventory → see "초보의 지팡이" highlighted as equipped
4. Click "Apprentice Staff" → wizard tints to soft cyan + cyan glyph appears + ATK +5 + projectile sprite swaps
5. Quit + restart → still has Apprentice Staff equipped + ATK preserved
6. Inspect SaveData JSON → `equippedWeaponId: "apprentice_staff"`, `ownedWeaponIds: ["wand_starter", "apprentice_staff"]`, `saveVersion: 2`

### Implementation note
**Stat recompute order on launch is critical:**
1. `SaveBinder.ApplyToGame` → applies base stats via `PlayerStats.ApplySnapshot`
2. Then load weapon inventory state via `WeaponInventory.LoadFromSave`
3. **Then** call `PlayerStats.RecomputeWithEquipped(inventory.Equipped?.statBonuses)`

Skipping step 3 results in base stats + zero bonus → confused player.

---

## Task P — Gacha Service + UI + Gem Currency

**Status:** 🔴 TODO
**Depends On:** N, O ✅

### 🎯 Goal
Players can spend Gems to draw weapons by weighted rarity. Pity system guarantees Rare+ every 30 pulls. New weapons are added to the inventory.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] First launch (after save wipe) → starts with 300 gems (default migration)
- [ ] HUD shows gem count next to gold
- [ ] Open Gacha panel → see banner, 1× and 10× buttons, pity counter
- [ ] 1× pull: gem -30, weapon added (random by weight), result modal
- [ ] 10× pull: gem -270, 10 weapons added, result modal lists all 10 in rarity order
- [ ] Pity: 30 consecutive pulls without Rare+ → 30th pull guaranteed ≥ Rare; pityCounter resets
- [ ] Insufficient gems → 1× / 10× buttons disabled or show error feedback
- [ ] Save round-trip preserves `gems` and `pityCounter`
- [ ] Sub-bundle 6C regression tests 5~8 pass

### 📂 Files to Add
- `Assets/Scripts/Weapons/RarityWeight.cs`
  ```csharp
  [Serializable]
  public struct RarityWeight
  {
      public Rarity rarity;
      public float weight;
  }
  ```
- `Assets/Scripts/Weapons/GachaDefinition.cs`
  ```csharp
  [CreateAssetMenu(menuName = "Wizard Grower/Gacha Definition")]
  public class GachaDefinition : ScriptableObject
  {
      public string gachaId;          // "standard"
      public string displayName;
      public int costSingle;          // 30
      public int costTen;             // 270
      public WeaponDatabase pool;
      public RarityWeight[] weights;
      public int pityThreshold;       // 30
      public Rarity pityFloor;        // Rare
  }
  ```
- `Assets/Scripts/Weapons/GachaService.cs`
  ```csharp
  public class GachaService : MonoBehaviour
  {
      public Initialize(CurrencyWallet wallet, WeaponInventory inv, GachaDefinition definition, SaveService save);
      public bool TrySinglePull(out WeaponDefinition pulled);
      public bool TryTenPull(out List<WeaponDefinition> pulled);
      public int CurrentPity { get; }
  }
  ```
- `Assets/Scripts/UI/GachaPanel.cs` — banner art + 1× / 10× buttons + pity display
- `Assets/Scripts/UI/GachaResultPanel.cs` — modal showing pulled weapons in rarity order with fade-in
- `Assets/Scripts/UI/RarityVisuals.cs` — color/border per rarity (used by both inventory and gacha)
- `Assets/Data/Gacha/Standard.asset` — seeded weights:
  - Common: 50
  - Uncommon: 30
  - Rare: 15
  - Epic: 4.5
  - Legendary: 0.5
  - pityThreshold: 30, pityFloor: Rare

### 📂 Files to Modify
- `Assets/Scripts/Economy/CurrencyWallet.cs` — extend with:
  ```csharp
  public int Gems { get; }
  public event Action<int> GemsChanged;
  public void AddGems(int amount);
  public bool TrySpendGems(int amount);
  ```
- `Assets/Scripts/Save/SaveData.cs` — add `int gems` (default 300 in migration), `int pityCounter` (default 0)
- `Assets/Scripts/Save/SaveDataDocument.cs` — mirror in Firestore POCO
- `Assets/Scripts/Save/SaveBinder.cs` — capture / apply gems + pityCounter
- `Assets/Scripts/Save/SaveService.cs.MigrateIfNeeded` — defaults missing `gems` to 300, `pityCounter` to 0 when v1 → v2
- `Assets/Scripts/UI/HUDController.cs` — gem icon + count display + gacha toggle button
- `Assets/Scripts/Core/GameContext.cs` — register `GachaService` + `GachaDefinition` reference

### 🚫 Do Not Touch
- WeaponDefinition / WeaponDatabase / WeaponStatComposer (Task N)
- WeaponInventory / WeaponVisualController (Task O — ONLY add via its `Add(weaponId)` API; do not touch internals)
- Stage / boss / chat / presence

### 🧪 Validation
1. Compile clean
2. Delete `save.json` → PlayMode → HUD shows `Gem 300`
3. Open Gacha → 1× pull → gem 270, modal shows pulled weapon, inventory grows by 1
4. Repeat 1× until 30 pulls → 30th pull is at minimum Rare; pity counter resets to 0
5. Save state has `pityCounter: 0` and `gems: 0` after spending all
6. Save + restart → gems / pity preserved
7. With 0 gems, 1× button visibly disabled or shows "젬이 부족합니다" feedback

### Implementation note
The `WeightedRandom` helper inside GachaService should be testable in isolation — accept an `IRandom` interface so the validation can run a 1000-pull dry-run with a fixed seed and check that distribution matches weights ± reasonable variance (e.g. < 2% per category).

---

# Bundle 6 Release Gate

When all sub-bundles ✅ DONE, run a single end-to-end PlayMode session validating the entire feature surface:

1. **Cold start** (delete save.json + Firestore doc): app boots into LoginScene → splash visible ≥ 0.8s
2. **Login**: Google login → nickname registration modal → enter "TestPlayer" → MainScene loads
3. **Initial state**: HUD shows `Gold 0`, `Gem 300`, ATK reflects starter weapon (no bonus); chapter 1 stage 1 active; presence entry written to RTDB
4. **Multiplayer presence**: launch a second Editor instance (via ParrelSync) → log in as different account → enter chapter 1 stage 1 → both screens show the other player's `RemoteWizard` ghost
5. **Movement sync**: instance A moves → B sees the movement within ~300ms
6. **Chat**: A sends "안녕" in World tab → B receives → B replies in Stage tab → A receives
7. **Gacha**: A opens gacha → 10× pull → gem -270, 10 weapons appear in result modal with rarities visible
8. **Inventory + equip**: A opens inventory → equips the highest-rarity weapon → wizard tints + glyph appears + ATK increases + projectile sprite changes; B's screen also shows A's wizard with the new tint (because A's RemoteWizard.tint propagates via... WAIT — see "Future work" below)
9. **Persistence**: A quits → relaunches → all state (login, weapon, gem count, pity counter) restored

> **Future work flagged during release gate:** the current presence schema only carries `(x, y, displayName)`. To make remote players' weapons visible on each other's screens, the presence node would need to carry `equippedWeaponId` and the `RemotePlayerView` would need to resolve and apply tint/glyph. **Out of scope for v6** — listed here as a known follow-up.

---

## Appendix A — Reviewer Checklist (Per Task)

The reviewer verifies:
1. **DoD 100% met** — all checkboxes pass
2. **Files changed match spec** — no unauthorized files modified (`git diff` to confirm)
3. **"Do Not Touch" areas unchanged**
4. **Regression tests pass** — run validation steps directly
5. **Spec consistency** — implementation matches the design intent
6. **Exactly one git commit** — message includes the task ID
7. **No regression in `Tasks.md` (Bundle 1–5) tasks** — sanity check Bundle 1–5 still functional

Record review results in Appendix E.

---

## Appendix B — Sub-bundle / Bundle Gate Checklist

After the last task of a sub-bundle reaches ✅ DONE:
1. Sub-bundle combined regression tests pass
2. `git log` clean — every task in the sub-bundle has a commit
3. 0 errors / 0 warnings
4. No regressions in other sub-bundles (run their regression tests once)

After all three sub-bundles ✅ + Bundle 6 release gate runbook completes → Bundle 6 ✅.

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-08 | Planner | Document created. Bundle 6 split into 6A (J), 6B (K, L, M), 6C (N, O, P). User decisions resolved: separate LoginScene, Firebase Realtime Database for presence/chat, separate Gem currency, player sprite swap on equip. Initial weapon pool scoped to 6 weapons. RTDB user prework documented for Sub-bundle 6B. Cross-track coordination rules with `Tasks.md` (Bundle 1–5) added in §0.5. |

---

## Appendix D — Combined Work Log (implementer)

> Implementers append a row here on each task transition. Format: `YYYY-MM-DD | Task X | <one-line summary>`.

| Date | Task | Entry |
|------|------|-------|
| 2026-05-08 | Task J | Added dedicated LoginScene, splash/login prefabs, AuthBootstrapHolder handoff, MainScene bootstrapped-auth consumption, build scene order, and PlayMode validation for auto-skip/guest-to-MainScene UID/HUD flow. |

---

## Appendix E — Combined Review Log (reviewer)

> Reviewers append a row here after each code review. Format: `YYYY-MM-DD | Task X | <verdict + key findings>`.

| Date | Task | Entry |
|------|------|-------|
| (empty) | | |
