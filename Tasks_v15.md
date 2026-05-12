# Wizard Grower — Tasks v15 (Bundle 15: 강화 시스템 + 강화석 던전)

> Follow-up work track to `Tasks_v14.md`. **Do NOT edit prior Tasks*.md.**

---

## 0. Common Work Rules

(Tasks_v9~v14 의 §0 규칙을 그대로 준수.)

### 0.5 Cross-Track Coordination ⚠️

| Risk | Mitigation |
|---|---|
| Save schema 변경 (v10 → v11) | Task BC가 schema bump + 마이그레이션 담당 |
| 강화석 currency 신설 — Bundle 12 의 ServerCurrencyAuthority 와 통합 | spendCurrency / grantCurrency Cloud Functions 가 `kind: "enhancement_stone"` 추가 처리. functions/src/currency.ts 확장 |
| 강화 능력치 증가 — 무기/방어구/장신구 모두 적용 | 각 인벤토리의 OwnedXxxEntry 에 `enhancementLevel: int` 필드 추가. StatComposer 가 (base + enhancement) 합산 |
| 강화 비용 곡선 + 강화 성공률 | 시드: 비용 = `100 × 1.5^level`, 성공률 = 100% (단순화) — 실패 시스템은 향후 폴리시 |
| 강화석 던전 — Bundle 10 GoldDungeon 패턴 | 거의 동일 구조. EntryPanel 통합 (골드/EXP/강화석 3 탭) |
| 메인UI01 강화 슬롯 — 기존 능력치 업그레이드 vs 신규 장비 강화 | **기존 강화 슬롯에 sub-tab 추가**: "능력치 강화" (기존) / "장비 강화" (신규). 또는 신규는 **인벤토리 패널 상세에 "강화" 버튼 통합**. 권장: 인벤토리 상세 통합 |
| 합성 vs 강화 동시성 | 합성: 같은 ID 3개 → 다음 등급. 강화: 같은 아이템 강화 레벨 +1. 별개 시스템. 강화된 아이템도 합성 가능 (단 강화 레벨은 손실) |

---

## 1. Task Dependency Graph

```
Bundle 15
AZ → BA → BB → BC → Bundle 15 Release Gate

AZ:  강화석 currency 신설 + 강화석 던전 Scene/Service
BA:  EnhancementService + 강화 비용 공식 + 무기/방어구/장신구 통합 강화
BB:  강화 UI (인벤토리 상세에 "강화" 버튼 통합) + 강화 모달
BC:  Save Schema v11 + Cross-Feature Regression
```

---

## 2. Task Status Board

| Bundle | ID | Title | Status | Depends On |
|---|---|---|---|---|
| 15 | AZ | 강화석 currency + 강화석 던전 Scene/Service | 🔴 TODO | v14 baseline |
| 15 | BA | EnhancementService + 강화 비용 + 통합 강화 | 🔴 TODO | AZ ✅ |
| 15 | BB | 강화 UI (인벤토리 상세 통합) + 강화 모달 | 🔴 TODO | BA ✅ |
| 15 | BC | Save v11 + Regression | 🔴 TODO | BB ✅ |

Status legend: 🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED

---

# Bundle 15 — 강화 시스템 + 강화석 던전

**Goal:** 무기/방어구/장신구 모든 슬롯에 대해 "강화 레벨" 도입 — 강화석 currency 사용. 강화 시 능력치 증가. 강화석 던전 신설 (Bundle 10 패턴 재사용).

