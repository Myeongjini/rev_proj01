# Wizard Grower — Task B~I 묶음 작업 초안 (Draft)

> ⚠️ **본 문서는 Planner의 임시 작업 파일이다.**
> Tasks.md는 현재 Task A 진행 중인 다른 에이전트가 사용 중. 본 문서는 검토 통과된 묶음 단위로 Tasks.md에 이관된다.
> **구현 에이전트는 본 문서를 참조하지 말 것.** 정식 작업 지시는 Tasks.md만이다.

---

## 0. 묶음 구조 개요

작업을 5개 **묶음(Bundle)** 으로 분할한다. 한 묶음 단위로 게이트 검토를 거치며, 게이트 통과 전엔 다음 묶음을 시작하지 않는다.

| Bundle | 포함 Task | 위임 단위 | 사용자 개입 필요 |
|--------|-----------|-----------|------------------|
| **Bundle 1** | A → B | 능력치 + 전투 정합성 | ❌ |
| **Bundle 2** | C → D → E | 챕터/스테이지 시스템 (데이터 + 흐름 + UI) | 씬 편집 검증 |
| **Bundle 3** | F | 업그레이드 UI 드로어 | 씬/프리팹 편집 검증 |
| **Bundle 4** | G | 로컬 저장 | ❌ |
| **Bundle 5** | H → I | Firebase Auth + Firestore 동기화 | ✅ Firebase·Apple 콘솔 사전 작업 |

**현재 진행 상황 (2026-05-06):**
- Bundle 1 / Task A: 🟢 IN PROGRESS — 다른 에이전트가 Tasks.md 보고 작업 중
- Bundle 1 / Task B: 본 문서에 초안만 존재. A 완료 검토 후 Tasks.md로 이관 예정
- Bundle 2~5: 본 문서에 초안만 존재

---

## 1. 묶음 게이트 규칙 (Tasks.md §0에 이관 시 반드시 추가)

각 묶음의 마지막 태스크가 ✅ DONE 처리되기 전까지 **다음 묶음의 어떤 태스크도 시작 금지**.

### 게이트 통과 조건 (검토자가 확인)
1. 묶음 내 모든 태스크의 Status가 ✅ DONE
2. `git status` 깨끗 (또는 의도된 미커밋 변경만)
3. Unity Console 컴파일 에러/경고 0
4. 묶음 종합 회귀 테스트 통과 (각 묶음 하단 명시)
5. 다음 묶음 첫 태스크의 "선행 가정"이 코드 베이스와 일치 (검토자 재확인)

### 자동 진행 금지 사항 (구현 에이전트에게)
- 다른 묶음의 태스크 임의 시작 금지
- 시그니처 충돌·기획 충돌 발견 시 **임의 결정 금지**, 반드시 작업 로그에 기록 후 검토자 답 대기
- "건드리지 말 것" 영역 임시 수정 절대 금지 (컴파일 깨지면 차라리 BLOCKED 처리)
- 매 태스크 종료 시 git commit 권장 (롤백 가능성 확보)

---

## Bundle 1 — 능력치 + 전투 정합성

**목표:** 데이터 모델 정비 + 기본 전투 흐름의 버그 제거. UI 손대지 않음.

### 포함 Task
- **Task A** — PlayerStats 능력치 필드 확장 (Tasks.md에 이미 존재)
- **Task B** — 전투 정합성 + 발사 속도 stat 반영 + 기존 버그 수정 (본 문서)

### Bundle 1 종합 회귀 테스트
1. 자동공격 정상 동작 (Auto ON, 1초 간격)
2. Auto OFF 토글 시 자동공격 즉시 정지
3. Fire 버튼 → 마나 소모 없이 발사, `manualAttackInterval` 쿨다운 적용
4. 인스펙터에서 PlayerStats 9필드 노출, EnemyBase armor 노출
5. armorPen vs armor 데미지 계산 정확

---

## Task B — 전투 정합성 + 발사 속도 stat 반영 + 기존 버그 수정

**Status:** 🔴 TODO (Bundle 1)
**선행:** A

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
- `TryFireNow()`에도 동일 가드 적용

> 주의: 현재 코드의 `movement.IsManualMoving` 체크는 유지. `AutoModeEnabled` 추가만.

