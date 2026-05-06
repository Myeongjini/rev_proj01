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
| 1 | B | 전투 정합성 + 발사속도 stat 반영 + 버그 수정 | ✅ DONE | A |
| 2 | C | ChapterDefinition / StageDefinition 데이터 모델 | ✅ DONE | Bundle 1 게이트 |
| 2 | D | StageManager 흐름 리팩터 (필드 ↔ 보스방) | ✅ DONE | C |
| 2 | E | 보스 입장 버튼 + HUD 챕터/스테이지 표시 | ✅ DONE | D |
| 3 | F | 업그레이드 드로어 UI (하단 토글, 2열 스크롤) | ✅ DONE | Bundle 2 게이트 ✅ |
| 4 | G | SaveData 모델 + 로컬 저장 | 🔴 TODO | Bundle 3 게이트 ✅ |
| 5 | H | 서버 로그인 + 유저 식별 등록 | 🔴 TODO | Bundle 4 게이트 + 사용자 사전작업 |
| 5 | I | Firestore 서버 정본(canonical) 게임 상태 | 🔴 TODO | H |

> **Bundle 1·2·3 게이트 통과 (2026-05-06).** Bundle 4 / Task G 진행 가능.

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

**Status:** ✅ DONE
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
- 2026-05-06 리뷰 수정: 사용자 요청에 따라 Fire 버튼 수동 공격의 ManualAttackInterval 쿨다운 제거. 빠른 3연타 모두 즉시 발사(projectileDelta=3) 확인.

### 🔍 검토 노트 (검토자 기록)
- 2026-05-06: 코드 검증 완료.
  - **AutoAttackController**: `Update()` + `TryFireNow()` 모두 `(!AutoModeEnabled || IsManualMoving)` 가드 적용. 스펙 정확.
  - **ClickAttackController**: `mana`/`manualManaCost` 필드 + Initialize의 mana 인자 모두 제거됨. `TryRepairCalculator()` 내부 보호 정확.
  - **GameManager**: `ClickAttack.Initialize` 호출에 mana 인자 제거됨.
  - **사용자 요청 반영(쿨다운 제거)**: 작업 로그 명시대로 `lastFireTime` 필드와 인터벌 가드 모두 제거됨. `TryFireManual`이 매 호출마다 즉시 발사. **검토자 승인** — DoD §검증 5번(연타 시 인터벌 발사) 항목은 사용자 결정에 따라 폐기됨.
  - **부가 변경**: D 단계에서 `enemySpawner.CurrentEnemy` → `enemySpawner.GetNearestEnemy(...)`로 타겟 획득 변경됨 (다중 몬스터 지원의 일환). B 자체 스펙은 `CurrentEnemy` 사용이었으나 D의 다중 몬스터 도입에 맞춰 자연스럽게 마이그레이션됨. 합당한 부가 변경.
  - **결론: ✅ DONE.**

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

**Status:** ✅ DONE
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
- 2026-05-06 시작: Bundle 2 Task C 착수. 옵션 A 선택: 기존 Serializable StageDefinition은 LegacyStageBalance로 분리하고 StageManager 필드는 컴파일 호환용으로 LegacyStageBalance를 참조하도록 최소 변경.
- 2026-05-06 종료: StageDefinition을 ScriptableObject로 전면 교체, ChapterDefinition/ChapterDatabase/LegacyStageBalance 추가. Assets/Data/Chapters 아래 Chapter01_GloomyForest, Stage01~08, ChapterDatabase 자산 생성 및 DB stages 8개 직렬화 확인. 스크립트 검증 에러/경고 0. Console에는 MCP client handler 종료 로그가 Exception 타입으로 남으나 게임 코드 에러/경고는 없음.

### 🔍 검토 노트 (검토자 기록)
- 2026-05-06: 코드 + 자산 검증 완료.
  - **StageDefinition.cs**: ScriptableObject + `[CreateAssetMenu]` 메뉴 정확. 필드 9종(stageNumber/displayLabel/fieldMonster*/boss*/optional Visual) 모두 스펙 일치.
  - **ChapterDefinition.cs / ChapterDatabase.cs**: 신규 작성, 스펙 일치. `GetChapter(chapterNumber)` 헬퍼 정확.
  - **LegacyStageBalance.cs**: 옵션 A 선택 — 기존 Serializable 정의 분리. 작업 로그 명시 OK.
  - **자산**: `Assets/Data/Chapters/Chapter01_GloomyForest.asset`, `Stages/Stage01~08`, `ChapterDatabase.asset` 모두 존재. 디렉토리 구조 정확.
  - **결론: ✅ DONE.** (실제 발랜스값 직렬화 정확성은 Task D PlayMode 검증에서 함께 확인됨)

---

## Task D — StageManager 흐름 리팩터 (필드 ↔ 보스방)