### Bundle 15 Release Regression Tests
1. 강화석 currency 신설 — HUD 에 gold·gem 옆에 강화석 표시 (아이콘 + 수량)
2. 강화석 던전: EntryPanel "강화석" 탭 → 60초 자동 전투 → 처치당 강화석 보상 → 결과 모달
3. 강화석 던전 일일 입장 3회 + Sweep + 광고 2배 (Bundle 10 패턴)
4. 인벤토리 상세 패널에 "강화" 버튼 추가 (기존 "장착" 옆)
5. "강화" 클릭 → 강화 모달: 현재 강화 레벨 + 다음 레벨 능력치 + 비용 (강화석 N개) + 강화 버튼
6. 강화 비용 곡선: `cost(level) = round(100 × 1.5^level)`. 0→1 = 100, 1→2 = 150, 2→3 = 225, ..., 9→10 = 100×1.5^9 ≈ 3,844
7. 강화 레벨 cap = 10
8. 강화 시 능력치 증가: `enhancementBonus = baseStats × 0.1 × level` (시드값)
9. 강화 성공률: 100% (단순화 — 실패 시스템 향후 폴리시)
10. 강화석 부족 시 강화 버튼 비활성 + "강화석이 부족합니다" 피드백
11. saveVersion = 11
12. 합성된 결과물의 강화 레벨: 0 (강화는 합성 시 손실)
13. Save + restart → 강화 레벨 보존
14. (Cross-feature regression) Bundle 5~14 모두 정상

---

## Task AZ — 강화석 Currency + 강화석 던전 Scene/Service

**Status:** 🔴 TODO
**Depends On:** v14 baseline

### 🎯 Goal
강화석을 별도 currency 로 신설. Cloud Functions wallet 확장. 강화석 던전 Scene + Service.

### ✅ Definition of Done
- [ ] CurrencyKind 에 `EnhancementStone` 추가 (Bundle 12 currency.ts 확장)
- [ ] /users/{uid}/wallet 에 `enhancementStone: long` 필드 추가
- [ ] `CurrencyWallet.AddEnhancementStone(int)`, `TrySpendEnhancementStone(int)` 메서드 (Cloud Functions 호출)
- [ ] HUD 에 강화석 아이콘 + 수량 표시 (gold/gem 옆)
- [ ] `Assets/Scenes/EnhancementStoneDungeonScene.unity` 신규
- [ ] Build Settings: `[4] EnhancementStoneDungeonScene`
- [ ] `EnhancementStoneDungeonService` MonoBehaviour — Bundle 10 GoldDungeonService / Bundle 11 EXPDungeonService 패턴 복사. 보상 = 강화석
- [ ] `EnhancementStoneDungeonBootstrap` MonoBehaviour
- [ ] 보상 시드: 처치당 강화석 +1 (60초 = 평균 30~50개)
- [ ] DungeonEntryPanel 에 "강화석" 탭 추가 (Bundle 11 AL의 통합 EntryPanel 확장)
- [ ] 일일 입장 3회 + Sweep + 광고 2배

### 📂 Files to Add
- `Assets/Scenes/EnhancementStoneDungeonScene.unity`
- `Assets/Scripts/Dungeons/EnhancementStoneDungeonBootstrap.cs`
- `Assets/Scripts/Dungeons/EnhancementStoneDungeonService.cs`
- `Assets/Scripts/Dungeons/EnhancementStoneDungeonState.cs`
- `Assets/Scripts/UI/EnhancementStoneDungeonResultModal.cs`
- `Assets/Prefabs/UI/EnhancementStoneDungeonResultModal.prefab`
- `Assets/Art/Generated/Icons/enhancement_stone.png` — 강화석 아이콘 placeholder
- `functions/src/currency.ts` — EnhancementStone 처리 추가

### 📂 Files to Modify
- `Assets/Scripts/Economy/CurrencyWallet.cs` — EnhancementStone API
- `Assets/Scripts/UI/HUDController.cs` — 강화석 표시
- `Assets/Scripts/UI/DungeonEntryPanel.cs` (Bundle 11에서 통합 명명 — 골드/EXP/강화석 3 탭)
- `Assets/Scripts/Save/SaveData.cs`
  ```csharp
  public EnhancementStoneDungeonState enhancementStoneDungeon = new();
  ```
- `Assets/Scripts/Save/SaveDataDocument.cs` + `SaveDataMapper.cs` + `SaveBinder.cs`
- `Assets/Scripts/Core/GameContext.cs` + `GameManager.cs` — Initialize

### 🚫 Do Not Touch
- Bundle 10 GoldDungeon / Bundle 11 EXPDungeon 내부
- Bundle 12 Cloud Functions 다른 부분
- 인벤토리 / 가챠 / 메인UI01

