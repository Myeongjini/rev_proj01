# Wizard Grower — Tasks v11 (Bundle 11: Player Level + EXP + Skill Auto-Unlock + EXP Dungeon)

> Follow-up work track to `Tasks_v10.md`.
> Different agents, different bundle, different file.
> **Do NOT edit prior Tasks*.md from this track.** Read-only access only.
>
> The **Planner / Reviewer (Claude)** is the sole editor of this document.
> Implementers read this document, modify the code, and append to Appendix D.
>
> 장기 로드맵 (Bundle 9~30) 은 `References.md` §4.1 참조. 본 문서는 Bundle 11만 다룸.

---

## 0. Common Work Rules

(Tasks_v9/v10 의 §0 규칙을 그대로 준수. Common Work Rules / Bundle Gate Rules / Auto-Progression Restrictions / Git Commit Rules 동일. 차이만 명시:)

### 0.5 Cross-Track Coordination ⚠️
Bundle 11 builds on the v10 baseline.

| Risk | Mitigation |
|---|---|
| Save schema 변경 (v6 → v7) | Task AM이 schema bump + 마이그레이션 담당. AJ~AL은 SaveData 필드 추가만 가능 |
| `SkillCastOrchestrator` (Bundle 8 X/Y) 수정 — 잠금 슬롯 처리 추가 | Task AK 가 직접 수정. 인터페이스 호환 유지 — 기존 EquipSkill API 가 잠금 레벨 검사를 추가하는 형태 |
| Player Level 도입으로 Bundle 10 GoldDungeon Lv2~Lv5 해금 가능 | `GoldDungeonService` 의 `GoldDungeonDifficulty.unlockPlayerLevel` 로직이 Player Level 시스템 도입 시 자동 적용. Bundle 11 은 Player Level 만 도입, Bundle 10 코드 수정 X — 실시간 readonly 체크로 동작 |
| EXP Dungeon Scene 추가 — Build Settings 변경 | LoginScene=0 / MainScene=1 / GoldDungeonScene=2 / EXPDungeonScene=3 |
| 레벨업 popup 과 Bundle 7 Q 의 전투력 popup 동시 발생 가능 | 두 popup 은 별도 layer. 전투력 popup 은 1초 표시, 레벨업 popup 은 1초 표시. Stacking OK |
| 오프라인 보상 (Bundle 9) 의 EXP 누적 — Bundle 11에서 OfflineRewardCalculator 확장 | 새 EXP 보상 라인 추가. Bundle 9 베이스 골드 보상은 그대로, EXP 만 추가 |

---

## 1. Task Dependency Graph

