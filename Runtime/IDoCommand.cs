using System;

namespace inonego.DoSession
{

   // ====================================================================
   /// <summary>
   /// <br/> Command interface defining the execute/undo contract.
   /// <br/> All undoable operations must implement this interface.
   /// </summary>
   // ====================================================================
   public interface IDoCommand
   {

      // ------------------------------------------------------------
      /// <summary>
      /// Executes the operation.
      /// </summary>
      // ------------------------------------------------------------
      void Do();

      // ------------------------------------------------------------
      /// <summary>
      /// Reverts the operation. Inverse of Do().
      /// </summary>
      // ------------------------------------------------------------
      void Undo();

      // ------------------------------------------------------------
      /// <summary>
      /// Whether this command can currently be undone.
      /// </summary>
      // ------------------------------------------------------------
      bool CanUndo { get; }

      // ------------------------------------------------------------
      /// <summary>
      /// Human-readable description of the operation.
      /// </summary>
      // ------------------------------------------------------------
      string Desc { get; }

   }

}
