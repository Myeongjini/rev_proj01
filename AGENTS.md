# AGENTS.md — Agent Instructions for `rev_proj01` (Wizard Grower / 마법사 키우기)

> 코드베이스 컨벤션 + 작업 규칙 문서. 변경 빈도 낮음.
> 현재 진행 중인 Task와 장기 로드맵은 별도 문서를 본다 (§1 문서 계층).

---

## 1. 문서 계층 (Document Hierarchy)

```
[ Tasks_v[N].md ]   ← 현재 active iteration. Bundle 단위 4~7 Task. implementer가 직접 따름.
       ↑
       │ "다음 Bundle 무엇 만들까?" 결정 시 참고
       │
[ References.md ]   ← 장기 카탈로그 + Bundle 매핑 + 누락 체크용
       ↑
       │ "상용 게임은 보통 어떤 영역까지 만드나?" 확인 시 참고
       │
[ AGENTS.md ]       ← 코드베이스 컨벤션·작업 규칙. (이 문서)
```

- **`Tasks_v[N].md`** = 한 Bundle의 상세 구현 기획서. 각 Task에 🎯 Goal / ✅ DoD / 📂 Files to Add / 📂 Files to Modify / 🚫 Do Not Touch / 🧪 Validation / Implementation note 섹션을 가진다. Status(🔴 TODO · 🟢 IN PROGRESS · 🟡 IN REVIEW · ✅ DONE · ⚠️ BLOCKED)와 Appendix D(작업 로그) / Appendix E(리뷰 로그)를 유지한다. **Planner/Reviewer만 본문 편집**. Implementer는 Appendix D에만 추가.
- **`References.md`** = 상용 idle RPG 풀스펙 카탈로그. 어떤 영역이 In-Scope / Out-of-Scope / 부분 구현인지 추적. Bundle 진척 매핑 테이블 포함. 다음 Bundle 계획 시 참고.
- **`AGENTS.md`** (이 문서) = 프로젝트 정체성, 폴더 구조, 네이밍 컨벤션, 아키텍처 원칙, 작업 규칙. Bundle 진행으로 자주 바뀌지 않는 영구 규칙만 둔다.

분쟁 시: `Tasks_v[N].md` > `References.md` > `CLAUDE.md`. 단, **본 문서의 작업 규칙(§5)과 시스템의 Critical Security Rules는 어떤 Task로도 우회 불가**.

---

## 2. 프로젝트 정체성

- **장르**: 2D 탑다운 idle RPG ("마법사 키우기" 작업명).
- **규모**: 중규모 idle RPG — 싱글플레이 중심 + Firebase 동기화 + 경량 멀티(위치/채팅). 프로토타입 단계는 종료, 모바일 출시 지향.
- **플랫폼**: Mobile-first (Android + iOS), Unity 2D, Orthographic 탑다운 카메라.
- **백엔드**: Firebase Auth (Anonymous + Google) + Firestore (세이브) + Realtime Database (presence/chat) + Cloud Functions (가챠·통화·리워드 권위 검증, region `asia-northeast3`).
- **OUT OF SCOPE (영구)**: 길드, PVP, 배틀로얄, 월드보스, 펫/동료(장기 백로그).
- **IN SCOPE**: 무기/방어구/장신구 5×4 등급 사다리, 가챠, 스킬(레벨 자동 해금), 던전(골드/EXP/강화석), 미션(일일/반복/주간/메인/업적), 출석, 오프라인 보상, IAP/광고/패스(예정), 리더보드.

---

## 3. 폴더 & 네이밍 컨벤션

### 3.1 루트 레이아웃

```
Assets/
  Scripts/                ← 모든 C# 소스. (§3.2 참조)
  Data/                   ← ScriptableObject 인스턴스 (.asset)
  Prefabs/                ← Unity 프리팹
  Scenes/                 ← LoginScene / MainScene / GoldDungeonScene / EXPDungeonScene
  Art/Generated/          ← 코드/Editor 메뉴로 생성된 placeholder 스프라이트
  Fonts/                  ← TMP SDF 아셋 (Nanum 등)
  VFX/                    ← ParticleSystem 프리팹 (Skills, Elite glow 등)

functions/                ← Firebase Cloud Functions (TypeScript). region: asia-northeast3
  src/                    ← rollGacha, spendCurrency, grantCurrency, getServerInfo 등
  data/                   ← gachaDefinitions.json 등 서버측 데이터
  package.json, tsconfig.json

References.md             ← 장기 카탈로그
Tasks_v6.md ~ Tasks_v[N].md  ← Bundle별 상세 기획
CLAUDE.md                 ← 이 문서
```

