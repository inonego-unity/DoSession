using System;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> Interface exposing only the core API of DoSession.
   /// <br/> Intended for DI, testing, and mocking.
   /// </summary>
   // ============================================================
   public interface IDoSession
   {

      // ------------------------------------------------------------
      /// <summary>
      /// Executes the command and pushes it onto the undo stack.
      /// </summary>
      // ------------------------------------------------------------
      void Do(IDoCommand command);

      // ------------------------------------------------------------
      /// <summary>
      /// Undoes the last operation. Returns true on success.
      /// </summary>
      // ------------------------------------------------------------
      bool Undo();

      // ------------------------------------------------------------
      /// <summary>
      /// Redoes the last undone operation. Returns true on success.
      /// </summary>
      // ------------------------------------------------------------
      bool Redo();

      // ------------------------------------------------------------
      /// <summary>
      /// Whether undo is possible.
      /// </summary>
      // ------------------------------------------------------------
      bool CanUndo { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// Whether redo is possible.
      /// </summary>
      // ------------------------------------------------------------
      bool CanRedo { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// Raised when the history changes.
      /// </summary>
      // ------------------------------------------------------------
      event Action OnChange;

   }

}
