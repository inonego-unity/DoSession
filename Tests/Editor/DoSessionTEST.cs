using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using inonego.DoSession;

// ============================================================
/// <summary>
/// DoSession 시스템 전체 테스트.
/// </summary>
// ============================================================
public class DoSessionTEST
{

#region 헬퍼

   // ============================================================
   /// <summary>
   /// 테스트용 간단한 Command. 값을 증감시킨다.
   /// </summary>
   // ============================================================
   private class TestCommand : IDoCommand
   {
      public int Value = 0;
      public bool CanUndo { get; set; } = true;
      public string Desc => "테스트";

      public void Do() => Value++;
      public void Undo() => Value--;
   }

   private class TestTarget
   {
      public int Health = 0;
   }

#endregion

#region Do / Undo / Redo 기본

   // ------------------------------------------------------------
   /// <summary>
   /// Do 실행 후 UndoStack에 추가되는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_01_Do_스택_추가()
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
   /// Undo 실행 후 Command가 되돌려지는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_02_Undo_되돌리기()
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

   // ------------------------------------------------------------
   /// <summary>
   /// Redo 실행 후 Command가 재실행되는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_03_Redo_다시실행()
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
   /// Undo 후 새 Do 시 Redo 스택이 삭제되는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_04_Do_후_Redo스택_삭제()
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
   /// 빈 스택에서 Undo/Redo 시 false를 반환하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_05_빈_스택_Undo_Redo()
   {
      var session = new DoSession();

      Assert.IsFalse(session.Undo());
      Assert.IsFalse(session.Redo());
   }

#endregion

#region 복합 명령

   // ------------------------------------------------------------
   /// <summary>
   /// BeginGroup/EndGroup으로 묶인 Command들이 하나의 Undo 단위로 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_06_그룹_단일_Undo()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.BeginGroup("그룹");
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
   /// 그룹 Undo 후 Redo가 정상 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_07_그룹_Redo()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.BeginGroup("그룹");
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
   /// 중첩 BeginGroup 시 예외가 발생하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_08_그룹_중첩_예외()
   {
      var session = new DoSession();

      session.BeginGroup("1");

      Assert.Throws<InvalidOperationException>(() => session.BeginGroup("2"));
   }

   // ------------------------------------------------------------
   /// <summary>
   /// BeginGroup 없이 EndGroup 호출 시 예외가 발생하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_09_EndGroup_없는_Begin_예외()
   {
      var session = new DoSession();

      Assert.Throws<InvalidOperationException>(() => session.EndGroup());
   }

#endregion

#region MaxSize

   // ------------------------------------------------------------
   /// <summary>
   /// MaxSize 초과 시 가장 오래된 Command가 제거되는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_10_MaxSize_초과_제거()
   {
      var session = new DoSession { MaxSize = 3 };

      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Do(new TestCommand());

      Assert.AreEqual(3, session.UndoCount);
   }

#endregion

#region 람다 헬퍼