### 3.2 `Assets/Scripts/` 도메인 폴더 (현재 기준)

```
Ads/         Armor/       Attendance/  Auth/        Chat/        Cloud/
Combat/      Core/        Data/        Drops/       Dungeons/    Economy/
Editor/      Enemies/     Login/       Missions/    Multiplayer/ Offline/
Player/      Save/        Skills/      Stages/      UI/          Upgrades/
Utilities/   Weapons/
```

- **하나의 시스템 = 하나의 폴더**. 새 시스템 추가 시 새 폴더를 만든다 (예: 펫 시스템 도입 시 `Pets/`).
- 각 폴더에 `MonoBehaviour` 서비스 + `ScriptableObject` 정의 + serializable POCO state가 함께 들어간다. UI 전용 클래스는 `UI/` 로 모은다.

### 3.3 네이밍

- **C# 네임스페이스**: `WizardGrower.<Domain>` (예: `WizardGrower.Skills`, `WizardGrower.Save`).
- **클래스 어휘 규칙**:
  - `<X>Definition` = ScriptableObject 데이터 정의 (예: `SkillDefinition`, `ArmorDefinition`).
  - `<X>Database` = `<X>Definition` 배열을 모은 ScriptableObject (`GetById`, `OrderedXxx` API).
  - `<X>State` = serializable POCO (Save에 직렬화되는 단일 상태 묶음).
  - `<X>Service` = 런타임 도메인 로직 (트랜잭션, 검증, 이벤트 발행). MonoBehaviour 또는 pure C#.
  - `<X>Controller` = 프레임/씬 수명 기반 동작 (입력, 스폰, UI 토글).
  - `<X>View` / `<X>Panel` / `<X>Modal` = UI 클래스. 상태 표시 + 버튼 이벤트 전달만, 게임 로직 금지.
  - `<X>Coordinator` = 다중 패널/시스템 상호배제 / 흐름 조율 (예: `MainUI01Coordinator`, `SecondaryPanelCoordinator`).
  - `<X>Bootstrap` = 씬 진입 시 초기 wiring 담당.
- **에셋 파일명**: PascalCase 또는 snake-case ID 일관 (예: `Helmet_Common_Beginner.asset`, `StandardAttendanceConfig.asset`).
- **인게임 한국어 문자열**은 코드에 그대로 둔다 (예: `"전투력"`, `"보스 입장"`, `"잠시만 기다려주세요"`). 향후 i18n 도입 전까지 한국어 우선.

---

## 4. 아키텍처 원칙

### 4.1 책임 분리

- **`GameContext`**: 모든 도메인 서비스의 컴포지션 루트. 모든 Service/Coordinator/UI 참조를 한 곳에서 보유.
- **`GameManager`**: 씬 단위 lifecycle 진입점. Auth 후 → SaveService 로드 → 도메인 서비스 Initialize → HUD wiring → popup queue 실행 순서로 부팅.
- **도메인 서비스**는 자기 책임 영역 외 데이터를 직접 만지지 않는다. 통화는 `CurrencyWallet`, 인벤토리는 `WeaponInventory`/`ArmorInventory`, 레벨은 `PlayerLevelService`, 미션 카운터는 `MissionService` 등.
- **UI 클래스**는 도메인 로직 계산 금지. 서비스의 이벤트 구독 + 버튼 클릭 → 서비스 메서드 호출만.
- 거대한 `MonoBehaviour` 한 개에 게임 로직을 몰지 않는다. pure C# 클래스 + serializable data + ScriptableObject로 쪼갠다.

### 4.2 이벤트 패턴

- 도메인 서비스는 `event Action<...>` 또는 `Action`을 노출 (예: `OnGoldChanged`, `OnEnemyKilled`, `OnLevelUp`, `OnSlotChanged`, `OnAutoModeChanged`).
- 구독 측은 `GameManager` / `GameContext` 또는 같은 도메인 내부에서 wiring.
- 한 시스템이 다른 시스템의 내부 상태를 polling 하지 않는다.
- 이벤트 이름/페이로드는 단순하게 유지.

### 4.3 세이브 아키텍처

