# Wizard Grower — 작업 지시서 (Tasks.md)

> 이 문서는 **구현 에이전트(다른 AI)** 를 위한 작업 명세서다.
> **기획·검토 담당자(Claude / Planner)** 가 유일하게 편집하는 문서이며,
> 구현자는 본 문서를 읽고 코드를 수정한다.
> 문서와 실제 코드가 어긋나면 **문서가 정답**이다.

---

## 0. 작업 규칙 (모든 태스크 공통)

### 0.1 기본 규칙
1. **한 번에 하나의 태스크만** 진행한다. 태스크 ID(예: `Task A`)를 명시하고 시작/종료한다.
2. 시작 전: 본 문서의 해당 태스크 섹션을 정독한다. **"건드리지 말 것"** 영역은 절대 수정 금지.
3. 작업 중: Unity 프로젝트 경로는 `/Users/kmj/rev_proj01`. 네임스페이스는 `WizardGrower.*`.
4. 종료 시:
   - Unity Console에서 컴파일 에러/경고 **0건** 확인
   - PlayMode 진입 후 **회귀 테스트** (각 태스크 하단 명시) 모두 통과
   - 본 문서의 해당 태스크 상태를 `🟡 IN REVIEW`로 변경 (Status 줄)
   - **본 문서의 다른 부분은 절대 수정하지 말 것** (Status 줄, 작업 로그만 수정 허용)
5. 검토자가 코드 리뷰 후 본 문서의 상태를 `✅ DONE` 또는 다시 `🔴 TODO`로 갱신한다.
6. 빌드 헬퍼 `Wizard Grower → Build Prototype Scene` 메뉴는 **씬을 덮어쓴다** — 명시적 요청 없이 실행 금지.
7. 파일 추가/삭제 시 `.meta` 파일 동반 처리 (Unity 에디터가 자동 처리).

### 0.2 묶음 게이트 규칙 ⭐
태스크는 5개 **묶음(Bundle)** 으로 분할된다. 묶음의 마지막 태스크가 ✅ DONE 처리되기 전까지 **다음 묶음의 어떤 태스크도 시작 금지**.

**게이트 통과 조건 (검토자가 확인):**
1. 묶음 내 모든 태스크 Status가 ✅ DONE
2. Unity Console 컴파일 에러/경고 0
3. 묶음 종합 회귀 테스트 통과 (각 묶음 시작 섹션에 명시)
4. 다음 묶음 첫 태스크의 "선행 가정"이 코드 베이스와 일치 (검토자 재확인)

> 한 묶음 **내부의 태스크끼리는** 선행 관계만 지키면 연속 진행 가능. 묶음 간 전환에서만 게이트가 작동한다.

### 0.3 자동 진행 금지 사항 (구현 에이전트)
- 다른 묶음의 태스크 임의 시작 금지
- 시그니처 충돌·기획 충돌 발견 시 **임의 결정 금지** — 작업 로그에 기록 후 검토자 답을 기다린다.
- "건드리지 말 것" 영역 임시 수정 절대 금지. 컴파일이 깨지면 차라리 Status `⚠️ BLOCKED`로 두고 사유 기록.
- 본 문서에 명시되지 않은 새 시스템·새 파일 추가 금지 (예외는 작업 로그에 기록 + 검토자 승인 대기).

### 0.4 git 커밋 규칙
- **매 태스크 종료 시 git commit** 1회 필수. 메시지: `Task X done: <한줄 요약>` (예: `Task B done: manual fire mana cost removed, auto-mode guard added`)
- 본 저장소는 GitHub Desktop으로 이미 초기화되어 있고 origin이 설정되어 있다. **에이전트는 push 하지 않는다** — push는 사용자가 GitHub Desktop으로 수동 처리.
- 작업 시작 시 `git status`로 깨끗한지 확인. 미커밋 변경이 있으면 작업 로그에 명시.
- 검토 단계에서 문제 발견 시 `git revert`로 롤백 가능하므로, 한 태스크 = 한 커밋 원칙 유지.

---

## 1. 태스크 의존성 그래프

```
[Bundle 1] A ✅ → B ──┐
                       ├─→ [Bundle 3] F
[Bundle 2] C → D → E ──┤
                       └─→ [Bundle 4] G
                                       │
                          H (사전작업 후) ─┴─→ [Bundle 5] I
```

**진행 순서:** Bundle 1 → Bundle 2 → Bundle 3 → Bundle 4 → (사용자 사전작업) → Bundle 5

---

## 2. 태스크 상태 보드

| Bundle | ID | 제목 | Status | 선행 |
|--------|----|----|--------|------|
| 1 | A | PlayerStats 능력치 확장 | ✅ DONE | — |
| 1 | B | 전투 정합성 + 발사속도 stat 반영 + 버그 수정 | 🟡 IN REVIEW | A |
| 2 | C | ChapterDefinition / StageDefinition 데이터 모델 | 🔴 TODO | Bundle 1 게이트 |
| 2 | D | StageManager 흐름 리팩터 (필드 ↔ 보스방) | 🔴 TODO | C |
| 2 | E | 보스 입장 버튼 + HUD 챕터/스테이지 표시 | 🔴 TODO | D |
| 3 | F | 업그레이드 드로어 UI (하단 토글, 2열 스크롤) | 🔴 TODO | Bundle 2 게이트 |
| 4 | G | SaveData 모델 + 로컬 저장 | 🔴 TODO | Bundle 3 게이트 |
| 5 | H | Firebase Auth (익명/Google/Apple) | 🔴 TODO | Bundle 4 게이트 + 사용자 사전작업 |
| 5 | I | Firestore 클라우드 동기화 | 🔴 TODO | H |

상태 기호:
- 🔴 TODO: 미착수
- 🟢 IN PROGRESS: 구현 중 (구현자가 표시)
- 🟡 IN REVIEW: 구현 완료, 검토 대기 (구현자가 표시)
- ✅ DONE: 검토 통과 (검토자만 표시)
- ⚠️ BLOCKED: 차단됨 (사유 함께 기록)

---

# Bundle 1 — 능력치 + 전투 정합성

**목표:** 데이터 모델 정비 + 기본 전투 흐름 버그 제거. UI 손대지 않음.

### Bundle 1 종합 회귀 테스트
1. 자동공격 정상 동작 (Auto ON, 1초 간격)
2. Auto OFF 토글 시 자동공격 즉시 정지
3. Fire 버튼 → 마나 소모 없이 발사, `manualAttackInterval` 쿨다운 적용
4. PlayerStats 9필드 + EnemyBase armor 인스펙터 노출
5. armorPen vs armor 데미지 계산 정확 (최소 1 보장)