### 🧪 Validation
1. Compile clean (Unity + functions tsc)
2. PlayMode → HUD 강화석 표시 (초기 0)
3. DungeonEntryPanel → "강화석" 탭 → 입장 → EnhancementStoneDungeonScene
4. 60초 자동 전투 → 처치 30마리 → 강화석 30개 → 결과 모달
5. "받기" → 강화석 +30 → HUD 갱신
6. "광고 2배" → 강화석 +60
7. Sweep → bestStoneScore 그대로 받기
8. 일일 3회 제한 정상

### Implementation note
**Cloud Functions wallet 확장:**
```typescript
// functions/src/currency.ts
type CurrencyKind = "gold" | "gem" | "enhancement_stone";

export const grantCurrency = functions.https.onCall(async (data, context) => {
    const { kind, amount, reason } = data;
    if (!["gold", "gem", "enhancement_stone"].includes(kind)) {
        throw new functions.https.HttpsError("invalid-argument", "Unknown currency.");
    }
    // ... transaction
});
```

**Bundle 11 EntryPanel 명칭:** Bundle 11 AL 에서 GoldDungeonEntryPanel 을 DungeonEntryPanel 로 일반화하는 것이 implementer 에게 권장됨. Bundle 11 시점에서 일반화하지 않았다면 Bundle 15 AZ 가 일반화 작업도 포함.

---

## Task BA — EnhancementService + 강화 비용 공식 + 무기/방어구/장신구 통합 강화

**Status:** 🔴 TODO
**Depends On:** AZ ✅

### 🎯 Goal
3개 슬롯 카테고리 (무기/방어구/장신구) 의 단일 아이템 강화. 강화 레벨 + 능력치 증가 + 강화석 비용.

### ✅ Definition of Done
- [ ] `EnhancementService` MonoBehaviour — 통합 강화 로직
- [ ] 강화 레벨 cap = 10
- [ ] 강화 비용: `cost(currentLevel) = round(100 × 1.5^currentLevel)`. 0→1 = 100, ..., 9→10 ≈ 3844
- [ ] 강화 능력치 증가 공식: `enhancementBonus = baseStat × 0.1 × level` (각 stat 별 별도 적용)
- [ ] 강화 성공률: 100% (실패 시스템 없음)
- [ ] OwnedWeaponEntry / OwnedArmorEntry / OwnedAccessoryEntry 모두에 `enhancementLevel: int` 필드 추가
- [ ] WeaponStatComposer / ArmorStatComposer / AccessoryStatComposer 모두 강화 레벨 반영하여 stat 합산
- [ ] 합성 시 강화 레벨 손실 (3개 합치면 결과는 강화 0)
- [ ] `EnhancementService.TryEnhanceAsync(slotKind, itemId, currentLevel)` Cloud Functions 호출 (서버 권위 — Bundle 12 패턴)

### 📂 Files to Add
- `Assets/Scripts/Enhancement/EnhancementService.cs`
- `Assets/Scripts/Enhancement/EnhancementCostCalculator.cs` (pure)
- `functions/src/enhancement.ts` — `enhanceItem` Callable Function (서버 권위)

### 📂 Files to Modify
- `Assets/Scripts/Weapons/OwnedWeaponEntry.cs` — `public int enhancementLevel = 0;` 추가
- `Assets/Scripts/Armor/OwnedArmorEntry.cs` — 동일
- `Assets/Scripts/Accessory/OwnedAccessoryEntry.cs` — 동일
- `Assets/Scripts/Weapons/WeaponStatComposer.cs` — enhancement bonus 반영
- `Assets/Scripts/Armor/ArmorStatComposer.cs` — enhancement bonus 반영
- `Assets/Scripts/Accessory/AccessoryStatComposer.cs` — enhancement bonus 반영
- `Assets/Scripts/Weapons/WeaponFusionService.cs` — 합성 결과의 enhancementLevel = 0
- `Assets/Scripts/Armor/ArmorFusionService.cs` — 동일
- `Assets/Scripts/Accessory/AccessoryFusionService.cs` — 동일
- `functions/src/index.ts` — enhancement export

### 🚫 Do Not Touch
- AZ 의 강화석 currency / 던전 코드
- Bundle 7/13/14 의 인벤토리 핵심 로직 (필드 추가만)