- **로컬 권위**: `SaveData` (serializable POCO) ↔ `SaveService` (로드/저장/마이그레이션). 모든 영속 필드는 `SaveData`에 직접 필드로 둔다.
- **클라우드 mirror**: `SaveDataDocument` (Firestore-친화 형태) + `SaveDataMapper` (POCO↔Document 변환) + `CloudSyncService` (Firestore 라운드트립) + `SyncCoordinator` (충돌 정책).
- **wiring**: `SaveBinder`가 각 도메인 서비스의 capture/apply를 모아 SaveData에 직렬화.
- **마이그레이션 규칙**:
  - schema 변경 시 `SaveData.saveVersion`을 +1.
  - `SaveService.MigrateIfNeeded()`에 `if (data.saveVersion < N) { Migrate...ToVN(data); data.saveVersion = N; }` 분기 추가.
  - **버전을 건너뛰지 않는다**. 모든 마이그레이션은 additive (필드 삭제 금지, default-fill 우선).
  - 신규 필드 추가 시 SaveData / SaveDataDocument / SaveDataMapper / SaveBinder 4곳 모두 mirror.
- 서버 권위 데이터(wallet, transactions)는 별도. Bundle 12 이후 Cloud Functions가 권위.

### 4.4 Cloud Functions 권위 (Bundle 12+)

- 가챠 롤, 통화 변경, 보상 지급은 **Cloud Functions가 최종 결정**. 클라이언트는 호출 + 응답 반영만.
- 클라이언트 측 시뮬레이션 fallback은 `#if UNITY_EDITOR` 또는 명시 시뮬레이션 모드 플래그로 가드. **Live build에서는 절대 클라이언트가 가챠 결과 결정 금지**.
- `/users/{uid}/wallet` 및 `/users/{uid}/transactions` 컬렉션은 Firestore Security Rules로 클라이언트 write 금지. 클라이언트는 read-only listener.
- `grantCurrency` callable은 server-internal custom claim 검사로 일반 클라이언트 호출 거부. 보상은 reward-source별 server-side helper(`grantCurrencyInternal`)에서만 호출.
- 모든 통화 변경 path(가챠/미션/출석/던전/오프라인/업그레이드)는 단계적으로 server-grant 경유로 마이그레이션.

### 4.5 씬 구조

- **LoginScene** (build idx 0): Auth + `AuthBootstrapHolder` 생성. 성공 시 MainScene 전환.
- **MainScene** (build idx 1): 메인 게임 루프. HUD, 가챠, 인벤토리, 미션, 출석, 스킬, 전투.
- **GoldDungeonScene** (build idx 2) / **EXPDungeonScene** (build idx 3): 별도 던전 씬. `AuthBootstrapHolder` 통해 auth 상태 유지.
- `SceneTransfer` 패턴으로 결과(보상)를 다음 씬에 pending 모달로 표시 (e.g. 던전 종료 후 MainScene 진입 시 결과 모달 popup).

### 4.6 HUD / UI 영역 약속

- **상단**: stage, gold, gem, DPS, 보스 타이머.
- **메인 HUD**: 마나 바, 스킬 바 (1×5), 업그레이드 버튼, Fire 버튼, Active Skill 버튼.
- **우측 작은 아이콘 스택(Auto 토글 아래)**: 업적 → 출석 → 채팅 등. `SecondaryPanelCoordinator`로 상호배제 (한 번에 하나만 슬라이드업).
- **MainUI01Bar**: 강화 / 무기 / 소환 / 스킬 / 던전 / (예약 슬롯). `MainUI01Coordinator`로 상호배제 + 내부 X 버튼 + 같은 탭 재클릭 닫기.
- **버튼 클릭 vs 조이스틱 입력 분리**: 입력이 UI 위에서 시작되었으면 조이스틱으로 처리하지 않는다 (`EventSystem.current.IsPointerOverGameObject()` + 터치 overload 필터).
- 런타임 UI 생성 금지:
   - new GameObject(...) 로 UI 오브젝트 생성 금지
   - AddComponent<Image>() 금지
   - AddComponent<Button>() 금지
   - AddComponent<TextMeshProUGUI>() 금지
   - AddComponent<RectTransform>() 금지
   - CreateText / CreateButton / EnsureUi 패턴 금지
- 예외:
   - 동적 리스트, 인벤토리 슬롯, 가챠 결과 카드, 보상 아이템 등 반복 요소는 런타임 생성이 가능하다.
   - 단, 반드시 미리 만들어진 Prefab을 Instantiate(prefab, parent) 하는 방식만 허용한다.
   - 런타임에 UI 컴포넌트를 AddComponent로 조립하는 것은 금지한다.
