# Wizard Grower — v6 Design Document

> Audience: 기획자(사용자) / 검토자
> Implementation guide for agents lives in `Tasks_v6.md` (parallel work track to `Tasks.md`).
> 작성일: 2026-05-08

---

## 1. Overview & Motivation

Bundle 1~5는 단일 플레이어 방치형 RPG의 기본 골격(전투 / 스테이지 / 저장 / 인증)을 완성했다. v6는 **사용자 경험을 한 단계 끌어올리고**, **수집형 모바일 RPG의 메타 게임 사이클**을 도입하는 단계다.

도입할 5가지 기능:

1. **로그인 화면 폴리시** — 첫 인상 개선
2. **같은 스테이지 플레이어 위치 공유** — 가벼운 멀티플레이어 감각
3. **월드/스테이지 채팅** — 커뮤니티 형성
4. **무기 장착 시스템** — 능력치 외 수집 욕구
5. **무기 뽑기(가챠) 시스템** — 메타 진행 루프 + 매출 모델 기반

전체적으로 **Bundle 5(Firebase 인증/Firestore)** 위에 얹혀 동작하며, 멀티플레이어/채팅용으로 **Firebase Realtime Database**가 추가된다.

---

## 2. Architecture Decisions

### 2.1 Storage 분리

| 데이터 | 저장소 | 이유 |
|---|---|---|
| 게임 상태 (gold, stage, stats) | **Firestore** `users/{uid}` | 기존 (Task I 정본 모델) |
| 유저 프로필 (displayName, accountType) | **Firestore** `users/{uid}/profile/main` | 기존 (Task H) |
| 위치 공유 (5–10 Hz 쓰기) | **Firebase Realtime Database** `presence/{stage}/{uid}` | 고빈도 쓰기 + 자동 disconnect 정리 |
| 채팅 메시지 | **Firebase Realtime Database** `chat/world`, `chat/stage/{stage}` | 푸시 구독 + `limitToLast()` |
| 무기 인벤토리 / 가챠 결과 | **Firestore** (SaveData 확장) | 저빈도 쓰기, 정본 사이클 |

→ Firestore와 RTDB를 병행 사용. 각각의 강점에 맞게 분리.

### 2.2 사용자 결정 (2026-05-08 확정)

| # | 항목 | 선택 |
|---|---|---|
| 1 | 로그인 화면 구성 | **별도 LoginScene** (Build Settings index 0) |
| 2 | 멀티플레이어 백엔드 | **Firebase Realtime Database** |
| 3 | 가챠 통화 | **별도 Gem 통화 추가** (gold와 분리) |
| 4 | 무기 비주얼 | **플레이어 스프라이트도 함께 스왑** (런타임 tint + glyph overlay) |

### 2.3 Save 스키마 변경

`SaveData.saveVersion`을 1 → 2로 증가. 신규 필드:
- `string equippedWeaponId` (default: `"wand_starter"`)
- `List<string> ownedWeaponIds` (default: `["wand_starter"]`)
- `int gems` (default: 300 — 첫 실행 보상)
- `int pityCounter` (default: 0)

`SaveService.MigrateIfNeeded`가 v1 → v2 자동 마이그레이션. 기존 유저 진행도 보존.

---

## 3. Feature 1 — 로그인 화면 폴리시

### Why
현재는 MainScene이 즉시 시작하면서 인증이 백그라운드에서 비동기로 실행된다. 첫 인상이 약하고, 익명 UID 발급 전에 게임 상태가 잠시 일관성 없이 보일 수 있다. 정식 출시 게임처럼 **스플래시 → 로그인 → 게임 시작** 흐름을 도입한다.

### What
- 부팅 시 **LoginScene**이 먼저 로드됨 (Build index 0)
- 로고 + 로딩 인디케이터가 0.8초 이상 표시 (빠른 기기에서도 깜빡임 방지)
- 익명 로그인이 백그라운드에서 즉시 시작
- 스플래시가 페이드아웃되면 로그인 패널 등장:
  - **"게스트로 시작"** (익명 진행)
  - **"Google로 계속하기"** (정식 계정 연동)
- Google 첫 연동 시 닉네임 입력 모달 1회
- 로그인 + 프로필 준비 완료 → MainScene 비동기 로드 → HUD 표시

### How
- `LoginBootstrap` MonoBehaviour가 LoginScene 진입점
- Firebase 초기화 / `AuthService.SignInAnonymouslyAsync` / `UserProfileService.GetOrCreateAsync`를 LoginBootstrap이 담당
- `AuthBootstrapHolder` (DontDestroyOnLoad 싱글톤)가 인증 결과(UID, AccountType, Profile)를 MainScene으로 운반
- `MainScene.GameManager.Awake()`는 Firebase 재초기화 없이 `AuthBootstrapHolder.Instance`를 소비