### 🧪 Validation
1. Compile clean (Unity + functions)
2. 무기 helmet_common_beginner 보유 → enhancementLevel = 0
3. EnhancementService.TryEnhanceAsync(Weapon, weaponId, 0) → 강화석 -100, enhancementLevel = 1, 능력치 +10%
4. 강화 1→2 → 강화석 -150, 능력치 base + 20%
5. 강화 9→10 → 강화석 -3844, 능력치 base + 100% (= 2배)
6. 강화 10에서 추가 시도 → cap 도달 거부
7. 강화석 부족 시 시도 → 거부
8. 합성 (helmet_common_beginner enhancementLevel=5 × 3) → helmet_common_intermediate enhancementLevel=0
9. 무기/방어구/장신구 모두 같은 강화 흐름

### Implementation note
**서버 권위 강화:** Bundle 12 패턴 동일. enhanceItem Callable Function 이 강화석 차감 + 인벤토리 enhancementLevel 증가 atomic 처리. 클라이언트 단순 호출.

**Stat 합산 공식 갱신 예 (WeaponStatComposer):**
```csharp
// 강화 보너스 적용
WeaponStats finalStats = baseStats;
finalStats.attackDamage *= (1 + 0.1f * enhancementLevel);
finalStats.criticalChance *= (1 + 0.1f * enhancementLevel);
// ... 모든 필드에 동일
```

---

## Task BB — 강화 UI (인벤토리 상세 통합) + 강화 모달

**Status:** 🔴 TODO
**Depends On:** BA ✅

### 🎯 Goal
인벤토리 상세 패널에 "강화" 버튼 추가. 클릭 시 강화 모달 표시.

### ✅ Definition of Done
- [ ] WeaponDetailView / ArmorDetailView / AccessoryDetailView 모두 "강화" 버튼 추가 (장착 버튼 옆)
- [ ] "강화" 버튼 비활성 조건: 보유 0개 또는 cap 도달 (level 10) 또는 강화석 부족
- [ ] 클릭 → `EnhancementModal` 표시
- [ ] EnhancementModal 내용:
  - 아이템 이름 + 현재 강화 레벨 (`+5` 표시)
  - 다음 레벨 능력치 비교 (현재 vs 다음, 차이 강조)
  - 강화 비용 (`💎 강화석 N`)
  - 보유 강화석 표시
  - "강화" 버튼 (비활성 조건 강화석 < cost)
  - 닫기 X 버튼
- [ ] 강화 성공 시 짧은 시각 효과 + 다음 강화 가능 (모달 닫지 않음 — 연속 강화)
- [ ] 강화 레벨 표시: 인벤토리 슬롯에 `+N` 라벨 (오른쪽 하단)

### 📂 Files to Add
- `Assets/Scripts/UI/EnhancementModal.cs`
- `Assets/Prefabs/UI/EnhancementModal.prefab`
- `Assets/VFX/EnhancementSuccess.prefab` (ParticleSystem — 빛 burst)

### 📂 Files to Modify
- `Assets/Scripts/UI/WeaponDetailView.cs` — "강화" 버튼 + 강화 레벨 표시
- `Assets/Scripts/UI/ArmorDetailView.cs` — 동일
- `Assets/Scripts/UI/AccessoryDetailView.cs` — 동일
- `Assets/Scripts/UI/WeaponSlotView.cs` — 슬롯에 `+N` 라벨
- `Assets/Scripts/UI/ArmorSlotView.cs` — 동일 (Bundle 13)
- `Assets/Scripts/UI/AccessorySlotView.cs` — 동일 (Bundle 14)
- `Assets/Scripts/Core/GameContext.cs` + `GameManager.cs` — EnhancementModal Bind

### 🚫 Do Not Touch
- AZ / BA 코드
- 합성 로직 / 장착 로직

