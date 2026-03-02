using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> 복합 명령. 여러 Command를 하나의 Undo 단위로 묶는다.
   /// <br/> DoSession.BeginGroup/EndGroup에서 내부적으로 생성.
   /// </summary>
   // ============================================================
   internal class DoGroupCommand : IDoCommand
   {

   #region 필드

      private readonly List<IDoCommand> commands = null;

      // ------------------------------------------------------------
      /// <summary>
      /// 모든 자식 Command가 되돌리기 가능해야 true.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => commands.TrueForAll(cmd => cmd.CanUndo);

      // ------------------------------------------------------------
      /// <summary>
      /// 그룹 설명.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc { get; }

   #endregion

   #region 생성자

      public DoGroupCommand(List<IDoCommand> commands, string desc)
      {
         this.commands = commands;

         Desc = desc;
      }

   #endregion

   #region 메서드

      // ------------------------------------------------------------
      /// <summary>
      /// 순방향 실행. Redo 시에만 호출된다.
      /// </summary>
      // ------------------------------------------------------------
      public void Do()
      {
         foreach (var cmd in commands)
         {
            cmd.Do();
         }
      }

      // ------------------------------------------------------------
      /// <summary>
      /// 역순으로 되돌리기.
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
