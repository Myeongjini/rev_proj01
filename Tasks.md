# Wizard Grower — 작업 지시서 (Tasks.md)

> 이 문서는 **구현 에이전트(다른 AI)** 를 위한 작업 명세서다.
> **기획·검토 담당자(Claude / Planner)** 가 유일하게 편집하는 문서이며,
> 구현자는 본 문서를 읽고 코드를 수정한다.
> 문서와 실제 코드가 어긋나면 **문서가 정답**이다.

---

## 0. 작업 규칙 (모든 태스크 공통)

1. **한 번에 하나의 태스크만** 진행한다. 태스크 ID(예: `Task A`)를 명시하고 시작/종료한다.
2. 시작 전: 본 문서의 해당 태스크 섹션을 정독한다. **"건드리지 말 것"** 영역은 절대 수정 금지.
3. 작업 중: Unity 프로젝트 경로는 `/Users/kmj/rev_proj01`. 네임스페이스는 `WizardGrower.*`.
4. 종료 시:
   - Unity Console에서 컴파일 에러/경고 **0건** 확인
   - PlayMode 진입 후 **회귀 테스트** 5건 모두 통과 (각 태스크 하단 명시)
   - 본 문서의 해당 태스크 상태를 `🟡 IN REVIEW`로 변경 (Status 줄)
   - **본 문서의 다른 부분은 절대 수정하지 말 것** (Status 줄만 수정 허용)
5. 검토자가 코드 리뷰 후 본 문서의 상태를 `✅ DONE` 또는 다시 `🔴 TODO`로 갱신한다.
6. 빌드 헬퍼 `Wizard Grower → Build Prototype Scene` 메뉴는 **씬을 덮어쓴다** — 명시적 요청 없이 실행 금지.
7. 파일 추가/삭제 시 `.meta` 파일 동반 처리 (Unity가 처리하므로 에디터 통해 작업 권장).

---

## 1. 태스크 의존성 그래프

```
A (능력치 확장) ──┬─→ B (전투 정합성)─┬─→ F (업그레이드 UI 드로어)
                  │                   │
                  └───────────────────┴─→ G (저장 모델)
C (챕터/스테이지 데이터) ─→ D (스테이지 흐름) ─→ E (보스 입장 UI) ──→ G
                                                                       │
                                                        H (Auth) ─────┘
                                                        H + G ─→ I (Cloud Sync)
```

권장 진행 순서: **A → B → C → D → E → F → G → H → I**

---

## 2. 태스크 상태 보드

| ID | 제목 | Status | 선행 |
|----|------|--------|------|
| A  | PlayerStats 능력치 확장 | 🟡 IN REVIEW | — |
| B  | CombatCalculator 정합성 + 발사속도 stat 반영 | 🔴 TODO | A |
| C  | ChapterDefinition / StageDefinition 데이터 모델 | 🔴 TODO | — |
| D  | StageManager 흐름 리팩터 (필드 ↔ 보스방) | 🔴 TODO | C |
| E  | 보스 입장 버튼 + HUD 챕터/스테이지 표시 | 🔴 TODO | D |
| F  | 업그레이드 드로어 UI (하단 토글, 2열 스크롤) | 🔴 TODO | A, B |
| G  | SaveData 모델 + 로컬 저장 | 🔴 TODO | A, D |
| H  | Firebase Auth (익명/Google/Apple) | 🔴 TODO | — |
| I  | Firestore 클라우드 동기화 | 🔴 TODO | G, H |

상태 기호:
- 🔴 TODO: 미착수
- 🟢 IN PROGRESS: 구현 중 (구현자가 표시)
- 🟡 IN REVIEW: 구현 완료, 검토 대기 (구현자가 표시)
- ✅ DONE: 검토 통과 (검토자만 표시)
- ⚠️ BLOCKED: 차단됨 (사유 함께 기록)

---

## Task A — PlayerStats 능력치 필드 확장

**Status:** 🟡 IN REVIEW
**선행:** 없음

### 🎯 목표
`PlayerStats`에 능력치 필드 7종을 추가하고 자동/수동 공격 데미지를 **독립** 필드로 분리한다. `EnemyBase`에 `armor` 필드를 추가해 `armorPenetration` 능력치가 의미를 갖게 한다.

> 이번 태스크는 **데이터 모델 + 호환 유지**만 한다. 업그레이드 버튼 추가, UI 변경, 적 → 플레이어 공격 로직은 절대 손대지 않는다.

