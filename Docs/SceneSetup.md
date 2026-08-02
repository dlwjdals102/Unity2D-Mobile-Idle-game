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
    ├── KillProgressLabel    TextMeshProUGUI
    ├── BossTimerLabel       TextMeshProUGUI
    └── HealthBar
        └── Fill             Image (Filled)
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
| Kill Progress Label | `KillProgressLabel` |
| Boss Timer Label | `BossTimerLabel` |
| Health Bar Fill | `Fill` |

## 5. 스포너와 진입점

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
| Attack Power | 5 |
| Attacks Per Second | 2 |
| Critical Chance | 0.15 |
| Critical Multiplier | 2 |

4. `Ctrl + S`로 씬 저장

---

## 6. 실행 확인

플레이 버튼을 누르고 아래를 확인한다.

- [ ] 초당 2번씩 데미지 숫자가 떠오르며 사라진다
- [ ] 가끔(15%) 노란색의 큰 숫자가 뜬다 — 치명타
- [ ] 체력 바가 줄었다가 몬스터가 죽으면 다시 찬다
- [ ] `3 / 10` 처치 카운트가 올라가고, 10이 되면 층이 1 오르고 0으로 돌아간다
- [ ] 골드가 계속 쌓이고, 커지면 `1.23K` → `4.56M` 형태로 단위가 바뀐다
- [ ] **10층에 도달하면** 보스 타이머가 나타나고 30.0부터 줄어든다
- [ ] 보스 체력이 훨씬 많다(12배). 시작 스탯으로는 30초 안에 못 잡고, 시간이 끝나면 체력이 꽉 찬 채로 다시 시작된다

마지막 항목은 **정상 동작**이다. 지금은 성장 수단이 없어서 보스 층이 벽이 된다.
스탯 강화(W5)가 들어가면 뚫린다. 당장 뒤를 보고 싶으면 `Game` 오브젝트의
Attack Power를 크게 올리면 된다.

## 문제가 생기면

| 증상 | 원인 |
|---|---|
| `GameLoop: Hud 또는 PopupSpawner가 인스펙터에 연결되지 않았다` | 5-3의 연결을 빠뜨렸다 |
| `NullReferenceException` (BattleHud) | 4-7의 라벨 연결 중 빠진 것이 있다 |
| 글자가 안 보인다 | 0단계 TMP 리소스 임포트를 안 했다 |
| 데미지 숫자가 안 보인다 | 팝업 프리팹이 카메라 시야 밖이거나, 3-6에서 원본을 안 지웠다 |
| 체력 바가 안 줄어든다 | `Fill`의 Image Type이 `Filled`가 아니다 |
