using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using inonego.DoSession;

// ============================================================
/// <summary>
/// DoSession full system tests.
/// </summary>
// ============================================================
public class DoSessionTEST
{

#region Helpers

   // ============================================================
   /// <summary>
   /// Serializable test command. Increments/decrements a value.
   /// </summary>
   // ============================================================
   [Serializable]
   private class TestCommand : IDoCommand
   {
      public int Value = 0;
      public bool CanUndo { get; set; } = true;
      public string Desc => "Test";

      public void Do() => Value++;
      public void Undo() => Value--;
   }

   private class TestTarget
   {
      public int Health = 0;
   }

#endregion

#region Do / Undo / Redo

   // ------------------------------------------------------------
   /// <summary>
   /// Do pushes command onto the undo stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_01_Do_PushesToUndoStack()
   {
      var session = new DoSession();
      var cmd = new TestCommand();

      session.Do(cmd);

      Assert.AreEqual(1, session.UndoCount);
      Assert.IsTrue(session.CanUndo);
      Assert.AreEqual(1, cmd.Value);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Undo reverts the command and moves it to redo stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_02_Undo_RevertsCommand()
   {
      var session = new DoSession();
      var cmd = new TestCommand();

      session.Do(cmd);
      var result = session.Undo();

      Assert.IsTrue(result);
      Assert.AreEqual(0, cmd.Value);
      Assert.AreEqual(0, session.UndoCount);
      Assert.AreEqual(1, session.RedoCount);
   }

   // -----------------------------------------------------------------
   /// <summary>
   /// Redo re-executes the command and moves it back to undo stack.
   /// </summary>
   // -----------------------------------------------------------------
   [Test]
   public void DoSession_03_Redo_ReExecutesCommand()
   {
      var session = new DoSession();
      var cmd = new TestCommand();

      session.Do(cmd);
      session.Undo();
      var result = session.Redo();

      Assert.IsTrue(result);
      Assert.AreEqual(1, cmd.Value);
      Assert.AreEqual(1, session.UndoCount);
      Assert.AreEqual(0, session.RedoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// New Do after Undo clears the redo stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_04_Do_ClearsRedoStack()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Undo();
      Assert.AreEqual(1, session.RedoCount);

      session.Do(new TestCommand());
      Assert.AreEqual(0, session.RedoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Undo/Redo on empty stacks returns false.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_05_EmptyStack_ReturnsFalse()
   {
      var session = new DoSession();

      Assert.IsFalse(session.Undo());
      Assert.IsFalse(session.Redo());
   }

#endregion

#region Composite Commands

   // ------------------------------------------------------------
   /// <summary>
   /// Grouped commands are undone as a single unit.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_06_Group_SingleUndo()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.BeginGroup("Group");
      session.Do(cmd1);
      session.Do(cmd2);
      session.EndGroup();

      Assert.AreEqual(1, cmd1.Value);
      Assert.AreEqual(1, cmd2.Value);
      Assert.AreEqual(1, session.UndoCount);

      session.Undo();

      Assert.AreEqual(0, cmd1.Value);
      Assert.AreEqual(0, cmd2.Value);
      Assert.AreEqual(0, session.UndoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Group Redo re-executes all grouped commands.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_07_Group_Redo()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.BeginGroup("Group");
      session.Do(cmd1);
      session.Do(cmd2);
      session.EndGroup();

      session.Undo();
      session.Redo();

      Assert.AreEqual(1, cmd1.Value);
      Assert.AreEqual(1, cmd2.Value);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Nested BeginGroup throws InvalidOperationException.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_08_Group_NestedThrows()
   {
      var session = new DoSession();

      session.BeginGroup("1");

      Assert.Throws<InvalidOperationException>(() => session.BeginGroup("2"));
   }

   // ------------------------------------------------------------
   /// <summary>
   /// EndGroup without BeginGroup throws InvalidOperationException.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_09_EndGroup_WithoutBeginThrows()
   {
      var session = new DoSession();

      Assert.Throws<InvalidOperationException>(() => session.EndGroup());
   }

#endregion

#region MaxSize

   // ------------------------------------------------------------
   /// <summary>
   /// Oldest command is removed when MaxSize is exceeded.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_10_MaxSize_RemovesOldest()
   {
      var session = new DoSession { MaxSize = 3 };

      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Do(new TestCommand());

      Assert.AreEqual(3, session.UndoCount);
   }

#endregion

#region Lambda Helper

   // ------------------------------------------------------------
   /// <summary>
   /// Lambda helper Do/Undo/Redo works correctly.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_11_Lambda_DoUndoRedo()
   {
      var session = new DoSession();
      int value = 0;

      session.Do(() => value++, () => value--, "Increment");

      Assert.AreEqual(1, value);

      session.Undo();
      Assert.AreEqual(0, value);

      session.Redo();
      Assert.AreEqual(1, value);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Lambda canUndo callback acts as a barrier.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_12_Lambda_CanUndoBarrier()
   {
      var session = new DoSession();
      int value = 0;
      bool canUndo = true;

      session.Do(() => value++, () => value--, "Increment", () => canUndo);
      Assert.IsTrue(session.CanUndo);

      canUndo = false;
      Assert.IsFalse(session.CanUndo);
      Assert.IsFalse(session.Undo());
   }

#endregion

#region DoPropertyCommand

   // -----------------------------------------------------------------
   /// <summary>
   /// DoPropertyCommand captures old value and restores on Undo.
   /// </summary>
   // -----------------------------------------------------------------
   [Test]
   public void DoSession_13_PropertyCommand_CaptureAndRestore()
   {
      var target = new TestTarget { Health = 100 };
      var session = new DoSession();

      session.Do(new DoPropertyCommand<TestTarget, int>(
         target, t => t.Health, (t, v) => t.Health = v, 50, "Change health"));

      Assert.AreEqual(50, target.Health);

      session.Undo();
      Assert.AreEqual(100, target.Health);

      session.Redo();
      Assert.AreEqual(50, target.Health);
   }

#endregion

#region DoCollectionCommand

   // ------------------------------------------------------------
   /// <summary>
   /// DoCollectionCommand Add and Undo works correctly.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_14_CollectionCommand_AddUndo()
   {
      var list = new List<string>();
      var session = new DoSession();

      session.Do(DoCollectionCommand<string>.Add(list, "A", "Add"));
      Assert.AreEqual(1, list.Count);
      Assert.AreEqual("A", list[0]);

      session.Undo();
      Assert.AreEqual(0, list.Count);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoCollectionCommand Remove and Undo works correctly.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_15_CollectionCommand_RemoveUndo()
   {
      var list = new List<string> { "A", "B" };
      var session = new DoSession();

      session.Do(DoCollectionCommand<string>.Remove(list, "A", "Remove"));
      Assert.AreEqual(1, list.Count);
      Assert.AreEqual("B", list[0]);

      session.Undo();
      Assert.AreEqual(2, list.Count);
      Assert.IsTrue(list.Contains("A"));
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoCollectionCommand works with HashSet.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_16_CollectionCommand_HashSet()
   {
      var set = new HashSet<int>();
      var session = new DoSession();

      session.Do(DoCollectionCommand<int>.Add(set, 42, "Add"));
      Assert.IsTrue(set.Contains(42));

      session.Undo();
      Assert.IsFalse(set.Contains(42));
   }

#endregion

#region DoDictionaryCommand

   // ------------------------------------------------------------
   /// <summary>
   /// DoDictionaryCommand Add and Undo works correctly.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_17_DictionaryCommand_AddUndo()
   {
      var dict = new Dictionary<string, int>();
      var session = new DoSession();

      session.Do(DoDictionaryCommand<string, int>.Add(dict, "HP", 100, "Add"));
      Assert.AreEqual(100, dict["HP"]);

      session.Undo();
      Assert.IsFalse(dict.ContainsKey("HP"));
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoDictionaryCommand Remove and Undo restores original value.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_18_DictionaryCommand_RemoveUndo()
   {
      var dict = new Dictionary<string, int> { { "HP", 100 } };
      var session = new DoSession();

      session.Do(DoDictionaryCommand<string, int>.Remove(dict, "HP", "Remove"));
      Assert.IsFalse(dict.ContainsKey("HP"));

      session.Undo();
      Assert.AreEqual(100, dict["HP"]);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoDictionaryCommand works with SortedDictionary.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_19_DictionaryCommand_SortedDictionary()
   {
      var dict = new SortedDictionary<string, int>();
      var session = new DoSession();

      session.Do(DoDictionaryCommand<string, int>.Add(dict, "MP", 50, "Add"));
      Assert.AreEqual(50, dict["MP"]);

      session.Undo();
      Assert.IsFalse(dict.ContainsKey("MP"));
   }

#endregion

#region Events

   // ------------------------------------------------------------
   /// <summary>
   /// Do/Undo/Redo raise correct events.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_20_Events_RaisedCorrectly()
   {
      var session = new DoSession();

      int doCount = 0;
      int undoCount = 0;
      int redoCount = 0;
      int changeCount = 0;

      session.OnDo += _ => doCount++;
      session.OnUndo += _ => undoCount++;
      session.OnRedo += _ => redoCount++;
      session.OnChange += () => changeCount++;

      session.Do(new TestCommand());
      Assert.AreEqual(1, doCount);
      Assert.AreEqual(1, changeCount);

      session.Undo();
      Assert.AreEqual(1, undoCount);
      Assert.AreEqual(2, changeCount);

      session.Redo();
      Assert.AreEqual(1, redoCount);
      Assert.AreEqual(3, changeCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Group events fire only at EndGroup.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_21_Group_EventAtEndGroup()
   {
      var session = new DoSession();
      int doCount = 0;
      session.OnDo += _ => doCount++;

      session.BeginGroup("Group");
      session.Do(new TestCommand());
      session.Do(new TestCommand());

      Assert.AreEqual(0, doCount);

      session.EndGroup();

      Assert.AreEqual(1, doCount);
   }

#endregion

#region Peek

   // ------------------------------------------------------------
   /// <summary>
   /// PeekUndo returns top without modifying stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_22_PeekUndo_NoModification()
   {
      var session = new DoSession();
      var cmd = new TestCommand();

      session.Do(cmd);

      Assert.AreSame(cmd, session.PeekUndo);
      Assert.AreEqual(1, session.UndoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// PeekRedo returns top without modifying stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_23_PeekRedo_NoModification()
   {
      var session = new DoSession();
      var cmd = new TestCommand();

      session.Do(cmd);
      session.Undo();

      Assert.AreSame(cmd, session.PeekRedo);
      Assert.AreEqual(1, session.RedoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Peek on empty stack returns null.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_24_Peek_EmptyReturnsNull()
   {
      var session = new DoSession();

      Assert.IsNull(session.PeekUndo);
      Assert.IsNull(session.PeekRedo);
   }

#endregion

#region History

   // ------------------------------------------------------------
   /// <summary>
   /// UndoHistory returns commands in order.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_25_UndoHistory_InOrder()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.Do(cmd1);
      session.Do(cmd2);

      var history = session.UndoHistory;

      Assert.AreEqual(2, history.Count);
      Assert.AreSame(cmd1, history[0]);
      Assert.AreSame(cmd2, history[1]);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// RedoHistory returns commands correctly.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_26_RedoHistory_Correct()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Undo();
      session.Undo();

      Assert.AreEqual(2, session.RedoHistory.Count);
   }

#endregion

#region Clear

   // ------------------------------------------------------------
   /// <summary>
   /// ClearUndo only clears the undo stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_27_ClearUndo_OnlyUndoStack()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Undo();

      session.ClearUndo();

      Assert.AreEqual(0, session.UndoCount);
      Assert.AreEqual(1, session.RedoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// ClearRedo only clears the redo stack.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_28_ClearRedo_OnlyRedoStack()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Undo();

      session.ClearRedo();

      Assert.AreEqual(1, session.UndoCount);
      Assert.AreEqual(0, session.RedoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// Clear removes both stacks.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_29_Clear_BothStacks()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Undo();

      session.Clear();

      Assert.AreEqual(0, session.UndoCount);
      Assert.AreEqual(0, session.RedoCount);
   }

#endregion

#region Barrier (CanUndo == false)

   // ------------------------------------------------------------
   /// <summary>
   /// CanUndo false acts as a barrier.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_30_CanUndo_Barrier()
   {
      var session = new DoSession();
      var barrier = new TestCommand { CanUndo = false };

      session.Do(new TestCommand());
      session.Do(barrier);

      Assert.IsFalse(session.CanUndo);
      Assert.IsFalse(session.Undo());
      Assert.AreEqual(2, session.UndoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// GroupCommand CanUndo checks all children.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_31_Group_CanUndoChecksChildren()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.BeginGroup("Group");
      session.Do(cmd1);
      session.Do(cmd2);
      session.EndGroup();

      Assert.IsTrue(session.CanUndo);

      cmd2.CanUndo = false;

      Assert.IsFalse(session.CanUndo);
      Assert.IsFalse(session.Undo());
   }

#endregion

#region Null Validation

   // ------------------------------------------------------------
   /// <summary>
   /// Null command throws ArgumentNullException.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_32_NullCommand_Throws()
   {
      var session = new DoSession();

      Assert.Throws<ArgumentNullException>(() => session.Do((IDoCommand)null));
   }

#endregion

}
