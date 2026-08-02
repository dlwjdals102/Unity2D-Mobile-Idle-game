# 검은탑 키우기 (가제) — 게임 기획 & 기술 설계서

> 포트폴리오 목적: **클라이언트 프로그래밍(구조 설계 · 최적화) 역량 증명**
> 개발 기간: 12주 (2~3개월) / 1인 개발
> 환경: Unity 6000.3.9f1, URP 2D, Input System, 세로 모드 모바일

---

## 1. 컨셉

**한 줄 피치**
> 끝없이 이어지는 탑을 자동으로 올라가는 검사를 키우는, 한 손으로 즐기는 판타지 방치형 RPG.

| 항목 | 내용 |
|------|------|
| 장르 | 2D 방치형 성장 RPG (판타지) |
| 플랫폼 | Android (세로 모드, 1080x1920 기준) |
| 아트 톤 | 도트 or 미니멀 실루엣 — 통일된 팔레트 우선, 물량 최소화 |
| 세션 설계 | 1일 3~5회 접속 × 회당 2~4분 |
| 타깃 플레이타임 | 프레스티지 1회차 도달까지 약 3~4시간 |

**의도적 스코프 제한**
- 성장 축 **3개**로 고정 (스탯 / 장비 / 정령)
- 서버 없음 — 전부 로컬. 랭킹·길드·PvP 제외
- 캐릭터 1종 (외형만 장비에 따라 변화)

---

## 2. 코어 루프

```
[접속]
  └─ 오프라인 보상 팝업 (+광고 2배)
       └─ 골드 소비 → 스탯 강화 5~20회
            └─ 층 돌파 → 새 장비/정령 해금
                 └─ 벽에 막힘 → 장비 뽑기 or 정령 강화
                      └─ 자동 전투 켜두고 이탈
                           └─ (반복) ─ 벽이 두꺼워지면 → [환생]
```

**세션 첫 60초 안에 성장 피드백 3회 이상 발생**시키는 것이 최우선 요구사항.

### 스테이지 구조
- 탑의 **층(Floor)** 단위로 진행. 1층부터 무한
- 일반 층: 몬스터 10마리 처치 → 자동으로 다음 층
- **10층마다 보스**: 제한시간 30초, 실패 시 해당 구간 반복 (= 성장 정체 지점 = 결제/광고 접점)
- 돌파한 최고 층까지는 자유 이동 가능 (파밍용)

---

## 3. 시스템 목록

### MVP (반드시 구현)

| 시스템 | 내용 | 구현 난이도 |
|--------|------|------------|
| 자동 전투 | 캐릭터 자동 공격, 몬스터 스폰/사망, 데미지 계산 | 중 |
| 층 진행 | 처치 카운트, 보스 타이머, 최고 층 기록 | 하 |
| 성장축 1 — 스탯 강화 | 공격력 / 체력 / 치명타 확률 / 치명타 배율 / 골드 획득량. 골드 소비, 무한 레벨 | 하 |
| 성장축 2 — 장비 | 무기·방어구·장신구 3슬롯. 가챠(다이아) → 인벤 → 착용 → 동일 등급 3개 합성 승급 | 중 |
| 성장축 3 — 정령 | 수집형 펫 12종. 1기 장착, 패시브 스탯 + 등급별 배수. 조각 모아 승급 | 중 |
| 오프라인 보상 | 상한 8시간, 효율 60%, 광고 시청 시 2배 | 중 |
| 재화 | 골드(무한 소비) / 다이아(가챠) / 환생석(영구 강화) | 하 |
| 프레스티지 "환생" | 층·골드·스탯 리셋, 환생석 획득 → 영구 배수 트리 | 중 |
| 세이브 | JSON 로컬 저장, 버전 마이그레이션, 백그라운드 진입 시 자동 저장 | 중 |
| 상점 | 광고 제거 / 성장 패키지 / 다이아 (더미 결제 — IAP 실연동 없이 UI만) | 하 |

### 2차 (시간 남으면)
- 일일 미션 + 출석 보상
- 도감 (정령/장비 컬렉션)
- 스킬 4종 (수동 발동 → 자동 발동 토글)

### 명시적 제외
서버 연동 / 랭킹 / 길드 / PvP / 실제 IAP·광고 SDK 연동 / 다중 캐릭터 / 스토리 컷신

---

## 4. 클라이언트 아키텍처 (포트폴리오 핵심)

### 4.1 어셈블리 레이어 분리 (.asmdef)

의존성은 **위에서 아래로만** 흐른다. 역방향 참조는 컴파일 단계에서 차단.