**Status:** ✅ DONE
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
- 2026-05-06 시작: Bundle 2 Task D 착수. StageManager를 ChapterDatabase 기반 Field/BossRoom 모드 흐름으로 전면 교체하고, EnemySpawner armor 인자/ChapterDatabase 주입/GameManager 초기화/HUDController 구독 제거만 수행 예정.
- 2026-05-06 종료: StageMode 추가, StageManager 필드 무한 리스폰/보스방 진입/클리어/실패 복귀 흐름 구현, EnemySpawner SpawnNormal/SpawnBoss armor 인자 반영, GameContext ChapterDatabase 필드 추가 및 MainScene 할당, GameManager 초기화 시그니처 갱신, HUDController의 기존 StageChanged 구독/핸들러 제거 완료. PlayMode 직접 검증: 초기 필드 1-1 NormalEnemy, 일반 몬스터 5회 처치 후 stage 1 유지 + 골드 획득, EnterBossRoom 보스/20초 타이머 시작, 보스 처치 stage 2 Field 복귀, 보스 실패 stage 2 유지 Field 복귀 통과. Console 게임 코드 에러/경고 없음.
- 2026-05-06 리뷰 수정: 필드 모드가 단일 몬스터 반복이던 구조를 다중 일반 몬스터 필드로 재구성. EnemySpawner 활성 적 목록/가까운 적 탐색/NormalEnemy 5마리 그룹 스폰 추가, EnemyWanderController로 필드 몬스터 자유 배회 구현, 자동 이동/자동 공격/수동 공격/액티브 스킬 타겟을 가까운 생존 적 기준으로 변경. 보스 입장 시 필드 몬스터 전체 정리 후 BossEnemy 1마리만 유지 확인.
- 2026-05-06 리뷰 수정: 필드 몬스터가 전체 사망 후에만 리스폰되던 문제 수정. EnemySpawner에 SpawnNormalReplacement 추가, StageManager에 RespawnFieldEnemyAfterDelay 코루틴과 fieldSpawnVersion 가드 추가, 몬스터 1마리 처치 시 7→6→7로 개별 리스폰 확인. EnemyHealthBarView를 추가해 모든 필드 몬스터와 보스가 각자 월드 체력바를 갖도록 변경. 필드 몬스터 수 7마리, 스폰 범위 x -5.8~5.8 / y -3.25~3.25, 최소 간격 1.15로 확장 적용. 보스방 진입 시 일반 몬스터 0, BossEnemy 1, 보스 체력바 1 확인. Console 게임 코드 에러/경고 없음.
- 2026-05-06 리뷰 수정: 필드(맵) 크기가 작다는 피드백 반영. PlayerMovementController 이동 bounds를 x -12~12 / y -7~7로 확장, EnemySpawner 필드 몬스터 수 10마리 및 스폰/배회 범위를 x -12~12 / y -7~7 기준으로 재설정. MobileCameraFitter에 followTarget/followOffset/mapCenter/mapSize 추가 후 PlayerWizard를 추적하도록 MainScene에 연결, PlayMode에서 player=(11.80,6.80) 이동 후 camera=(11.80,6.80,-10.00), centerDelta=(0,0,0) 확인. 필드 몬스터 10마리 넓은 범위 분산 생성 확인. Console 게임 코드 에러/경고 없음.

### 🔍 검토 노트 (검토자 기록)
- 2026-05-06: 코드 검증 완료. 핵심 흐름 + 다중 사용자 추가 요청 모두 적절히 반영됨.
  - **StageManager.cs**: 스펙 그대로 — `Initialize`/`EnterBossRoom`/`OnEnemyKilled`/`OnBossFailed`/`AdvanceToNextStage`/`ReturnToField`/`SpawnFieldEnemies`/`ResolveCurrentStage`/`RaiseStateChanged` 모두 구현. `RespawnFieldEnemyAfterDelay` 코루틴에 `fieldSpawnVersion` 가드로 모드 전환 시 race 제거. AdvanceToNextStage의 챕터 끝 처리(다음 챕터 부재 시 마지막 스테이지 유지 + "All Cleared" 피드백) 정확.
  - **EnemySpawner.cs**: 스펙 외 다중 적 지원 추가. `activeEnemies` 리스트, `SpawnNormalGroup`/`SpawnNormalReplacement`/`ClearEnemies`/`GetNearestEnemy` 신설. `CurrentEnemy`는 backward compat용으로 유지(가장 최근 살아있는 적 반환). `Spawn()` 내부에서 EnemyHealthBarView/EnemyWanderController 자동 부착. 스페이싱 알고리즘(24회 retry + minSpawnSpacing) 합리적.
  - **GameContext / GameManager**: ChapterDatabase 필드 + Initialize 시그니처 마이그레이션 정확.
  - **HUDController D-7 처리**: 옛 `StageChanged` 구독·핸들러 제거 확인 (Task E에서 새 이벤트로 재구독됨).
  - **🆕 사용자 추가 요청 반영분 (DoD 외 추가 작업, 작업 로그에 모두 기록됨):**
    - `EnemyWanderController.cs` 신규 — 필드 몬스터 자유 배회
    - `EnemyHealthBarView.cs` 신규 — 몬스터별 월드 체력바
    - 필드 몬스터 1마리씩 개별 리스폰 (전체 사망 후 일괄 → 1킬당 1리스폰)
    - 맵 확장: 이동 bounds + 스폰 bounds + 카메라 추종 (PlayerMovementController, MobileCameraFitter, EnemySpawner)
    - 자동/수동/스킬 타겟 모두 가까운 생존 적 기준
  - **부가 메모**: `EnemySpawner.CurrentEnemy` 프로퍼티는 D 이후 사용처가 없을 수 있음 (Auto/Click/Skill 모두 `GetNearestEnemy` 사용). 추후 정리 후보지만 현재 호환성 안전망으로 무해.
  - **결론: ✅ DONE.** Bundle 2 종합 회귀 통과 보고도 Task E 작업 로그에 명시되어 있음.