#### B-2. `Assets/Scripts/Combat/ClickAttackController.cs`
- 필드 제거:
  - `manualManaCost` 삭제
  - `mana` 필드 삭제
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
- UI (`HUDController` 등)
- `StageManager`, `BossStageController`, `EnemySpawner`
- 씬, 프리팹
- `ActiveSkillController` (본 태스크 범위 밖)

### 🧪 검증
1. Unity Console 클린
2. PlayMode 진입 → Auto Toggle OFF → 5초 대기 → 적 HP 변화 없음
3. Auto Toggle ON 복구 → 1초 간격 자동 공격 재개
4. Fire 버튼 1회 클릭 → 마나바 변화 없음, 적 HP는 `manualAttackDamage`만큼 감소
5. Fire 버튼 빠르게 5회 클릭 → 첫 발만 즉시, 나머지는 `manualAttackInterval` 간격으로 1회씩

### 📝 작업 로그 (구현자 기록)
- (비어있음)

### 🔍 검토 노트 (검토자 기록)
- (비어있음)

---

## Bundle 2 — 챕터/스테이지 시스템

**목표:** "5킬마다 보스" → "챕터 8스테이지 + 보스방 도전" 구조로 재설계. 데이터 → 흐름 → UI 순서로 진행.

### 포함 Task
- **Task C** — ChapterDefinition / StageDefinition 데이터 모델
- **Task D** — StageManager 흐름 리팩터 (필드 ↔ 보스방)
- **Task E** — 보스 입장 버튼 + HUD 챕터/스테이지 표시

### Bundle 2 종합 회귀 테스트
1. 일반 몬스터 처치 → 자동 리스폰, 스테이지 진행 안 됨
2. 보스 입장 버튼 클릭 → 보스 등장, 20초 타이머 시작
3. 보스 클리어 → 다음 스테이지 필드로 자동 전환, HUD 라벨 갱신
4. 보스 시간 초과 → 필드 복귀, 페널티 없음
5. 8스테이지 보스 클리어 → 다음 챕터 1스테이지로 전환 (또는 마지막 챕터면 "All Cleared")
6. HUD 라벨 형식: "음산한 숲 1-3"

---

## Task C — ChapterDefinition / StageDefinition 데이터 모델

**Status:** 🔴 TODO (Bundle 2)
**선행:** Bundle 1 게이트 통과

### 🎯 목표
ScriptableObject 기반 챕터·스테이지 데이터 모델 정의 + 첫 챕터 "음산한 숲" 자산 1세트 작성. 흐름 변경(D)은 이 데이터 위에서 동작.

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] 새 자산 생성 메뉴 노출 (`Assets > Create > Wizard Grower > Chapter / Stage / Chapter Database`)
- [ ] "음산한 숲" 챕터 자산 1개 + 스테이지 8개 + ChapterDatabase 1개 생성
- [ ] 기존 `StageDefinition`(Serializable struct)이 새 ScriptableObject로 대체

### 📂 변경 파일

#### C-1. `Assets/Scripts/Stages/StageDefinition.cs` (전면 교체)
> 기존 Serializable 클래스를 `LegacyStageBalance.cs`로 rename 후, `StageDefinition.cs`를 ScriptableObject로 재작성.
> 또는 기존 파일에서 클래스 정의 자체를 ScriptableObject로 변경하고, 기존 호출부(StageManager의 `[SerializeField] private StageDefinition definition`)는 Task D에서 정리.

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

> ⚠️ **Task C 범위 처리:** 기존 `StageManager`가 참조하는 `definition.killsPerStage` 등 필드는 Task D에서 흐름과 함께 정리. C에서는 ScriptableObject 도입에 의해 컴파일이 깨질 수 있는데, 그 경우:
> - 기존 `StageDefinition` 클래스를 임시로 두 개로 분리 가능 (LegacyStageBalance 유지하면서 새 ScriptableObject 추가)
> - 또는 기존 파일을 백업 클래스명으로 두고 C에서는 컴파일만 통과시키는 최소 처리. **단, 이 결정 사항은 작업 로그에 명시 필수.**