---

## Task A — PlayerStats 능력치 필드 확장

**Status:** ✅ DONE
**선행:** 없음

### 🎯 목표
`PlayerStats`에 능력치 필드 7종을 추가하고 자동/수동 공격 데미지를 **독립** 필드로 분리한다. `EnemyBase`에 `armor` 필드를 추가해 `armorPenetration` 능력치가 의미를 갖게 한다.

> 이번 태스크는 **데이터 모델 + 호환 유지**만 한다. 업그레이드 버튼 추가, UI 변경, 적 → 플레이어 공격 로직은 절대 손대지 않는다.

### ✅ Definition of Done
- [x] Unity Console: 컴파일 에러 0, 경고 0
- [x] PlayMode 회귀 테스트(아래 §검증) 5건 통과
- [x] 인스펙터에 `PlayerStats`의 신규 필드 9종 노출
- [x] `EnemyBase` 인스펙터에 `armor` 노출
- [x] 기존 호출부(`UpgradeSystem`, `AutoAttackController`, `ClickAttackController`, `ActiveSkillController`, `EnemyScalingService`, `CombatCalculator`) 모두 새 API 사용

### 📂 변경 파일

#### A-1. `Assets/Scripts/Player/PlayerStats.cs` (수정)
필드 (전체 교체):
```csharp
[SerializeField] private float autoAttackDamage   = 10f;
[SerializeField] private float manualAttackDamage = 20f;
[SerializeField] private float autoAttackInterval = 1f;       // 초당 발사 = 1/interval
[SerializeField] private float manualAttackInterval = 0.3f;
[SerializeField, Range(0f,1f)] private float criticalChance     = 0.1f;
[SerializeField] private float criticalMultiplier = 2f;
[SerializeField] private float armorPenetration   = 0f;        // flat
[SerializeField] private float maxHealth          = 100f;
[SerializeField] private float currentHealth      = 100f;
[SerializeField] private float combatPower        = 10f;       // 표시용 캐시
```

Public 프로퍼티 (PascalCase): `AutoAttackDamage`, `ManualAttackDamage`, `AutoAttackInterval`, `ManualAttackInterval`, `CriticalChance`, `CriticalMultiplier`, `ArmorPenetration`, `MaxHealth`, `CurrentHealth`, `CombatPower`.

Add 메서드:
- `AddAutoDamage(float)` / `AddManualDamage(float)`
- `AddAutoFireRate(float)` / `AddManualFireRate(float)` — interval 감소 방향, 최소 0.05f clamp
- `AddCriticalChance(float)` (0~1 clamp)
- `AddCriticalMultiplier(float)`
- `AddArmorPenetration(float)`
- `AddMaxHealth(float)` — 증가 시 currentHealth도 동일량 증가 (max clamp)
- `Heal(float)` (max clamp), `TakeHealth(float)` (0 floor)

이벤트:
- 기존 `event Action Changed` 유지 — 모든 stat 변경 시 발생
- 신규 `event Action HealthChanged` — HP 변경 전용 (UI 분리용)

CombatPower 재계산:
```csharp
combatPower = autoAttackDamage * (1f + criticalChance * (criticalMultiplier - 1f));
```

**호환:** 기존 `AttackDamage` 프로퍼티, `ManualAttackMultiplier`, `AddAttack` **삭제**. 호출부 모두 새 이름으로 마이그레이션.

#### A-2. `Assets/Scripts/Combat/DamageInfo.cs` (수정)
- `readonly float ArmorPenetration` 필드 추가
- 생성자 시그니처 확장:
```csharp
public DamageInfo(float amount, bool isCritical, DamageType type, GameObject source, float armorPenetration = 0f)
```

#### A-3. `Assets/Scripts/Combat/CombatCalculator.cs` (수정)
- `Auto(source)` → `stats.AutoAttackDamage`
- `Manual(source)` → `stats.ManualAttackDamage` (multiplier 곱셈 **제거**)
- `Skill(source, mult)` → `stats.AutoAttackDamage * mult`
- 모든 `Build()`에서 `stats.ArmorPenetration` 전달

#### A-4. `Assets/Scripts/Enemies/EnemyBase.cs` (수정)
- 필드: `[SerializeField] private float armor = 0f;` + `public float Armor => armor;`
- 시그니처: `public virtual void Initialize(float health, int reward, float armor = 0f)`
- `TakeDamage` 내부 변경:
```csharp
float effectiveArmor = Mathf.Max(0f, armor - info.ArmorPenetration);
float dealt          = Mathf.Max(1f, info.Amount - effectiveArmor);  // 최소 1 보장
currentHealth = Mathf.Max(0f, currentHealth - dealt);
```

#### A-5. `Assets/Scripts/Data/PlayerStatProfile.cs` (수정)
신규 필드 미러링: `autoAttackDamage`, `manualAttackDamage`, `manualAttackInterval`, `armorPenetration`, `maxHealth`. 기존 `baseAttack` 필드는 **삭제**하고 호출부를 `autoAttackDamage`로 교체.

#### A-6. `Assets/Scripts/Upgrades/UpgradeSystem.cs` (호출부 마이그레이션)
- `stats.AddAttack(x)` → `stats.AddAutoDamage(x)`
- 신규 능력치용 업그레이드 정의 추가는 **Task F에서** — 본 태스크에서는 기존 3종 업그레이드만 컴파일/동작하면 됨

#### A-7. `Assets/Scripts/Combat/AutoAttackController.cs` (점검)
`stats.AutoAttackInterval` 그대로 사용 (이름 변경 없음).

#### A-8. `Assets/Scripts/Combat/ClickAttackController.cs` (점검)
- 데미지는 반드시 `CombatCalculator.Manual()` 경유. 직접 stat 참조 금지.
- 입력 쿨다운이 필요하면 `stats.ManualAttackInterval` 사용.

#### A-9. `Assets/Scripts/Enemies/EnemyScalingService.cs` (호출부 수정)
`EnemyBase.Initialize()` 호출 시 armor 인자 `0f` 전달 (스케일링은 Task D에서).

#### A-10. `Assets/Scripts/UI/HUDController.cs` (호출부 1줄만 수정 — 예외 허용)
**검토자 승인됨 (2026-05-06):** UI 위젯은 원칙적으로 수정 금지이지만, 본 파일은 `wizard.Stats.AttackDamage`를 표시 목적으로 참조하는 **유일한 UI 호출부**다. `AttackDamage` 프로퍼티를 삭제하면서 컴파일을 유지하기 위해, 아래 한 줄만 교체한다.

