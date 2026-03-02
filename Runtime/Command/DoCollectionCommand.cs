using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> ICollection&lt;T&gt; 대상 Add/Remove 전용 Command.
   /// <br/> List, HashSet, LinkedList, SortedSet 등을 지원한다.
   /// </summary>
   // ============================================================
   [Serializable]
   public class DoCollectionCommand<T> : IDoCommand
   {

   #region 필드

      private readonly ICollection<T> collection;
      private readonly T item;
      private readonly bool isAdd;

      // ------------------------------------------------------------
      /// <summary>
      /// 되돌리기 가능 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => collection != null;

      // ------------------------------------------------------------
      /// <summary>
      /// 작업 설명.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc { get; }

   #endregion

   #region 생성자

      private DoCollectionCommand(ICollection<T> collection, T item, bool isAdd, string desc)
      {
         this.collection = collection;
         this.item = item;
         this.isAdd = isAdd;

         Desc = desc;
      }

   #endregion

   #region 팩토리

      // ------------------------------------------------------------
      /// <summary>
      /// 컬렉션에 항목을 추가하는 Command를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static DoCollectionCommand<T> Add(ICollection<T> collection, T item, string desc)
         => new DoCollectionCommand<T>(collection, item, true, desc);

      // ------------------------------------------------------------
      /// <summary>
      /// 컬렉션에서 항목을 제거하는 Command를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static DoCollectionCommand<T> Remove(ICollection<T> collection, T item, string desc)
         => new DoCollectionCommand<T>(collection, item, false, desc);

   #endregion

   #region 메서드

      public void Do()
      {
         if (isAdd) collection.Add(item);
         else collection.Remove(item);
      }

      public void Undo()
      {
         if (isAdd) collection.Remove(item);
         else collection.Add(item);
      }

   #endregion

   }

}
