// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Description: Delegate and args for the ItemsChanged event.
//
// Specs:       Data Styling.mht
//

using System.Collections.Specialized;


namespace System.Windows.Controls.Primitives
{
    /// <summary>
    /// The ItemsChanged event is raised by an ItemContainerGenerator to inform
    /// layouts that the items collection has changed.
    /// </summary>
    public class ItemsChangedEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        internal ItemsChangedEventArgs(NotifyCollectionChangedAction action,
                                        GeneratorPosition position,
                                        GeneratorPosition oldPosition,
                                        int itemCount,
                                        int itemUICount)
        {
            _action = action;
            _position = position;
            _oldPosition = oldPosition;
            _itemCount = itemCount;
            _itemUICount = itemUICount;
        }

        internal ItemsChangedEventArgs(NotifyCollectionChangedAction action,
                                        GeneratorPosition position,
                                        int itemCount,
                                        int itemUICount) : this(action, position, new GeneratorPosition(-1, 0), itemCount, itemUICount)
                 
        {
        }

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        /// <summary> What happened </summary>
        public NotifyCollectionChangedAction Action { get { return _action; } }

        /// <summary> Where it happened </summary>
        public GeneratorPosition Position       { get { return _position; } }

        /// <summary> Where it happened </summary>
        public GeneratorPosition OldPosition    { get { return _oldPosition; } }

        /// <summary> How many items were involved </summary>
        public int ItemCount                    { get { return _itemCount; } }

        /// <summary> How many UI elements were involved </summary>
        public int ItemUICount                  { get { return _itemUICount; } }


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        private NotifyCollectionChangedAction  _action;
        private GeneratorPosition       _position;
        private GeneratorPosition       _oldPosition;
        private int                     _itemCount;
        private int                     _itemUICount;
    }

    /// <summary>
    ///     The delegate to use for handlers that receive ItemsChangedEventArgs.
    /// </summary>
    public delegate void ItemsChangedEventHandler(object sender, ItemsChangedEventArgs e);
}