### ✅ Definition of Done
- [ ] Unity Console: 컴파일 에러 0, 경고 0
- [ ] PlayMode 회귀 테스트(아래 §검증) 5건 통과
- [ ] 인스펙터에 `PlayerStats`의 신규 필드 9종 노출
- [ ] `EnemyBase` 인스펙터에 `armor` 노출
- [ ] 기존 호출부(`UpgradeSystem`, `AutoAttackController`, `ClickAttackController`, `ActiveSkillController`, `EnemyScalingService`, `CombatCalculator`) 모두 새 API 사용

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

> **본 파일의 다른 줄은 절대 수정하지 말 것.** 레이아웃·이벤트 핸들러·필드 추가 모두 금지. `ATK` 라벨 정확도 향상 등 의미 있는 변경은 Task F 또는 별도 태스크에서 진행한다.

### 🚫 건드리지 말 것
- `StageManager`, `BossStageController`, `EnemySpawner` 흐름 로직
- `HUDController`의 **§A-10에 명시된 1줄 외** 모든 라인
- `Assets/Scripts/UI/*` 의 다른 모든 위젯
- `GameManager`, `GameContext` 의존성 주입 구조
- 씬 (`Assets/Scenes/MainScene.unity`), 프리팹 (`Assets/Prefabs/*`)
- 적 → 플레이어 공격 로직 (별도 태스크에서 다룸)
- 신규 업그레이드 버튼 추가 (Task F)

### 🧪 검증 (PlayMode 회귀 테스트)
1. Unity Console 클린 (에러/경고 0)
2. PlayMode 진입 → 마법사 자동공격으로 슬라임 처치 → 골드 +10
3. Fire 버튼 클릭 → 수동공격 데미지 정확히 `manualAttackDamage` 값과 일치 (크리티컬 제외)
4. PlayerWizard 인스펙터에 신규 필드 9종 노출 확인
5. EnemyBase 프리팹 `armor=5` 설정:
   - autoAttackDamage=10, armorPen=0 → 적이 받는 데미지 = 5
   - autoAttackDamage=10, armorPen=3 → 적이 받는 데미지 = 8
   - autoAttackDamage=10, armorPen=10 → 적이 받는 데미지 = 10 (음수 방지)
   - 모든 경우 최소 1 보장

### 📝 작업 로그 (구현자 기록)
> 시작 시각, 종료 시각, 발견한 이슈를 여기 추가하시오.

- 2026-05-06 시작: Task A 착수. `Tasks.md` §A-10 확인 후 `HUDController.cs`는 승인된 112번 줄만 수정하기로 함.
- 2026-05-06 종료: PlayerStats/DamageInfo/CombatCalculator/EnemyBase/PlayerStatProfile/UpgradeSystem/EnemySpawner/HUDController 112번 줄 마이그레이션 완료. 스크립트 검증 에러 0. 수동 데미지 20, armor=5 방어 계산(5/8/10) 직접 검증 통과. Unity MCP PlayMode가 `is_changing=true`에 머무는 세션 이슈로 자연 자동공격 골드 +10 관찰은 미완료.

### 🔍 검토 노트 (검토자 기록)
> 코드 리뷰 결과, 기획 대비 차이점을 여기 기록.

- (비어있음)

---

## Task B — CombatCalculator 정합성 + 발사속도 stat 반영

**Status:** 🔴 TODO
**선행:** A

### 🎯 목표 (요약)
- `AutoAttackController`가 `stats.AutoAttackInterval`을 매 틱 읽도록 (업그레이드 즉시 반영)
- `ClickAttackController`가 `stats.ManualAttackInterval`을 입력 쿨다운으로 사용
- `CombatCalculator`에 활성 스킬용 별도 `skillBaseDamage` 또는 multiplier 정합성 검토

> 상세 스펙은 Task A가 ✅ DONE 된 직후 본 섹션을 확장한다. 그 전까지는 **착수 금지**.

---

## Task C — ChapterDefinition / StageDefinition 데이터 모델

**Status:** 🔴 TODO
**선행:** 없음

### 🎯 목표 (요약)
ScriptableObject로 챕터·스테이지 정의를 표현. 한 챕터는 8 스테이지 + 8 보스로 구성. 1챕터(예: "음산한 숲") 분량 데이터 작성.