#### C-2. `Assets/Scripts/Stages/ChapterDefinition.cs` (신규)
```csharp
using UnityEngine;

namespace WizardGrower.Stages
{
    [CreateAssetMenu(menuName = "Wizard Grower/Chapter Definition", fileName = "Chapter")]
    public class ChapterDefinition : ScriptableObject
    {
        [Header("Identification")]
        public int chapterNumber;        // 1, 2, 3...
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
경로: `Assets/Data/Chapters/`

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

bossTimeLimit는 모두 20초. fieldMonsterArmor / bossArmor는 모두 0으로 설정 (밸런싱은 추후).

### 🚫 건드리지 말 것
- `StageManager`, `BossStageController`, `EnemySpawner`, `EnemyScalingService` 코드 (Task D)
- 씬, 프리팹

### 🧪 검증
1. 컴파일 클린
2. `Project` 창 우클릭 → `Create > Wizard Grower > Chapter / Stage / Chapter Database` 메뉴 노출
3. `Assets/Data/Chapters/` 아래에 챕터 1 + 스테이지 8 + DB 1 자산 존재
4. ChapterDatabase 인스펙터에서 chapters[0]을 펼쳤을 때 stages 8개 모두 정상 직렬화

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## Task D — StageManager 흐름 리팩터 (필드 ↔ 보스방)

**Status:** 🔴 TODO (Bundle 2)
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
- 모드 전환: `EnterBossRoom()`, `ReturnToField()` (private)
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
                           BossStageController bossStageController, PlayerProgression progression) { ... }
    public bool EnterBossRoom() { ... }    // 외부 호출 (Task E의 보스 입장 버튼)
    private void OnEnemyKilled(EnemyBase) { ... }
    private void OnBossFailed() { ... }
    private void AdvanceToNextStage() { ... }
    private void SpawnFieldEnemy() { ... }
    private void SpawnBossEnemy() { ... }
    private void RaiseStateChanged() { ... }
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
  - 다음 챕터 없으면 Feedback("All Cleared") + 마지막 스테이지 유지
  - mode=Field, SpawnFieldEnemy
- `OnBossFailed`: mode=Field, SpawnFieldEnemy, Feedback("Boss Failed")
- 모든 상태 변경 후 `StateChanged` + `BossEntryAvailabilityChanged(CanEnterBoss)` 발신

**기존 `StageChanged(int, bool, int, int)` 이벤트는 폐기**. HUDController는 Task E에서 새 이벤트로 마이그레이션.

#### D-3. `Assets/Scripts/Enemies/EnemySpawner.cs` (수정)
- `SpawnNormal(float health, int reward, float armor)` — armor 인자 추가
- `SpawnBoss(float health, int reward, float armor)` — armor 인자 추가
- 기존 `Initialize(health, reward, 0f)` 호출에서 armor 전달 받도록 수정

#### D-4. `Assets/Scripts/Stages/BossStageController.cs`
점검만 — 변경 없음. `StartTimer/StopTimer` 그대로 사용.

#### D-5. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `[field: SerializeField] public ChapterDatabase ChapterDatabase { get; private set; }`

#### D-6. `Assets/Scripts/Core/GameManager.cs` (수정)
- `StageManager.Initialize(...)` 호출 시그니처 변경에 맞게 수정:
```csharp
context.StageManager.Initialize(context.ChapterDatabase, context.EnemySpawner, context.Wallet, context.BossStage, context.Progression);
```

#### D-7. `Assets/Scripts/UI/HUDController.cs` (최소 수정)
- 기존 `stageManager.StageChanged += OnStageChanged;` 라인을 **임시로 제거 또는 주석 처리** (이벤트 폐기됨)
- `OnStageChanged` 핸들러 메서드도 제거
- `stageLabel.text` 업데이트는 Task E에서 새 `StateChanged` 이벤트로 재연결
- **본 태스크에서 stageLabel은 일시적으로 비어 있을 수 있음** — 이는 D 끝에선 정상이고 E에서 채워짐

> ⚠️ HUDController는 원칙적으로 UI 위젯이라 수정 금지지만, 시그니처 변경에 따른 컴파일 유지를 위해 **이벤트 구독 제거 + 핸들러 제거** 두 가지만 허용. 그 외 변경 절대 금지.

#### D-8. `MainScene` (Unity Editor 작업)
- GameContext의 ChapterDatabase 필드에 `Assets/Data/Chapters/ChapterDatabase.asset` 할당

### 🚫 건드리지 말 것
- `PlayerStats`, `CombatCalculator`, `Projectile`
- 다른 UI 위젯 (HUDController는 위 D-7 범위만)
- 보스 입장 버튼 추가 (Task E)
- `EnemyScalingService` 삭제 — 유지 가능 (사용 안 해도 무방)

### 🧪 검증
1. 컴파일 클린
2. PlayMode 진입 → 챕터1 스테이지1 필드 시작 (HUD 라벨은 비어있어도 OK, Task E에서 채움)
3. 일반 몬스터 처치 5회 → 스테이지 자동 진행 안 됨, 매번 골드 획득
4. **(임시 검증 방법)** StageManager 인스펙터에 `[ContextMenu("Debug Enter Boss")]` 메서드 추가하거나 `EnterBossRoom()`을 public 메서드로 노출 → 호출 시 보스 등장 + 20초 타이머
5. 보스 처치 → currentStageNumber=2, 필드 모드 복귀, 새 일반 몬스터 등장
6. 다음 보스 도전 → 시간 초과 → 필드 복귀, currentStageNumber=2 유지

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## Task E — 보스 입장 버튼 + HUD 챕터/스테이지 표시

**Status:** 🔴 TODO (Bundle 2)
**선행:** D

### 🎯 목표
- HUD 라벨에 "음산한 숲 1-3" 형식 표시
- 화면 상단/우상단에 "보스 입장" 버튼 — 필드 모드에서만 활성
- 버튼 클릭 → `StageManager.EnterBossRoom()` 호출

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] HUD 라벨 형식: `{챕터명} {챕터번호}-{스테이지번호}` (필드), `... BOSS` (보스방)
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
- `Initialize` 내부에 이벤트 재구독 (Task D에서 제거된 부분 복구):
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
- 디버그용 `EnterBossRoom` 호출 코드 제거 (Task D에서 임시 추가했다면)

> ⚠️ MainScene 직접 편집. `Wizard Grower → Build Prototype Scene` 실행 금지.

### 🚫 건드리지 말 것
- 다른 HUD 위젯의 위치/이벤트
- StageManager 내부 (Task D 확정)
- 업그레이드 UI (Task F)

### 🧪 검증
1. 컴파일 클린
2. PlayMode 진입 → 라벨 "음산한 숲 1-1" 표시, 보스 입장 버튼 활성
3. 보스 입장 클릭 → 보스 등장, 라벨 "음산한 숲 1-1 BOSS", 버튼 비활성
4. 보스 처치 → 라벨 "음산한 숲 1-2" 갱신, 버튼 재활성

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## Bundle 3 — 업그레이드 UI 드로어

**목표:** 업그레이드를 화면 하단 드로어로 분리. 신규 능력치 모두에 대응하는 업그레이드 항목 제공.

### 포함 Task
- **Task F** — 업그레이드 드로어 UI

### Bundle 3 종합 회귀 테스트
1. 토글 버튼 하단 노출, 패널 접힘 상태로 시작
2. 토글 클릭 → 패널 슬라이드 업, 한 줄 2버튼 그리드, 세로 스크롤 가능
3. 9종 업그레이드 모두 클릭 가능, 골드 차감 + 능력치 변경
4. 토글 다시 클릭 → 패널 슬라이드 다운
5. 업그레이드 효과가 PlayMode 전투에 즉시 반영

---

## Task F — 업그레이드 드로어 UI (하단 토글, 2열 스크롤)

**Status:** 🔴 TODO (Bundle 3)
**선행:** Bundle 1 (A·B), Bundle 2 까지 게이트 통과 권장

> ⚠️ 사실상 Task F는 Bundle 2 없이 A·B만 완료돼도 진행 가능. 단, 검토 효율을 위해 Bundle 2 후에 진행 권장.

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
  - `Mana` → 기존 그대로

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
- 기존 `[SerializeField] private Sprite[] upgradeIcons;` 제거 (또는 동적 생성에서 활용)
- 신규 직렬화 필드:
```csharp
[SerializeField] private UpgradeDrawerView upgradeDrawer;
[SerializeField] private Transform upgradeButtonContainer;     // ScrollRect Content
[SerializeField] private UpgradeButtonView upgradeButtonPrefab;
[SerializeField] private Sprite[] upgradeIcons;                // 9개, 순서 = upgrades 순서
```
- `BindUpgradeButtons` 재작성: `system.Upgrades` 순회 → Instantiate(prefab, container) → Bind
- `RefreshUpgradeButtons`: 컨테이너의 모든 자식 view에 Refresh()

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

> ⚠️ Build Prototype Scene 실행 금지. 씬·프리팹 직접 편집.

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

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## Bundle 4 — 로컬 저장

**목표:** 게임 진행 상태를 디스크에 영속화.

### 포함 Task
- **Task G** — SaveData 모델 + 로컬 저장

### Bundle 4 종합 회귀 테스트
1. PlayMode → 골드 100 + 업그레이드 1회 → 종료 → 재진입 → 모두 복원
2. save.json 사람이 읽을 수 있는 형식
3. saveVersion 필드 존재
4. save.json 삭제 후 재진입 → 신규 게임 정상 시작
5. 챕터/스테이지 진행도 복원

---

## Task G — SaveData 모델 + 로컬 저장

**Status:** 🔴 TODO (Bundle 4)
**선행:** Bundle 1, Bundle 2 게이트 통과

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
            // 향후: if (data.saveVersion < 2) { ... }
            return data;
        }
    }
}
```