---

## Task E — 보스 입장 버튼 + HUD 챕터/스테이지 표시

**Status:** ✅ DONE
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
- 2026-05-06 시작: Bundle 2 Task E 착수. HUDController에 StageManager.StateChanged/BossEntryAvailabilityChanged 연결, 보스 입장 버튼 생성/할당, Task D 디버그 보스 진입 메뉴 제거 예정.
- 2026-05-06 종료: HUDController에 bossEntryButton/bossEntryButtonLabel 필드 및 새 StateChanged/BossEntryAvailabilityChanged 핸들러 추가, BossEntryButton 씬 생성 및 필드 할당, StageManager의 Debug Enter Boss ContextMenu 제거 완료. 한글 HUD 표시 경고 방지를 위해 macOS 기본 AppleGothic 기반 TMP 폰트 에셋을 Assets/Fonts에 생성하고 StageLabel/BossEntryButton Label에 할당. PlayMode 검증: "음산한 숲 1-1" 라벨 + 보스 입장 버튼 활성, 클릭 시 "음산한 숲 1-1 BOSS" + 버튼 비활성 + BossEnemy, 보스 처치 후 "음산한 숲 1-2" + 버튼 재활성 통과. Bundle 2 종합 흐름: 필드 처치 stage 유지, 보스 실패 stage 유지 Field 복귀, 1-8 보스 클리어 후 마지막 챕터 All Cleared 경로 통과. Console 게임 코드 에러/경고 없음.
- 2026-05-06 리뷰 수정: BossEntryButton을 하단 우측(anchor 1,0 / pos -24,124 / size 180x54)으로 이동해 ActiveSkillButton과 겹치지 않도록 조정. 버튼 라벨은 "보스 입장" 유지, AppleGothic_TMP 폰트 적용 상태 재확인.
- 2026-05-06 리뷰 수정: 신규 EnemyHealthBarView와 기존 HUD 단일 HealthBarView가 동시에 노출되던 문제 수정. HUDController에서 spawner.EnemySpawned += healthBar.Bind 구독 제거, Initialize 시 기존 HUD HealthBarView 비활성화 처리. PlayMode에서 HUD HealthBarView active=false, EnemyHealthBarView 7개, 살아있는 몬스터 7마리 확인. Console 게임 코드 에러/경고 없음.

### 🔍 검토 노트 (검토자 기록)
- 2026-05-06: 코드 검증 완료.
  - **HUDController.cs**: `bossEntryButton`/`bossEntryButtonLabel` 필드 추가, `Initialize`에서 `StateChanged`/`BossEntryAvailabilityChanged` 구독 + 버튼 onClick으로 `EnterBossRoom()` 연결. 핸들러 `OnStateChanged` 형식 `"{displayName} {chapter}-{stage}[ BOSS]"` 정확. `OnBossEntryAvailabilityChanged`에서 interactable 토글 + 라벨 "보스 입장" 유지.
  - **HUD HealthBarView 비활성화**: `Initialize`에서 `healthBar.gameObject.SetActive(false)` — 기존 단일 HP바와 신규 EnemyHealthBarView 충돌 방지. 합당.
  - **사용자 추가 요청**: 한글 폰트(AppleGothic_TMP / KoreanFallback_TMP) `Assets/Fonts/`에 추가, BossEntryButton 위치 우하단(anchor 1,0 / pos -24,124 / size 180x54)으로 조정.
  - **결론: ✅ DONE.** Bundle 2 종합 회귀 통과 보고 — 1-1~1-8 진행 + All Cleared 경로 확인됨.

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

