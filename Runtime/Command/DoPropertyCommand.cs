using System;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> 프로퍼티 변경 전용 Command.
   /// <br/> getter/setter 람다로 이전 값을 자동 캡처한다.
   /// </summary>
   // ============================================================
   [Serializable]
   public class DoPropertyCommand<TTarget, TValue> : IDoCommand
   {

   #region 필드

      private readonly TTarget target;
      private readonly Func<TTarget, TValue> getter;
      private readonly Action<TTarget, TValue> setter;
      private readonly TValue oldValue;
      private readonly TValue newValue;

      // ------------------------------------------------------------
      /// <summary>
      /// 되돌리기 가능 여부. 타겟이 null이면 불가.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => target != null;

      // ------------------------------------------------------------
      /// <summary>
      /// 작업 설명.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc { get; }

   #endregion

   #region 생성자

      // ------------------------------------------------------------
      /// <summary>
      /// <br/> 프로퍼티 변경 Command를 생성한다.
      /// <br/> 생성 시점의 현재 값을 oldValue로 캡처한다.
      /// </summary>
      // ------------------------------------------------------------
      public DoPropertyCommand(TTarget target, Func<TTarget, TValue> getter, Action<TTarget, TValue> setter, TValue newValue, string desc)
      {
         this.target = target;
         this.getter = getter;
         this.setter = setter;
         this.oldValue = getter(target);
         this.newValue = newValue;

         Desc = desc;
      }

   #endregion

   #region 메서드

      public void Do() => setter(target, newValue);
      public void Undo() => setter(target, oldValue);

   #endregion

   }

}
