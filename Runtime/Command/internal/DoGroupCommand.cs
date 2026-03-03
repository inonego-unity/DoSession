using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.DoSession
{

   // ===============================================================================
   /// <summary>
   /// <br/> Composite command. Bundles multiple commands into a single undo unit.
   /// <br/> Created internally by DoSession.BeginGroup/EndGroup.
   /// </summary>
   // ===============================================================================
   [Serializable]
   internal class DoGroupCommand : IDoCommand
   {

   #region Fields

      [SerializeReference]
      private List<IDoCommand> commands = null;
      public IReadOnlyList<IDoCommand> Commands => commands;

      [SerializeField]
      private string desc = null;

      // ------------------------------------------------------------
      /// <summary>
      /// True only if all child commands can be undone.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => commands.TrueForAll(cmd => cmd.CanUndo);

      // ------------------------------------------------------------
      /// <summary>
      /// Group description.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc => desc;

   #endregion

   #region Constructor

      public DoGroupCommand(List<IDoCommand> commands, string desc)
      {
         this.commands = commands;
         this.desc = desc;
      }

   #endregion

   #region Methods

      // --------------------------------------------------------------------------
      /// <summary>
      /// Executes all commands in forward order. Only called during redo.
      /// </summary>
      // --------------------------------------------------------------------------
      public void Do()
      {
         foreach (var cmd in commands)
         {
            cmd.Do();
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// Undoes all commands in reverse order.
      /// </summary>
      // ------------------------------------------------------------
      public void Undo()
      {
         for (int i = commands.Count - 1; i >= 0; i--)
         {
            commands[i].Undo();
         }
      }

   #endregion

   }

}
