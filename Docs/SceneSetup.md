# 씬 배치 가이드 (W3-b)

스크립트는 모두 작성되어 있다. 이 문서대로 씬을 구성하면 자동 전투가 화면에 나온다.
대상 씬은 `Assets/Scenes/SampleScene.unity` (빌드 목록의 유일한 씬).

완성 후 하이어라키:

```
SampleScene
├── Main Camera              Orthographic, Size 5
├── Game                     GameLoop
├── PopupSpawner             DamagePopupSpawner
├── MonsterAnchor            빈 오브젝트 — 데미지 숫자가 뜨는 위치
└── Canvas                   BattleHud
    ├── FloorLabel           TextMeshProUGUI
    ├── GoldLabel            TextMeshProUGUI
    ├── DiamondLabel         TextMeshProUGUI
    ├── KillProgressLabel    TextMeshProUGUI
    ├── BossTimerLabel       TextMeshProUGUI
    ├── HealthBar
    │   └── Fill             Image (Filled)
    └── UpgradePanel                     하단 강화 영역
        ├── AttackPowerButton            Button + StatUpgradeButton
        │   ├── NameLabel
        │   ├── LevelLabel
        │   └── CostLabel
        └── CriticalMultiplierButton     위와 같은 구조
```

---

## 0. TMP 리소스 임포트 (최초 1회)

`Window > TextMeshPro > Import TMP Essential Resources`

이걸 건너뛰면 TextMeshPro 오브젝트를 만들 때 폰트가 없어 글자가 보이지 않는다.

## 1. Game 뷰 해상도

Game 뷰 좌상단 해상도 드롭다운 → `+` → **1080 x 1920 Portrait** 추가 후 선택.
(Player Settings는 이미 세로 고정이지만, 에디터 Game 뷰는 별도로 지정해야 한다.)

## 2. 카메라

`Main Camera` 선택 후:

| 항목 | 값 |
|---|---|
| Projection | Orthographic |
| Size | 5 |
| Position | (0, 0, -10) |

## 3. 데미지 팝업 프리팹

1. 하이어라키 우클릭 → `3D Object > Text - TextMeshPro`
   - **UI 아래가 아니라 씬 루트에** 만든다. 월드 좌표로 움직이는 오브젝트다.
   - 이름을 `DamagePopup`으로 변경
2. Inspector에서 TextMeshPro 컴포넌트 설정
   - Text: `0` (실행 시 덮어써지므로 아무 값)
   - Font Size: `4`
   - Alignment: 가운데 정렬(가로/세로 모두)
   - Rect Transform Width/Height: `4 / 1`
3. `Add Component` → **DamagePopup** 스크립트 추가
4. DamagePopup 컴포넌트의 **Label** 필드에, 같은 오브젝트의 **TextMeshPro** 컴포넌트를 드래그
5. `Assets/_Project/Prefabs/` 폴더를 만들고 오브젝트를 끌어다 놓아 프리팹으로 저장
6. **하이어라키에 남은 원본은 삭제** (스포너가 프리팹에서 복제하므로 씬에 있으면 안 된다)

## 4. Canvas와 HUD

1. 하이어라키 우클릭 → `UI > Canvas`
2. Canvas 컴포넌트: Render Mode = `Screen Space - Overlay`
3. **Canvas Scaler** 컴포넌트:

| 항목 | 값 |
|---|---|
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | 1080 x 1920 |
| Screen Match Mode | Match Width Or Height |
| Match | 0.5 |

4. Canvas 아래에 `UI > Text - TextMeshPro`로 라벨 4개를 만든다.
   이름과 배치는 아래를 따르되, 정확한 위치는 취향껏 조정해도 된다.

| 이름 | 위치 | 용도 |
|---|---|---|
| `FloorLabel` | 상단 중앙 | `12층` |
| `GoldLabel` | 상단 우측 | `1.23K` |
| `DiamondLabel` | 상단 우측, 골드 아래 | `15` |
| `KillProgressLabel` | 상단 중앙, 층 아래 | `3 / 10` |
| `BossTimerLabel` | 화면 중앙 상단 | `28.4` |