#### G-3. `Assets/Scripts/Save/SaveBinder.cs` (신규)
**책임:** SaveData ↔ 게임 시스템(`PlayerStats`, `CurrencyWallet`, `StageManager`, `UpgradeSystem`) 양방향 매핑

```csharp
public class SaveBinder : MonoBehaviour
{
    public void ApplyToGame(SaveData data, GameContext ctx);
    public SaveData CaptureFromGame(GameContext ctx);
    public void RegisterAutoSaveTriggers(GameContext ctx, SaveService service);
}
```

**자동 저장 트리거:**
- `wallet.GoldChanged` (debounce 1초 이상 권장)
- `upgradeSystem.UpgradePurchased`
- `stageManager.StateChanged`
- `OnApplicationPause(true)`, `OnApplicationQuit`

**적용 시 주의:**
- `PlayerStats`에 `Set/ApplySnapshot(snapshot)` 메서드 필요 → PlayerStats에 신규 메서드 1개 추가 (예외적 허용, 작업 로그에 명시)
- `StageManager`에 `LoadProgress(int chapter, int stage)` 메서드 필요 → 추가
- `UpgradeSystem`에 `LoadLevels(List<UpgradeLevelEntry>)` 메서드 필요 → 추가

#### G-4. `Assets/Scripts/Core/GameManager.cs` (수정)
- `Awake()` 도입부:
```csharp
context.SaveService.TryLoad();   // 실패해도 빈 SaveData로 진행
// ... 기존 Initialize 호출들 ...
context.SaveBinder.ApplyToGame(context.SaveService.CurrentData, context);
context.SaveBinder.RegisterAutoSaveTriggers(context, context.SaveService);
```
- `OnApplicationPause(bool paused)`, `OnApplicationQuit()` 콜백 추가:
```csharp
private void OnApplicationPause(bool paused) { if (paused) context.SaveService.Save(); }
private void OnApplicationQuit() { context.SaveService.Save(); }
```

