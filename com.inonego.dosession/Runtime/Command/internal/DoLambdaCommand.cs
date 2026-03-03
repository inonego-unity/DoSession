using System;

namespace inonego.DoSession
{

   // ===============================================================================
   /// <summary>
   /// <br/> Lambda-based command.
   /// <br/> Used internally by the DoSession.Do(Action, Action, string) helper.
   /// </summary>
   // ===============================================================================
   internal class DoLambdaCommand : IDoCommand
   {

   #region Fields

      private Action doAction = null;
      private Action undoAction = null;
      private Func<bool> canUndo = null;
      private string desc = null;

      // ----------------------------------------------------------------------
      /// <summary>
      /// Whether undo is possible. Always true if canUndo is null.
      /// </summary>
      // ----------------------------------------------------------------------
      public bool CanUndo => canUndo?.Invoke() ?? true;

      // ------------------------------------------------------------
      /// <summary>
      /// Operation description.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc => desc;

   #endregion

   #region Constructor

      public DoLambdaCommand(Action doAction, Action undoAction, string desc, Func<bool> canUndo = null)
      {
         this.doAction = doAction;
         this.undoAction = undoAction;
         this.canUndo = canUndo;
         this.desc = desc;
      }

   #endregion

   #region Methods

      public void Do() => doAction();
      public void Undo() => undoAction();

   #endregion

   }

}
