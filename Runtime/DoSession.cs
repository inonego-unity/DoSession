using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ==================================================================================
   /// <summary>
   /// <br/> Generic Undo/Redo session.
   /// <br/> Manages operation history based on the Command pattern.
   /// <br/> Create independent instances per system or share across related systems.
   /// </summary>
   // ==================================================================================
   public class DoSession : IDoSession
   {

   #region Fields

      private readonly List<IDoCommand> undoStack = new();

      private readonly List<IDoCommand> redoStack = new();

      private bool isGrouping = false;

      private List<IDoCommand> groupBuffer = null;

      private string groupDesc = null;

      private int maxSize = 100;

      // ----------------------------------------------------------------------
      /// <summary>
      /// Maximum history size. Oldest entries are removed when exceeded.
      /// </summary>
      // ----------------------------------------------------------------------
      public int MaxSize
      {
         get => maxSize;
         set => maxSize = value;
      }

      // --------------------------------------------------------------------------
      /// <summary>
      /// <br/> Whether undo is possible.
      /// <br/> False if stack is empty or top command's CanUndo is false.
      /// </summary>
      // --------------------------------------------------------------------------
      public bool CanUndo => undoStack.Count > 0 && undoStack[undoStack.Count - 1].CanUndo;

      // ------------------------------------------------------------
      /// <summary>
      /// Whether redo is possible.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanRedo => redoStack.Count > 0;

      // ------------------------------------------------------------
      /// <summary>
      /// Number of entries in the undo stack.
      /// </summary>
      // ------------------------------------------------------------
      public int UndoCount => undoStack.Count;

      // ------------------------------------------------------------
      /// <summary>
      /// Number of entries in the redo stack.
      /// </summary>
      // ------------------------------------------------------------
      public int RedoCount => redoStack.Count;

      // ------------------------------------------------------------
      /// <summary>
      /// Next undo target command. Null if empty.
      /// </summary>
      // ------------------------------------------------------------
      public IDoCommand PeekUndo => undoStack.Count > 0 ? undoStack[undoStack.Count - 1] : null;

      // ------------------------------------------------------------
      /// <summary>
      /// Next redo target command. Null if empty.
      /// </summary>
      // ------------------------------------------------------------
      public IDoCommand PeekRedo => redoStack.Count > 0 ? redoStack[redoStack.Count - 1] : null;

      // ------------------------------------------------------------
      /// <summary>
      /// Undo history list. Higher index means more recent.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<IDoCommand> UndoHistory => undoStack;

      // ------------------------------------------------------------
      /// <summary>
      /// Redo history list. Higher index means more recent.
      /// </summary>
      // ------------------------------------------------------------
      public IReadOnlyList<IDoCommand> RedoHistory => redoStack;

   #endregion

   #region Events

      // ------------------------------------------------------------
      /// <summary>
      /// Raised after Do is executed.
      /// </summary>
      // ------------------------------------------------------------
      public event Action<IDoCommand> OnDo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// Raised after Undo is executed.
      /// </summary>
      // ------------------------------------------------------------
      public event Action<IDoCommand> OnUndo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// Raised after Redo is executed.
      /// </summary>
      // ------------------------------------------------------------
      public event Action<IDoCommand> OnRedo = null;

      // ------------------------------------------------------------
      /// <summary>
      /// Raised when the history changes.
      /// </summary>
      // ------------------------------------------------------------
      public event Action OnChange = null;

   #endregion

   #region Core API

      // ------------------------------------------------------------
      /// <summary>
      /// Executes the command and pushes it onto the undo stack.
      /// </summary>
      // ------------------------------------------------------------
      public void Do(IDoCommand command)
      {
         if (command == null)
         {
            throw new ArgumentNullException("command is null.");
         }

         // In group mode, accumulate in buffer and only execute individually
         if (isGrouping)
         {
            command.Do();
            groupBuffer.Add(command);
            return;
         }

         command.Do();

         undoStack.Add(command);
         redoStack.Clear();

         // Remove oldest entries when exceeding MaxSize
         while (undoStack.Count > MaxSize)
         {
            undoStack.RemoveAt(0);
         }

         OnDo?.Invoke(command);
         OnChange?.Invoke();
      }

      // --------------------------------------------------------------------------
      /// <summary>
      /// <br/> Lambda helper. Executes a simple operation without a Command class.
      /// <br/> If canUndo is null, the command is always undoable.
      /// </summary>
      // --------------------------------------------------------------------------
      public void Do(Action doAction, Action undoAction, string desc, Func<bool> canUndo = null)
      {
         Do(new DoLambdaCommand(doAction, undoAction, desc, canUndo));
      }

      // --------------------------------------------------------------------------
      /// <summary>
      /// <br/> Undoes the last operation.
      /// <br/> If the top command's CanUndo is false, acts as a barrier and returns false.
      /// </summary>
      // --------------------------------------------------------------------------
      public bool Undo()
      {
         if (undoStack.Count == 0) return false;

         var cmd = undoStack[undoStack.Count - 1];

         // Barrier: if CanUndo is false, leave the stack untouched and fail
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
      /// Redoes the last undone operation.
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

   #region Composite Commands

      // ------------------------------------------------------------------------
      /// <summary>
      /// Begins a group. Subsequent Do() calls accumulate in the group.
      /// </summary>
      // ------------------------------------------------------------------------
      public void BeginGroup(string desc)
      {
         if (isGrouping)
         {
            throw new InvalidOperationException("A group is already in progress.");
         }

         isGrouping = true;
         groupBuffer = new List<IDoCommand>();
         groupDesc = desc;
      }

      // ---------------------------------------------------------------------------------------
      /// <summary>
      /// <br/> Ends the group.
      /// <br/> Accumulated commands are bundled into a GroupCommand and pushed onto the stack.
      /// </summary>
      // ---------------------------------------------------------------------------------------
      public void EndGroup()
      {
         if (!isGrouping)
         {
            throw new InvalidOperationException("No group is in progress.");
         }

         isGrouping = false;

         if (groupBuffer.Count > 0)
         {
            var group = new DoGroupCommand(groupBuffer, groupDesc);

            undoStack.Add(group);
            redoStack.Clear();

            // Remove oldest entries when exceeding MaxSize
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

   #region Management

      // ------------------------------------------------------------
      /// <summary>
      /// Clears both the undo and redo stacks.
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
      /// Clears only the undo stack.
      /// </summary>
      // ------------------------------------------------------------
      public void ClearUndo()
      {
         undoStack.Clear();

         OnChange?.Invoke();
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Clears only the redo stack.
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