**Status:** ✅ DONE
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
- 2026-05-06 시작: Bundle 3 Task F 착수. `Tasks.md` 확인 결과 Bundle 1·2 게이트 통과, Task F 진행 가능 상태. 시작 시점의 `Tasks.md` 미커밋 번역분, `Tasks_kr.md`, `.DS_Store`는 사용자/주변 변경으로 간주하고 무관한 편집은 피함. UpgradeType 9-stat 마이그레이션, UpgradeSystem 기본 항목/switch case, UpgradeDrawerView, HUD 동적 업그레이드 버튼 바인딩, UpgradeButton 프리팹, MainScene 드로어 구성 진행 예정.
- 2026-05-06 종료: UpgradeType을 9개 stat 전용 항목으로 교체 후 UpgradeSystem 기본 정의/Apply switch 마이그레이션 완료. UpgradeDrawerView, UpgradeDrawerGridFitter 추가. HUDController가 ScrollRect Content 아래에 UpgradeButtonView 프리팹을 동적 생성하도록 변경. `Assets/Prefabs/UI/UpgradeButton.prefab` 신규 생성, MainScene에 하단 중앙 UpgradeToggleButton + UpgradeDrawerPanel/ScrollRect/GridLayoutGroup 구성. 구현 중 리뷰 피드백 반영: 버튼 셀이 드로어 폭을 채우는 반응형 2열 그리드로 동작하며, 셀이 더 커져(PlayMode 측정 셀 사이즈 907x176) 세로 스크롤이 의미 있도록 함. UpgradeButtonView 런타임 리스너 바인딩 수정으로 버튼 클릭 시 업그레이드 구매 정상 동작. PlayMode 검증: 패널이 y=-520에서 닫힌 상태로 시작, 토글 시 y=84로 펼쳐짐, 자식 9개·2열·세로 스크롤 활성, 9종 업그레이드 클릭 모두 골드 차감 + autoDamage/manualDamage/autoFireRate/manualFireRate/critChance/critMultiplier/armorPen/maxHealth/mana 즉시 반영, 토글 재클릭 시 y=-520으로 닫힘. Console에 게임 코드 에러/경고 없음.
- 2026-05-06 리뷰 수정: 사용자가 메이플스토리/라테일 풍 2D 치비 비주얼 폴리시를 Wizard/Slime/Boss/Background에 적용 요청, Wizard 애니메이션 추가, 카메라 추종 변경 후 자연스러운 맵/배경 스케일, 데미지 텍스트 가독성 향상, 업그레이드 드로어의 깨진 TMP 렌더링 수정 요청. `VisualAssetUpdater` 에디터 유틸 추가, Wizard/Slime/Boss/TopDownBackground 스프라이트 재생성, Wizard idle/run 애니메이션 자산 + 이동 기반 애니메이션 컨트롤러 추가, 생성된 스프라이트를 프리팹/MainScene에 재할당, 런타임 데미지 텍스트 크기 확대, 모든 씬/업그레이드 TMP 텍스트를 `AppleGothic_TMP`로 재할당. PlayMode 검증: Wizard가 Run 스프라이트 + Animator + 드라이버 사용, 필드 배경이 `TopDownBackground` 사용, 스폰된 슬라임이 새 `Slime` 사용, `DamageText.prefab` 폰트 크기 38, 씬 TMP 중 비-AppleGothic 카운트 0.