> 세로 고정이지만 기기 화면비는 제각각이다. 라벨은 고정 좌표 대신 **앵커를 상단에 붙여서** 배치한다.

5. 체력 바
   - Canvas 우클릭 → `UI > Image`, 이름 `HealthBar` (배경. 어두운 색)
   - `HealthBar` 우클릭 → `UI > Image`, 이름 `Fill` (빨간색)
   - `Fill`의 Image 컴포넌트:

| 항목 | 값 |
|---|---|
| Image Type | Filled |
| Fill Method | Horizontal |
| Fill Origin | Left |
| Fill Amount | 1 |

   - `Fill`의 Rect Transform은 부모에 꽉 차게 (앵커 프리셋에서 `Alt + 우하단 stretch` 선택)

6. `Canvas` 오브젝트에 `Add Component` → **BattleHud** 추가
7. BattleHud의 필드에 방금 만든 것들을 드래그

| 필드 | 연결할 것 |
|---|---|
| Floor Label | `FloorLabel` |
| Gold Label | `GoldLabel` |
| Diamond Label | `DiamondLabel` |
| Kill Progress Label | `KillProgressLabel` |
| Boss Timer Label | `BossTimerLabel` |
| Health Bar Fill | `Fill` |

## 5. 강화 버튼 (W5)

> **Layout Group을 쓰지 않는다.** 버튼이 두 개뿐이라 앵커로 영역을 나누는 쪽이 단순하고,
> 무엇보다 결과가 예측 가능하다. Layout Group은 자식의 위치를 자기가 소유하기 때문에
> 손으로 옮길 수 없고, Control Child Size가 꺼져 있으면 크기도 그대로 둬서 혼란스럽다.

### 앵커를 다룰 때 반드시 지킬 것

인스펙터에서 **Anchor Min/Max를 숫자로 바꿔도 사각형은 원래 자리에 그대로 남는다.**
Unity가 위치를 유지하려고 오프셋에 보정값을 넣기 때문이다.
그래서 앵커를 바꾼 뒤에는 **항상 Left / Right / Top / Bottom을 전부 0으로** 만들어야 한다.

(Anchor Min/Max가 두 축 모두 벌어져 있으면 인스펙터에 Pos/Width/Height 대신
Left/Right/Top/Bottom이 표시된다.)

### 5-1. UpgradePanel

Canvas 우클릭 → `Create Empty`, 이름 `UpgradePanel`. RectTransform에 아래를 넣는다.

| 항목 | 값 | 의미 |
|---|---|---|
| Anchor Min | X `0` / Y `0` | 화면 하단에 가로로 꽉 |
| Anchor Max | X `1` / Y `0.28` | 아래쪽 28%를 차지 |
| Left / Right | `40` / `40` | 좌우 여백 |
| Top / Bottom | `0` / `40` | 아래 여백 |

### 5-2. AttackPowerButton

`UpgradePanel` 우클릭 → `UI > Button - TextMeshPro`, 이름 `AttackPowerButton`.

| 항목 | 값 |
|---|---|
| Anchor Min | X `0` / Y `0.55` |
| Anchor Max | X `1` / Y `1` |
| Left / Right / Top / Bottom | 전부 `0` |

패널 위쪽 절반을 채운다.

### 5-3. 버튼 안의 라벨 3개

버튼을 만들면 자식 `Text (TMP)`가 하나 딸려 온다. 이름을 `NameLabel`로 바꾼다.
그다음 `AttackPowerButton` 우클릭 → `UI > Text - TextMeshPro`를 **두 번** 만들어
`LevelLabel`, `CostLabel`로 이름을 바꾼다.

세 라벨이 가로를 나눠 갖도록 앵커를 준다. **오프셋을 0으로 만드는 것을 잊지 말 것.**

| 라벨 | Anchor Min | Anchor Max | Left | Right | Top/Bottom | 정렬 |
|---|---|---|---|---|---|---|
| `NameLabel` | `0`, `0` | `0.45`, `1` | `24` | `0` | `0` | 좌 / 중앙 |
| `LevelLabel` | `0.45`, `0` | `0.70`, `1` | `0` | `0` | `0` | 중앙 / 중앙 |
| `CostLabel` | `0.70`, `0` | `1`, `1` | `0` | `24` | `0` | 우 / 중앙 |