- 기존 UI의 기능, 텍스트, 버튼 동작, 화면 전환 로직을 유지하라.
- 임의로 UI 디자인을 크게 변경하지 말 것. 기존 배치와 동작을 최대한 보존하되, Editor에서 수정 가능하도록 구조만 변경하라.

### 4.7 입력 / Auto 모드

- 조이스틱은 floating: 필드를 누른 위치에서 발생, 떼면 idle.
- `AutoModeController.CanAutoAct = AutoEnabled && !PlayerMovement.IsBeingControlledByJoystick`. 이 규칙만 충족할 때 auto move + auto attack 실행.
- 수동 Fire 버튼, 액티브 스킬, 스킬바 슬롯 manual cast는 Auto/조이스틱 상태와 무관. 자체 mana/cooldown만 따른다.

---

## 5. 작업 규칙 (Bundle Workflow)

### 5.1 Status Legend

| 마커 | 의미 |
|---|---|
| 🔴 TODO | 미착수 또는 review에서 reject되어 재작업 필요 |
| 🟢 IN PROGRESS | implementer가 현재 작업 중 |
| 🟡 IN REVIEW | implementer가 완료 처리 + Appendix D 기재. Planner 검토 대기 |
| ✅ DONE | Planner 검토 통과 + Appendix E 기재 |
| ⚠️ BLOCKED | 외부 의존성(사용자 prework, 라이브 배포 등)으로 진행 불가 |

### 5.2 Task 단위 작업 규칙 (implementer 측)

1. **한 번에 하나의 Task만**. 시작/종료 시 Task ID를 명시.
2. 시작 전 해당 Task 섹션 + Do Not Touch 영역을 정독.
3. Unity 프로젝트 경로 = `/Users/kmj/rev_proj01`. 네임스페이스 = `WizardGrower.*`.
4. 완료 시:
   - Unity Console: **0 errors / 0 warnings** (pre-existing 노이즈는 Appendix D에 명시).
   - PlayMode regression test 통과.
   - Status → `🟡 IN REVIEW`.
   - **Appendix D — Combined Work Log** 에 한 행 추가 (`YYYY-MM-DD | Task X | <요약>`).
5. **자기 자신을 ✅ DONE으로 승격하지 않는다**. 다음 Task로 자동 진행하지 않는다.
6. Spec/signature 충돌 시 단독 결정 금지 — Appendix D에 기록 후 Planner 대기.
7. Do Not Touch 영역을 임시로라도 수정 금지. 컴파일이 깨지면 `⚠️ BLOCKED` 처리.
8. 본 Task에 명시되지 않은 신규 시스템/파일을 도입하지 않는다 (컴파일에 필수일 때만 예외, Appendix D에 기록).
9. **Tasks_v[N].md 본문(Task 섹션, Status, Status Board)은 implementer가 편집하지 않는다.** 오직 Appendix D만 append.
10. Unity MCP가 본인과 연결되어 있는 점 숙지할 것. 연결되어 있지 않은 것이 확인될 경우, 사용자에게 확인 요청. Unity MCP를 활용하여 개발을 할 수 있도록 한다. Unity Editor 조작으로 해결 가능할 경우, 스크립트 작성이 아닌 Unity Editor와 MCP 연결을 통해 해결하도록 한다.
11. 개발 시 SOLID 원칙을 준수하여 개발을 진행하여야 한다.
12. 에이전트가 코드 수정만으로 해결할 수 없어 사용자에게 환경 설정/외부 작업 안내를 전달하며 종료한 경우, 같은 지시 묶음의 다음 Task를 진행하지 않는다. 사용자가 해당 외부 작업 완료를 명시한 뒤에만 다음 Task를 시작한다.

### 5.3 Git 규칙

- **Task 1개 = commit 1개**. 메시지 형식: `Task <ID> done: <one-line summary>`.
- 작업 시작 시 `git status` 로 working tree 상태 확인. 무관한 dirty 파일은 Appendix D에 노트만 남기고 unstaged.
- `--no-verify` / `--no-gpg-sign` 등 hook 우회 금지.
- **agents는 push 금지**. push는 사용자 책임.
- 롤백은 `git revert`. `git reset --hard` / `git push --force` / branch 삭제는 명시적 사용자 지시 없이 금지.

### 5.4 Planner / Reviewer 측 규칙