### 🔍 검토 노트 (검토자 기록)
- 2026-05-06: 코드 + 자산 검증 완료. 스펙 부합 + 사용자 요청 비주얼 폴리시 모두 적절히 반영됨.
  - **UpgradeDefinition.cs**: enum 9종(AutoDamage / ManualDamage / AutoFireRate / ManualFireRate / CriticalChance / CriticalMultiplier / ArmorPenetration / MaxHealth / Mana)으로 교체. 기존 `Attack` / `Critical` 제거. 스펙 일치.
  - **UpgradeSystem.cs**: `EnsureDefaults`가 9개 항목을 정확한 한글 displayName, baseCost, value로 추가. `Apply` switch가 9 case 모두를 올바른 `PlayerStats.AddXxx` / `PlayerMana.IncreaseMax`에 매핑. 우발적 초기화 방지를 위한 `HasCurrentDefaultSet()` 방어 체크(9개 ID 순서 비교) 합리적 추가.
  - **UpgradeDrawerView.cs**: `Toggle` / `Animate` / `ApplyImmediate` 스펙대로 구현, "강화 닫기/열기" 라벨 정확. 핫리로드 시 중복 바인딩 방지를 위한 `RemoveListener(Toggle)` 사전 호출 — 사소한 개선.
  - **UpgradeDrawerGridFitter.cs (스펙 외)**: 패널 폭 기반으로 셀 폭을 동적 계산 (220 최소 + 설정 가능한 cellHeight=176). 스펙 외 합리적 반응형 솔루션, DoD 영향 없음.
  - **HUDController.cs (F-4)**: 옛 `upgradeButtons[]` 배열 제거; 신규 필드 `upgradeDrawer` / `upgradeButtonContainer` / `upgradeButtonPrefab` / `upgradeIcons` 배치. `BindUpgradeButtons`가 리스트 클리어, 이전 자식 제거, `system.Upgrades` 순회, 프리팹 인스턴스화, 바인드, `upgradeButtonViews` 리스트 추적까지 정확히 수행. `RefreshUpgradeButtons`이 동적 리스트 사용. 스펙 일치.
  - **UpgradeButton.prefab**: `Assets/Prefabs/UI/`에 존재.
  - **🆕 사용자 요청 비주얼 폴리시 (commit 95d022b — 리뷰 수정):**
    - `Wizard.png` / `Slime.png` / `Boss.png` / `TopDownBackground.png` 재생성
    - Wizard 애니메이션: `Wizard_Idle_0/1.png` + `Wizard_Run_0/1.png` + `Wizard.controller` + `Wizard_Idle.anim` + `Wizard_Run.anim`
    - 신규 `WizardAnimationController.cs` (29줄) — LateUpdate에서 위치 변화 임계값으로 Animator `Moving` bool 토글. 깔끔.
    - 신규 `Editor/VisualAssetUpdater.cs` (451줄) — 스프라이트 재생성 유틸
    - `DamageTextView.cs` 런타임 폰트 크기 확대
    - 모든 씬/드로어 TMP 텍스트를 `AppleGothic_TMP`로 재할당 (한글 렌더링 수정)
    - 카메라 추종 / 맵 스케일 조정이 MainScene + 관련 프리팹에 반영
  - **결론: ✅ DONE.** Bundle 3 게이트 통과 가능 — F가 Bundle 3의 유일한 태스크.

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

> **로컬 저장의 아키텍처상 역할:** Bundle 5 출시 후에는 **서버 DB가 정본(canonical)** 이 된다. 로컬 `save.json`은 그 시점부터 (a) 첫 실행 / 로그인 전 fallback과 (b) 클라우드 동기화(Task I)가 서버와 정합시키는 오프라인 캐시 두 가지 역할만 수행한다. 본 태스크에서는 H/I가 아직이므로 로컬 저장이 자족적인 단일 저장소처럼 동작하도록 구현하면 되고, 오프라인 캐시 역할로의 격하는 Task I에서 적용된다.

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

# Bundle 5 — 서버 로그인 + 서버 정본(canonical) 유저 데이터베이스

**목표:** 모든 유저는 Google / Apple / 익명으로 서버에 로그인하여 고유 식별자를 등록하고, 게임 상태를 **서버측 DB(정본)** 에 저장한다. 로컬 `save.json`(Task G)은 오프라인 캐시로 격하된다. 동일 계정을 사용하는 두 기기는 진행도를 공유한다.

**아키텍처 (정본 모델):**
```
   게임 상태 변경
         │
         ▼
   ┌───────────────┐  push (debounce)    ┌──────────────────┐
   │  로컬 캐시    │ ───────────────────▶│  Firestore       │  ← 정본
   │  save.json    │                     │  users/{uid}     │
   │  (오프라인 OK)│ ◀───────────────── │  + profile doc    │
   └───────────────┘    pull / restore   └──────────────────┘
                              │
                              ▼
                    동일 UID 다른 기기
                    → 로그인 시 자동 복원
```

**저장소 선택:** Firebase Auth + Cloud Firestore. 인디 방치형/클리커 게임에서 사실상 표준 — 무료 한도가 넓고, 오프라인 영속성 내장, 도큐먼트 단위 보안 규칙 가능. 대안(PlayFab, Supabase, 자체 백엔드)은 Task H 시작 전 사용자가 결정 변경하지 않는 한 범위 외.

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
1. 첫 실행 → 익명 로그인 → UID 발급 → `users/{uid}` 프로필 도큐먼트 + 게임 상태 도큐먼트 자동 생성
2. Google 로그인 → UID 연결 → 기존 익명 진행도가 연결 계정으로 이관
3. `save.json` 삭제 → 같은 계정으로 재로그인 → Firestore에서 상태 복원 (서버가 정본)
4. 오프라인 상태로 PlayMode → 로컬 캐시로 정상 동작 → 온라인 복귀 → 서버에 자동 푸시
5. 두 기기에서 동시 진행 → 더 늦은 저장(`updatedAtUnixMs` 기준)이 승리
6. 첫 Google/Apple 연동 시 닉네임 등록 → Firestore 프로필 도큐먼트에 반영

---

## Task H — 서버 로그인 + 유저 식별 등록

**Status:** 🔴 TODO
**선행:** Bundle 4 게이트 통과 + Bundle 5 사용자 사전작업 완료