### Out of scope
- 이메일/비밀번호 로그인 (Apple과 함께 차후)
- 로그아웃 흐름 (현재 → 게스트 모드 회귀)
- 캐릭터 생성 / 외형 선택

---

## 4. Feature 2 — 같은 스테이지 플레이어 위치 공유

### Why
같은 챕터·스테이지에 있는 다른 플레이어가 보이면 "혼자 하는 게임이 아니다"라는 감각이 생긴다. 협동/경쟁 메커닉 없이도 **사회적 존재감**이 게임 체류 시간을 늘린다.

### What
- 같은 `(chapterNumber, stageNumber)`에 접속한 다른 유저들의 마법사가 화면에 반투명 고스트로 보임
- 위치는 5–10 Hz로 갱신 (부드럽지만 트래픽 절약)
- 닉네임 라벨 표시
- 충돌 / AI / 공격 등 인터랙션은 **없음** (순수 시각 표시)
- 스테이지 이동 시 자동 갱신 (이전 스테이지 유저 사라지고, 새 스테이지 유저 등장)
- 앱 종료 / 크래시 시 자동 정리 (Firebase `OnDisconnect` 이용)

### How
**RTDB 데이터 구조:**
```
presence/{chapterId}_{stageNumber}/{uid}
  x: float
  y: float
  displayName: string
  lastUpdateUnixMs: long
```

- `PresenceCoordinator`가 클라이언트 측에서 100~200ms 주기 throttle
- `PresenceService`가 RTDB 읽기/쓰기 담당
- 스테이지의 모든 entry를 listen하다가 비-자기 UID가 나오면 `RemotePlayerView` (RemoteWizard.prefab) instantiate
- 위치는 받은 값으로 직접 set하지 않고 `Vector2.Lerp` (~200ms)로 부드럽게 보간
- 30초 이상 갱신 안 된 entry는 ghost 처리 (silent crash 대비)

### Out of scope
- 채팅 풍선 / 이모지 / 인사
- 같은 보스방 입장 (보스방은 1인 인스턴스)
- PVP / 공격 / 거래

---

## 5. Feature 3 — 월드/스테이지 채팅

### Why
방치형 게임의 retention은 커뮤니티에서 나온다. 같은 스테이지에서 마주친 사람과 가볍게 인사하거나, 월드 채팅으로 정보 공유 / 밈을 즐기는 경험을 제공한다.

### What
- HUD 좌측 하단에 채팅 토글 버튼
- 채팅 패널: **월드** / **스테이지** 탭 전환
- 메시지 길이 1~200자, 단일 줄
- 보내기 throttle: 2초당 1회 (스팸 방지)
- 최근 50개 메시지만 표시 (`limitToLast(50)`)
- 보낸 사람: 닉네임 + 메시지

### How
**RTDB 데이터 구조:**
```
chat/world/{pushId}
  uid, displayName, text, ts
chat/stage/{chapterId}_{stageNumber}/{pushId}
  uid, displayName, text, ts
```
- 메시지 작성 시 `Push().SetValueAsync()` (자동 시간순 키)
- 읽기는 `OrderByKey().LimitToLast(50).OnChildAdded`로 실시간 tailing
- 클라이언트는 새 메시지가 들어오면 패널 하단에 추가 + auto-scroll

**보안 규칙(요약):**
- 인증된 유저만 R/W
- 메시지의 `uid`는 본인의 `auth.uid`와 일치해야 함 (스푸핑 방지)
- `text` 길이 ≤ 200 자 (서버 측 enforce)

### Out of scope
- 욕설/스팸 필터 (프로토타입)
- 1:1 귓속말 / 친구 / 차단
- 멀티미디어 (이모지/이미지)
- 메시지 기록 영구 보관 (오래된 것은 클라이언트에서 안 보일 뿐, 서버에 누적됨 — 차후 Cloud Functions로 TTL 정리 권장)

---

## 6. Feature 4 — 무기 장착 시스템

### Why
플레이어 능력치가 업그레이드 골드 투자 곡선만으로 결정되면 **수집 욕구**가 약하다. 무기는:
- 시각적 차별화 (빛깔/장식 변화)
- 능력치 점프 (희귀도 단계로 큰 차이)
- 가챠/메타 진행의 목표