정렬은 TextMeshPro 컴포넌트의 `Alignment`에서 가로/세로를 각각 지정한다.
글자가 잘리면 `Auto Size`를 켜거나 Font Size를 줄인다.

### 5-4. 스크립트 연결

`AttackPowerButton`에 `Add Component` → **StatUpgradeButton**

| 필드 | 값 |
|---|---|
| Display Name | `공격력` |
| Name Label | `NameLabel` |
| Level Label | `LevelLabel` |
| Cost Label | `CostLabel` |
| Button | `AttackPowerButton` 자신의 Button 컴포넌트 |

### 5-5. 두 번째 버튼

`AttackPowerButton`을 복제(`Ctrl + D`)해 `CriticalMultiplierButton`으로 이름을 바꾼다.
복제하면 Unity가 계층 안쪽을 가리키던 참조를 새 사본으로 다시 이어주므로,
**실제로 바꿀 것은 두 가지뿐이다.**

| 항목 | 값 |
|---|---|
| Display Name | `치명타 배율` |
| Anchor Min / Max | Y를 `0` / `0.45`로 (패널 아래쪽 절반) |

> 버튼은 골드가 모자라면 자동으로 비활성(회색)이 된다. 코드가 `interactable`을 제어하므로
> 인스펙터에서 직접 끄지 않는다.

## 6. 스포너와 진입점

1. 빈 오브젝트 생성 (`Ctrl + Shift + N`), 이름 `MonsterAnchor`, Position `(0, 0, 0)`
2. 빈 오브젝트 생성, 이름 `PopupSpawner` → `Add Component` → **DamagePopupSpawner**

| 필드 | 연결할 것 |
|---|---|
| Prefab | `Assets/_Project/Prefabs/DamagePopup.prefab` |
| Spawn Anchor | `MonsterAnchor` |
| Prewarm Count | 16 |
| Spawn Radius | 0.35 |

3. 빈 오브젝트 생성, 이름 `Game` → `Add Component` → **GameLoop**

| 필드 | 연결할 것 |
|---|---|
| Hud | `Canvas` |
| Popup Spawner | `PopupSpawner` |
| Attack Power Button | `AttackPowerButton` |
| Critical Multiplier Button | `CriticalMultiplierButton` |
| Attacks Per Second | 2 |
| Critical Chance | 0.15 |

> 공격력과 치명타 배율은 인스펙터에 없다. 강화 단계에서 계산되므로
> `StatUpgrades.CreateDefault`가 기준값을 들고 있다.

4. `Ctrl + S`로 씬 저장

---

## 7. 실행 확인

플레이 버튼을 누르고 아래를 확인한다.

- [ ] 초당 2번씩 데미지 숫자가 떠오르며 사라진다
- [ ] 가끔(15%) 노란색의 큰 숫자가 뜬다 — 치명타
- [ ] 체력 바가 줄었다가 몬스터가 죽으면 다시 찬다
- [ ] `3 / 10` 처치 카운트가 올라가고, 10이 되면 층이 1 오르고 0으로 돌아간다
- [ ] 골드가 계속 쌓이고, 커지면 `1.23K` → `4.56M` 형태로 단위가 바뀐다
- [ ] **10층에 도달하면** 보스 타이머가 나타나고 30.0부터 줄어든다
- [ ] 보스 체력이 훨씬 많다(12배). 잡지 못하면 시간이 끝나고 체력이 꽉 찬 채로 다시 시작된다
- [ ] 강화 버튼이 골드가 모자라면 회색이고, 충분해지면 눌린다
- [ ] 강화를 누르면 골드가 줄고 `Lv.` 가 올라가며, **다음 비용이 더 비싸진다**
- [ ] 공격력을 몇 단계 올리면 **보스가 뚫린다**
- [ ] 게임을 정지했다 다시 실행하면 층·골드·강화 단계가 이어진다

마지막 항목이 세이브 확인이다. 파일 위치는
`C:\Users\pc\AppData\LocalLow\DefaultCompany\My project\save.json`이고,
초기화하려면 이 파일을 지우면 된다.

