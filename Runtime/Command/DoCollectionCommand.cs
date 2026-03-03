using System;
using System.Collections;
using System.Collections.Generic;

namespace inonego.DoSession
{

   // ===================================================================
   /// <summary>
   /// <br/> Command for ICollection&lt;T&gt; Add/Remove operations.
   /// <br/> Supports List, HashSet, LinkedList, SortedSet, etc.
   /// </summary>
   // ===================================================================
   public class DoCollectionCommand<T> : IDoCommand
   {

   #region Fields

      private ICollection<T> collection;
      private T item;
      private bool isAdd;
      private string desc;

      // ------------------------------------------------------------
      /// <summary>
      /// Whether undo is possible.
      /// </summary>
      // ------------------------------------------------------------
      public bool CanUndo => collection != null;

      // ------------------------------------------------------------
      /// <summary>
      /// Operation description.
      /// </summary>
      // ------------------------------------------------------------
      public string Desc => desc;

   #endregion

   #region Constructor

      private DoCollectionCommand(ICollection<T> collection, T item, bool isAdd, string desc)
      {
         this.collection = collection;
         this.item = item;
         this.isAdd = isAdd;
         this.desc = desc;
      }

   #endregion

   #region Factory

      // ----------------------------------------------------------------------
      /// <summary>
      /// Creates a command that adds an item to the collection.
      /// </summary>
      // ----------------------------------------------------------------------
      public static DoCollectionCommand<T> Add(ICollection<T> collection, T item, string desc)
         => new DoCollectionCommand<T>(collection, item, true, desc);

      // ----------------------------------------------------------------------
      /// <summary>
      /// Creates a command that removes an item from the collection.
      /// </summary>
      // ----------------------------------------------------------------------
      public static DoCollectionCommand<T> Remove(ICollection<T> collection, T item, string desc)
         => new DoCollectionCommand<T>(collection, item, false, desc);

   #endregion

   #region Methods

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
