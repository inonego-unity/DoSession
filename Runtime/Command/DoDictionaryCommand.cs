using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ============================================================
   /// <summary>
   /// <br/> IDictionary&lt;TKey, TValue&gt; 대상 Add/Remove 전용 Command.
   /// <br/> Dictionary, SortedDictionary, SortedList 등을 지원한다.
   /// </summary>
   // ============================================================
   [Serializable]
   public class DoDictionaryCommand<TKey, TValue> : IDoCommand
   {

   #region 필드

      private readonly IDictionary<TKey, TValue> dictionary;
      private readonly TKey key;
      private readonly TValue value;
      private readonly bool isAdd;

      // ------------------------------------------------------------
      /// <summary>
      /// 되돌리기 가능 여부.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => dictionary != null;

      // ------------------------------------------------------------
      /// <summary>
      /// 작업 설명.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc { get; }

   #endregion

   #region 생성자

      private DoDictionaryCommand(IDictionary<TKey, TValue> dictionary, TKey key, TValue value, bool isAdd, string desc)
      {
         this.dictionary = dictionary;
         this.key = key;
         this.value = value;
         this.isAdd = isAdd;

         Desc = desc;
      }

   #endregion

   #region 팩토리

      // ------------------------------------------------------------
      /// <summary>
      /// 딕셔너리에 항목을 추가하는 Command를 생성한다.
      /// </summary>
      // ------------------------------------------------------------
      public static DoDictionaryCommand<TKey, TValue> Add(IDictionary<TKey, TValue> dictionary, TKey key, TValue value, string desc)
         => new DoDictionaryCommand<TKey, TValue>(dictionary, key, value, true, desc);

      // ------------------------------------------------------------
      /// <summary>
      /// <br/> 딕셔너리에서 항목을 제거하는 Command를 생성한다.
      /// <br/> 현재 값을 자동으로 캡처한다.
      /// </summary>
      // ------------------------------------------------------------
      public static DoDictionaryCommand<TKey, TValue> Remove(IDictionary<TKey, TValue> dictionary, TKey key, string desc)
         => new DoDictionaryCommand<TKey, TValue>(dictionary, key, dictionary[key], false, desc);

   #endregion

   #region 메서드

      public void Do()
      {
         if (isAdd) dictionary.Add(key, value);
         else dictionary.Remove(key);
      }

      public void Undo()
      {
         if (isAdd) dictionary.Remove(key);
         else dictionary.Add(key, value);
      }

   #endregion

   }

}
