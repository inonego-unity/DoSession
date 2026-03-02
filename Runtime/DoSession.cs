using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> 범용 Undo/Redo 세션.
   /// <br/> Command 패턴 기반으로 작업 히스토리를 관리한다.
   /// <br/> 시스템마다 독립 인스턴스를 생성하거나 공유할 수 있다.
   /// </summary>
   // ============================================================
   public class DoSession : IDoSession
   {

   #region 필드

      private readonly List<IDoCommand> undoStack = new List<IDoCommand>();
      private readonly List<IDoCommand> redoStack = new List<IDoCommand>();

      private bool isGrouping = false;
      private List<IDoCommand> groupBuffer = null;
      private string groupDesc = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 히스토리 최대 크기. 초과 시 가장 오래된 항목부터 제거.
      /// </summary>
      // ------------------------------------------------------------
      public int MaxSize { get; set; } = 100;

      // ------------------------------------------------------------
      /// <summary>
      /// Undo 가능 여부. 스택이 비어있거나 top의 CanUndo가 false면 불가.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => undoStack.Count > 0 && undoStack[undoStack.Count - 1].CanUndo;

      // ------------------------------------------------------------
      /// <summary>
      /// Redo 가능 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanRedo => redoStack.Count > 0;

      // ------------------------------------------------------------
      /// <summary>
      /// Undo 스택의 항목 수.
      /// </summary>
      // ------------------------------------------------------------
      public int UndoCount => undoStack.Count;

      // ------------------------------------------------------------
      /// <summary>
      /// Redo 스택의 항목 수.
      /// </summary>
      // ------------------------------------------------------------
      public int RedoCount => redoStack.Count;

      // ------------------------------------------------------------
      /// <summary>
      /// 다음 Undo 대상 Command. 없으면 null.
      /// </summary>
      // ------------------------------------------------------------
      public IDoCommand PeekUndo => undoStack.Count > 0 ? undoStack[undoStack.Count - 1] : null;

      // ------------------------------------------------------------
      /// <summary>
      /// 다음 Redo 대상 Command. 없으면 null.
      /// </summary>
      // ------------------------------------------------------------
      public IDoCommand PeekRedo => redoStack.Count > 0 ? redoStack[redoStack.Count - 1] : null;

      // ------------------------------------------------------------
      /// <summary>
      /// Undo 히스토리 목록. 인덱스가 클수록 최신.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<IDoCommand> UndoHistory => undoStack;

      // ------------------------------------------------------------
      /// <summary>
      /// Redo 히스토리 목록. 인덱스가 클수록 최신.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<IDoCommand> RedoHistory => redoStack;

   #endregion

   #region 이벤트

      // ------------------------------------------------------------
      /// <summary>
      /// Do 실행 후 발생.
      /// </summary>
      // ------------------------------------------------------------
      public event Action<IDoCommand> OnDo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// Undo 실행 후 발생.
      /// </summary>
      // ------------------------------------------------------------
      public event Action<IDoCommand> OnUndo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// Redo 실행 후 발생.
      /// </summary>
      // ------------------------------------------------------------
      public event Action<IDoCommand> OnRedo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 히스토리 변경 시 발생.
      /// </summary>
      // ------------------------------------------------------------
      public event Action OnChange = null;

   #endregion

   #region 핵심 API

      // ------------------------------------------------------------
      /// <summary>
      /// Command를 실행하고 Undo 스택에 추가한다.
      /// </summary>
      // ------------------------------------------------------------
      public void Do(IDoCommand command)
      {
         if (command == null)
         {
            throw new ArgumentNullException("command가 null입니다.");
         }

         // 그룹 모드에서는 버퍼에 누적하고 개별 실행만 수행
         if (isGrouping)
         {
            command.Do();
            groupBuffer.Add(command);
            return;
         }

         command.Do();

         undoStack.Add(command);
         redoStack.Clear();

         // MaxSize 초과 시 가장 오래된 항목 제거
         while (undoStack.Count > MaxSize)
         {
            undoStack.RemoveAt(0);
         }

         OnDo?.Invoke(command);
         OnChange?.Invoke();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// <br/> 람다 헬퍼. 간단한 작업을 Command 클래스 없이 실행한다.
      /// <br/> canUndo가 null이면 항상 되돌리기 가능.
      /// </summary>
      // ------------------------------------------------------------
      public void Do(Action doAction, Action undoAction, string desc, Func<bool> canUndo = null)
      {
         Do(new DoLambdaCommand(doAction, undoAction, desc, canUndo));
      }

      // ------------------------------------------------------------
      /// <summary>
      /// <br/> 마지막 작업을 되돌린다.
      /// <br/> top Command의 CanUndo가 false면 장벽으로 작동하여 false를 반환한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool Undo()
      {
         if (undoStack.Count == 0) return false;

         var cmd = undoStack[undoStack.Count - 1];

         // 장벽: CanUndo가 false면 스택을 건드리지 않고 실패
         if (!cmd.CanUndo) return false;

         undoStack.RemoveAt(undoStack.Count - 1);
         cmd.Undo();
         redoStack.Add(cmd);

         OnUndo?.Invoke(cmd);
         OnChange?.Invoke();

         return true;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 마지막으로 되돌린 작업을 다시 실행한다.
      /// </summary>
      // ------------------------------------------------------------
      public bool Redo()
      {
         if (redoStack.Count == 0) return false;

         var cmd = redoStack[redoStack.Count - 1];

         redoStack.RemoveAt(redoStack.Count - 1);
         cmd.Do();
         undoStack.Add(cmd);

         OnRedo?.Invoke(cmd);
         OnChange?.Invoke();

         return true;
      }

   #endregion

   #region 복합 명령

      // ------------------------------------------------------------
      /// <summary>
      /// 그룹 시작. 이후 Do() 호출은 그룹에 누적된다.
      /// </summary>
      // ------------------------------------------------------------
      public void BeginGroup(string desc)
      {
         if (isGrouping)
         {
            throw new InvalidOperationException("이미 그룹이 진행 중입니다.");
         }

         isGrouping = true;
         groupBuffer = new List<IDoCommand>();
         groupDesc = desc;
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 그룹 종료. 누적된 Command들을 GroupCommand로 묶어 스택에 추가.
      /// </summary>
      // ------------------------------------------------------------
      public void EndGroup()
      {
         if (!isGrouping)
         {
            throw new InvalidOperationException("진행 중인 그룹이 없습니다.");
         }

         isGrouping = false;

         if (groupBuffer.Count > 0)
         {
            var group = new DoGroupCommand(groupBuffer, groupDesc);

            undoStack.Add(group);
            redoStack.Clear();

            // MaxSize 초과 시 가장 오래된 항목 제거
            while (undoStack.Count > MaxSize)
            {
               undoStack.RemoveAt(0);
            }

            OnDo?.Invoke(group);
            OnChange?.Invoke();
         }

         groupBuffer = null;
         groupDesc = null;
      }

   #endregion

   #region 관리

      // ------------------------------------------------------------
      /// <summary>
      /// Undo 스택과 Redo 스택을 모두 비운다.
      /// </summary>
      // ------------------------------------------------------------
      public void Clear()
      {
         undoStack.Clear();
         redoStack.Clear();

         OnChange?.Invoke();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Undo 스택만 비운다.
      /// </summary>
      // ------------------------------------------------------------
      public void ClearUndo()
      {
         undoStack.Clear();

         OnChange?.Invoke();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Redo 스택만 비운다.
      /// </summary>
      // ------------------------------------------------------------
      public void ClearRedo()
      {
         redoStack.Clear();

         OnChange?.Invoke();
      }

   #endregion

   }

}