### 🎯 목표
- Firebase Unity SDK 통합 (Auth 모듈)
- 게임 시작 시 익명 로그인을 자동 수행 — 모든 유저가 첫 프레임부터 서버 식별자를 갖도록
- 익명 계정을 Google / Apple 정식 계정으로 "linking"으로 업그레이드 가능
- 첫 서버 접속 시 `users/{uid}/profile/main` 경로에 **유저 프로필 도큐먼트** 생성 (게임 상태 도큐먼트와 분리) — 닉네임, 계정 타입, 등록 타임스탬프 같은 고유 유저 정보가 여기 저장됨
- Google/Apple 연동 시 닉네임을 1회 입력받거나 OAuth 프로필에서 자동 채움 → 프로필 도큐먼트에 기록

### 서버측 데이터 구조
```
users/{uid}                    ← 최상위 도큐먼트 (게임 상태, Task I)
users/{uid}/profile/main       ← 유저 식별 정보용 서브컬렉션 도큐먼트 (본 태스크)
    displayName: string
    accountType: "anonymous" | "google" | "apple"
    createdAtUnixMs: number
    lastLoginAtUnixMs: number
    locale: string             (선택)
```

> 프로필을 `users/{uid}` 최상위 필드가 아닌 서브컬렉션 도큐먼트에 두는 이유: 식별 메타데이터가 빈번히 변하는 게임 상태와 논리적으로 분리되고, Firestore 보안 규칙이 단순해지며, 프로필 읽기/쓰기와 게임 상태 동기화가 서로 다른 주기로 동작 가능.

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 첫 실행 → 익명 UID 발급, `users/{uid}/profile/main` 도큐먼트 자동 생성, `accountType="anonymous"`
- [ ] Google 로그인 → 계정 선택 → UID 연결, 프로필 도큐먼트가 `accountType="google"`로 업데이트, `displayName` 채워짐
- [ ] Apple 로그인 → 계정 선택 → UID 연결, `accountType="apple"`로 업데이트 (iOS only)
- [ ] 첫 정식 제공자 연동 시 닉네임 등록 UI 노출 (또는 익명 거부 시 첫 실행에서)
- [ ] 매 로그인 성공 시 `lastLoginAtUnixMs` 업데이트
- [ ] 회귀 테스트 5건 통과 (디바이스 또는 시뮬레이터)

### 📂 변경 파일

#### H-1. 패키지 도입
- Firebase Unity SDK 12.x 이상 (Auth 모듈 + Firestore 모듈)
- Apple Sign In Unity Plugin (iOS)
- Google Sign In Unity Plugin (iOS / Android)

> 정확한 패키지 이름/버전은 Firebase 콘솔 Unity 가이드 참고. 로그인 직후 프로필 도큐먼트를 즉시 쓰기 때문에 Task H 시점부터 Firestore 모듈 필요.

#### H-2. `Assets/Scripts/Auth/AuthService.cs` (신규)
```csharp
public class AuthService : MonoBehaviour
{
    public string CurrentUid { get; private set; }
    public AccountType CurrentAccountType { get; private set; }    // Anonymous / Google / Apple
    public event Action<string, AccountType> UserChanged;

    public Task<string> SignInAnonymouslyAsync();
    public Task<bool>   LinkWithGoogleAsync();
    public Task<bool>   LinkWithAppleAsync();
    public Task         SignOutAsync();
}

public enum AccountType { Anonymous, Google, Apple }
```

#### H-3. `Assets/Scripts/Auth/UserProfile.cs` (신규)
프로필 도큐먼트의 직렬화 모델.
```csharp
[FirestoreData]
public class UserProfile
{
    [FirestoreProperty] public string displayName;
    [FirestoreProperty] public string accountType;
    [FirestoreProperty] public long   createdAtUnixMs;
    [FirestoreProperty] public long   lastLoginAtUnixMs;
    [FirestoreProperty] public string locale;
}
```

#### H-4. `Assets/Scripts/Auth/UserProfileService.cs` (신규)
**책임:** Firestore의 `users/{uid}/profile/main` 도큐먼트 read/write.
```csharp
public class UserProfileService
{
    public Task<UserProfile> GetOrCreateAsync(string uid, AccountType type);
    public Task UpdateDisplayNameAsync(string uid, string displayName);
    public Task UpdateAccountTypeAsync(string uid, AccountType type);
    public Task TouchLastLoginAsync(string uid);
}
```

`GetOrCreateAsync`는 도큐먼트가 없으면 생성(첫 접속), 있으면 `lastLoginAtUnixMs` 갱신.

#### H-5. `Assets/Scripts/UI/LoginPanel.cs` (신규)
- 메인 화면 또는 계정 옵션에서 호출
- 버튼: **Google 로그인**, **Apple 로그인** (iOS만), **건너뛰기 / 게스트로 계속**
- AuthService 결과 및 에러 UI 반영
- Google/Apple 연동 성공 시 `displayName`이 비어있으면 닉네임 입력 필드를 1회 노출 후 `UserProfileService.UpdateDisplayNameAsync` 호출