```
Bundle 11
AJ → AK → AL → AM → Bundle 11 Release Gate

AJ:  PlayerLevelService + EXP Curve + LevelUpPopup
AK:  Skill Auto-Unlock by Level (meteor=Lv1 / cold_beam=Lv5 / charge=Lv10)
AL:  EXP Dungeon (Scene + Service + UI)
AM:  Save Schema v7 Migration + Offline Reward EXP 확장 + Cross-Feature Regression
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 11 | AJ | PlayerLevelService + EXP Curve + LevelUpPopup | 🟡 IN REVIEW | v10 baseline |
| 11 | AK | Skill Auto-Unlock by Level | 🟡 IN REVIEW | AJ ✅ |
| 11 | AL | EXP Dungeon (Scene + Service + UI) | 🔴 TODO | AK ✅ |
| 11 | AM | Save Schema v7 Migration + Offline EXP 확장 + Regression | 🔴 TODO | AL ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 11 — Player Level + EXP + Skill Auto-Unlock + EXP Dungeon

**Goal:** idle RPG 표준 캐릭터 레벨·경험치 시스템 도입. 무기 성장 축과 별개로 캐릭터 자체의 성장. 레벨에 따른 스킬 자동 해금. EXP 전용 던전.

### Bundle 11 Release Regression Tests
1. Fresh save → 캐릭터 Lv 1, 현재 EXP 0, 다음 레벨까지 100
2. 몬스터 처치 시 EXP 획득 (시드: 일반 몬스터 +10, 보스 +50)
3. EXP 100 도달 → 자동 레벨업 → "Level Up! Lv.2" 1초 popup 표시
4. 레벨업 시 +5 attack / +20 HP per level 자동 증가 (전투력 popup도 함께 표시)
5. EXP curve = 100 × 1.15^level (cumulative). Lv 1→2 = 100, Lv 2→3 = 115, Lv 3→4 = 132, ..., Lv 49→50 = 100×1.15^49 ≈ 130,861
6. Level cap = 50. Lv 50 도달 시 EXP 더 누적되어도 레벨업 X, 추가 EXP는 무시 또는 표시만
7. 스킬 자동 해금: Lv 1에서 meteor 사용 가능, Lv 5에서 cold_beam 슬롯 자동 해금, Lv 10에서 charge 슬롯 자동 해금
8. 미해금 스킬 슬롯은 잠금 표시 (자물쇠 아이콘 + "Lv N 필요" 라벨)
9. SkillTabPanel 의 카드도 잠금 상태 시각화
10. EXP Dungeon: MainUI01 의 "추가예정?" 슬롯 또는 GoldDungeon 패턴처럼 별도 진입 — Bundle 10 패턴 그대로 재사용
11. EXP Dungeon: 60초 + 처치 EXP 보상 (배수 3x), 일일 입장 3회, Sweep, 광고 2배
12. 오프라인 보상에 EXP 누적 추가: 12시간 동안 누적된 EXP 도 모달에 표시 + 받기 시 EXP +N
13. Bundle 10 GoldDungeon Lv2~Lv5: Player Level 이 unlockPlayerLevel 이상이면 자동 활성화 (재진입 시 변화 확인)
14. saveVersion = 7
15. Save + restart → playerLevel / playerExp / 스킬 해금 상태 모두 보존
16. (Cross-feature regression) Bundle 5/6/7/8/9/10 모두 정상

---

## Task AJ — PlayerLevelService + EXP Curve + LevelUpPopup

**Status:** 🟡 IN REVIEW
**Depends On:** v10 baseline

### 🎯 Goal
캐릭터 레벨·경험치 시스템 핵심 — 몬스터 처치 EXP 획득, 자동 레벨업, +5 atk / +20 HP per level 자동 증가, 레벨업 popup.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `PlayerLevelService` MonoBehaviour 신규 — 레벨/EXP 상태 + EXP 획득/레벨업 로직
- [ ] EXP curve: `expToNext(level) = round(100 * pow(1.15, level - 1))` (cumulative 아니라 next level 까지의 단일 값)
- [ ] Level cap = 50. Lv 50 도달 시 추가 EXP 무시, expToNext = -1 (또는 max)
- [ ] 레벨업 시: PlayerStats.AttackDamage += 5, PlayerStats.MaxHealth += 20, PlayerStats.CurrentHealth = MaxHealth (full heal)
  - 전투력 변경 → CombatPowerService.Recalculate(showFeedback=true) → 전투력 popup도 함께 발생
- [ ] EnemySpawner.EnemyKilled 이벤트 구독 → 일반 몬스터 +10 EXP, 보스 +50 EXP (시드값)
- [ ] `LevelUpPopupView` UI — "Level Up! Lv.N" 1초 popup (전투력 popup 과 같은 위치 인근, 시각적 차별화)
- [ ] HUD 에 EXP 바 + 현재 레벨 표시 추가 (mana 바 위 또는 아래, 또는 캐릭터 머리 위)

### 📂 Files to Add
- `Assets/Scripts/Player/PlayerLevelService.cs`
  ```csharp
  public class PlayerLevelService : MonoBehaviour
  {
      [SerializeField] private int levelCap = 50;
      [SerializeField] private int baseExpToNext = 100;
      [SerializeField] private float expCurveBase = 1.15f;
      [SerializeField] private int normalEnemyExp = 10;
      [SerializeField] private int bossEnemyExp = 50;
      [SerializeField] private int statPerLevel_Attack = 5;
      [SerializeField] private int statPerLevel_MaxHealth = 20;

      public int CurrentLevel { get; }
      public int CurrentExp { get; }
      public int ExpToNext { get; }      // 다음 레벨까지 남은 EXP

      public event Action<int> LevelChanged;             // (newLevel)
      public event Action<int, int> ExpChanged;          // (currentExp, expToNext)
      public event Action<int, int, int> LeveledUp;      // (newLevel, statAttackGained, statHpGained)

      public void Initialize(PlayerStats stats, EnemySpawner spawner, CombatPowerService combatPower);
      public void GrantExp(int amount);                  // EXP 직접 부여 (오프라인 보상, EXP 던전, 미션 보상 등)
  }
  ```
- `Assets/Scripts/UI/LevelUpPopupView.cs`
- `Assets/Scripts/UI/PlayerExpBarView.cs` — HUD 의 EXP 바
- `Assets/Prefabs/UI/LevelUpPopup.prefab`
- `Assets/Prefabs/UI/PlayerExpBar.prefab`

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  public int playerLevel = 1;
  public int playerCurrentExp = 0;
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — mirror
- `Assets/Scripts/Save/SaveBinder.cs` — capture/apply
- `Assets/Scripts/Player/PlayerStats.cs` — `Add*` mutator 가 LevelUp 시 호출됨. 직접 추가 메서드는 없음 (PlayerLevelService 가 PlayerStats.AddAttackDamage, AddMaxHealth 호출)
- `Assets/Scripts/Core/GameContext.cs` — `PlayerLevelService` + `LevelUpPopupView` + `PlayerExpBarView` 등록
- `Assets/Scripts/Core/GameManager.cs` — Auth + StageManager.Initialize 후 `playerLevelService.Initialize(stats, spawner, combatPower)` 호출
- `Assets/Scripts/UI/HUDController.cs` — PlayerExpBarView 와이어링

### 🚫 Do Not Touch
- 무기 성장 시스템 (Bundle 7) — 무기 능력치는 PlayerStats 의 base 위에 weapon delta 로 합성됨. PlayerLevelService 는 base 만 변경
- 전투력 계산 (Bundle 7 Q) 의 공식 자체는 그대로
- Bundle 5~10 시스템

### 🧪 Validation
1. Compile clean
2. Fresh save → Lv 1, currentExp = 0, expToNext = 100
3. 일반 몬스터 1마리 처치 → currentExp = 10, expToNext = 90
4. 일반 몬스터 10마리 처치 → currentExp = 0 (레벨업), Lv 2, expToNext = 115, "Level Up! Lv.2" popup, attack +5 / HP +20, 전투력 popup 동반
5. 보스 처치 → currentExp += 50
6. EXP curve 검증: Lv 2→3 = 115, Lv 3→4 = 132, Lv 49→50 = 100×1.15^49 ≈ 130,861
7. Lv 50 도달 후 EXP 누적 → 레벨업 X
8. Save + restart → playerLevel / playerCurrentExp 보존

### Implementation note
**EXP curve 공식 정밀:** `expToNext(level) = (int)Math.Round(100 * Math.Pow(1.15, level - 1))`. 1->2 = 100, 2->3 = 115 (round(115.0) = 115), 3->4 = 132 (round(132.25) = 132), 4->5 = 152 (round(151.875) = 152), ...

**LevelUpPopup vs CombatPowerPopup 위치 충돌:** Bundle 7 Q 의 전투력 popup 도 화면 위 어딘가 1초. 레벨업 popup 은 좀 더 큰 화면 중앙·상단에 1.5초 가량. 둘 다 발생 시 stacking. 레벨업 popup 이 먼저 사라지고 전투력 popup 이 이어 표시되도록 fade timing 분리.

**EXP 바 위치 권장:** mana 바 바로 위. mana 바와 시각적으로 구분 (mana 파란색, EXP 보라색·노란색).

---

## Task AK — Skill Auto-Unlock by Level

**Status:** 🟡 IN REVIEW
**Depends On:** AJ ✅

### 🎯 Goal
캐릭터 레벨에 따른 스킬 자동 해금: meteor = Lv 1, cold_beam = Lv 5, charge = Lv 10. 미해금 스킬은 슬롯 잠금 표시 + 장착 거부.

### ✅ Definition of Done
- [x] Unity Console clean
- [x] `SkillDefinition` 에 `unlockLevel: int` 필드 추가
- [x] 시드값:
  - meteor.unlockLevel = 1
  - cold_beam.unlockLevel = 5
  - charge.unlockLevel = 10
- [x] `SkillCastOrchestrator.EquipSkill(slotIndex, skillId)` 가 PlayerLevel 검사:
  - 현재 레벨 < skill.unlockLevel → return false + 피드백 "Lv N 필요"
- [x] `SkillBarSlotView` 의 슬롯이 잠금 상태일 때 자물쇠 아이콘 + "Lv N" 라벨 표시
- [x] `SkillTabPanel` 의 카드도 잠금 상태 시각화 — 어두운 오버레이 + 자물쇠 아이콘 + "Lv N 필요" 라벨, 장착 버튼 비활성
- [x] PlayerLevelService.LevelChanged 이벤트 구독 → 새 레벨에서 해금되는 스킬이 있으면 알림 popup ("새 스킬: 콜드빔 해금됨!")
- [x] 자동 슬롯 배치: 레벨 도달로 신규 해금된 스킬이 자동으로 빈 슬롯에 장착되는지 → **No, 사용자가 직접 장착해야 함** (Bundle 8 Y 의 장착 버튼 흐름 유지)

### 📂 Files to Add
- `Assets/Scripts/UI/SkillUnlockPopupView.cs` — "새 스킬 해금" 알림 popup
- `Assets/Prefabs/UI/SkillUnlockPopup.prefab`

### 📂 Files to Modify
- `Assets/Scripts/Skills/SkillDefinition.cs`
  ```csharp
  public int unlockLevel = 1;   // 신규 필드
  ```
- `Assets/Data/Skills/Meteor.asset` — unlockLevel = 1
- `Assets/Data/Skills/ColdBeam.asset` — unlockLevel = 5
- `Assets/Data/Skills/Charge.asset` — unlockLevel = 10
- `Assets/Scripts/Skills/SkillCastOrchestrator.cs`
  - `EquipSkill` 에 PlayerLevel 검사 추가
  - `Initialize` 에 PlayerLevelService 의존성 주입 추가
- `Assets/Scripts/UI/SkillBarSlotView.cs` — 잠금 상태 표시
- `Assets/Scripts/UI/SkillCardView.cs` — 잠금 상태 + 장착 버튼 비활성
- `Assets/Scripts/UI/SkillTabPanel.cs` — 잠금 카드 표시 처리
- `Assets/Scripts/Core/GameContext.cs` — SkillUnlockPopupView 등록
- `Assets/Scripts/Core/GameManager.cs` — `skillUnlockPopupView.Bind(playerLevelService, skillDatabase)` 호출

### 🚫 Do Not Touch
- AJ 의 PlayerLevelService 내부
- Bundle 8 X 의 SkillRuntime / SkillCastOrchestrator 의 Cast 로직 자체 (Equip 검사만 추가)
- 가챠 / 무기 / 메인UI01 / 채팅

### 🧪 Validation
1. Compile clean
2. Fresh save (Lv 1) → SkillTabPanel: meteor 활성, cold_beam·charge 잠금 + "Lv 5 필요" / "Lv 10 필요"
3. SkillBar: 슬롯 0 = meteor, 슬롯 1~4 = 빈 슬롯
4. cold_beam 카드의 장착 버튼 비활성. 클릭 시도 → "Lv 5 필요" 피드백
5. EXP 농사 → Lv 5 도달 → "새 스킬: 콜드빔 해금됨!" popup → cold_beam 카드 활성화 + 장착 가능
6. Lv 10 도달 → charge 해금 popup
7. Save + restart → 해금 상태가 PlayerLevel 에 의해 동적 결정 (별도 저장 X)

### Implementation note
**해금 popup vs 레벨업 popup:** 둘 다 레벨업 시 동시 발생 가능. 레벨업 popup → 전투력 popup → 해금 popup 순으로 fade in/out. 또는 큐로 직렬화.

**잠금 슬롯 시각화:** 슬롯 배경 어두운 회색 + 자물쇠 아이콘 (Generated/Icons/lock.png — Bundle 17 sound polish 시 폴리시 가능. Bundle 11에서는 placeholder 텍스트 "🔒" 나 시스템 폰트로 충분).

---

## Task AL — EXP Dungeon (Scene + Service + UI)

**Status:** 🔴 TODO
**Depends On:** AK ✅

### 🎯 Goal
EXP 전용 던전 — Bundle 10 GoldDungeon 패턴 재사용. 별도 Scene + 일일 입장 + Sweep + 광고 2배. EXP 보상 3배.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `Assets/Scenes/EXPDungeonScene.unity` 신규 (GoldDungeonScene 복제 + EXP 보상 모드)
- [ ] Build Settings: `[3] EXPDungeonScene`
- [ ] **MainUI01 진입점:** 현재 "골드던전" 이 5번 슬롯. EXP 던전 진입은 별도 위치 고민 필요 — 권장: GoldDungeonEntryPanel 의 "EXP 던전" 탭으로 합치는 방식 (한 모달 내 두 던전 선택)
- [ ] `EXPDungeonService` MonoBehaviour — GoldDungeonService 패턴 복사 + EXP 보상으로 변경
- [ ] EXP 보상: 처치당 EXP × 3.0 multiplier (시드값)
- [ ] 일일 입장 3회 (시드값) — GoldDungeon 과 별도 카운터
- [ ] Difficulty Lv1~Lv5 — Lv1 활성, Lv2~Lv5 는 PlayerLevel unlockLevel 기반 잠금
- [ ] Sweep + 광고 2배 모두 지원
- [ ] EXP 보상 받기 → PlayerLevelService.GrantExp(N) → 레벨업 가능 (기존 메커니즘 사용)

### 📂 Files to Add
- `Assets/Scenes/EXPDungeonScene.unity`
- `Assets/Scripts/Dungeons/EXPDungeonBootstrap.cs` (GoldDungeonBootstrap 패턴 복사)
- `Assets/Scripts/Dungeons/EXPDungeonService.cs` (GoldDungeonService 패턴 복사)
- `Assets/Scripts/Dungeons/EXPDungeonState.cs` (Serializable POCO)
- `Assets/Scripts/UI/EXPDungeonResultModal.cs`
- `Assets/Prefabs/UI/EXPDungeonResultModal.prefab`

### 📂 Files to Modify
- `Assets/Scripts/UI/GoldDungeonEntryPanel.cs` (Bundle 10 AF) — 상단 탭 분리: "골드 던전" / "EXP 던전". 두 탭은 같은 모달 내 다른 service binding
  - 또는 EntryPanel을 일반화하여 `DungeonEntryPanel` 로 리네이밍하는 옵션도 가능 — implementer 판단
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  public EXPDungeonState expDungeon = new();   // GoldDungeonState 와 같은 구조
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` + `SaveBinder.cs` — mirror
- `Assets/Scripts/Core/GameContext.cs` — `EXPDungeonService` 등록
- `Assets/Scripts/Core/GameManager.cs` — Initialize
- `Assets/Scripts/Core/GameStartupPopupQueue.cs` — EXPDungeonResultModal 등록 가능