- `Tasks_v[N].md` 본문(Task 섹션, Status Board, Status 라인) 편집은 **Planner 전용**.
- 검토 후:
  - 통과 → Status `✅ DONE` + Appendix E 한 줄 (`YYYY-MM-DD | Task X | ✅ DONE. <검증 근거>`).
  - 불통과 → Status `🔴 TODO (Rejected YYYY-MM-DD)` + 해당 Task 본문에 `### 🔁 Reviewer Findings (YYYY-MM-DD) — 수정 요청 사항` 섹션 삽입 + Appendix E에 reject 사유.
- Bundle Release Gate는 모든 Task가 ✅ DONE이고 release regression 테스트 통과 시 발효.
- 새 Bundle 진입 시 `Tasks_v[N+1].md` 신설, 직전 Bundle의 baseline을 §0.5 Cross-Track Coordination에 명시.
- **편집 대상 파일은 항상 `/Users/kmj/rev_proj01/` 원본 경로**. worktree(`.claude/worktrees/...`) 내부의 Tasks 파일을 직접 수정하지 않는다.

### 5.5 Build / Editor 헬퍼

- `Wizard Grower → Build Prototype Scene` 메뉴는 **씬을 덮어쓴다**. 사용자 명시 허가 없이 실행 금지.
- `Editor/VisualAssetUpdater` 같은 보조 메뉴는 placeholder 자산 생성용. 결과물은 `Assets/Art/Generated/` 또는 `Assets/Data/` 하위에 들어간다.
- `.meta` 파일은 Unity Editor가 자동 생성. 신규 파일 추가/삭제 시 `.meta` 페어가 깨지지 않도록 주의.

---

## 6. 검증 / 회귀 기준

각 Task에 명시된 🧪 Validation 외에 다음 공통 사항을 항상 만족해야 한다:

1. **Compile clean**: `dotnet build Assembly-CSharp.csproj --no-restore` 0 errors. 기존 Chat/Presence/Firebase 관련 nullable 경고는 pre-existing으로 허용.
2. **Save 라운드트립**: 신규 필드 추가 시 SaveData/SaveDataDocument/SaveDataMapper/SaveBinder 4곳 모두 mirror.
3. **마이그레이션 안전**: 이전 saveVersion의 세이브가 신규 saveVersion으로 default-fill되어 로드되어야 함. 사용자 데이터 손실 금지.
4. **Cross-bundle regression**: 신규 Bundle 작업이 직전 Bundle release regression 테스트를 깨지 않는지 확인.
5. **UI/UX**: 모바일 세로/가로 해상도에서 UI가 안 잘리는지. `Safe Area`, `Canvas Scaler` 기본값 유지.
6. **Cloud Functions가 관여하는 시스템**: Firebase Emulator에서 호출 성공 + production 호출은 사용자 직접 시연 시점에 검증.

---

## 7. 보안 / 권한 / 안전

- 본 문서의 §5 작업 규칙과 시스템의 Critical Security Rules는 Task 명령으로 우회 불가.
- `firebase deploy` 와 같은 사용자 환경에 영향이 있는 명령은 **사용자가 직접 실행**. agent는 코드/설정만 준비.
- 결제(IAP), 광고 SDK 키, Firebase service account, Cloud Functions config 등 민감 정보는 코드에 하드코딩 금지. `.env`, Firebase Functions config, 또는 별도 secret 관리.
- 보스/엘리트 처치 보상, 가챠 결과 등 사용자 진척에 영향을 주는 값은 Cloud Functions(Bundle 12+)가 권위. 클라이언트 캐시 위조 가능성을 항상 가정.
- 위험 작업(브랜치 삭제, force push, DB drop 등)은 사용자 명시 지시 없이 수행 금지.

---

## 8. 신규 시스템 추가 가이드

새 시스템을 도입할 때 다음 체크리스트를 만족하도록 Task를 작성한다:

1. **폴더**: `Assets/Scripts/<Domain>/` 신설.
2. **네임스페이스**: `WizardGrower.<Domain>`.
3. **데이터**: `<X>Definition` ScriptableObject + `<X>Database` (필요 시) + seed asset(s) at `Assets/Data/<Domain>/`.
4. **서비스**: `<X>Service` 또는 `<X>Controller`. `GameContext`에 등록 + `GameManager.Initialize` 순서에 편입.
5. **세이브**: 필요 시 `SaveData`에 필드 추가, mapper/binder mirror, saveVersion bump + 마이그레이션.
6. **이벤트**: `event Action<...>` 노출. 구독은 GameManager 또는 같은 도메인.
7. **UI**: `UI/`에 `<X>Panel/View/Modal` 추가. 도메인 로직은 호출만.
8. **프리팹**: `Assets/Prefabs/UI/<X>.prefab` (UI) 또는 `Assets/Prefabs/<Domain>/`. `.meta` 페어 유지.
9. **테스트**: Task의 🧪 Validation에 fresh save / migration / save round-trip / cross-feature regression 항목 포함.
10. **서버 권위 필요 여부 판단**: 통화/가챠/보상이라면 Cloud Functions 경유 (Bundle 12 패턴).