#### H-6. `Assets/Scripts/UI/NicknameRegistrationPanel.cs` (신규)
- 첫 정식 계정 연동 후 트리거되는 모달 패널
- TMP_InputField 1개 + Submit 버튼
- trim, 길이 1~20 검증, 공백만은 거부, `UserProfileService.UpdateDisplayNameAsync` 호출

#### H-7. `Assets/Scripts/Core/GameManager.cs` (수정)
- `Awake()`에서: Firebase 초기화 → `AuthService.SignInAnonymouslyAsync()` → `UserProfileService.GetOrCreateAsync(uid, Anonymous)` → UID를 SaveBinder에 전달
- `AuthService.UserChanged` 구독 → 계정 타입 변경 시 `UserProfileService.UpdateAccountTypeAsync` 호출

#### H-8. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `AuthService`, `UserProfileService`

#### H-9. `MainScene` 구성
- LoginPanel + NicknameRegistrationPanel 캔버스 추가 (초기 비활성, 인증 흐름이 활성화)

### 🚫 건드리지 말 것
- 전투, 스테이지 로직
- 다른 HUD 위젯 (StageLabel, BossEntryButton, UpgradeDrawer 등)
- `SaveService` 핵심 (Task G가 정본; 본 태스크는 **프로필** 도큐먼트만 쓰고 게임 상태 도큐먼트는 다루지 않음)

### 🧪 검증
1. 컴파일 클린 (Firebase SDK 패키지 import + 설정 파일 배치 후)
2. 첫 실행 → 익명 UID 콘솔 출력; Firebase 콘솔에서 `users/{uid}/profile/main` 도큐먼트가 `accountType="anonymous"` + `createdAtUnixMs`와 함께 새로 생성됨
3. Google 로그인 → 계정 선택 → 성공 시 프로필 도큐먼트가 `accountType="google"`로 업데이트, displayName이 비어있었다면 닉네임 입력 모달 표시 → 입력값이 Firestore에 반영
4. Apple 로그인 (iOS 빌드) → `accountType="apple"`로 동일 흐름
5. 종료 후 재실행 → 같은 UID, 프로필 도큐먼트의 `lastLoginAtUnixMs`가 증가

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

## Task I — Firestore 서버 정본(canonical) 게임 상태

**Status:** 🔴 TODO
**선행:** H

### 🎯 목표
- 게임 상태를 Firestore `users/{uid}` 도큐먼트에 **정본(canonical)** 으로 영속화
- 로컬 `save.json`(Task G)은 **오프라인 캐시**로 격하
- 로그인 시 서버 도큐먼트를 pull; 존재하면 서버가 승리하여 로컬을 덮어쓴다. 없으면 로컬 캐시를 서버에 push (해당 기기 첫 접속 시)
- 의미 있는 상태 변경(골드, 업그레이드 구매, 스테이지 진행, 앱 pause/quit)마다 debounce 후 Firestore에 push
- 동시 온라인 두 기기 충돌: **`updatedAtUnixMs` 기준 newer-wins**
- 네트워크 실패 시: 로컬 캐시로 게임 진행 가능, 큐잉된 쓰기는 재연결 시 플러시

### 서버 정본 정합성 규칙
1. **로그인(매 실행) 시:** `users/{uid}` 도큐먼트 pull.
   - **remote 존재 + `remote.updatedAtUnixMs > local.updatedAtUnixMs`** → 로컬 캐시 덮어쓰기 + "RestoredFromServer" 피드백 발신
   - **remote 존재 + `remote.updatedAtUnixMs <= local.updatedAtUnixMs`** → 로컬을 push (서버보다 더 최근에 오프라인 진행한 경우)
   - **remote 없음** → 로컬을 push (첫 부트스트랩)
2. **플레이 중:** 모든 상태 변경 트리거가 Firestore에 push (debounce 5초). Firestore 오프라인 영속성이 짧은 끊김을 투명하게 처리.
3. **앱 pause / quit 시:** 보류 중인 쓰기 즉시 강제 플러시.
4. **계정 연동(익명 → Google/Apple) 시:** Firebase Auth의 `LinkWithCredentialAsync`가 UID를 보존하므로 도큐먼트 위치 불변. 데이터 마이그레이션 불필요.

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] PlayMode 골드 획득 → Firestore 콘솔의 `users/{uid}` 도큐먼트가 5초 이내 갱신
- [ ] 로컬 `save.json` 삭제 → 다음 실행 시 Firestore에서 전체 상태 pull
- [ ] 쓰기 중 강제 종료 → 다음 실행 시 마지막 성공 push 상태 유지
- [ ] Wi-Fi OFF → 정상 동작; Wi-Fi ON → 큐잉된 쓰기 자동 플러시
- [ ] 동일 Google 계정 두 기기 → 다음 동기화 시 더 늦은 저장이 승리
- [ ] 회귀 테스트 6건 통과