#### G-5. `Assets/Scripts/Core/GameContext.cs` (수정)
- 필드 추가: `SaveService`, `SaveBinder`

#### G-6. `Assets/Scripts/Player/PlayerStats.cs` (예외적 수정 — 메서드 1개 추가)
- 신규 메서드 `public void ApplySnapshot(PlayerStatsSnapshot s)` — 직렬화 데이터로부터 필드 일괄 적용
- 신규 메서드 `public PlayerStatsSnapshot CaptureSnapshot()` — 현재 stat을 snapshot으로 추출

> ⚠️ Task A에서 "PlayerStats 손대지 말 것" 했지만, Task G는 영속화 기능이라 메서드 추가가 필수. **본 추가는 stat 변경이 아니라 IO 어댑터**이므로 허용. 작업 로그에 명시.

### 🚫 건드리지 말 것
- 게임 로직(전투, 보스, 스테이지 흐름) 자체
- UI 위젯 (HUDController는 변경 없음)

### 🧪 검증
1. 컴파일 클린
2. PlayMode → 골드 100 획득, 자동공격력 업그레이드 1회 → 정지 → 재진입 → 골드, 능력치 복원
3. `save.json` 텍스트 에디터로 열기, 사람이 읽을 수 있는 JSON 확인
4. saveVersion=1 필드 존재
5. save.json 삭제 → 재진입 → 신규 게임 정상 시작 (currentChapter=1, currentStage=1, gold=0)

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## Bundle 5 — Firebase Auth + Firestore 동기화