### 🚫 Do Not Touch
- AJ / AK 코드
- Bundle 10 GoldDungeonScene / GoldDungeonService 의 골드 보상 로직 (참조만)

### 🧪 Validation
1. Compile clean
2. PlayMode → MainUI01 "던전" 클릭 (또는 골드던전 슬롯) → EntryPanel 표시 + 탭 "골드/EXP"
3. EXP 탭 → 잔여 입장 횟수 표시 + Lv1 활성
4. "입장" → EXPDungeonScene → 60초 자동 전투
5. 종료 → MainScene → 결과 모달 (EXP 표시, 광고 2배 옵션)
6. "받기" → playerExp += N → 가능하면 레벨업 popup
7. Sweep → bestEXPScore 그대로 받기
8. 일일 카운터: GoldDungeon 과 별도 (각 3회씩)
9. Save + restart → expDungeonState 보존

### Implementation note
**EntryPanel 통합 vs 분리:** 두 던전 모두 비슷한 UX 라 통합 모달 (탭 분리) 가 좋음. 향후 Bundle 15 강화석 던전도 같은 모달에 추가 가능.

**Build index 4 예약:** Bundle 15에서 강화석 던전 (References 241-250) 추가 시 Build Settings index 4 가 됨. 본 Bundle 11 에서는 index 3 까지만.

