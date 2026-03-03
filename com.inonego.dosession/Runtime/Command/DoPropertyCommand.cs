using System;

namespace inonego.DoSession
{

   // ===============================================================================
   /// <summary>
   /// <br/> Command for property changes.
   /// <br/> Automatically captures the previous value via getter/setter lambdas.
   /// </summary>
   // ===============================================================================
   public class DoPropertyCommand<TTarget, TValue> : IDoCommand
   {

   #region Fields

      private TTarget target;
      private Func<TTarget, TValue> getter;
      private Action<TTarget, TValue> setter;
      private TValue oldValue;
      private TValue newValue;
      private string desc;

      // ------------------------------------------------------------
      /// <summary>
      /// Whether undo is possible. False if target is null.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => target != null;

      // ------------------------------------------------------------
      /// <summary>
      /// Operation description.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc => desc;

   #endregion

   #region Constructor

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Creates a property change command.
      /// <br/> Captures the current value as oldValue at construction time.
      /// </summary>
      // ----------------------------------------------------------------------
      public DoPropertyCommand(TTarget target, Func<TTarget, TValue> getter, Action<TTarget, TValue> setter, TValue newValue, string desc)
      {
         this.target = target;
         this.getter = getter;
         this.setter = setter;
         this.oldValue = getter(target);
         this.newValue = newValue;
         this.desc = desc;
      }

   #endregion

   #region Methods

      public void Do() => setter(target, newValue);
      public void Undo() => setter(target, oldValue);

   #endregion

   }

}
