# DoSession

A generic Undo/Redo system based on the Command pattern. Supports undo and redo for any kind of operation.

[한국어](README.md)

---

## Features

- **Session-based** — Create independent instances per system or share across related systems
- **Command pattern** — Extend freely via `IDoCommand` interface
- **Lambda helper** — Register simple operations without creating a class
- **Composite commands** — Group multiple operations into a single undo unit with `BeginGroup`/`EndGroup`
- **Irreversible barrier** — Commands with `CanUndo = false` automatically block further undo
- **Pure C#** — Works in both editor and runtime

## Structure

```
DoSession/
  IDoCommand.cs              — Command interface
  IDoSession.cs              — Session interface (for DI/testing)
  DoSession.cs               — Main session class
  Command/
    DoLambdaCommand.cs       — Lambda-based command (internal)
    DoGroupCommand.cs        — Composite command (internal)
    DoPropertyCommand.cs     — Property change command
    DoCollectionCommand.cs   — ICollection<T> Add/Remove command
    DoDictionaryCommand.cs   — IDictionary<TKey, TValue> Add/Remove command
```

## Usage

### Basic — Custom Command

Implement `IDoCommand` to define an operation.

```csharp
public class MoveCommand : IDoCommand
{
    private readonly Transform target;
    private readonly Vector3 oldPos;
    private readonly Vector3 newPos;

    public string Desc => "Move object";
    public bool CanUndo => target != null;

    public MoveCommand(Transform target, Vector3 newPos)
    {
        this.target = target;
        this.oldPos = target.position;
        this.newPos = newPos;
    }

    public void Do() => target.position = newPos;
    public void Undo() => target.position = oldPos;
}

var session = new DoSession();
session.Do(new MoveCommand(transform, new Vector3(1, 2, 3)));
session.Undo();  // Back to original position
session.Redo();  // Back to (1, 2, 3)
```

### Lambda Helper

Register simple operations without creating a command class.

```csharp
var old = transform.position;
var next = new Vector3(1, 2, 3);

session.Do(
    () => transform.position = next,
    () => transform.position = old,
    "Change position"
);
```

### Property Change

Pass getter/setter lambdas to automatically capture the previous value.

```csharp
session.Do(new DoPropertyCommand<Transform, Vector3>(
    transform,
    t => t.position,
    (t, v) => t.position = v,
    new Vector3(1, 2, 3),
    "Change position"
));
```

### Collection Add/Remove

Works with any collection implementing `ICollection<T>` (List, HashSet, LinkedList, SortedSet, etc.).

```csharp
var list = new List<string>();

session.Do(DoCollectionCommand<string>.Add(list, "item", "Add item"));
session.Do(DoCollectionCommand<string>.Remove(list, "item", "Remove item"));
```

### Dictionary Add/Remove

Works with any dictionary implementing `IDictionary<TKey, TValue>` (Dictionary, SortedDictionary, etc.).

```csharp
var dict = new Dictionary<string, int>();

session.Do(DoDictionaryCommand<string, int>.Add(dict, "health", 100, "Add health"));
session.Do(DoDictionaryCommand<string, int>.Remove(dict, "health", "Remove health"));
```

### Composite Commands (Group)

Group multiple operations into a single undo unit.

```csharp
session.BeginGroup("Full reset");

session.Do(() => hp = 100, () => hp = oldHp, "Reset health");
session.Do(() => mp = 50, () => mp = oldMp, "Reset mana");

session.EndGroup();

session.Undo();  // Both health and mana revert
```

### Independent Sessions

Create separate sessions per system to isolate history.

```csharp
var editorSession = new DoSession { MaxSize = 200 };
var inventorySession = new DoSession { MaxSize = 50 };
```

### History Queries

```csharp
session.PeekUndo;      // Next undo target (null if empty)
session.PeekRedo;      // Next redo target
session.UndoHistory;   // Full undo list (IReadOnlyList)
session.RedoHistory;   // Full redo list
session.UndoCount;     // Undo stack size
session.RedoCount;     // Redo stack size
```

### Events

```csharp
session.OnDo += cmd => Debug.Log($"Executed: {cmd.Desc}");
session.OnUndo += cmd => Debug.Log($"Undone: {cmd.Desc}");
session.OnRedo += cmd => Debug.Log($"Redone: {cmd.Desc}");
session.OnChange += () => Debug.Log("History changed");
```

## Design Summary

| Item | Detail |
|------|--------|
| Pattern | Command |
| Redo policy | Clear entire redo stack on new Do |
| Irreversible handling | `IDoCommand.CanUndo` returns false = barrier (stack preserved, undo blocked) |
| Serialization | Runtime only (none) |
| Threading | Main thread only |
| History limit | `MaxSize` (default 100) |

## Namespace

```csharp
using inonego.DoSession;
```