```
Game.Presentation   (UI View, 연출, 사운드)   ─┐
        ↓                                      │ 이벤트로 역방향 통지
Game.Gameplay       (전투, 성장, 프레스티지)   ←┘
        ↓
Game.Data           (테이블 SO, 세이브 모델)
        ↓
Game.Core           (BigNumber, EventBus, Pool, TickManager, Save)
```

- `Game.Core`는 UnityEngine 의존을 최소화 → **순수 C# 유닛 테스트 가능**
- `Game.Core.Tests` 어셈블리로 Unity Test Framework 연결 (이미 패키지 포함됨)

### 4.2 직접 구현할 것 (= 어필 포인트)

**① BigNumber (구조체)**
```csharp
public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
{
    private readonly double _mantissa;  // 1.0 ~ 9.999...
    private readonly int _exponent;     // 10의 지수
    // 연산자 오버로딩: + - * / < > == 
    // ToString(): 1.23K / 4.56M / 7.89B / 1.23aa / ...
}
```
- `double`의 표현 한계(~1e308)를 넘어서는 성장 곡선 대응
- 정규화(normalize) 로직, 지수 차이가 큰 덧셈의 조기 반환 최적화
- **유닛 테스트 30케이스 이상** — 이게 문서화되면 평가자에게 가장 강한 신호

**② TickManager (중앙 업데이트 루프)**
- 수백 개 오브젝트의 `MonoBehaviour.Update()` 개별 호출을 제거
- `ITickable` 인터페이스 등록 → 단일 Update에서 배열 순회
- 전투 틱(0.1s)과 렌더 틱 분리
- **Before/After 프로파일러 스크린샷을 포트폴리오에 첨부**

**③ Generic ObjectPool\<T\>**
- 데미지 팝업 텍스트, 몬스터, 이펙트, 골드 드랍
- 사전 워밍업 + 자동 확장 + 반환 누락 감지(디버그 빌드)
- 목표: **전투 중 GC Alloc 0B/frame**

**④ EventBus (타입 기반 pub/sub)**
```csharp
EventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
EventBus.Publish(new GoldChangedEvent(newAmount));
```
- UI가 게임 로직을 직접 참조하지 않도록 격리
- 구독 해제 누락 방지를 위한 `IDisposable` 핸들 반환

**⑤ SaveSystem**
- JSON 직렬화 + `saveVersion` 필드 기반 **마이그레이션 체인**
- 저장 데이터 해시 검증 (단순 변조 감지)
- 오프라인 시간 계산 시 **로컬 시간 되돌리기 방어** (마지막 저장 시각보다 과거면 0 처리)
- 자동 저장: `OnApplicationPause`, `OnApplicationQuit`, 30초 주기

**⑥ 데이터 파이프라인**
- CSV 원본 → **에디터 툴로 ScriptableObject 자동 생성**
- 밸런싱 수치를 코드 재컴파일 없이 수정 가능하게
- `[MenuItem("Tools/Rebuild Data Tables")]`

### 4.3 라이브러리로 해결할 것
| 용도 | 선택 | 이유 |
|------|------|------|
| 트윈/연출 | DOTween | 직접 구현 실익 없음, 연출 품질에 시간 투자 |
| 비동기 | UniTask | 코루틴 대비 GC 없음 — 이것도 어필 포인트 |
| 리소스 로딩 | Addressables | 메모리 관리 역량 어필 |
| UI | uGUI + TextMeshPro | UI Toolkit은 모바일 런타임 성숙도 리스크 |

### 4.4 UI 패턴
- **MVP (Model-View-Presenter)** — View는 `MonoBehaviour`, Presenter는 순수 C#
- 팝업 스택 매니저 (뒤로가기 키 처리 포함)
- 하단 5탭 + 상단 재화 바 고정

---

## 5. 데이터 테이블 스키마

**StatUpgradeTable.csv**
```
id, statType, baseValue, valueGrowth, baseCost, costGrowth, maxLevel
1,  Attack,   5,         1.075,       10,       1.095,      -1
```

**FloorTable.csv** — 층별 계수는 테이블이 아닌 수식으로 처리 (무한 층)
```
floorGroup, hpMultiplier, goldMultiplier, isBoss, bossTimeLimit
```

**EquipmentTable.csv**
```
id, slot, grade, name, baseAtk, baseHp, setId, iconKey
```

**SpiritTable.csv**
```
id, grade, name, passiveType, passiveValue, upgradeShardCost, iconKey
```

**GachaTable.csv**
```
poolId, itemId, weight, guaranteeCount
```

---