### 🧪 Validation
1. Compile clean
2. 인벤토리 → 무기 탭 → wand_common_beginner 슬롯 (보유 1개) 클릭 → 상세 → "강화" 버튼 활성
3. "강화" 클릭 → EnhancementModal 표시 — 현재 +0, 다음 +1 능력치 비교, 비용 100
4. 강화 클릭 → 강화석 -100, +1 표시, 능력치 +10% 적용
5. 모달은 그대로 (연속 강화) — 다음 비용 150 표시
6. 5회 연속 강화 → +5
7. 모달 닫고 다른 슬롯 클릭 시도 → 그 슬롯 강화 +0 → 동일 흐름
8. 강화 레벨 10 도달 → "강화" 버튼 비활성, "최대 강화 도달" 라벨
9. 슬롯에 `+10` 표시
10. 합성 → enhancement +0 인 결과물 (Bundle 13/14 합성 로직 검증)

### Implementation note
**연속 강화 UX:** 모달이 강화 후 자동 닫지 않고 다음 비용 표시로 갱신. "X" 또는 "다음 슬롯" 버튼으로 닫기. 사용자 편의.

**연출:** 강화 성공 시 EnhancementSuccess.prefab spawn → 0.4초 자동 destroy. 화려한 폴리시는 Bundle 19 시점.

---

## Task BC — Save Schema v11 Migration + Cross-Feature Regression

**Status:** 🔴 TODO
**Depends On:** BB ✅

### 🎯 Goal
saveVersion = 11 + 강화 레벨 default-fill + Bundle 5~14 회귀 시험.

### ✅ Definition of Done
- [ ] `SaveData.saveVersion = 11`
- [ ] v10 → v11 마이그레이션:
  - 모든 OwnedWeaponEntry / OwnedArmorEntry / OwnedAccessoryEntry 의 `enhancementLevel = 0` default-fill
  - `enhancementStoneDungeon = new()` default-fill
  - wallet.enhancementStone = 0 (Cloud Functions 측에서 default-fill — 첫 강화석 grant 시점에 자동 생성)
- [ ] Bundle 15 release regression 1~14번 모두 통과

### 📂 Files to Modify
- `Assets/Scripts/Save/SaveData.cs` — saveVersion = 11
- `Assets/Scripts/Save/SaveService.cs` — v10→v11 분기

### 🚫 Do Not Touch
- AZ / BA / BB 코드

### 🧪 Validation
1. Compile clean
2. v10 save 로딩 → v11 + 모든 강화 레벨 0 default-fill
3. Bundle 15 regression 1~14번 전체 통과
4. **Cross-feature regression**: Bundle 5~14 모두 정상

---

# Bundle 15 Release Gate

When Tasks AZ~BC are all `✅ DONE`:

1. HUD 강화석 표시
2. DungeonEntryPanel 에 강화석 탭 추가 → 60초 던전 → 강화석 +N
3. 인벤토리 상세 → "강화" 버튼 → 모달 → 강화 +1 → 능력치 +10%
4. 강화 10 cap
5. 합성 시 강화 레벨 손실
6. (Cross-feature regression) Bundle 5~14 모두 정상

---

## Appendix A~E

(Tasks_v9~v14 패턴 동일)

| Date | Author | Change |
|---|---|---|
| 2026-05-09 | Planner | Document created. Bundle 15 = 강화 시스템 + 강화석 던전. 4개 Task (AZ/BA/BB/BC). 강화석 currency 신설 (Cloud Functions wallet 확장). 강화 cap 10, 비용 곡선 100×1.5^level, 능력치 +10%/level, 성공률 100% (단순화). 합성 시 강화 손실. UI: 인벤토리 상세에 "강화" 버튼 통합 (별도 메인UI01 슬롯 추가 X). 강화석 던전: Bundle 10/11 패턴 재사용, EntryPanel 3 탭 (골드/EXP/강화석). |

### Appendix D — Combined Work Log

| Date | Task | Summary |
|---|---|---|
| 2026-05-12 | AZ | Added server-authoritative `enhancement_stone` currency support, enhancement stone wallet/save mirroring, EnhancementStoneDungeon scene/service/bootstrap/result modal, HUD stone label, DungeonEntryPanel third tab, Build Settings index 4, and placeholder enhancement stone icon. Validation: `dotnet build Assembly-CSharp.csproj --no-restore` passed with 4 pre-existing Chat/Presence warnings; `npm --prefix functions run build` passed; Unity PlayMode test job `99768b9349ce45a09bc998f967ef8a01` passed with 0 discovered tests. |