---

## 9. 핵심 시스템 인덱스 (Bundle별 산출물 요약)

> 자세한 DoD는 `Tasks_v[N].md`, 미구현 영역은 `References.md`.

| 영역 | 핵심 클래스 | 도입 Bundle |
|---|---|---|
| 입력 / 자동전투 | `VirtualJoystick`, `InputService`, `PlayerMovement`, `PlayerAutoMovement`, `AutoModeController`, `AutoAttackController`, `ManualFireButtonController` | 1 |
| 전투 계산 | `IDamageable`, `DamageInfo`, `CombatCalculator`, `Projectile`, `ProjectileFactory`, `TargetingService` | 1 |
| 스테이지 / 보스 | `StageManager`, `BossStageController`, `EnemySpawner`, `EnemyScalingService` | 2 |
| 경제 / 업그레이드 | `CurrencyWallet`, `RewardService`, `UpgradeSystem` | 2 |
| 세이브 / 클라우드 | `SaveData`, `SaveService`, `SaveDataDocument`, `SaveDataMapper`, `SaveBinder`, `CloudSyncService`, `SyncCoordinator` | 3, 5 |
| Auth / Login | `AuthService`, `LoginScene`, `AuthBootstrapHolder` | 5, 6 |
| 멀티(경량) | RTDB presence(5Hz), stage-scoped `ChatService` | 6 |
| 무기 / 가챠 | `WeaponDefinition`, `WeaponInventory`, `WeaponFusionService`, `GachaService`, `SummonLevel`, `MainUI01Bar`, `MainUI01Coordinator` | 7 |
| 스킬 / 미션 / 출석 | `SkillDefinition`, `SkillDatabase`, `SkillCastOrchestrator`, `SkillBarView`, `MissionService`, `MissionResetService`, `AttendanceService`, `SecondaryPanelCoordinator` | 8 |
| 오프라인 보상 | `OfflineTimeTracker`, `OfflineRewardCalculator`, `OfflineRewardService`, `OfflineRewardModal`, `GameStartupPopupQueue`, `AdSimulationService` | 9 |
| 던전 | `GoldDungeonScene/Service/State`, `EXPDungeonScene/Service/State`, `GoldDungeonResultModal`, `EXPDungeonResultModal` | 10, 11 |
| 플레이어 레벨 / 스킬 자동해금 | `PlayerLevelService`, `LevelUpPopupView`, `PlayerExpBarView`, `SkillUnlockPopupView`, `SkillDefinition.unlockLevel` | 11 |
| 서버 권위 (CF) | `functions/src/{index,gacha,currency}.ts`, `CloudFunctionsClient` | 12 |
| 방어구 / 엘리트 / Defense | `ArmorDefinition`, `ArmorDatabase`, `ArmorInventory`, `ArmorFusionService`, `ArmorStatComposer`, `EliteMonsterController`, `EliteSpawnTracker`, `ArmorDropTable`, `PlayerStats.Defense`, `CombatPowerService` | 13 |

---

## 10. 보고 / 종료 시

implementer가 Task 완료 보고 시 Appendix D에 다음을 포함:

- 추가/수정된 파일 (또는 의도적으로 미수정한 파일).
- 사용한 검증 수단 (PlayMode / batchmode / `dotnet build` / Unity MCP / Firebase Emulator).
- 회귀 결과 요약.
- 의도된 spec 외 변경 (있다면 사유).
- 무관한 dirty 파일 노트.

Planner가 Bundle 종료 시 Appendix E + Bundle Release Gate 점검 결과를 정리하고, 필요 시 References.md 진척 매핑을 갱신.

---

## 11. 도움말 / 피드백

- 사용자가 `/help`를 요청하면 Claude Code CLI 도움말을 안내.
- 버그/피드백은 https://github.com/anthropics/claude-code/issues 로 보고.