## 문제가 생기면

| 증상 | 원인 |
|---|---|
| `GameLoop: 인스펙터에 연결되지 않은 참조가 있다` | 6-3의 연결 중 빠진 것이 있다 |
| `NullReferenceException` (BattleHud) | 4-7의 라벨 연결 중 빠진 것이 있다 |
| 글자가 안 보인다 | 0단계 TMP 리소스 임포트를 안 했다. 한글만 깨지면 폴백 폰트 등록을 안 했다 |
| 데미지 숫자가 안 보인다 | 팝업 프리팹이 카메라 시야 밖이거나, 3-6에서 원본을 안 지웠다 |
| 체력 바가 안 줄어든다 | `Fill`의 Image Type이 `Filled`가 아니다 |
| 강화 버튼이 계속 회색이다 | 정상이다. 골드가 비용보다 적으면 눌리지 않는다 |
| 버튼을 눌러도 아무 일이 없다 | `StatUpgradeButton`의 Button 필드가 비었거나 다른 버튼을 가리킨다 |
| 두 버튼이 같이 올라간다 | 5-5 복제 후 라벨/Button 연결이 원본을 가리키고 있다 |
| 라벨 3개가 겹쳐 보인다 | 5-3의 앵커를 안 줬다. 새로 만든 TMP 텍스트는 기본이 중앙 200×50이라 전부 같은 자리에 생긴다 |
| 앵커를 바꿨는데 사각형이 안 움직인다 | Left/Right/Top/Bottom에 보정값이 남아 있다. 전부 0으로 만든다 |
| 자식을 손으로 옮겨도 제자리로 돌아온다 | 부모에 Layout Group이 붙어 있다. 이 가이드는 Layout Group을 쓰지 않는다 |

---

## 부록. 개발용 디버그 패널 (선택)

층이 오르기를 기다리지 않고 바로 다음 상태를 확인하기 위한 것이다.
**연결하지 않아도 게임은 정상 동작한다.** `GameLoop`의 연결 검사 대상이 아니다.

1. Canvas 우클릭 → `Create Empty`, 이름 `DebugPanel`
   - 앵커를 화면 우측 상단 등 HUD와 겹치지 않는 곳에 둔다
2. `DebugPanel` 우클릭 → `UI > Button - TextMeshPro`를 **두 개** 만든다

| 이름 | 버튼 안 텍스트 |
|---|---|
| `ClearFloorButton` | `층 클리어` |
| `DeleteSaveButton` | `세이브 삭제` |

3. `DebugPanel`에 `Add Component` → **DebugPanel**
4. 필드 연결

| 필드 | 연결할 것 |
|---|---|
| Clear Floor Button | `ClearFloorButton` |
| Delete Save Button | `DeleteSaveButton` |

5. `Game` 오브젝트의 `GameLoop` → **Debug Panel** 필드에 `DebugPanel` 연결

### 층 클리어

현재 층의 남은 몬스터가 전부 처치되고 다음 층으로 넘어간다.
**보상은 정상 처치와 동일하게 지급된다.** 골드가 쌓여야 강화를 바로 시험할 수 있기 때문이다.
보스 층에서 누르면 다이아도 나온다.

### 세이브 삭제

파일을 지우고 **그 자리에서 새 게임을 시작한다.** 1층 / 골드 0 / 다이아 0 / 강화 0단계로 돌아간다.

파일만 지우지 않는 이유가 있다. 플레이 모드를 빠져나갈 때 종료 저장이 실행되므로,
상태를 그대로 두면 지운 파일이 곧바로 되살아난다.

> `DebugPanel`이 전투 객체가 아니라 `GameLoop`을 들고 있는 것도 이 때문이다.
> 리셋으로 전투가 새로 만들어져도 버려진 객체를 가리키지 않는다.

---

`DebugPanel`은 에디터와 개발 빌드에서만 살아 있고, 배포 빌드에서는 `Awake`에서 스스로 꺼진다.
앞으로 필요한 디버그 기능이 생기면 이 패널에 버튼을 하나씩 추가한다.