### What
- 1개 무기를 동시 장착 (멀티 슬롯은 차후)
- 무기는 5단계 희귀도: Common / Uncommon / Rare / Epic / Legendary
- 인벤토리 패널: 보유 무기를 grid로 표시, 클릭 시 장착
- 장착 시:
  - 마법사 스프라이트가 무기 색으로 tint됨 (예: 화염 무기 → 붉은 톤)
  - 손에 액세서리 글리프 표시 (오브 / 보석 / 룬)
  - 자동공격 발사체 스프라이트도 무기 전용으로 교체
- 능력치 보너스:
  - autoAttackDamage / manualAttackDamage / autoFireRateBonus / criticalChance / criticalMultiplier / armorPenetration / maxHealth (가산)
- 첫 실행 시 자동으로 `wand_starter` (Common) 1개 지급 + 장착

### How
**`WeaponDefinition` (ScriptableObject):**
```csharp
public class WeaponDefinition : ScriptableObject {
    string weaponId;           // "wand_starter"
    string displayName;        // "초보의 지팡이"
    Rarity rarity;             // Common
    Sprite icon;               // 인벤토리 아이콘
    Color tintColor;           // 마법사 tint 색
    Sprite accessoryGlyph;     // 손 글리프
    Sprite projectileSprite;   // 자동공격 발사체
    WeaponStats statBonuses;
    string flavorText;
}
```

**Stat composition:**
- `PlayerStats`는 base layer로 유지 (saved as snapshot)
- `WeaponStatComposer`가 base + equipped delta 합산해 final stats 노출
- 저장에는 `equippedWeaponId` + `ownedWeaponIds`만 들어감 → 로드 시 재장착하면 stats 자동 재계산
- 이중 적용 방지

**Sprite swap (자동 합성):**
- 마법사 SpriteRenderer.color = `tintColor`
- 액세서리 자식 GameObject 생성 후 SpriteRenderer.sprite = `accessoryGlyph`
- 무기 변경 시 위 두 가지만 재설정 → 4프레임 애니메이션은 그대로 재사용

**시드 무기 목록 (6개):**

| weaponId | rarity | autoDmg | manualDmg | etc | 비고 |
|---|---|---|---|---|---|
| wand_starter | Common | +0 | +0 | — | 첫 지급 |
| apprentice_staff | Uncommon | +5 | +8 | — | |
| crystal_wand | Uncommon | +3 | +3 | crit +0.05 | |
| wizards_stave | Rare | +12 | +18 | — | |
| flame_rod | Rare | +8 | +10 | armorPen +3 | |
| arcane_scepter | Epic | +25 | +40 | crit +0.1, critMult +0.3 | |

### Out of scope
- 무기 강화 / 진화 (중복 흡수)
- 다중 슬롯 (메인+보조)
- 룬 / 옵션 / 옵션 재롤
- 무기별 고유 스킬

---

## 7. Feature 5 — 무기 뽑기(가챠)

### Why
방치형 RPG의 핵심 메타 루프. 골드 투자만으로는 한계가 있는 능력치 점프를, 가챠로 제공해 **장기 retention**을 만든다. 또한 정식 출시 시 매출 모델의 기반이 된다.

### What
- 가챠 화면 진입 (HUD에서 토글)
- 1회 / 10회 뽑기 버튼
- 통화는 **Gem** (gold와 분리)
- 결과 모달: 카드 페이드인으로 무기 표시, 희귀도별 색상 프레임
- 천장 시스템: **30회 뽑을 때 무조건 Rare 이상** 보장 (안정감)

### How
**`GachaDefinition` (ScriptableObject):**
```csharp
class GachaDefinition : ScriptableObject {
    string gachaId;          // "standard"
    string displayName;      // "기본 뽑기"
    int costSingle;          // 30 gems
    int costTen;             // 270 gems (10회 시 1회 무료)
    WeaponDatabase pool;     // 풀 제한
    RarityWeight[] weights;  // 희귀도별 확률
    int pityThreshold;       // 30
    Rarity pityFloor;        // Rare (천장 시 최소 등급)
}
```

**시드 확률 (standard 가챠):**

| 등급 | 가중치 | 확률 |
|---|---|---|
| Common | 50 | 50% |
| Uncommon | 30 | 30% |
| Rare | 15 | 15% |
| Epic | 4.5 | 4.5% |
| Legendary | 0.5 | 0.5% |

**알고리즘:**
1. pityCounter++
2. 가중치 기반 랜덤 등급 선택. pityCounter ≥ 30이면 강제 ≥ Rare + counter 리셋
3. 해당 등급의 무기 중 균등 랜덤
4. 인벤토리에 추가 (중복 허용 — 향후 "조각" 변환은 OOS)
5. 뽑기 비용 차감 (선차감 → 실패 시 환불)