   // ------------------------------------------------------------
   /// <summary>
   /// 람다 헬퍼 Do/Undo/Redo가 정상 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_11_람다_헬퍼()
   {
      var session = new DoSession();
      int value = 0;

      session.Do(() => value++, () => value--, "증가");

      Assert.AreEqual(1, value);

      session.Undo();
      Assert.AreEqual(0, value);

      session.Redo();
      Assert.AreEqual(1, value);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// 람다 헬퍼의 canUndo 콜백이 장벽으로 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_12_람다_CanUndo_장벽()
   {
      var session = new DoSession();
      int value = 0;
      bool canUndo = true;

      session.Do(() => value++, () => value--, "증가", () => canUndo);
      Assert.IsTrue(session.CanUndo);

      canUndo = false;
      Assert.IsFalse(session.CanUndo);
      Assert.IsFalse(session.Undo());
   }

#endregion

#region DoPropertyCommand

   // ------------------------------------------------------------
   /// <summary>
   /// DoPropertyCommand가 이전 값을 캡처하고 Undo 시 복원하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_13_PropertyCommand_값_캡처_복원()
   {
      var target = new TestTarget { Health = 100 };
      var session = new DoSession();

      session.Do(new DoPropertyCommand<TestTarget, int>(
         target, t => t.Health, (t, v) => t.Health = v, 50, "체력 변경"));

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
   /// DoCollectionCommand의 Add/Undo가 정상 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_14_CollectionCommand_Add_Undo()
   {
      var list = new List<string>();
      var session = new DoSession();

      session.Do(DoCollectionCommand<string>.Add(list, "A", "추가"));
      Assert.AreEqual(1, list.Count);
      Assert.AreEqual("A", list[0]);

      session.Undo();
      Assert.AreEqual(0, list.Count);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoCollectionCommand의 Remove/Undo가 정상 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_15_CollectionCommand_Remove_Undo()
   {
      var list = new List<string> { "A", "B" };
      var session = new DoSession();

      session.Do(DoCollectionCommand<string>.Remove(list, "A", "제거"));
      Assert.AreEqual(1, list.Count);
      Assert.AreEqual("B", list[0]);

      session.Undo();
      Assert.AreEqual(2, list.Count);
      Assert.IsTrue(list.Contains("A"));
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoCollectionCommand가 HashSet에서도 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_16_CollectionCommand_HashSet()
   {
      var set = new HashSet<int>();
      var session = new DoSession();

      session.Do(DoCollectionCommand<int>.Add(set, 42, "추가"));
      Assert.IsTrue(set.Contains(42));

      session.Undo();
      Assert.IsFalse(set.Contains(42));
   }

#endregion

#region DoDictionaryCommand

   // ------------------------------------------------------------
   /// <summary>
   /// DoDictionaryCommand의 Add/Undo가 정상 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_17_DictionaryCommand_Add_Undo()
   {
      var dict = new Dictionary<string, int>();
      var session = new DoSession();

      session.Do(DoDictionaryCommand<string, int>.Add(dict, "HP", 100, "추가"));
      Assert.AreEqual(100, dict["HP"]);

      session.Undo();
      Assert.IsFalse(dict.ContainsKey("HP"));
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoDictionaryCommand의 Remove/Undo가 원래 값으로 복원되는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_18_DictionaryCommand_Remove_Undo()
   {
      var dict = new Dictionary<string, int> { { "HP", 100 } };
      var session = new DoSession();

      session.Do(DoDictionaryCommand<string, int>.Remove(dict, "HP", "제거"));
      Assert.IsFalse(dict.ContainsKey("HP"));

      session.Undo();
      Assert.AreEqual(100, dict["HP"]);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// DoDictionaryCommand가 SortedDictionary에서도 동작하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_19_DictionaryCommand_SortedDictionary()
   {
      var dict = new SortedDictionary<string, int>();
      var session = new DoSession();

      session.Do(DoDictionaryCommand<string, int>.Add(dict, "MP", 50, "추가"));
      Assert.AreEqual(50, dict["MP"]);

      session.Undo();
      Assert.IsFalse(dict.ContainsKey("MP"));
   }

#endregion

#region 이벤트

   // ------------------------------------------------------------
   /// <summary>
   /// Do/Undo/Redo 시 이벤트가 정상 발생하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_20_이벤트_발행()
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
   /// 그룹 EndGroup 시점에만 이벤트가 발생하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_21_그룹_이벤트_EndGroup_시점()
   {
      var session = new DoSession();
      int doCount = 0;
      session.OnDo += _ => doCount++;

      session.BeginGroup("그룹");
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
   /// PeekUndo가 스택 변경 없이 top을 반환하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_22_PeekUndo_변경없이_반환()
   {
      var session = new DoSession();
      var cmd = new TestCommand();

      session.Do(cmd);

      Assert.AreSame(cmd, session.PeekUndo);
      Assert.AreEqual(1, session.UndoCount);
   }

   // ------------------------------------------------------------
   /// <summary>
   /// PeekRedo가 스택 변경 없이 top을 반환하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_23_PeekRedo_변경없이_반환()
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
   /// 빈 스택에서 Peek 시 null을 반환하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_24_Peek_빈_스택_null()
   {
      var session = new DoSession();

      Assert.IsNull(session.PeekUndo);
      Assert.IsNull(session.PeekRedo);
   }

#endregion

#region 히스토리

   // ------------------------------------------------------------
   /// <summary>
   /// UndoHistory가 전체 히스토리를 순서대로 반환하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_25_UndoHistory_순서_반환()
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
   /// RedoHistory가 정상 반환되는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_26_RedoHistory_반환()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Do(new TestCommand());
      session.Undo();
      session.Undo();

      Assert.AreEqual(2, session.RedoHistory.Count);
   }

#endregion

#region 부분 Clear

   // ------------------------------------------------------------
   /// <summary>
   /// ClearUndo가 Undo 스택만 비우는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_27_ClearUndo_Undo만_삭제()
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
   /// ClearRedo가 Redo 스택만 비우는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_28_ClearRedo_Redo만_삭제()
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
   /// Clear가 양쪽 스택을 모두 비우는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_29_Clear_양쪽_삭제()
   {
      var session = new DoSession();

      session.Do(new TestCommand());
      session.Undo();

      session.Clear();

      Assert.AreEqual(0, session.UndoCount);
      Assert.AreEqual(0, session.RedoCount);
   }

#endregion

#region 장벽 (CanUndo == false)

   // ------------------------------------------------------------
   /// <summary>
   /// CanUndo가 false인 Command가 장벽 역할을 하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_30_CanUndo_장벽()
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
   /// GroupCommand의 CanUndo가 모든 자식을 체크하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_31_그룹_CanUndo_자식_체크()
   {
      var session = new DoSession();
      var cmd1 = new TestCommand();
      var cmd2 = new TestCommand();

      session.BeginGroup("그룹");
      session.Do(cmd1);
      session.Do(cmd2);
      session.EndGroup();

      Assert.IsTrue(session.CanUndo);

      cmd2.CanUndo = false;

      Assert.IsFalse(session.CanUndo);
      Assert.IsFalse(session.Undo());
   }

#endregion

#region Null 검증

   // ------------------------------------------------------------
   /// <summary>
   /// null Command 전달 시 예외가 발생하는지 테스트합니다.
   /// </summary>
   // ------------------------------------------------------------
   [Test]
   public void DoSession_32_Null_Command_예외()
   {
      var session = new DoSession();

      Assert.Throws<ArgumentNullException>(() => session.Do((IDoCommand)null));
   }

#endregion

}