---

## Task AM — Save Schema v7 Migration + Offline Reward EXP 확장 + Cross-Feature Regression

**Status:** 🔴 TODO
**Depends On:** AL ✅

### 🎯 Goal
saveVersion = 7 bump + v6 → v7 마이그레이션 + Bundle 9 의 OfflineRewardCalculator 에 EXP 누적 추가 + Bundle 5/6/7/8/9/10 회귀 시험.

### ✅ Definition of Done
- [ ] Unity Console clean
- [ ] `SaveData.saveVersion = 7`
- [ ] v6 → v7 마이그레이션:
  - `playerLevel = 1`, `playerCurrentExp = 0`, `expDungeon = new()` default-fill
- [ ] Bundle 9 의 `OfflineRewardCalculator.CalculateGold` 옆에 `CalculateExp(...)` 추가
  - 시드: 처치당 EXP × elapsed × 효율 0.5 (오프라인 효율 페널티)
- [ ] `OfflineRewardSnapshot` 에 `baseExp` / `maxAdMultipliedExp` 추가
- [ ] OfflineRewardModal UI 에 EXP 표시 라인 추가 (`골드: <X>` + `EXP: <Y>`)
- [ ] Claim 시 골드와 EXP 모두 지급
- [ ] Bundle 11 release regression 1~16번 모두 통과