**Gem 통화:**
- 첫 실행 시 300 gem 지급 (10회 뽑기 1번 가능)
- 이후 획득처: (이번 iteration에선) 없음. 차후 보스 클리어 보상 / 일일 로그인 등으로 확장 예정

**UI:**
- `GachaPanel`: 배너 아트 + 1회/10회 버튼 + 천장 카운터 표시
- `GachaResultPanel`: 등급순 정렬, 카드별 fade-in, 탭하여 닫기

### Out of scope
- 픽업 가챠 (특정 무기 확률 상승)
- 한정 가챠 / 시즌 가챠
- 조각 / 듀플리케이트 처리
- 광고 시청 보상 / 인앱 결제

---

## 8. Cross-cutting Concerns

### 8.1 Save 마이그레이션

기존 `saveVersion=1` 저장 파일을 가진 유저가 v6 업데이트 후 처음 실행하면 `MigrateIfNeeded`가:
1. `saveVersion` → 2
2. `equippedWeaponId` 누락 → `"wand_starter"`
3. `ownedWeaponIds` 누락 → `["wand_starter"]`
4. `gems` 누락 → 300
5. `pityCounter` 누락 → 0

→ 기존 진행도(gold/stage/stats) 손실 없음.

### 8.2 Realtime Database 보안 규칙

```json
{
  "rules": {
    "presence": {
      "$stage": {
        "$uid": {
          ".read":  "auth != null",
          ".write": "auth != null && auth.uid === $uid"
        }
      }
    },
    "chat": {
      "world": {
        "$msg": {
          ".read":  "auth != null",
          ".write": "auth != null
                    && newData.child('uid').val() === auth.uid
                    && newData.child('text').isString()
                    && newData.child('text').val().length <= 200"
        }
      },
      "stage": {
        "$stage": {
          "$msg": {
            ".read":  "auth != null",
            ".write": "auth != null
                      && newData.child('uid').val() === auth.uid
                      && newData.child('text').isString()
                      && newData.child('text').val().length <= 200"
          }
        }
      }
    }
  }
}
```

### 8.3 사용자 사전작업 (Bundle 5 RTDB 추가분)

Tasks K / M 시작 전에 사용자가 직접:
1. Firebase Console → **Realtime Database** → 데이터베이스 생성 (지역 권장: `asia-northeast3`)
2. **Rules** 탭에 위 보안 규칙 붙여넣기
3. Firebase Unity SDK 재실행 → `FirebaseDatabase.unitypackage` 추가 import
4. import 후 폴리필 충돌 정리 (`Assets/PlayServicesResolver/`, `Assets/Parse/` 삭제 — Bundle 5에서 했던 것과 동일)
5. macOS: `xattr -dr com.apple.quarantine Assets/Firebase/`

### 8.4 코드와 문서의 위치 분리

이 기능들은 **별도 작업 트랙**으로 진행된다. `Tasks_v6.md`가 구현 지시서이며, 기존 `Tasks.md`(Bundle 1~5)와 **충돌 회피 규칙**이 §0에 명시되어 있다.

---

## 9. Out of Scope (전체 v6 차원)

다음 항목은 v6에서 다루지 않으며 별도 iteration으로 미룬다:
- Apple 로그인 (Bundle 5에서도 skip)
- 인앱 결제 / 광고
- 다중 무기 슬롯 / 룬 / 옵션
- PvP / 길드 / 친구 시스템
- 일일 미션 / 시즌 패스
- 푸시 알림
- 클라우드 보스 (월드 보스)

---

## 10. 검증 (Bundle 6 release gate)

1개의 PlayMode 세션에서 **5개 기능 모두를 한 번에** 검증:
1. LoginScene 진입 → 스플래시 → Google 로그인 → 닉네임 입력 → MainScene
2. 챕터1 스테이지1 입장 → (옵션) 다른 Editor instance를 띄워 같은 스테이지에 접속 → 두 개의 마법사가 서로의 화면에 표시
3. 채팅 토글 → 월드 탭에 메시지 전송 → 다른 인스턴스에서 즉시 수신
4. 가챠 토글 → 10회 뽑기 → 결과 모달
5. 인벤토리 토글 → 새로 뽑은 무기 장착 → 마법사 색이 바뀌고 손 글리프 등장 → 자동공격 발사체 변경 + ATK 수치 상승
6. 앱 재시작 → 모든 변경 사항 보존 (Firestore + 로컬 캐시 합치)