## 6. 밸런싱 수식 (1차안)

| 대상 | 수식 |
|------|------|
| 몬스터 HP | `HP(n) = 10 × 1.16⁽ⁿ⁻¹⁾` (n = 층, 1-based → 1층이 기본값) |
| 골드 드랍 | `Gold(n) = 5 × 1.14⁽ⁿ⁻¹⁾` |
| 스탯 효과 | `Value(k) = base × 1.075ᵏ` |
| 강화 비용 | `Cost(k) = base × 1.095ᵏ` |
| 보스 HP | 동일 층 일반 몬스터 × 12 |
| 오프라인 수익 | `초당 골드 × min(경과초, 28800) × 0.6` |
| 환생석 획득 | `floor(√(최고층 / 10))` |
| 환생 영구 배수 | 환생석 1개당 전체 골드/데미지 +2% (가산) |

**핵심 원칙**: `costGrowth(1.095) > valueGrowth(1.075)` → 레벨이 오를수록 필연적으로 정체 발생 → 다른 성장 축으로 유도.

**검증 방법**: 밸런싱 시뮬레이터를 별도 콘솔 앱 또는 에디터 창으로 제작해 "n시간 플레이 시 도달 층" 곡선을 출력. 이것도 포트폴리오 자료가 된다.

---

## 7. 12주 개발 일정

| 주차 | 목표 | 산출물 |
|------|------|--------|
| **W1** | 프로젝트 기반 | asmdef 4계층 분리, BigNumber 구현 + 유닛 테스트 30케이스, Git 브랜치 전략 |
| **W2** | 코어 인프라 | EventBus, ObjectPool\<T\>, TickManager, SaveSystem(+마이그레이션), CSV→SO 에디터 툴 |
| **W3** | 전투 코어 | 몬스터 스폰/사망, 데미지 계산, 자동 공격, 층 진행 |
| **W4** | 전투 완성 | 보스전 타이머, 데미지 팝업(풀링), 층 이동 UI, 첫 플레이 가능 빌드 |
| **W5** | 성장축 1 | 스탯 강화 시스템 + UI(MVP 패턴), 재화 시스템, 골드 싱크 검증 |
| **W6** | 성장축 2 | 장비 가챠 · 인벤토리 · 착용 · 3합성 승급 |
| **W7** | 성장축 3 | 정령 수집 · 장착 · 조각 승급, 도감 |
| **W8** | 방치 시스템 | 오프라인 보상, 광고 배수(스텁), 상점 UI, 자동 저장 |
| **W9** | 프레스티지 | 환생 시스템, 영구 강화 트리, **밸런싱 시뮬레이터 제작 및 1차 튜닝** |
| **W10** | 폴리싱 | DOTween 연출, 사운드, 화면 전환, 알림 뱃지, 튜토리얼 |
| **W11** | 최적화 | 프로파일링(Deep Profile), GC Alloc 제거, Draw Call 배칭, 실기기 테스트, APK 빌드 |
| **W12** | 문서화 | README, 아키텍처 다이어그램, 최적화 Before/After 리포트, 플레이 영상, 기술 블로그 3편 |

**주간 리스크 대응**: W6~W7 중 하나라도 밀리면 정령(성장축 3)을 잘라내고 W8로 진입. 성장축 2개 + 완성도 높은 최적화가, 성장축 3개 + 미완성보다 낫다.

---

## 8. 포트폴리오 산출물 체크리스트

- [ ] 실행 가능한 APK + 플레이 영상 (2분, 자막으로 기술 포인트 설명)
- [ ] README: 아키텍처 다이어그램, 핵심 구현 3가지 요약
- [ ] BigNumber 유닛 테스트 통과 스크린샷
- [ ] 최적화 리포트: TickManager 도입 전/후 프로파일러 비교, GC Alloc 0 달성 근거
- [ ] 데이터 파이프라인 에디터 툴 시연 GIF
- [ ] 밸런싱 시뮬레이터 결과 그래프
- [ ] 커밋 히스토리 (기능 단위로 정리, squash 금지)

---

## 9. 즉시 착수 항목 (W1)

1. `Assets/_Project/` 하위에 `Core / Data / Gameplay / Presentation` 폴더 + asmdef 생성
2. `Game.Core.Tests` 어셈블리 생성, EditMode 테스트 세팅
3. `BigNumber` 구조체 구현 + 테스트
4. `.gitignore` 검증 (Library/, Logs/, obj/, .vs/ 제외 확인)
5. Player Settings: 세로 고정, 최소 API 레벨, 패키지명, 목표 프레임 60
