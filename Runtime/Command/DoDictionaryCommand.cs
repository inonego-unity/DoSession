using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ===========================================================================
   /// <summary>
   /// <br/> Command for IDictionary&lt;TKey, TValue&gt; Add/Remove operations.
   /// <br/> Supports Dictionary, SortedDictionary, SortedList, etc.
   /// </summary>
   // ===========================================================================
   public class DoDictionaryCommand<TKey, TValue> : IDoCommand
   {

   #region Fields

      private IDictionary<TKey, TValue> dictionary;
      private TKey key;
      private TValue value;
      private bool isAdd;
      private string desc;

      // ------------------------------------------------------------
      /// <summary>
      /// Whether undo is possible.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => dictionary != null;

      // ------------------------------------------------------------
      /// <summary>
      /// Operation description.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc => desc;

   #endregion

   #region Constructor

      private DoDictionaryCommand(IDictionary<TKey, TValue> dictionary, TKey key, TValue value, bool isAdd, string desc)
      {
         this.dictionary = dictionary;
         this.key = key;
         this.value = value;
         this.isAdd = isAdd;
         this.desc = desc;
      }

   #endregion

   #region Factory

      // ----------------------------------------------------------------------
      /// <summary>
      /// Creates a command that adds an entry to the dictionary.
      /// </summary>
      // ----------------------------------------------------------------------
      public static DoDictionaryCommand<TKey, TValue> Add(IDictionary<TKey, TValue> dictionary, TKey key, TValue value, string desc)
         => new DoDictionaryCommand<TKey, TValue>(dictionary, key, value, true, desc);

      // ----------------------------------------------------------------------
      /// <summary>
      /// <br/> Creates a command that removes an entry from the dictionary.
      /// <br/> Automatically captures the current value.
      /// </summary>
      // ----------------------------------------------------------------------
      public static DoDictionaryCommand<TKey, TValue> Remove(IDictionary<TKey, TValue> dictionary, TKey key, string desc)
         => new DoDictionaryCommand<TKey, TValue>(dictionary, key, dictionary[key], false, desc);

   #endregion

   #region Methods

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