- 위치: `Assets/Scripts/UI/HUDController.cs:112`
- 변경 전: `attackLabel.text = $"ATK {wizard.Stats.AttackDamage:0}  CP {wizard.Stats.CombatPower:0}";`
- 변경 후: `attackLabel.text = $"ATK {wizard.Stats.AutoAttackDamage:0}  CP {wizard.Stats.CombatPower:0}";`

> **본 파일의 다른 줄은 절대 수정하지 말 것.**

### 🚫 건드리지 말 것
- `StageManager`, `BossStageController`, `EnemySpawner` 흐름 로직
- `HUDController`의 **§A-10에 명시된 1줄 외** 모든 라인
- `Assets/Scripts/UI/*` 의 다른 모든 위젯
- `GameManager`, `GameContext` 의존성 주입 구조
- 씬, 프리팹
- 적 → 플레이어 공격 로직
- 신규 업그레이드 버튼 추가 (Task F)

### 🧪 검증
1. Unity Console 클린 (에러/경고 0)
2. PlayMode 진입 → 마법사 자동공격으로 슬라임 처치 → 골드 +10
3. Fire 버튼 클릭 → 수동공격 데미지 정확히 `manualAttackDamage` 값과 일치
4. PlayerWizard 인스펙터에 신규 필드 9종 노출 확인
5. EnemyBase 프리팹 `armor=5` 설정:
   - autoAttackDamage=10, armorPen=0 → 데미지 = 5
   - autoAttackDamage=10, armorPen=3 → 데미지 = 8
   - autoAttackDamage=10, armorPen=10 → 데미지 = 10
   - 모든 경우 최소 1 보장

### 📝 작업 로그 (구현자 기록)
- 2026-05-06 시작: Task A 착수. `Tasks.md` §A-10 확인 후 `HUDController.cs`는 승인된 112번 줄만 수정하기로 함.
- 2026-05-06 종료: PlayerStats/DamageInfo/CombatCalculator/EnemyBase/PlayerStatProfile/UpgradeSystem/EnemySpawner/HUDController 112번 줄 마이그레이션 완료. 스크립트 검증 에러 0. 수동 데미지 20, armor=5 방어 계산(5/8/10) 직접 검증 통과. Unity MCP PlayMode가 `is_changing=true`에 머무는 세션 이슈로 자연 자동공격 골드 +10 관찰은 미완료.

### 🔍 검토 노트 (검토자 기록)
- 2026-05-06: 코드 검증 완료. PlayerStats 9필드 + 모든 Add/Heal/TakeHealth 메서드 + HealthChanged 이벤트 정확히 구현. CombatCalculator가 새 API 사용 + ArmorPenetration 전달. EnemyBase의 armor + effectiveArmor + 최소 1 보장 로직 정확. HUDController 112줄만 변경됨 (다른 라인 unchanged). 옛 심볼(AttackDamage/ManualAttackMultiplier/AddAttack) 잔재 0. **✅ DONE 처리.** 자동공격 자연 검증은 Bundle 1 종합 회귀 시 함께 확인.

---

## Task B — 전투 정합성 + 발사 속도 stat 반영 + 기존 버그 수정

**Status:** 🟡 IN REVIEW
**선행:** A ✅

### 🎯 목표
1. **버그 수정 #1:** 수동 공격(Fire 버튼)의 마나 소모 제거 — 기획상 수동공격은 무코스트
2. **버그 수정 #2:** `AutoAttackController`가 Auto OFF 상태를 무시하고 자동 발사하는 문제 수정
3. 수동 공격에 `ManualAttackInterval` 기반 쿨다운(연사 제한) 적용

### ✅ Definition of Done
- [ ] Unity Console: 에러 0, 경고 0
- [ ] PlayMode 회귀 테스트(아래 §검증) 5건 통과
- [ ] Auto OFF 상태에서 자동 발사 발생하지 않음
- [ ] Fire 버튼 클릭 시 마나가 감소하지 않음
- [ ] Fire 버튼 연타 시 `ManualAttackInterval` 간격으로만 발사

### 📂 변경 파일

#### B-1. `Assets/Scripts/Combat/AutoAttackController.cs`
- `Update()` 가드 추가:
```csharp
if (movement != null && (!movement.AutoModeEnabled || movement.IsManualMoving))
    return;
```
- `TryFireNow()`에도 동일 가드 적용:
```csharp
if (movement != null && (!movement.AutoModeEnabled || movement.IsManualMoving))
    return false;
```

> 주의: 현재 코드의 `movement.IsManualMoving` 체크는 **유지**. `AutoModeEnabled` 추가만.

#### B-2. `Assets/Scripts/Combat/ClickAttackController.cs`
- 필드 제거:
  - `[SerializeField] private float manualManaCost = 5f;` 삭제
  - `[SerializeField] private PlayerMana mana;` 삭제
- `Initialize` 시그니처에서 `PlayerMana mana` 인자 제거
- 새 필드: `private float lastFireTime = -999f;`
- `TryFireManual()` 로직 교체:
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
- `context.ClickAttack.Initialize(...)` 호출에서 `mana` 인자 제거:
  - 변경 전: `context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, context.Mana, calculator);`
  - 변경 후: `context.ClickAttack.Initialize(context.Wizard, context.EnemySpawner, context.ProjectileFactory, calculator);`

### 🚫 건드리지 말 것
- `PlayerStats`, `CombatCalculator` 내부 로직
- UI (`HUDController` 등) — 변경 없음
- `StageManager`, `BossStageController`, `EnemySpawner`
- 씬, 프리팹
- `ActiveSkillController` (본 태스크 범위 밖)

### 🧪 검증
1. Unity Console 클린
2. PlayMode 진입 → Auto Toggle OFF → 5초 대기 → 적 HP 변화 없음. 조이스틱 조작 시에도 자동 공격 없음.
3. Auto Toggle ON 복구 → 1초 간격 자동 공격 재개
4. Fire 버튼 1회 클릭 → 마나바 변화 없음, 적 HP는 `manualAttackDamage`만큼 감소 (크리티컬 제외)
5. Fire 버튼 빠르게 5회 클릭 → 첫 발만 즉시, 나머지는 `manualAttackInterval` 간격으로 1회씩

### 📝 작업 로그 (구현자 기록)
- 2026-05-06 시작: Bundle 1 Task B 착수. Auto OFF 가드, Fire 버튼 무마나화, manualAttackInterval 쿨다운 적용 예정.
- 2026-05-06 종료: AutoAttackController에 AutoModeEnabled 가드 추가, ClickAttackController에서 PlayerMana/manualManaCost 제거 및 ManualAttackInterval 쿨다운 적용, GameManager 초기화 시그니처 마이그레이션 완료. 스크립트 검증 에러/경고 0. PlayMode 직접 검증: Auto OFF 발사 false + HP 변화 없음, Auto ON 발사 true, Fire 마나 변화 없음(100→100), 빠른 연타 첫 발만 발사, interval 경과 후 재발사 통과. Console에는 MCP client handler 종료 로그가 Exception 타입으로 남으나 게임 코드 에러/경고는 없음.

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