**목표:** 사용자별 클라우드 저장. 디바이스 간 동기화.

### 포함 Task
- **Task H** — Firebase Auth (익명 / Google / Apple)
- **Task I** — Firestore 클라우드 동기화

### ⚠️ Bundle 5 시작 전 사용자 사전 작업 (필수)

**Firebase 설정 (Task H 시작 전):**
- [ ] Firebase 콘솔 → 프로젝트 생성, Project ID 확보
- [ ] Firebase에 iOS 앱 등록 → `GoogleService-Info.plist` 다운로드 → `Assets/`에 배치
- [ ] Firebase에 Android 앱 등록 → `google-services.json` 다운로드 → `Assets/`에 배치
- [ ] Authentication → 익명 / Google / Apple 제공자 활성화
- [ ] Apple Developer ($99/년) → Sign in with Apple 활성화, Service ID 발급
- [ ] Google Cloud Console → OAuth 클라이언트 ID 발급 (iOS/Android/웹)
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

**위 사전 작업이 모두 완료되어 사용자가 명시적으로 "Bundle 5 진행"이라 지시하기 전까지 H/I 시작 금지.**

### Bundle 5 종합 회귀 테스트
1. 첫 실행 → 익명 로그인, UID 발급, Firestore 도큐먼트 자동 생성
2. Google 로그인 → UID 연결, 기존 진행도 유지
3. save.json 삭제 → 같은 계정으로 재로그인 → Firestore에서 복원
4. 오프라인에서 PlayMode → 게임 정상 동작 → 온라인 복귀 → 자동 동기화
5. 두 기기에서 진행 → 더 늦게 저장한 쪽 우선

---

## Task H — Firebase Auth (익명 / Google / Apple 로그인)

**Status:** 🔴 TODO (Bundle 5)
**선행:** Bundle 5 사용자 사전 작업 완료 + Bundle 4 게이트 통과 (G의 SaveData가 userId 사용)

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
- 정확한 버전·이름은 Firebase 콘솔의 Unity 가이드 따라 결정

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
- AuthService 호출 결과를 UI에 반영

#### H-4. `Assets/Scripts/Core/GameManager.cs` (수정)
- `Awake()`에서 Firebase 초기화 → `AuthService.SignInAnonymouslyAsync()` → UID를 SaveBinder에 전달
- AuthService를 GameContext에 등록

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

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## Task I — Firestore 클라우드 동기화

**Status:** 🔴 TODO (Bundle 5)
**선행:** H

### 🎯 목표
- 로컬 SaveData를 Firestore `users/{uid}` 도큐먼트에 동기화
- 오프라인 우선 (로컬이 진실, 백그라운드 동기화)
- 충돌 해결: `updatedAtUnixMs` 비교 → newer-wins
- 네트워크 실패 시 로컬 동작 유지

### ✅ Definition of Done
- [ ] Unity Console 클린
- [ ] PlayMode 골드 획득 → 5초 이내 Firestore에서 갱신 확인
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
- remote가 더 최신(updatedAtUnixMs) → local에 덮어쓰기 + 저장
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

### 📝 작업 로그 / 🔍 검토 노트
(생략)

---

## 부록 — Tasks.md 이관 절차

각 묶음을 Tasks.md로 이관할 때:

1. **선행 묶음 게이트 통과 확인** — 모든 태스크 ✅ DONE
2. **본 초안의 가정과 현재 코드 일치 확인** — 다른 에이전트 작업 중 발견된 변경사항 반영
3. Tasks.md §2 상태 보드에서 묶음 내 태스크 상태 갱신
4. 본 초안의 묶음 섹션 전체를 Tasks.md 본문에 복사
5. Tasks.md §0에 묶음 게이트 규칙이 없다면 §1 게이트 규칙 항목 이관 (1회만)
6. 본 초안의 해당 묶음 섹션 제목에 `[이관됨 YYYY-MM-DD]` 표시
7. Tasks.md 변경 이력에 기록

---

## 변경 이력

| 일자 | 변경자 | 내용 |
|------|--------|------|
| 2026-05-06 | Planner | Task B~I 초안 작성 |
| 2026-05-06 | Planner | 묶음 구조로 재편 (Bundle 1~5), 게이트 규칙 추가 |
