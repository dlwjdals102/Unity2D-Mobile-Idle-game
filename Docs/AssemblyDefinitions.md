# Assembly Definition (asmdef) 사용법

이 프로젝트에서 asmdef를 쓰는 이유와, 실제로 손을 대야 할 때 무엇을 하면 되는지 정리한 문서.

---

## 1. 한 줄 요약

> **asmdef는 "이 폴더 아래 스크립트를 별도 DLL로 분리하고, 여기 적은 것만 참조하게 하라"는 선언이다.**

핵심은 **참조 제한**이다. 폴더 정리가 목적이 아니다.

---

## 2. 없을 때와 있을 때

### asmdef가 없으면

Unity는 `Assets` 아래 모든 스크립트를 **`Assembly-CSharp.dll` 하나**에 몰아넣는다.

- 모든 스크립트가 서로를 자유롭게 참조할 수 있다.
- 스크립트를 한 줄만 고쳐도 **전체가 다시 컴파일**된다.
- `UnityEngine`이 항상 참조되어 있으므로, 게임 로직에서 `Debug.Log`나 `MonoBehaviour`를 쓰는 걸 막을 방법이 없다.

### asmdef가 있으면

폴더마다 별도 DLL이 만들어지고, `references`에 적지 않은 어셈블리는 **쓸 수 없다**.

- 계층을 넘는 잘못된 참조가 **컴파일 에러**로 즉시 드러난다.
- 고친 어셈블리와 그것을 참조하는 어셈블리만 다시 컴파일된다.
- `noEngineReferences: true`면 `UnityEngine` 자체가 없어서, 그 어셈블리는 순수 C#이 된다.

**이 프로젝트에서 asmdef가 실제로 하고 있는 일:**
`BattleRunner.cs`에서 `Debug.Log("...")`를 한 줄 쓰면 컴파일이 실패한다.
"게임 로직에 엔진 의존을 넣지 말자"가 지키자는 약속이 아니라 **강제**가 된다.
이 덕분에 EditMode 테스트가 씬도 PlayMode도 없이 돈다.

---

## 3. 이 프로젝트의 어셈블리 구성

```
Game.Presentation        UnityEngine 사용 (MonoBehaviour, UI)
      │  참조
      ▼
Game.Gameplay            noEngineReferences: true
      │  참조
      ▼
Game.Core                noEngineReferences: true
```

의존은 **위에서 아래로만** 흐른다. 역방향은 컴파일이 막는다.

| 어셈블리 | 위치 | 참조하는 것 | 엔진 사용 |
|---|---|---|---|
| `Game.Core` | `Assets/_Project/Core/` | 없음 | ✕ |
| `Game.Gameplay` | `Assets/_Project/Gameplay/` | `Game.Core` | ✕ |
| `Game.Presentation` | `Assets/_Project/Presentation/` | `Game.Core`, `Game.Gameplay`, `Unity.TextMeshPro`, `UnityEngine.UI` | ○ |
| `Game.Core.Tests` | `Assets/_Project/Tests/EditMode/Core/` | `Game.Core` + 테스트 프레임워크 | 에디터 전용 |
| `Game.Gameplay.Tests` | `Assets/_Project/Tests/EditMode/Gameplay/` | `Game.Core`, `Game.Gameplay` + 테스트 프레임워크 | 에디터 전용 |

**이 5개가 최종 구성이다.** 앞으로 파일은 계속 늘지만 새 asmdef는 원칙적으로 만들지 않는다.

---

## 4. asmdef 파일의 필드 전부

`Game.Presentation.asmdef`를 예로 든다. 텍스트 에디터로 열면 JSON이다.