# Bundle 2 — 챕터/스테이지 시스템

**목표:** "5킬마다 보스" → "챕터 8스테이지 + 보스방 도전" 구조로 재설계. 데이터 → 흐름 → UI 순서로 진행.

### Bundle 2 종합 회귀 테스트
1. 일반 몬스터 처치 → 자동 리스폰, 스테이지 진행 안 됨
2. 보스 입장 버튼 클릭 → 보스 등장, 20초 타이머 시작
3. 보스 클리어 → 다음 스테이지 필드로 자동 전환, HUD 라벨 갱신
4. 보스 시간 초과 → 필드 복귀, 페널티 없음
5. 8스테이지 보스 클리어 → 다음 챕터 1스테이지로 전환 (또는 마지막 챕터면 "All Cleared")
6. HUD 라벨 형식: "음산한 숲 1-3"

---

## Task C — ChapterDefinition / StageDefinition 데이터 모델

**Status:** 🔴 TODO
**선행:** Bundle 1 게이트 통과

### 🎯 목표
ScriptableObject 기반 챕터·스테이지 데이터 모델 정의 + 첫 챕터 "음산한 숲" 자산 1세트 작성. 흐름 변경(D)은 이 데이터 위에서 동작.

> 본 태스크는 **데이터 정의만** 한다. `StageManager` 등 흐름 코드 수정 금지.

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 새 자산 생성 메뉴 노출 (`Assets > Create > Wizard Grower > Chapter / Stage / Chapter Database`)
- [ ] "음산한 숲" 챕터 자산 1개 + 스테이지 8개 + ChapterDatabase 1개 생성
- [ ] 기존 `StageDefinition`(Serializable struct)이 새 ScriptableObject로 대체됨

### 📂 변경 파일

#### C-1. `Assets/Scripts/Stages/StageDefinition.cs` (전면 교체)
기존 `StageDefinition`(Serializable 클래스) → ScriptableObject로 재작성.

> **호환 처리 결정:** 기존 클래스가 `StageManager`의 `[SerializeField] private StageDefinition definition` 필드에서 사용 중. ScriptableObject로 바꾸면 컴파일이 깨질 수 있음. 처리 방법:
> - 옵션 A (권장): 기존 Serializable 정의를 별도 파일 `Assets/Scripts/Stages/LegacyStageBalance.cs`로 이동 (클래스명도 `LegacyStageBalance`로 rename), `StageDefinition.cs`에는 ScriptableObject만 배치. `StageManager`의 기존 필드는 임시로 `LegacyStageBalance`를 가리키도록 해 컴파일 유지 (Task D에서 정리됨).
> - 옵션 B: `StageManager` 임시 처리 부분 자체를 Task C에서 살짝 손봐도 되지만, "흐름 코드 수정 금지" 제약이 있으니 옵션 A가 더 안전.
>
> **선택한 옵션을 작업 로그에 명시.**