### 📂 Files to Add
- (None)

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — `public int saveVersion = 7;`
- `Assets/Scripts/Save/SaveService.cs` — v6→v7 분기 추가
- `Assets/Scripts/Offline/OfflineRewardCalculator.cs` — `CalculateExp` 추가
- `Assets/Scripts/Offline/OfflineRewardService.cs` — `OfflineRewardSnapshot` 확장 + ClaimAsync 가 EXP 지급도 처리
- `Assets/Scripts/UI/OfflineRewardModal.cs` — EXP 라인 표시
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` — mirror

### 🚫 Do Not Touch
- AJ / AK / AL 코드
- v6 baseline 데이터 모델 (Bundle 10 까지)

### 🧪 Validation
1. Compile clean
2. v6 save 로딩 → playerLevel=1, playerCurrentExp=0 default-fill
3. Bundle 11 regression 1~16번 전체 통과
4. **Cross-feature regression**:
   - Bundle 10 Gold Dungeon → Lv1만 활성 → Player Level 5 도달 → Lv2 자동 활성화 (재입장 시 갱신)
   - Bundle 9 오프라인 보상 모달 → 골드 + EXP 두 줄 표시
   - Bundle 8 출석 / 일일 미션 / 스킬 바 / 가챠 / 무기 모두 정상
   - Bundle 6 채팅 / 위치공유 정상
   - Bundle 5 Cloud Sync 라운드트립 정상

### Implementation note
**Bundle 10 GoldDungeon Lv2~Lv5 자동 해금:** GoldDungeonService 가 PlayerLevelService 를 readonly 로 참조하면 됨. Bundle 11 에서 GameManager 가 GoldDungeonService.AttachPlayerLevel(playerLevelService) 호출 한 줄 추가.

---

# Bundle 11 Release Gate

When Tasks AJ~AM are all `✅ DONE`, run one integrated PlayMode session:

1. Delete local save and Firestore game document
2. Start from LoginScene → MainScene → Lv 1 + EXP 0/100 표시
3. 일반 몬스터 처치 → EXP +10 + EXP 바 갱신
4. 10마리 처치 → 자동 레벨업 → Lv 2 popup + atk +5 / HP +20 + 전투력 popup
5. SkillTab 열기 → meteor 활성, cold_beam·charge 잠금 + Lv N 필요
6. EXP 농사 → Lv 5 → 콜드빔 해금 popup
7. Lv 10 → 돌진 해금 popup
8. EXPDungeonEntry → Lv1 활성 → 60초 전투 → 결과 모달 → 광고 2배 → EXP +N×2
9. Lv 5 도달 → GoldDungeon Entry 재진입 → Lv 2 자동 활성화
10. 정상 종료 → 1시간 후 재접속 → OfflineRewardModal → 골드 + EXP 두 줄 표시 → 받기 → playerExp +N (레벨업 가능)
11. Save + restart → 모든 v11 상태 보존
12. v6 save 마이그레이션 → 모든 v10 진척 보존
13. (Cross-feature regression) Bundle 5/6/7/8/9/10 모두 정상

---

## Appendix A — Reviewer Checklist (Per Task)

(Tasks_v9/v10 의 Appendix A 와 동일 — DoD 100% / Files match / Do Not Touch / Regression / Spec consistency / 1 commit / no prior bundle regression)

---

## Appendix B — Bundle Gate Checklist

After Task AM reaches `✅ DONE`:
1. Bundle 11 release regression tests pass (전 13개)
2. `git log` clean — every task AJ~AM has exactly one implementation commit
3. Unity Console 0 errors / 0 warnings
4. Save migration v6 → v7 tested
5. No unauthorized edits to prior `Tasks*.md`

---

## Appendix C — Change History

| Date | Author | Change |
|------|--------|--------|
| 2026-05-09 | Planner | Document created. Bundle 11 = Player Level + EXP + Skill Auto-Unlock + EXP Dungeon. 4개 Task (AJ/AK/AL/AM). 사용자 결정사항: Level cap 50, EXP curve = round(100 × 1.15^(level-1)), per-level +5 atk / +20 HP, 스킬 unlock Lv1 meteor / Lv5 cold_beam / Lv10 charge. EXP Dungeon 은 Bundle 10 패턴 재사용 — EntryPanel 통합 (탭 분리). Offline Reward 의 EXP 누적은 Task AM에서 OfflineRewardCalculator 확장으로 추가. Bundle 10 GoldDungeon Lv2~Lv5 는 Player Level 도입으로 자동 해금. |

---

## Appendix D — Combined Work Log (implementer)

| Date | Task | Entry |
|------|------|-------|
| 2026-05-11 | Task AJ | Added `PlayerLevelService`, `LevelUpPopupView`, `PlayerExpBarView`, lightweight UI prefabs, player level/EXP save fields, Firestore mapper mirror, SaveBinder capture/apply/autosave wiring, GameContext/GameManager service binding, and HUD EXP bar runtime creation. Validation PASS: `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors / 0 warnings; MCP validation confirmed fresh Lv1 EXP 0/100, curve values Lv1=100/Lv2=115/Lv3=132/Lv4=152/Lv49 formula, EXP grants, Lv2 level-up, ATK +5, HP +20, full heal, combat power increase, Lv50 cap ignoring extra EXP, EXP bar binding, LevelUpPopup binding, and save mapper round-trip for `playerLevel/playerCurrentExp`. Unity Console residual entries are MCP stale-client disconnect logs only; no Task AJ product error was found. |
| 2026-05-11 | Task AK | Added skill unlock levels, PlayerLevel-gated `EquipSkill` with `"Lv N 필요"` feedback, locked skill bar/card UI states, `SkillUnlockPopupView` + prefab, and GameContext/GameManager binding. Validation PASS: `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors with 4 pre-existing Chat/Presence serialization warnings; MCP validation confirmed asset unlock seeds meteor=1/cold_beam=5/charge=10, Lv1 cold/charge lock, failed Lv1 equip feedback, Lv5 cold_beam equip, Lv10 charge equip, locked card label/button state, and locked slot label/button state. Unity Console product errors were not found; residual entries are MCP stale-client logs, and the Editor reported a stale `isCompiling=True` state after script import so the newly added popup type will finish loading on the next editor compilation/reload. |

---

## Appendix E — Combined Review Log (reviewer)

| Date | Task | Entry |
|------|------|-------|
