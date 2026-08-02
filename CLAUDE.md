# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

---

## 프로젝트 맥락

- **무엇인가:** 취업 포트폴리오로 1인 개발하는 Unity 2D 모바일 방치형 RPG.
  어필 대상은 콘텐츠 물량이 아니라 **클라이언트 프로그래밍(구조 설계·최적화)** 이다.
- **환경:** Unity 6000.3.9f1, URP 2D, Input System, uGUI + TextMeshPro. 세로 모드, Android.
- **기획서:** `Docs/GDD.md` — 컨셉, 시스템, 밸런싱 수식, 12주 일정.

### 언어

- 사용자 응답, 주석, 커밋 메시지, 문서: **한국어**
- 식별자(클래스·메서드·변수명), 네임스페이스, 파일명: **영어**
  국내 게임사도 식별자는 영어로 쓰며, Unity API와 섞였을 때 가독성이 떨어지기 때문이다.

### 커밋

시스템이 하나 끝날 때마다 커밋한다. 작업 덩어리 끝에 몰아서 하지 않는다.
모든 커밋은 그 자체로 빌드되고 테스트가 통과해야 한다.
Unity 애셋과 짝이 되는 `.meta`는 **항상 같은 커밋에** 넣는다. `.meta` 없이 스크립트만
커밋하면 다른 머신에서 GUID가 새로 생겨 씬·프리팹 참조가 깨진다.

### 검증

EditMode 테스트는 헤들리스로 돌린다. 에디터가 프로젝트를 잠그므로 먼저 닫아야 한다.

```bash
"C:/Program Files/Unity/Hub/Editor/6000.3.9f1/Editor/Unity.exe" -batchmode -projectPath "C:/Users/pc/Desktop/Unity2D-Mobile-Idle-game" -runTests -testPlatform EditMode -testResults "Logs/editmode-results.xml" -logFile -
```

### 대원칙 2번을 이 프로젝트에 적용할 때

포트폴리오 프로젝트이므로 일부 인프라(BigNumber, TickManager, ObjectPool)는 수단이 아니라
**산출물 자체**다. 이 예외는 좁게 적용한다. `Docs/GDD.md` §4.2에 명시된 항목에만 해당하며,
그 밖의 모든 것은 대원칙 2번을 그대로 따른다 — 호출자가 생겼을 때 만든다.
