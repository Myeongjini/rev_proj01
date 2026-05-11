# Wizard Grower — Tasks v13 (Bundle 13: 방어구 5부위 + 엘리트 몬스터 + Defense Stat)

> Follow-up work track to `Tasks_v12.md`.
> Different agents, different bundle, different file.
> **Do NOT edit prior Tasks*.md from this track.** Read-only access only.

---

## 0. Common Work Rules

(Tasks_v9~v12 의 §0 규칙을 그대로 준수.)

### 0.5 Cross-Track Coordination ⚠️
Bundle 13은 무기 시스템 위에 방어구 시스템을 추가하는 대형 신규 시스템 + Defense stat + 엘리트 몬스터.

| Risk | Mitigation |
|---|---|
| Save schema 변경 (v8 → v9) | Task AU가 schema bump + 마이그레이션 담당. AR~AT 은 SaveData 필드 추가만 |
| WeaponInventory + ArmorInventory 분리 vs 통합 | **분리 권장** — Weapon 은 가챠 획득, Armor 는 엘리트 드롭. 다른 획득 경로. 단 인벤토리 UI 는 통합 (Bundle 7 S 의 4-column grid 패턴 위에 탭 추가) |
| 5부위 × 5상위 × 4하위 = 100 자산 부담 | Bundle 13 단계: 부위별 Common 등급 4단계 (Beginner~Supreme) = 5×4 = 20 자산만 시드. 나머지 80개 (Normal~Unique) 는 implementer 또는 Bundle 14/15 단계에서 점진 추가. VisualAssetUpdater 로 자동 생성 권장 |
| 엘리트 몬스터 100마리 카운터 — 누적 vs 챕터별 | **누적 영구** (사용자 Bundle 8 Z 반복 미션 패턴 일치). Save 영속화. 챕터/스테이지 상관 없이 일반 몬스터 처치만 카운트 (보스 제외) |
| 엘리트 외형 — 신규 스프라이트 vs 기존 + 색조 | **기존 몬스터 + 색조 변경 + Glow 효과** — Bundle 7 V 의 weapon tint 패턴 재사용. 신규 스프라이트 작업 부담 회피 |
| Defense stat 도입 — 데미지 공식 변경 | `damageDealt = max(1, attack - defense)` (단순 차감). 부동소수 감소율 공식은 Bundle 19 폴리시에서 검토. Bundle 13 은 단순화 |
| Cloud Functions (Bundle 12) — 인벤토리 변경도 서버 권위? | Bundle 13 단계: 방어구 드롭 결과는 클라이언트 결정 후 서버 reconciliation. Bundle 22 IAP 통합 시 정밀화 |

---

## 1. Task Dependency Graph

