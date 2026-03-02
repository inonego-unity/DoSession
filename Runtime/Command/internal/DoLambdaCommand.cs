using System;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> 람다 기반 Command.
   /// <br/> DoSession.Do(Action, Action, string) 헬퍼에서 내부적으로 사용.
   /// </summary>
   // ============================================================
   internal class DoLambdaCommand : IDoCommand
   {

   #region 필드

      private readonly Action doAction = null;
      private readonly Action undoAction = null;
      private readonly Func<bool> canUndo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 되돌리기 가능 여부. canUndo가 null이면 항상 true.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => canUndo?.Invoke() ?? true;

      // ------------------------------------------------------------
      /// <summary>
      /// 작업 설명.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc { get; }

   #endregion

   #region 생성자

      public DoLambdaCommand(Action doAction, Action undoAction, string desc, Func<bool> canUndo = null)
      {
         this.doAction = doAction;
         this.undoAction = undoAction;
         this.canUndo = canUndo;

         Desc = desc;
      }

   #endregion

   #region 메서드

      public void Do() => doAction();
      public void Undo() => undoAction();

   #endregion

   }

}
