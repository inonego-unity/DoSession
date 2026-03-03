<div align="center">

# DoSession

![Unity](https://img.shields.io/badge/Unity-000000?logo=unity&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?logo=csharp&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-blue)

[English](README.md) | [한국어](README_KO.md)

</div>

---

범용 Undo/Redo 시스템입니다. Command 패턴 기반으로 어떤 종류의 작업이든 되돌리기/다시하기를 지원합니다.

## 특징

- **세션 기반** — 시스템마다 독립 인스턴스를 생성하거나 공유할 수 있습니다
- **Command 패턴** — `IDoCommand` 인터페이스로 자유롭게 확장할 수 있습니다
- **람다 헬퍼** — 클래스 없이 간단한 작업을 즉시 등록할 수 있습니다
- **복합 명령** — `BeginGroup`/`EndGroup`으로 여러 작업을 하나의 Undo 단위로 묶을 수 있습니다
- **비가역 장벽** — `CanUndo`가 false인 Command가 자동으로 Undo를 차단합니다

## 구성

```
Runtime/
  IDoCommand.cs              — Command 인터페이스
  IDoSession.cs              — Session 인터페이스 (DI/테스트용)
  DoSession.cs               — 메인 세션 클래스
  Command/
    DoPropertyCommand.cs     — 프로퍼티 변경 전용
    DoCollectionCommand.cs   — ICollection<T> Add/Remove 전용
    DoDictionaryCommand.cs   — IDictionary<TKey, TValue> Add/Remove 전용
    internal/
      DoLambdaCommand.cs     — 람다 기반 Command (내부용)
      DoGroupCommand.cs      — 복합 Command (내부용)
```

## 사용법

### 기본 — 커스텀 Command

`IDoCommand`를 구현하여 작업을 정의합니다.

```csharp
public class MoveCommand : IDoCommand
{
    private Transform target;
    private Vector3 oldPos;
    private Vector3 newPos;
    private string desc;

    public string Desc => desc;
    public bool CanUndo => target != null;

    public MoveCommand(Transform target, Vector3 newPos)
    {
        this.target = target;
        this.oldPos = target.position;
        this.newPos = newPos;
        this.desc = "오브젝트 이동";
    }

    public void Do() => target.position = newPos;
    public void Undo() => target.position = oldPos;
}

var session = new DoSession();
session.Do(new MoveCommand(transform, new Vector3(1, 2, 3)));
session.Undo();  // 원래 위치로 돌아갑니다
session.Redo();  // 다시 (1, 2, 3)으로 이동합니다
```

### 람다 헬퍼

간단한 작업은 클래스 없이 람다로 등록할 수 있습니다.

```csharp
var old = transform.position;
var next = new Vector3(1, 2, 3);

session.Do(
    () => transform.position = next,
    () => transform.position = old,
    "위치 변경"
);
```

### 프로퍼티 변경

getter/setter 람다를 넘기면 이전 값을 자동으로 캡처합니다.

```csharp
session.Do(new DoPropertyCommand<Transform, Vector3>(
    transform,
    t => t.position,
    (t, v) => t.position = v,
    new Vector3(1, 2, 3),
    "위치 변경"
));
```

### 컬렉션 Add/Remove

`ICollection<T>`를 구현하는 모든 컬렉션(List, HashSet, LinkedList, SortedSet 등)에서 사용할 수 있습니다.

```csharp
var list = new List<string>();

session.Do(DoCollectionCommand<string>.Add(list, "항목", "항목 추가"));
session.Do(DoCollectionCommand<string>.Remove(list, "항목", "항목 제거"));
```

### 딕셔너리 Add/Remove

`IDictionary<TKey, TValue>`를 구현하는 모든 딕셔너리(Dictionary, SortedDictionary 등)에서 사용할 수 있습니다.

```csharp
var dict = new Dictionary<string, int>();

session.Do(DoDictionaryCommand<string, int>.Add(dict, "체력", 100, "체력 추가"));
session.Do(DoDictionaryCommand<string, int>.Remove(dict, "체력", "체력 제거"));
```

### 복합 명령 (Group)

여러 작업을 하나의 Undo 단위로 묶을 수 있습니다.

```csharp
session.BeginGroup("전체 초기화");

session.Do(() => hp = 100, () => hp = oldHp, "체력 초기화");
session.Do(() => mp = 50, () => mp = oldMp, "마나 초기화");

session.EndGroup();

session.Undo();  // 체력, 마나 모두 원래 값으로 되돌아갑니다
```

### 독립 세션

시스템마다 별도 세션을 생성하여 히스토리를 분리할 수 있습니다.

```csharp
var editorSession = new DoSession { MaxSize = 200 };
var inventorySession = new DoSession { MaxSize = 50 };
```

### 히스토리 조회

```csharp
session.PeekUndo;      // 다음 Undo 대상 (null이면 없음)
session.PeekRedo;      // 다음 Redo 대상
session.UndoHistory;   // 전체 Undo 목록 (IReadOnlyList)
session.RedoHistory;   // 전체 Redo 목록
session.UndoCount;     // Undo 스택 크기
session.RedoCount;     // Redo 스택 크기
```

### 이벤트

```csharp
session.OnDo += cmd => Debug.Log($"실행: {cmd.Desc}");
session.OnUndo += cmd => Debug.Log($"되돌림: {cmd.Desc}");
session.OnRedo += cmd => Debug.Log($"다시실행: {cmd.Desc}");
session.OnChange += () => Debug.Log("히스토리가 변경되었습니다");
```

## 설계 요약

| 항목 | 내용 |
|------|------|
| 패턴 | Command |
| Redo 정책 | 새 작업 시 Redo 스택 전체 삭제 |
| 비가역 처리 | `IDoCommand.CanUndo`가 false이면 장벽으로 동작합니다 (스택 유지, Undo 불가) |
| 스레드 | 메인 스레드 전용 |
| 히스토리 제한 | `MaxSize` (기본 100) |

## 네임스페이스

```csharp
using inonego.DoSession;
```