```json
{
    "name": "Game.Presentation",
    "rootNamespace": "Game.Presentation",
    "references": ["Game.Core", "Game.Gameplay", "Unity.TextMeshPro", "UnityEngine.UI"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

| 필드 | 의미 | 이 프로젝트에서 |
|---|---|---|
| `name` | 만들어질 DLL 이름. **프로젝트 안에서 유일해야 한다.** | `Game.*` 규칙 |
| `rootNamespace` | 이 폴더에서 새 스크립트를 만들 때 Unity가 자동으로 붙여줄 네임스페이스. 강제력은 없다 | 어셈블리 이름과 동일 |
| `references` | 참조할 다른 어셈블리 목록. **여기 없으면 못 쓴다** | 손댈 일이 있는 유일한 필드 |
| `includePlatforms` | 이 어셈블리를 포함할 플랫폼. **비어 있으면 전부 포함** | 런타임은 빈 배열, 테스트는 `["Editor"]` |
| `excludePlatforms` | 제외할 플랫폼 | 안 씀 |
| `allowUnsafeCode` | `unsafe` 키워드 허용 여부 | `false` |
| `overrideReferences` | 프로젝트의 DLL을 자동 참조하지 않고 직접 고를지 | 테스트만 `true` |
| `precompiledReferences` | `overrideReferences`가 `true`일 때 참조할 DLL | 테스트의 `nunit.framework.dll` |
| `autoReferenced` | `Assembly-CSharp`이 이 어셈블리를 자동으로 참조할지 | 테스트만 `false` |
| `defineConstraints` | 이 심볼이 정의돼 있을 때만 컴파일 | 테스트의 `UNITY_INCLUDE_TESTS` |
| `versionDefines` | 특정 패키지 버전에 따라 심볼 정의 | 안 씀 |
| `noEngineReferences` | `true`면 `UnityEngine`·`UnityEditor`를 참조하지 않음 | **Core·Gameplay가 `true`** |

> 인스펙터에서는 이 필드들이 체크박스와 리스트로 보인다. JSON을 직접 고쳐도 되고 인스펙터로 해도 결과는 같다.

---

## 5. 새 asmdef 만드는 법

새로 만들 일은 거의 없지만, 필요하다면.

1. Project 창에서 대상 **폴더**를 우클릭
2. `Create > Assembly Definition`
   - Unity 6에서는 `Create > Scripting > Assembly Definition`에 있을 수 있다. Create 메뉴 상단 검색창에 `assembly`를 치면 바로 찾을 수 있다.
3. 파일 이름을 어셈블리 이름과 같게 짓는다 (예: `Game.Something`)
4. 인스펙터에서 `Root Namespace`, `References`, 필요하면 `No Engine References`를 설정
5. **Apply 버튼을 누른다** — 이걸 안 누르면 반영되지 않는다

만들자마자 그 폴더 아래 스크립트들이 새 어셈블리로 옮겨가므로, **참조가 끊기면서 에러가 무더기로 뜨는 게 정상**이다. `references`를 채우면 사라진다.

---

## 6. 참조 추가하는 법 (실제로 하게 될 유일한 작업)

**증상:** 분명히 존재하는 클래스인데 `찾을 수 없다`는 컴파일 에러가 난다.

**해결:**

1. Project 창에서 **에러가 난 쪽**의 `.asmdef` 파일을 클릭
2. 인스펙터의 `Assembly Definition References` 목록에서 `+` 클릭
3. 새로 생긴 빈 칸 오른쪽의 동그란 아이콘을 눌러 필요한 어셈블리를 고른다
4. **Apply**

### 실제 사례: `Game.Presentation`을 만들었을 때

TextMeshPro와 uGUI를 쓰려면 이 두 줄이 필요했다.

```json
"references": ["Game.Core", "Game.Gameplay", "Unity.TextMeshPro", "UnityEngine.UI"]
```

**패키지의 어셈블리 이름을 모를 때 찾는 법:**

```bash
find Library/PackageCache -name "*.asmdef" -path "*ugui*" -exec grep -o '"name"[^,]*' {} \;
```

또는 Project 창 검색창에 `t:asmdef`를 치면 패키지 것까지 전부 나온다.

---

## 7. 에러 메시지별 해결법

| 에러 | 원인 | 해결 |
|---|---|---|
| `The type or namespace name 'BigNumber' could not be found` | 참조 누락 | 해당 asmdef의 `references`에 `Game.Core` 추가 |
| `The name 'Debug' does not exist in the current context` (Core/Gameplay에서) | `noEngineReferences: true`라서 `UnityEngine`이 없다 | **참조를 추가하지 말 것.** 그 코드가 Core가 아니라 Presentation에 속한다는 신호다 |
| `Assembly with name 'X' already exists` | `name`이 중복 | 이름 변경 |
| `cyclic reference` | A가 B를, B가 A를 참조 | 계층이 잘못됐다. 공통 부분을 아래 어셈블리로 내린다 |
| Test Runner에 테스트가 안 보인다 | 테스트 asmdef의 `references`나 `includePlatforms` 문제 | `UnityEngine.TestRunner`, `UnityEditor.TestRunner` 참조와 `includePlatforms: ["Editor"]` 확인 |
| 스크립트를 옮겼더니 참조가 깨졌다 | 폴더를 옮기면 소속 어셈블리가 바뀐다 | 옮긴 곳의 asmdef에 참조 추가, 또는 원래 위치로 되돌림 |

### 특히 두 번째 항목이 중요하다

`Game.Gameplay`에서 `Debug.Log`가 필요해 보이면, **참조를 뚫지 말고 왜 필요한지 다시 본다.**
거의 항상 그 코드는 표현 계층에 속한다. 이 규칙을 한 번 깨면 asmdef를 쓰는 의미가 사라진다.

---

## 8. 언제 새로 만드나 (이 프로젝트의 규칙)

**기본값은 "만들지 않는다"다.** 새 폴더를 만들어도 asmdef는 만들지 않는다.
`Assets/_Project/Gameplay/Equipment/`를 만들면 그냥 `Game.Gameplay`에 속한다.

새 asmdef가 정당해지는 경우는 둘뿐이다.

1. **엔진 의존 여부가 다를 때** — 순수 C#으로 두고 싶은데 상위 어셈블리가 엔진을 쓰는 경우
2. **에디터 전용 코드일 때** — `UnityEditor`를 쓰는 툴은 빌드에 포함되면 안 되므로 `includePlatforms: ["Editor"]`인 별도 어셈블리가 필요하다

2번은 W5의 CSV → ScriptableObject 변환 툴에서 실제로 필요해질 가능성이 있다. 그때 `Game.Editor`를 만든다.

---

## 9. 테스트 어셈블리가 다른 점

```json
{
    "name": "Game.Core.Tests",
    "references": ["Game.Core", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "includePlatforms": ["Editor"],
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

일반 어셈블리와 다른 네 가지:

- `includePlatforms: ["Editor"]` — 빌드에 포함되지 않는다
- `overrideReferences: true` + `precompiledReferences: ["nunit.framework.dll"]` — NUnit을 쓰기 위해 필요하다
- `autoReferenced: false` — 게임 코드가 테스트 코드를 참조하는 사고를 막는다
- `defineConstraints: ["UNITY_INCLUDE_TESTS"]` — 테스트가 활성화된 상태에서만 컴파일된다

**하나의 폴더에는 asmdef가 하나만 올 수 있다.** 그래서 테스트가 `Tests/EditMode/Core/`와
`Tests/EditMode/Gameplay/`로 나뉘어 있다.

---

## 10. 확인 방법

### 어떤 어셈블리가 만들어졌는지

```bash
ls Library/ScriptAssemblies/Game.*.dll
```

```
Game.Core.dll
Game.Core.Tests.dll
Game.Gameplay.dll
Game.Gameplay.Tests.dll
Game.Presentation.dll
```

여기에 없으면 그 어셈블리는 컴파일되지 않은 것이다.

### 스크립트가 어느 어셈블리에 속하는지

`.cs` 파일을 클릭하면 인스펙터 상단에 `Assembly Information`으로 소속 어셈블리가 표시된다.

### 컴파일과 테스트를 한 번에

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe" -batchmode -projectPath "C:/Users/pc/Desktop/Unity2D-Mobile-Idle-game" -runTests -testPlatform EditMode -testResults "Logs/editmode-results.xml" -logFile -
```

에디터가 켜져 있으면 프로젝트가 잠겨서 실패한다. 먼저 닫아야 한다.