### 📂 변경 파일

#### I-1. 패키지 추가
- Firebase Firestore Unity 모듈 (Task H에서 프로필 도큐먼트용으로 이미 추가됐다면 모듈만 활성화)

#### I-2. `Assets/Scripts/Save/CloudSyncService.cs` (신규)
```csharp
public class CloudSyncService
{
    private FirebaseFirestore db;

    public Task              PushAsync(SaveData data);
    public Task<SaveData>    PullAsync(string uid);
    public Task              ResolveAndApply(SaveService localService, string uid);
    public Task              FlushPendingAsync();   // 큐잉된 쓰기 강제 플러시
}
```

`ResolveAndApply`는 위 정합성 규칙을 구현. 서버가 정본; 서버가 더 최신이면 로컬을 덮어씀.

#### I-3. `Assets/Scripts/Save/SyncCoordinator.cs` (신규)
**트리거:**
- `AuthService.UserChanged` → `ResolveAndApply()` 정확히 1회
- `wallet.GoldChanged` / `upgradeSystem.UpgradePurchased` / `stageManager.StateChanged` → debounce 5초 → `PushAsync(local)`
- `OnApplicationPause(true)` → `FlushPendingAsync()`
- `OnApplicationQuit` → `FlushPendingAsync()` (best-effort, 강제 종료 시 미완료 가능)
- 네트워크 온라인 복귀 → 큐잉된 push 플러시
- 구현자 메모: Firebase Firestore 내장 오프라인 영속성(`FirestoreSettings.PersistenceEnabled = true`) 사용 권장 — SDK가 끊긴 상태의 쓰기를 알아서 큐잉함. SyncCoordinator는 debounce + 라이프사이클 이벤트 시 플러시만 추가 책임.

#### I-4. `Assets/Scripts/Save/SaveData.cs` (Firestore 호환)
- 클래스에 `[FirestoreData]` 속성 추가
- 영속 필드별로 `[FirestoreProperty]` 추가
- 중첩 클래스 `PlayerStatsSnapshot`, `UpgradeLevelEntry`도 각각 `[FirestoreData]` + `[FirestoreProperty]` 부여

#### I-5. `Assets/Scripts/Save/SaveService.cs` (수정)
- Bundle 5 도입 후 로컬 파일은 **캐시**로 역할 변경. CloudSyncService가 서버 승리 정합 시 호출할 신규 메서드 `OverwriteFromServer(SaveData remote)` 추가; `CurrentData`를 원자적으로 교체하고 새 파일을 저장.

#### I-6. `Assets/Scripts/Core/GameManager.cs` (수정)
- `AuthService.SignInAnonymouslyAsync()` 성공 후 `SyncCoordinator.Start(uid)` 호출 — `ResolveAndApply` 시작 + 트리거 등록
- `AuthService.UserChanged` 시 `SyncCoordinator.OnUidChanged(newUid)` 호출 — 보통은 linking이 UID를 보존하지만 방어적으로 처리

#### I-7. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `CloudSyncService`, `SyncCoordinator`

### 🚫 건드리지 말 것
- 전투, 스테이지, UI 로직
- `SaveService` 핵심 파일 IO (신규 `OverwriteFromServer` 메서드만 허용; 기존 `Save` / `TryLoad` / `Reset` 시맨틱 변경 금지)
- `UserProfileService` (Task H 범위) — 프로필 도큐먼트와 게임 상태 도큐먼트 분리 유지

### 🧪 검증
1. 컴파일 클린
2. PlayMode 익명 로그인 → 골드 획득 → Firestore 콘솔에서 `users/{uid}` 5초 이내 갱신 확인
3. `save.json` 삭제 → 재실행 → Firestore에서 게임 상태 복원
4. PlayMode 중 Wi-Fi OFF → 업그레이드 → Wi-Fi ON → 10초 이내 Firestore 반영 확인
5. 동일 Google 계정 두 기기 동시 변경 → 다음 동기화 시 늦은 `updatedAtUnixMs`가 승리
6. 쓰기 중 Unity 강제 종료 → 재실행 → 마지막 성공 push 상태 무손실

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
| 2026-05-06 | Planner | Task B/C/D/E 검토 완료 → ✅ DONE 처리. Bundle 1·2 게이트 통과. 사용자 추가 요청(쿨다운 제거, 다중 몬스터 필드, 맵 확장 + 카메라 추종, 한글 폰트, 몬스터별 체력바)은 모두 작업 로그에 기록되어 검토자 승인. 신규 컴포넌트(`EnemyWanderController`, `EnemyHealthBarView`, `MobileCameraFitter` 추종) 도입 확인. |