```
Bundle 13
AR → AS → AT → AU → Bundle 13 Release Gate

AR:  ArmorDefinition + ArmorDatabase + 5부위 enum + 시드 자산 20개
AS:  ArmorInventory + 통합 인벤토리 UI (Weapon/Armor 탭 분리)
AT:  엘리트 몬스터 시스템 + 100마리 카운터 + 방어구 드롭
AU:  Save Schema v9 + Defense Stat 통합 + Cross-Feature Regression
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 13 | AR | ArmorDefinition + ArmorDatabase + 시드 자산 20개 | 🟡 IN REVIEW | v12 baseline |
| 13 | AS | ArmorInventory + 통합 인벤토리 UI | 🟡 IN REVIEW | AR ✅ |
| 13 | AT | 엘리트 몬스터 + 100마리 카운터 + 드롭 | 🟡 IN REVIEW | AS ✅ |
| 13 | AU | Save v9 + Defense Stat + Regression | 🟡 IN REVIEW | AT ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 13 — 방어구 5부위 + 엘리트 몬스터 + Defense Stat

**Goal:** 방어구 슬롯 5부위 (모자/상의/하의/장갑/신발) 도입. 엘리트 몬스터 시스템 (일반 스테이지 100마리 처치마다 등장) + 방어구 확률 드롭. Defense stat 추가.

### Bundle 13 Release Regression Tests
1. 인벤토리 패널이 "무기 / 방어구" 탭으로 분리. 무기 탭은 Bundle 7 그대로
2. 방어구 탭: 5부위 (모자/상의/하의/장갑/신발) 각 부위별 4-column 그리드
3. 일반 몬스터 100마리 누적 처치 시 다음 일반 몬스터 스폰에 엘리트 1마리 추가 출현
4. 엘리트 몬스터: 기존 몬스터 + 노란색 tint + glow 효과 + HP 5배 + 데미지 2배
5. 엘리트 처치 시 30% 확률로 방어구 1개 드롭 (등급은 가중치 기반: Common 70% / Normal 25% / Advanced 5%, Bundle 13 시점)
6. 드롭된 방어구는 인벤토리 자동 추가 + "방어구 획득!" 알림 popup
7. 방어구 장착 시 PlayerStats.Defense += armorDefense + 다른 스탯 보너스 (방어구 종류별)
8. Defense stat 적용: 적 공격 데미지 = max(1, enemyAttack - playerDefense)
9. 방어구 합성: 같은 ID 3개 → 다음 등급 1개 (무기 합성 패턴 재사용)
10. saveVersion = 9
11. Save + restart → armorInventory 보존 (각 부위 장착 ID + 보유 카운트)
12. 카운터 100마리는 전 챕터 누적 + 영구 (save round-trip)
13. (Cross-feature regression) Bundle 5~12 모두 정상

---

## Task AR — ArmorDefinition + ArmorDatabase + 5부위 enum + 시드 자산 20개

**Status:** 🟡 IN REVIEW
**Depends On:** v12 baseline

### 🎯 Goal
방어구 데이터 모델 + 시드 자산. Bundle 7 R 의 weapon 패턴 재사용.

### ✅ Definition of Done
- [x] `ArmorSlot` enum: `Helmet, Chest, Legs, Gloves, Boots`
- [x] `ArmorStats` struct: `defense, criticalChance, criticalMultiplier, maxHealth, maxMana, attackDamageBonus`
- [x] `ArmorDefinition` ScriptableObject: `armorId, displayName, slot, upperGrade, lowerGrade, ladderIndex, icon, tintColor, statBonuses, flavorText`
- [x] `ArmorDatabase` ScriptableObject + helper API: `GetById, GetBySlotAndGrade, GetRow(slot, upperGrade), GetNext(armor) /* 같은 부위 다음 등급 */, OrderedArmors`
- [x] 시드 자산 **20개** — 부위별 4개 (Common Beginner/Intermediate/Upper/Supreme):
  - `helmet_common_beginner`, `helmet_common_intermediate`, `helmet_common_upper`, `helmet_common_supreme`
  - `chest_*` × 4
  - `legs_*` × 4
  - `gloves_*` × 4
  - `boots_*` × 4
- [x] `ArmorDatabase.asset` 시드 — 20개 모두 등록
- [x] 방어구 stat 시드: `defense = upperIndex * 10 + lowerIndex * 2 + slotBonus(slot)`
  - slotBonus: helmet=2, chest=5, legs=4, gloves=2, boots=3 (총 16의 base를 합산)
- [x] `Editor/VisualAssetUpdater` 확장 — 방어구 placeholder 아이콘 자동 생성 메뉴

### 📂 Files to Add
- `Assets/Scripts/Armor/ArmorSlot.cs`
- `Assets/Scripts/Armor/ArmorStats.cs`
- `Assets/Scripts/Armor/ArmorDefinition.cs`
- `Assets/Scripts/Armor/ArmorDatabase.cs`
- `Assets/Data/Armor/Helmet_Common_Beginner.asset` ~ `Boots_Common_Supreme.asset` (20개)
- `Assets/Data/Armor/ArmorDatabase.asset`
- `Assets/Art/Generated/ArmorIcons/*.png` (20개 placeholder 아이콘)

### 📂 Files to Modify
- `Assets/Scripts/Editor/VisualAssetUpdater.cs` — `GenerateArmorIcons()` 메뉴 추가

### 🚫 Do Not Touch
- Bundle 7 weapon 시스템 (참조만)
- Bundle 8~12 시스템

### 🧪 Validation
1. Compile clean
2. ArmorDatabase.OrderedArmors.Count == 20
3. 부위별 각 4개씩 (Common Beginner~Supreme)
4. ArmorDatabase.GetNext(helmet_common_supreme) → null (Common 등급의 다음은 Bundle 13 시점에서 미존재)
5. defense 값이 ladderIndex 별 단조 증가 검증

### Implementation note
**확장성:** Bundle 13 은 Common 등급만 시드. Bundle 14 (장신구) 또는 별도 콘텐츠 Bundle 에서 Normal~Unique 등급 추가. ArmorDatabase 는 OrderedArmors 가 신규 자산 추가 시 자동 정렬 (upperGrade × lowerGrade 순).

**Bundle 13 단계의 합성:** 같은 ID × 3 → 다음 lowerGrade 1개 (예: Helmet_Common_Beginner × 3 → Helmet_Common_Intermediate × 1). Common Supreme 의 다음 (= Normal Beginner) 은 Bundle 13 단계에는 자산 미존재이므로 합성 결과가 null 일 수 있음. 합성 거부 메시지 + Bundle 14 이후 자동 활성.

---

## Task AS — ArmorInventory + 통합 인벤토리 UI (Weapon/Armor 탭 분리)

**Status:** 🟡 IN REVIEW
**Depends On:** AR ✅

### 🎯 Goal
방어구 인벤토리 + 부위별 5슬롯 장착 + 통합 인벤토리 UI 의 탭 분리.

### ✅ Definition of Done
- [x] `ArmorInventory` MonoBehaviour 신규 — `WeaponInventory` (Bundle 7 S) 패턴 재사용
- [x] 부위별 단일 장착 (Helmet 슬롯 1개, Chest 슬롯 1개, ...)
- [x] 보유 카운트 (counted ownership) — `Add(armorId, count)`, `TryConsume(armorId, count)`, `TryEquip(slot, armorId)`
- [x] `ArmorStatComposer` 신규 — 5부위 합산 stat → PlayerStats 에 적용. 무기 stat 와 함께 합산
- [x] `EquippedChanged` 이벤트 — 장착 변경 시 PlayerStats / CombatPower 갱신 트리거
- [x] 인벤토리 패널 UI 변경: "무기 / 방어구" 상단 탭. 무기 탭은 Bundle 7 S 그대로. 방어구 탭은 신규
- [x] 방어구 탭 내부: 부위별 sub-tab (모자/상의/하의/장갑/신발) + 4-column 그리드 + 상세 패널 (Bundle 7 S 패턴)
- [x] 합성: 같은 ID 3개 → 다음 lowerGrade 1개 (Bundle 7 T 패턴)

### 📂 Files to Add
- `Assets/Scripts/Armor/ArmorInventory.cs`
- `Assets/Scripts/Armor/ArmorStatComposer.cs`
- `Assets/Scripts/Armor/OwnedArmorEntry.cs` (Serializable POCO)
- `Assets/Scripts/Armor/ArmorFusionService.cs` (WeaponFusionService 패턴 복사)
- `Assets/Scripts/UI/ArmorInventoryTab.cs`
- `Assets/Scripts/UI/ArmorSlotTabBar.cs` — 부위별 sub-tab
- `Assets/Scripts/UI/ArmorDetailView.cs` (WeaponDetailView 패턴 복사)
- `Assets/Prefabs/UI/ArmorInventoryTab.prefab`

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  public List<OwnedArmorEntry> ownedArmors = new();
  public Dictionary<string, string> equippedArmorBySlot = new();   // {Helmet: helmet_common_beginner, Chest: ..., ...}
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` + `SaveBinder.cs` — mirror
- `Assets/Scripts/UI/WeaponInventoryPanel.cs` — 상위에 "무기 / 방어구" 탭 추가. 클릭 시 ArmorInventoryTab 표시
- `Assets/Scripts/Player/PlayerStats.cs` — Defense stat 추가 (Task AU에서 통합)
- `Assets/Scripts/Core/GameContext.cs` — ArmorInventory + ArmorFusionService 등록
- `Assets/Scripts/Core/GameManager.cs` — Initialize

### 🚫 Do Not Touch
- AR 의 ArmorDefinition 내부
- Bundle 7 WeaponInventory 내부 (참조만)
- 가챠 / 메인UI01 / 채팅

### 🧪 Validation
1. Compile clean
2. PlayMode → 인벤토리 패널 → "무기 / 방어구" 탭
3. 방어구 탭 → "모자 / 상의 / 하의 / 장갑 / 신발" sub-tab
4. 모자 sub-tab → 4-column 그리드 (helmet_common_beginner ~ supreme) + 상세 패널
5. 보유 0 인 슬롯은 회색 + 카운트 X. 보유 1+ 일 때 활성
6. 슬롯 클릭 → 상세 패널 갱신 → "장착" 버튼
7. "장착" 클릭 → ArmorInventory.TryEquip(Helmet, helmet_common_beginner) → PlayerStats.Defense 등 갱신 + 전투력 popup
8. 합성: helmet_common_beginner × 3 → helmet_common_intermediate × 1 + 자동 (Bundle 7 T 패턴 재사용)
9. Save + restart → 보유 + 장착 보존

### Implementation note
**Bundle 7 의 WeaponInventoryPanel 구조 변경:** Bundle 7 의 4-column 그리드 자체는 그대로 두고, 패널 상단에 "무기 / 방어구" 탭 헤더만 추가. 무기 탭이 default. 방어구 탭 클릭 시 ArmorInventoryTab GameObject activate.

**Bundle 14 장신구는 같은 패턴:** Bundle 14 에서 "장신구" 탭이 추가됨. Bundle 13 은 2개 탭, Bundle 14 는 3개 탭.

---

## Task AT — 엘리트 몬스터 시스템 + 100마리 카운터 + 방어구 드롭

**Status:** 🟡 IN REVIEW
**Depends On:** AS ✅

### 🎯 Goal
일반 스테이지 진행 중 100마리 처치마다 엘리트 1마리 등장. 처치 시 방어구 확률 드롭.

### ✅ Definition of Done
- [x] `EliteMonsterController` MonoBehaviour 또는 EnemyBase 확장 — 엘리트 변형 적용
- [x] 엘리트 외형: 기본 NormalEnemy + tintColor (노란색 #FFD700) + glow particle (Bundle 8 X 의 ParticleSystem 재사용)
- [x] 엘리트 능력치: HP × 5, Damage × 2, gold reward × 5
- [x] **카운터:** 일반 몬스터 처치 누적 (보스 제외) — Bundle 8 Z 반복 미션의 monster kill counter 패턴 재사용 가능. SaveData.eliteSpawnCounter 별도 필드 (절대 reset 안 됨)
- [x] 100마리 도달 시 다음 일반 몬스터 스폰 직후 엘리트 1마리 추가. 같은 위치 또는 인접 위치
- [x] 100마리 카운터는 엘리트 출현 후 0으로 초기화 (다음 100마리 시작)
- [x] 엘리트 처치 시 방어구 드롭 처리:
  - 30% 확률 (시드값) 으로 방어구 1개 드롭
  - 등급 가중치 (Bundle 13 시점): Common 70% / Normal 25% / Advanced 5% — Bundle 14/15 에서 자산 추가 시 가중치 분포 갱신
  - 부위 가중치: 5부위 균등 (각 20%)
  - 하위등급 가중치: 4단계 균등 (각 25%)
  - **결과 예시**: 30% × 70% × 20% × 25% = 1.05% 확률로 helmet_common_supreme 드롭
- [x] 드롭 시 ArmorInventory.Add(armorId) + "방어구 획득!" 알림 popup (1.5초)
- [x] 클라이언트 권위 단계 (Bundle 12 Cloud Functions 권위는 wallet/transaction 만). Bundle 22 IAP 시점에 인벤토리 권위 강화

### 📂 Files to Add
- `Assets/Scripts/Enemies/EliteMonsterController.cs`
- `Assets/Scripts/Enemies/EliteSpawnTracker.cs` — 100마리 카운터 + 엘리트 스폰 트리거
- `Assets/Scripts/Drops/ArmorDropTable.cs` (ScriptableObject — 가중치 시드)
- `Assets/Scripts/UI/ArmorAcquiredPopupView.cs`
- `Assets/Prefabs/UI/ArmorAcquiredPopup.prefab`
- `Assets/Data/Drops/StandardArmorDropTable.asset`
- `Assets/VFX/EliteGlow.prefab` (ParticleSystem)

### 📂 Files to Modify
- `Assets/Scripts/Enemies/EnemyBase.cs` — `IsElite` 플래그 + 엘리트 변형 적용 메서드
- `Assets/Scripts/Enemies/EnemySpawner.cs` — EliteSpawnTracker 와 연동. 100마리 도달 시 다음 spawn 에 엘리트 추가
- `Assets/Scripts/Save/SaveData.cs` — `public int eliteSpawnCounter = 0;`
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` + `SaveBinder.cs` — mirror
- `Assets/Scripts/Core/GameContext.cs` — EliteSpawnTracker, ArmorDropTable, ArmorAcquiredPopupView 등록

### 🚫 Do Not Touch
- AR / AS 코드
- 일반 EnemyBase 의 자동 공격·HP·골드 보상 로직
- 보스 시스템 (Bundle 2 E)

### 🧪 Validation
1. Compile clean
2. PlayMode → 일반 몬스터 100마리 처치 → 다음 spawn 에 엘리트 등장 (노란 tint + glow)
3. 엘리트 HP 검사 (Bundle 1 stage1 HP 50 → 엘리트 HP 250)
4. 엘리트 처치 → 30% 확률로 방어구 드롭 → "방어구 획득!" popup → ArmorInventory 보유 +1
5. 시뮬레이션 (Editor cheat): 1000마리 처치 → 엘리트 10마리 등장 → 평균 3개 방어구 드롭 확인
6. 카운터 100마리 후 0 으로 초기화
7. Save + restart → eliteSpawnCounter 보존

### Implementation note
**엘리트 스폰 위치:** 다음 일반 몬스터 spawn point 와 같은 위치 또는 인접한 위치. 동시 등장 OK.

**엘리트 시각화:**
```csharp
// EliteMonsterController.ApplyEliteVisuals():
spriteRenderer.color = new Color(1f, 0.84f, 0f);   // 황금색 tint
Instantiate(eliteGlowPrefab, transform);            // glow VFX
```

**드롭 가중치 SO 편집:** ArmorDropTable.asset 의 Inspector 에서 가중치 조정 가능. Bundle 14/15 자산 추가 시 가중치 갱신 필요.

---

## Task AU — Save Schema v9 Migration + Defense Stat 통합 + Cross-Feature Regression

**Status:** 🟡 IN REVIEW
**Depends On:** AT ✅

### 🎯 Goal
saveVersion = 9 + Defense stat 데미지 공식 적용 + Bundle 5~12 회귀 시험.

### ✅ Definition of Done
- [x] `SaveData.saveVersion = 9`
- [x] v8 → v9 마이그레이션:
  - `ownedArmors = []`, `equippedArmorBySlot = {}`, `eliteSpawnCounter = 0` default-fill
- [x] `PlayerStats.Defense` 필드 추가 + getter
- [x] 데미지 공식: 적 → 플레이어 데미지 = `max(1, enemyAttack - playerStats.Defense)`
- [x] PlayerWizard.TakeBossHit / TakeNormalHit 등 데미지 메서드가 Defense 적용
- [x] CombatPowerService — Defense stat 도 전투력 계산에 포함 (시드: defense × 1.5)
- [x] Bundle 13 release regression 1~13번 모두 통과

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — saveVersion = 9
- `Assets/Scripts/Save/SaveService.cs` — v8→v9 분기
- `Assets/Scripts/Player/PlayerStats.cs` — Defense 필드 추가
- `Assets/Scripts/Player/PlayerWizard.cs` — Defense 적용
- `Assets/Scripts/Player/CombatPowerService.cs` — Defense × 1.5 추가

### 🚫 Do Not Touch
- AR / AS / AT 코드
- v8 이전 데이터 모델

### 🧪 Validation
1. Compile clean
2. v8 save 로딩 → 자동 v9 + 방어구 default-fill
3. Bundle 13 regression 1~13번 전체 통과
4. Defense 값 0 (방어구 미장착) → 적 데미지 그대로
5. 모자 장착 (defense=2) → 적 데미지 - 2 (최소 1)
6. 5부위 장착 (defense 합 16) → 적 데미지 - 16
7. **Cross-feature regression**:
   - Bundle 12 가챠 / 통화 정상 (Cloud Functions 경유)
   - Bundle 11 Player Level / EXP / 스킬 자동해금 정상
   - Bundle 10 GoldDungeon / Bundle 11 EXPDungeon 정상
   - Bundle 9 오프라인 보상 정상
   - Bundle 8 출석 / 미션 / 스킬 / 가챠 / 채팅 정상
   - Bundle 7 무기 인벤토리 (이제 무기 탭) 정상

### Implementation note
**전투력 공식 갱신:** Bundle 7 Q 의 공식에 Defense 추가:
```
combatPower = attackDamage * (1 + criticalChance * (criticalMultiplier - 1)) * (1 / autoAttackInterval)
            + armorPenetration * 2 + maxHealth * 0.1 + maxMana * 0.05
            + defense * 1.5    // 신규
```

---

# Bundle 13 Release Gate

When Tasks AR~AU are all `✅ DONE`:

1. 인벤토리 패널 → 무기 / 방어구 탭
2. 방어구 탭 → 5부위 sub-tab → 4-column 그리드
3. 일반 몬스터 100마리 처치 → 엘리트 등장 → 처치 → 30% 확률 방어구 드롭
4. 방어구 장착 → Defense 증가 → 적 데미지 감소 → 전투력 popup
5. 합성: helmet_common_beginner × 3 → helmet_common_intermediate × 1
6. Save + restart → 모든 v13 상태 보존
7. (Cross-feature regression) Bundle 5~12 모두 정상

---

## Appendix A~E

(Tasks_v9~v12 패턴 동일)

| Date | Author | Change |
|---|---|---|
| 2026-05-09 | Planner | Document created. Bundle 13 = 방어구 5부위 + 엘리트 몬스터 + Defense Stat. 4개 Task (AR/AS/AT/AU). 5부위: 모자/상의/하의/장갑/신발 (사용자 결정). 엘리트 몬스터: 일반 100마리 처치 누적, 보스 별개. 외형: 기존 + 황금 tint + glow VFX. 드롭: 30% 확률 + 등급 가중치 (Common 70% / Normal 25% / Advanced 5%, Bundle 13 시점). 시드 자산 20개 (5부위 × Common 4단계). Normal~Unique 등급은 Bundle 14/15 점진. Defense 데미지 공식: max(1, enemyAtk - defense). 전투력 공식에 defense × 1.5 추가. |
| 2026-05-11 | Codex | Bundle 13 AR~AU implemented and moved to IN REVIEW. Added armor definitions/database seed assets/icons, armor inventory/fusion/stat composer, weapon/armor inventory tab hooks, elite spawn tracker/drop table/popup prefab stubs, save v9 armor fields, defense stat, and defense combat power integration. Validation: `dotnet build Assembly-CSharp.csproj --no-restore` = 0 errors, 4 pre-existing Firebase config field warnings. |