새 `StageDefinition.cs`:
```csharp
using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Stage Definition", fileName = "Stage")]
    public class StageDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int stageNumber;          // 1~8
        public string displayLabel;      // 예: "음산한 숲 1-3"

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

#### C-2. `Assets/Scripts/Stages/ChapterDefinition.cs` (신규)
```csharp
using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Chapter Definition", fileName = "Chapter")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int chapterNumber;
        public string displayName;       // 예: "음산한 숲"
        public string themeDescription;

        [Header("Stages (8개 권장)")]
        public StageDefinition[] stages = new StageDefinition[8];

        [Header("Theme Visuals")]
        public Sprite backgroundSprite;
        public Color  ambientTint = Color.white;
    }
}
```

#### C-3. `Assets/Scripts/Stages/ChapterDatabase.cs` (신규)
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

#### C-4. 자산 생성 (Unity Editor 작업)
경로: `Assets/Data/Chapters/` (폴더 신규 생성)

**생성:**
1. `Chapter01_GloomyForest.asset` — chapterNumber=1, displayName="음산한 숲"
2. `Stages/Stage01_GloomyForest_1.asset` ~ `Stage08_GloomyForest_8.asset`
3. `ChapterDatabase.asset` — chapters[0] = Chapter01

**스테이지별 발랜스값:**

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

(공식: `fieldHP = 50 * 1.25^(n-1)`, `fieldReward = round(10 * 1.18^(n-1))`, `bossHP = fieldHP * 8`, `bossReward = fieldReward * 10`)

bossTimeLimit는 모두 20초. fieldMonsterArmor / bossArmor는 모두 0으로 설정.

### 🚫 건드리지 말 것
- `StageManager`, `BossStageController`, `EnemySpawner`, `EnemyScalingService` 흐름 로직 (Task D)
- 씬, 프리팹

### 🧪 검증
1. 컴파일 클린
2. `Project` 창 우클릭 → `Create > Wizard Grower > Chapter / Stage / Chapter Database` 메뉴 노출
3. `Assets/Data/Chapters/` 아래에 챕터 1 + 스테이지 8 + DB 1 자산 존재
4. ChapterDatabase 인스펙터에서 chapters[0]을 펼쳤을 때 stages 8개 모두 정상 직렬화

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

## Task D — StageManager 흐름 리팩터 (필드 ↔ 보스방)

**Status:** 🔴 TODO
**선행:** C

### 🎯 목표
- **필드 모드:** 동일 스테이지 일반 몬스터를 무한 리스폰. 처치 시 골드 획득. 스테이지 자동 진행 없음.
- **보스방 모드:** 유저가 보스 입장 버튼(Task E)으로 진입. 보스 1마리, 20초 제한.
  - 클리어 → 다음 스테이지 필드 (8 끝나면 다음 챕터)
  - 시간 초과 / 실패 → 필드 모드 복귀, 페널티 없음

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 회귀 테스트 6건 통과
- [ ] 필드 모드에서 몬스터 처치해도 스테이지 자동 진행 안 됨
- [ ] 보스방 클리어 → 다음 스테이지 필드로 자동 전환
- [ ] 보스방 실패 → 필드 모드 복귀, 보스 입장 가능 상태로 복원
- [ ] ChapterDatabase 자산을 GameContext에서 주입받아 사용

### 📂 변경 파일

#### D-1. `Assets/Scripts/Stages/StageMode.cs` (신규)
```csharp
namespace WizardGrower.Stages
{
    public enum StageMode { Field, BossRoom }
}
```

#### D-2. `Assets/Scripts/Stages/StageManager.cs` (전면 재작성)
**책임:**
- 현재 챕터/스테이지/모드 관리
- 모드 전환: `EnterBossRoom()` (public, 외부 호출), `ReturnToField()` (private)
- 필드 모드: 일반 몬스터 처치 시 자동 리스폰 (지연 = `fieldRespawnDelay`)
- 보스방 모드: 보스 처치 시 다음 스테이지 진행, 실패 시 필드 복귀

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

**핵심 동작:**
- `Initialize` 후 currentChapter=1, currentStageNumber=1, mode=Field로 시작 → SpawnFieldEnemy
- `OnEnemyKilled(필드)`: 골드 추가 → `Invoke(SpawnFieldEnemy, fieldRespawnDelay)`
- `OnEnemyKilled(보스방)`: 골드 추가 → `bossStageController.StopTimer()` → `AdvanceToNextStage()`
- `EnterBossRoom()`: mode=BossRoom → 현재 적 제거 → SpawnBossEnemy → `bossStageController.StartTimer(stage.bossTimeLimit)` → 이벤트 발신
- `AdvanceToNextStage()`:
  - currentStageNumber++
  - 8 초과 시 currentChapter++, currentStageNumber=1
  - 다음 챕터가 DB에 없으면 Feedback("All Cleared") + 마지막 스테이지 유지
  - mode=Field, SpawnFieldEnemy
- `OnBossFailed`: mode=Field, SpawnFieldEnemy, Feedback("Boss Failed")
- 모든 상태 변경 후 `StateChanged` + `BossEntryAvailabilityChanged(CanEnterBoss)` 발신

**기존 `StageChanged(int, bool, int, int)` 이벤트는 폐기**. HUDController는 §D-7에서 임시 정리, Task E에서 새 이벤트로 마이그레이션.

#### D-3. `Assets/Scripts/Enemies/EnemySpawner.cs` (수정)
- `SpawnNormal(float health, int reward, float armor)` — armor 인자 추가
- `SpawnBoss(float health, int reward, float armor)` — armor 인자 추가
- `Initialize(health, reward, armor)` 호출에서 armor 전달 (이미 EnemyBase는 armor 인자 받음)

#### D-4. `Assets/Scripts/Stages/BossStageController.cs`
점검만. 변경 없음. `StartTimer/StopTimer` 그대로 사용.

#### D-5. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `[field: SerializeField] public ChapterDatabase ChapterDatabase { get; private set; }`

#### D-6. `Assets/Scripts/Core/GameManager.cs` (수정)
- `StageManager.Initialize(...)` 호출 시그니처 변경에 맞게 수정:
```csharp
context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
```

#### D-7. `Assets/Scripts/UI/HUDController.cs` (최소 수정 — 예외 허용)
- 기존 `stageManager.StageChanged += OnStageChanged;` 라인 **제거**
- `OnStageChanged(int, bool, int, int)` 메서드 **제거**
- `stageLabel.text` 업데이트는 Task E에서 새 `StateChanged` 이벤트로 재연결됨
- **본 태스크 종료 후 stageLabel은 일시적으로 빈 상태일 수 있음** — Task E에서 채워짐 (정상)

> ⚠️ HUDController는 원칙적으로 UI 위젯이라 수정 금지지만, 시그니처 변경에 따른 컴파일 유지를 위해 **이벤트 구독 + 핸들러 제거** 두 가지만 허용. 그 외 절대 금지. 이는 Task A의 §A-10과 같은 종류의 예외 승인.

#### D-8. `MainScene` (Unity Editor 작업)
- GameContext의 ChapterDatabase 필드에 `Assets/Data/Chapters/ChapterDatabase.asset` 할당

> ⚠️ `Wizard Grower → Build Prototype Scene` 실행 금지. 씬 직접 편집.

### 🚫 건드리지 말 것
- `PlayerStats`, `CombatCalculator`, `Projectile`
- 다른 UI 위젯 (HUDController는 §D-7 범위만)
- 보스 입장 버튼 추가 (Task E)
- `EnemyScalingService` — 사용 안 해도 무방, 삭제 금지

### 🧪 검증
1. 컴파일 클린
2. PlayMode 진입 → 챕터1 스테이지1 필드 시작 (HUD stageLabel은 비어있어도 OK, Task E에서 채움)
3. 일반 몬스터 처치 5회 → 스테이지 자동 진행 안 됨, 매번 골드 획득
4. **(임시 검증 방법)** StageManager에 `[ContextMenu("Debug Enter Boss")]` 메서드를 추가하거나, `EnterBossRoom()`을 인스펙터에서 호출 가능하게 만들어 호출 → 보스 등장 + 20초 타이머
5. 보스 처치 → currentStageNumber=2, 필드 모드 복귀, 새 일반 몬스터 등장
6. 다음 보스 도전 → 시간 초과 → 필드 복귀, currentStageNumber=2 유지

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

## Task E — 보스 입장 버튼 + HUD 챕터/스테이지 표시

**Status:** 🔴 TODO
**선행:** D

### 🎯 목표
- HUD 라벨에 "음산한 숲 1-3" 형식 표시
- 화면 상단/우상단에 "보스 입장" 버튼 — 필드 모드에서만 활성
- 버튼 클릭 → `StageManager.EnterBossRoom()` 호출

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] HUD 라벨 형식: `{챕터명} {챕터번호}-{스테이지번호}` (필드), `... BOSS` 접미 (보스방)
- [ ] 필드 모드: 보스 입장 버튼 활성, 클릭 가능
- [ ] 보스방 모드: 버튼 비활성 (interactable=false)
- [ ] 회귀 테스트 4건 통과

### 📂 변경 파일

#### E-1. `Assets/Scripts/UI/HUDController.cs` (수정)
- 신규 직렬화 필드:
```csharp
[SerializeField] private Button bossEntryButton;
[SerializeField] private TMP_Text bossEntryButtonLabel;
```
- `Initialize` 내부에 이벤트 구독 추가:
```csharp
stageManager.StateChanged += OnStateChanged;
stageManager.BossEntryAvailabilityChanged += OnBossEntryAvailabilityChanged;
bossEntryButton.onClick.AddListener(() => stageManager.EnterBossRoom());
```
- 새 핸들러:
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
- Task D에서 임시 제거된 `OnStageChanged` 핸들러는 영구 제거 상태 유지

#### E-2. `MainScene` HUD 구성 (Unity Editor 작업)
- HUD Canvas 안에 새 Button 추가:
  - 이름: `BossEntryButton`
  - 위치: 화면 상단 중앙 또는 우상단 (stageLabel 근처)
  - 크기: 가로 200, 세로 60 권장
  - 자식 TMP_Text: "보스 입장"
- HUDController 인스펙터에서 `bossEntryButton`, `bossEntryButtonLabel` 필드 할당
- Task D에서 임시 추가한 디버그 진입 호출 코드/메뉴 제거

> ⚠️ Build Prototype Scene 실행 금지. 씬 직접 편집.

### 🚫 건드리지 말 것
- 다른 HUD 위젯의 위치/이벤트
- StageManager 내부 (Task D 확정)
- 업그레이드 UI (Task F)

### 🧪 검증
1. 컴파일 클린
2. PlayMode 진입 → 라벨 "음산한 숲 1-1" 표시, 보스 입장 버튼 활성
3. 보스 입장 클릭 → 보스 등장, 라벨 "음산한 숲 1-1 BOSS", 버튼 비활성
4. 보스 처치 → 라벨 "음산한 숲 1-2" 갱신, 버튼 재활성

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

# Bundle 3 — 업그레이드 UI 드로어

**목표:** 업그레이드를 화면 하단 드로어로 분리. 신규 능력치 모두에 대응하는 업그레이드 항목 제공.

### Bundle 3 종합 회귀 테스트
1. 토글 버튼 하단 노출, 패널 접힘 상태로 시작
2. 토글 클릭 → 패널 슬라이드 업, 한 줄 2버튼 그리드, 세로 스크롤 가능
3. 9종 업그레이드 모두 클릭 가능, 골드 차감 + 능력치 변경
4. 토글 다시 클릭 → 패널 슬라이드 다운
5. 업그레이드 효과가 PlayMode 전투에 즉시 반영

---

## Task F — 업그레이드 드로어 UI (하단 토글, 2열 스크롤)

**Status:** 🔴 TODO
**선행:** Bundle 2 게이트 통과

### 🎯 목표
1. 화면 하단 토글 버튼 → 클릭 시 업그레이드 패널 슬라이드 업/다운
2. 패널 내부: ScrollRect + GridLayoutGroup(열=2)
3. 신규 능력치 모두에 대응하는 `UpgradeDefinition` 항목 추가

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 토글 버튼 하단 중앙에 항상 노출
- [ ] 펼친 패널: 한 줄 2개 버튼, 세로 스크롤 가능
- [ ] 업그레이드 항목 9종 (autoDamage, manualDamage, autoFireRate, manualFireRate, critChance, critMultiplier, armorPen, maxHealth, mana)
- [ ] 회귀 테스트 5건 통과

### 📂 변경 파일

#### F-1. `Assets/Scripts/Upgrades/UpgradeDefinition.cs` (수정)
**`UpgradeType` enum 전면 교체:**
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

> 기존 `Attack`, `Critical` enum 값 **삭제**. UpgradeSystem switch-case 호출부도 새 이름으로 마이그레이션.

#### F-2. `Assets/Scripts/Upgrades/UpgradeSystem.cs` (수정)
- `EnsureDefaults()` 9개 항목으로 확장:
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
- `Apply(definition)` switch에 신규 case 모두 추가:
  - `AutoDamage` → `wizard.Stats.AddAutoDamage(value)`
  - `ManualDamage` → `wizard.Stats.AddManualDamage(value)`
  - `AutoFireRate` → `wizard.Stats.AddAutoFireRate(value)`
  - `ManualFireRate` → `wizard.Stats.AddManualFireRate(value)`
  - `CriticalChance` → `wizard.Stats.AddCriticalChance(value)`
  - `CriticalMultiplier` → `wizard.Stats.AddCriticalMultiplier(value)`
  - `ArmorPenetration` → `wizard.Stats.AddArmorPenetration(value)`
  - `MaxHealth` → `wizard.Stats.AddMaxHealth(value)`
  - `Mana` → `mana.IncreaseMax(value); mana.IncreaseRegeneration(1f);` (기존 동작 유지)

#### F-3. `Assets/Scripts/UI/UpgradeDrawerView.cs` (신규)
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

#### F-4. `Assets/Scripts/UI/HUDController.cs` (수정)
- 기존 `[SerializeField] private UpgradeButtonView[] upgradeButtons;` 제거
- 기존 `[SerializeField] private Sprite[] upgradeIcons;` 제거 (또는 동적 생성에 활용)
- 신규 직렬화 필드:
```csharp
[SerializeField] private UpgradeDrawerView upgradeDrawer;
[SerializeField] private Transform upgradeButtonContainer;     // ScrollRect Content
[SerializeField] private UpgradeButtonView upgradeButtonPrefab;
[SerializeField] private Sprite[] upgradeIcons;                // 9개, 순서 = upgrades 순서
```
- `BindUpgradeButtons` 재작성: `system.Upgrades` 순회 → Instantiate(prefab, container) → Bind
- `RefreshUpgradeButtons`: 컨테이너 내 모든 view에 Refresh()

#### F-5. `MainScene` 구성 (Unity Editor 작업)
HUD Canvas 안에:
1. **하단 토글 버튼**: `UpgradeToggleButton` — 화면 하단 중앙
2. **드로어 패널**: `UpgradeDrawerPanel` (RectTransform, anchoredPosition Y로 슬라이드)
   - 자식 ScrollRect (Vertical only)
   - ScrollRect.Content: GridLayoutGroup
     - Constraint = FixedColumnCount, Count = 2
     - Cell Size 320x140 (조정 가능)
3. UpgradeDrawerView 컴포넌트를 패널에 부착 + 인스펙터 필드 할당
4. 신규 프리팹 `Assets/Prefabs/UI/UpgradeButton.prefab` 생성:
   - UpgradeButtonView + Button + TMP_Text + Image 구성
   - HUDController의 `upgradeButtonPrefab` 필드에 할당

> ⚠️ Build Prototype Scene 실행 금지.

### 🚫 건드리지 말 것
- `PlayerStats`, `CombatCalculator`
- `StageManager`, 보스 흐름
- 다른 HUD 위젯 위치

### 🧪 검증
1. 컴파일 클린
2. PlayMode → 하단 토글 버튼 노출, 패널 화면 밖
3. 토글 클릭 → 슬라이드 업, 2열 그리드, 세로 스크롤 가능
4. 9종 업그레이드 각각 클릭 → 골드 차감 + 해당 능력치 증가 (인스펙터로 PlayerStats 검증)
5. 토글 재클릭 → 슬라이드 다운

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

# Bundle 4 — 로컬 저장

**목표:** 게임 진행 상태를 디스크에 영속화.

### Bundle 4 종합 회귀 테스트
1. PlayMode → 골드 100 + 업그레이드 1회 → 종료 → 재진입 → 모두 복원
2. save.json 사람이 읽을 수 있는 형식
3. saveVersion 필드 존재
4. save.json 삭제 후 재진입 → 신규 게임 정상 시작
5. 챕터/스테이지 진행도 복원

---

## Task G — SaveData 모델 + 로컬 저장

**Status:** 🔴 TODO
**선행:** Bundle 3 게이트 통과

### 🎯 목표
- 능력치, 골드, 챕터/스테이지, 업그레이드 레벨을 직렬화
- `Application.persistentDataPath/save.json`에 JSON 저장
- 게임 시작 시 자동 로드, 주요 이벤트마다 자동 저장
- `saveVersion` + 마이그레이션 훅

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 종료 후 재진입 시 골드/챕터·스테이지/능력치 모두 복원
- [ ] save.json 사람이 읽을 수 있는 JSON
- [ ] 회귀 테스트 5건 통과

### 📂 변경 파일

#### G-1. `Assets/Scripts/Save/SaveData.cs` (신규)
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

#### G-2. `Assets/Scripts/Save/SaveService.cs` (신규)
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

#### G-3. `Assets/Scripts/Save/SaveBinder.cs` (신규)
**책임:** SaveData ↔ 게임 시스템(`PlayerStats`, `CurrencyWallet`, `StageManager`, `UpgradeSystem`) 양방향 매핑 + 자동 저장 트리거 등록.

```csharp
public class SaveBinder : MonoBehaviour
{
    public void ApplyToGame(SaveData data, GameContext ctx);
    public SaveData CaptureFromGame(GameContext ctx);
    public void RegisterAutoSaveTriggers(GameContext ctx, SaveService service);
}
```

**자동 저장 트리거:**
- `wallet.GoldChanged` (debounce 1초 권장)
- `upgradeSystem.UpgradePurchased`
- `stageManager.StateChanged`
- `OnApplicationPause(true)`, `OnApplicationQuit`

**적용 시 의존 메서드 추가 필요:**
- `PlayerStats`에 `ApplySnapshot(snapshot)`, `CaptureSnapshot()` 메서드 (G-6)
- `StageManager`에 `LoadProgress(int chapter, int stage)` 메서드 추가
- `UpgradeSystem`에 `LoadLevels(List<UpgradeLevelEntry>)` 메서드 추가

#### G-4. `Assets/Scripts/Core/GameManager.cs` (수정)
- `Awake()` 도입부:
```csharp
context.SaveService.TryLoad();
// ... 기존 Initialize 호출들 ...
context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
context.SaveBinder.RegisterAutoSaveTriggers(context, context.SaveService);
```
- 라이프사이클 콜백 추가:
```csharp
private void OnApplicationPause(bool paused) { if (paused) context.SaveService.Save(); }
private void OnApplicationQuit() { context.SaveService.Save(); }
```

#### G-5. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `SaveService`, `SaveBinder`

#### G-6. `Assets/Scripts/Player/PlayerStats.cs` (예외적 수정 — 메서드 2개 추가)
**검토자 승인 (Task G 본문에 명시):** Task A의 "PlayerStats 손대지 말 것"은 stat 변경 금지 의미. 본 추가는 IO 어댑터(스냅샷 적용/추출)이며 stat 의미를 바꾸지 않으므로 허용.

- `public void ApplySnapshot(PlayerStatsSnapshot s)` — 직렬화 데이터로부터 필드 일괄 적용 + Changed/HealthChanged 이벤트 발신
- `public PlayerStatsSnapshot CaptureSnapshot()` — 현재 stat을 snapshot으로 추출

> 본 추가는 작업 로그에 명시 필수.

### 🚫 건드리지 말 것
- 게임 로직(전투, 보스, 스테이지 흐름) 자체
- UI 위젯 (HUDController는 변경 없음)

### 🧪 검증
1. 컴파일 클린
2. PlayMode → 골드 100 획득, 자동공격력 업그레이드 1회 → 정지 → 재진입 → 골드, 능력치 복원
3. `save.json` 텍스트 에디터로 열기, 사람이 읽을 수 있는 JSON 확인
4. saveVersion=1 필드 존재
5. save.json 삭제 → 재진입 → 신규 게임 정상 시작 (currentChapter=1, currentStage=1, gold=0)

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

# Bundle 5 — Firebase Auth + Firestore 동기화

**목표:** 사용자별 클라우드 저장. 디바이스 간 동기화.

### ⚠️ Bundle 5 시작 전 사용자 사전 작업 (필수)

**Firebase 설정 (Task H 시작 전):**
- [ ] Firebase 콘솔 → 프로젝트 생성, Project ID 확보
- [ ] Firebase에 iOS 앱 등록 → `GoogleService-Info.plist` 다운로드 → `Assets/`에 배치
- [ ] Firebase에 Android 앱 등록 → `google-services.json` 다운로드 → `Assets/`에 배치
- [ ] Authentication → 익명 / Google / Apple 제공자 활성화
- [ ] Apple Developer ($99/년) → Sign in with Apple 활성화, Service ID 발급
- [ ] Google Cloud Console → OAuth 클라이언트 ID 발급
- [ ] Bundle ID / Package Name 확정 (예: `com.kmj.wizardgrower`)

**Firestore 설정 (Task I 시작 전):**
- [ ] Firestore 데이터베이스 생성 (Production 모드 권장)
- [ ] Security Rules 설정:
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

> **위 사전 작업이 모두 완료되어 사용자가 명시적으로 "Bundle 5 진행"이라 지시하기 전까지 H/I 시작 금지.**

### Bundle 5 종합 회귀 테스트
1. 첫 실행 → 익명 로그인, UID 발급, Firestore 도큐먼트 자동 생성
2. Google 로그인 → UID 연결, 기존 진행도 유지
3. save.json 삭제 → 같은 계정으로 재로그인 → Firestore에서 복원
4. 오프라인에서 PlayMode → 게임 정상 동작 → 온라인 복귀 → 자동 동기화
5. 두 기기에서 진행 → 더 늦게 저장한 쪽 우선

---

## Task H — Firebase Auth (익명 / Google / Apple 로그인)

**Status:** 🔴 TODO
**선행:** Bundle 4 게이트 통과 + Bundle 5 사용자 사전작업 완료

### 🎯 목표
- Firebase Unity SDK 통합 (Auth 모듈)
- 게임 시작 시 익명 로그인 자동 수행
- Google / Apple 계정 연결 가능
- 로그인 성공 시 UID를 SaveData.userId에 반영

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 첫 실행 시 익명 UID 발급, 콘솔 로그 출력
- [ ] Google 로그인 버튼 클릭 → 계정 선택 → UID 연결
- [ ] Apple 로그인 버튼 클릭 → 계정 선택 → UID 연결 (iOS only)
- [ ] 회귀 테스트 4건 통과 (디바이스 또는 시뮬레이터)

### 📂 변경 파일

#### H-1. 패키지 도입
- Firebase Unity SDK 12.x 이상 (Auth 모듈)
- Apple Sign In Unity Plugin
- Google Sign In Unity Plugin

> 정확한 패키지 이름·버전은 Firebase 콘솔의 Unity 가이드를 따름.

#### H-2. `Assets/Scripts/Auth/AuthService.cs` (신규)
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

#### H-3. `Assets/Scripts/UI/LoginPanel.cs` (신규)
- 옵션 화면 또는 메인 화면에서 호출
- 버튼: Google, Apple(iOS만), Skip
- AuthService 호출 결과 UI 반영

#### H-4. `Assets/Scripts/Core/GameManager.cs` (수정)
- `Awake()`에서 Firebase 초기화 → `AuthService.SignInAnonymouslyAsync()` → UID를 SaveBinder에 전달

#### H-5. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `AuthService`

#### H-6. `MainScene` 구성
- LoginPanel UI 추가 (옵션)

### 🚫 건드리지 말 것
- 전투, 스테이지 로직
- 다른 UI 위젯

### 🧪 검증
1. 컴파일 클린 (Firebase SDK 패키지 import 후)
2. 첫 실행 → 익명 UID 콘솔 출력
3. Google 로그인 → UID에 Google credential 연결
4. Apple 로그인 (iOS 빌드) → 동일 흐름

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

## Task I — Firestore 클라우드 동기화

**Status:** 🔴 TODO
**선행:** H

### 🎯 목표
- 로컬 SaveData를 Firestore `users/{uid}` 도큐먼트에 동기화
- 오프라인 우선 (로컬이 진실, 백그라운드 동기화)
- 충돌 해결: `updatedAtUnixMs` 비교 → newer-wins
- 네트워크 실패 시 로컬 동작 유지

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] PlayMode 골드 획득 → 5초 이내 Firestore 갱신 확인
- [ ] save.json 삭제 후 재로그인 → Firestore에서 복원
- [ ] 오프라인 정상 동작
- [ ] 회귀 테스트 5건 통과

### 📂 변경 파일

#### I-1. 패키지 추가
- Firebase Firestore Unity 모듈

#### I-2. `Assets/Scripts/Save/CloudSyncService.cs` (신규)
```csharp
public class CloudSyncService
{
    private FirebaseFirestore db;