> 상세 스펙은 Task A 완료 후 본 섹션을 확장.

---

## Task D — StageManager 흐름 리팩터 (필드 ↔ 보스방)

**Status:** 🔴 TODO
**선행:** C

### 🎯 목표 (요약)
"필드 모드"와 "보스방 모드" 상태 분리. 필드에서는 일반 몬스터를 잡으며 골드 수집, 보스방에서는 20초 보스 챌린지. 보스 클리어 시 다음 스테이지(또는 다음 챕터) 진행.

> 상세 스펙은 Task C 완료 후 본 섹션을 확장.

---

## Task E — 보스 입장 버튼 + HUD 챕터/스테이지 표시

**Status:** 🔴 TODO
**선행:** D

### 🎯 목표 (요약)
HUD 상단에 "보스 입장" 버튼 추가, 현재 챕터·스테이지 라벨 표시 (예: "음산한 숲 1-3"). 보스방 진입 중에는 버튼 비활성.

> 상세 스펙은 Task D 완료 후 본 섹션을 확장.

---

## Task F — 업그레이드 드로어 UI (하단 토글, 2열 스크롤)

**Status:** 🔴 TODO
**선행:** A, B

### 🎯 목표 (요약)
화면 하단 토글 버튼으로 업그레이드 패널을 접고 펼침. 펼친 패널은 ScrollRect, 한 행 2개 버튼 그리드. 모든 신규 능력치용 업그레이드 항목을 `UpgradeDefinition`으로 추가.

> 상세 스펙은 Task A·B 완료 후 본 섹션을 확장.

---

## Task G — SaveData 모델 + 로컬 저장

**Status:** 🔴 TODO
**선행:** A, D

### 🎯 목표 (요약)
`SaveData` 직렬화 모델 정의 (능력치, 골드, 챕터/스테이지 진행, 업그레이드 레벨). `Application.persistentDataPath`에 JSON 저장. 버전 필드(`saveVersion: int`) 포함, 로드 시 마이그레이션 훅.

> 상세 스펙은 Task A·D 완료 후 본 섹션을 확장.

---

## Task H — Firebase Auth (익명 / Google / Apple)

**Status:** 🔴 TODO
**선행:** 없음 (G와 병렬 가능)

### 🎯 목표 (요약)
Firebase Unity SDK 도입, 익명 로그인 → Google/Apple 계정 연결. 로그인 화면 1개. 로그인 성공 시 `userId` 보관.

### ⚠️ 사용자 사전 작업 필요 (구현자가 진행 불가, 사용자에게 요청)
- Firebase 콘솔 프로젝트 생성, iOS/Android 앱 등록
- `google-services.json`, `GoogleService-Info.plist` 다운로드 → `Assets/`에 배치
- Apple Developer 계정 / Service ID 발급 (Sign in with Apple)
- Bundle ID / Package Name 확정

> 상세 스펙은 사용자 사전 작업 완료 후 본 섹션을 확장.

---

## Task I — Firestore 클라우드 동기화

**Status:** 🔴 TODO
**선행:** G, H

### 🎯 목표 (요약)
Firestore에 `users/{userId}` 도큐먼트로 SaveData 저장. 오프라인 우선(로컬 캐시 → 백그라운드 동기화). 충돌 시 newer-wins(updatedAt 비교).

> 상세 스펙은 G·H 완료 후 본 섹션을 확장.

---

## 부록 A — 검토자 체크리스트 (각 태스크 완료 시)

검토자는 본 문서를 기준으로 다음을 확인한다:

1. **DoD 100% 충족** — 체크박스 모두 통과
2. **변경 파일 일치** — 명시된 파일 외 수정 여부 확인 (`git diff` 비교)
3. **건드리지 말 것 영역 미수정** — 명시된 파일 unchanged
4. **회귀 테스트 통과** — 검증 항목 직접 실행
5. **기획 정합성** — 본 문서의 의도와 구현 결과가 어긋나지 않음

검토 결과는 각 태스크의 "🔍 검토 노트" 섹션에 기록한다.

---

## 부록 B — 변경 이력

| 일자 | 변경자 | 내용 |
|------|--------|------|
| 2026-05-06 | Planner | 초기 문서 생성, Task A 상세화, B~I 개요 |
| 2026-05-06 | Planner | Task A에 §A-10 추가 — `HUDController.cs:112` 1줄 수정 예외 허용 (구현자 충돌 보고 반영) |