    public Task PushAsync(SaveData data);
    public Task<SaveData> PullAsync(string uid);
    public Task ResolveAndApply(SaveService localService, string uid);
}
```

`ResolveAndApply` 동작:
- Firestore에서 remote 가져옴
- remote 없으면 → local push
- remote가 더 최신(updatedAtUnixMs) → local 덮어쓰기 + 저장
- 그 외 → local push

#### I-3. `Assets/Scripts/Save/SyncCoordinator.cs` (신규)
**트리거:**
- AuthService.UserChanged → `ResolveAndApply()` 1회
- SaveData 변경 시 debounce 5초 후 push
- `OnApplicationPause(true)` → 즉시 push
- 네트워크 오프라인 시 큐잉, 온라인 복귀 시 플러시

#### I-4. `Assets/Scripts/Save/SaveData.cs` (Firestore 호환)
- `[FirestoreData]`, `[FirestoreProperty]` 어트리뷰트 추가 (필드별)

#### I-5. `Assets/Scripts/Core/GameManager.cs` (수정)
- AuthService 로그인 콜백에서 SyncCoordinator 시작

### 🚫 건드리지 말 것
- 전투, 스테이지, UI 로직
- SaveService 핵심 (CloudSyncService와 분리 유지)

### 🧪 검증
1. 컴파일 클린
2. PlayMode 익명 로그인 → 골드 획득 → Firestore 콘솔에서 `users/{uid}` 갱신 확인
3. save.json 삭제 → 같은 UID 재로그인 → Firestore에서 복원
4. Wi-Fi OFF로 PlayMode → 정상 동작 → Wi-Fi ON → 자동 동기화
5. 두 기기 동시 진행 → 더 늦게 저장 쪽 우선

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

## 부록 A — 검토자 체크리스트 (각 태스크 완료 시)

검토자는 본 문서를 기준으로 다음을 확인한다:

1. **DoD 100% 충족** — 체크박스 모두 통과
2. **변경 파일 일치** — 명시된 파일 외 수정 여부 확인 (`git diff` 비교)
3. **건드리지 말 것 영역 미수정** — 명시된 파일 unchanged
4. **회귀 테스트 통과** — 검증 항목 직접 실행
5. **기획 정합성** — 본 문서의 의도와 구현 결과가 어긋나지 않음
6. **git 커밋 1개 존재** — 태스크 ID 포함된 메시지

검토 결과는 각 태스크의 "🔍 검토 노트" 섹션에 기록한다.

---

## 부록 B — 묶음 게이트 통과 시 검토자 체크리스트

묶음의 마지막 태스크가 ✅ DONE 처리된 후, 다음 묶음 시작 전 검토:

1. **묶음 종합 회귀 테스트 통과** (각 묶음 시작 섹션 명시)
2. **`git log` 정리** — 묶음 내 모든 태스크의 commit 존재
3. **컴파일 / 경고 0**
4. **다음 묶음 첫 태스크의 "선행 가정" 일치** — 코드 베이스 변경이 본 문서의 가정과 어긋나지 않는지 재검증

이상 없으면 검토자가 "Bundle X 게이트 통과" 메시지를 사용자에게 보고하고 다음 묶음으로 진행.

---

## 부록 C — 변경 이력

| 일자 | 변경자 | 내용 |
|------|--------|------|
| 2026-05-06 | Planner | 초기 문서 생성, Task A 상세화, B~I 개요 |
| 2026-05-06 | Planner | Task A에 §A-10 추가 — `HUDController.cs:112` 1줄 수정 예외 허용 |
| 2026-05-06 | Planner | Task A ✅ DONE 처리 (검토 완료) |
| 2026-05-06 | Planner | Bundle 1~5 묶음 구조 도입, B~I 풀 스펙 이관, §0에 묶음 게이트/자동진행 금지/git 커밋 규칙 추가 |
