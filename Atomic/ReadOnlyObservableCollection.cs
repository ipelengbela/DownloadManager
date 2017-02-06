﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;

namespace Atomic.Core
{
    /// <summary>
    /// Read-only wrapper around an ObservableCollection.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public class ReadOnlyObservableCollection<T> : ReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of ReadOnlyObservableCollection that
        /// wraps the given ObservableCollection.
        /// </summary>
        public ReadOnlyObservableCollection(ObservableCollection<T> list) : base(list)
        {
            ((INotifyCollectionChanged)Items).CollectionChanged += new NotifyCollectionChangedEventHandler(HandleCollectionChanged);
            ((INotifyPropertyChanged)Items).PropertyChanged += new PropertyChangedEventHandler(HandlePropertyChanged);
        }

        #endregion Constructors

        #region Interfaces

        //------------------------------------------------------
        //
        //  Interfaces
        //
        //------------------------------------------------------

        #region INotifyCollectionChanged

        /// <summary>
        /// CollectionChanged event (per <see cref="INotifyCollectionChanged" />).
        /// </summary>
        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add { CollectionChanged += value; }
            remove { CollectionChanged -= value; }
        }

        /// <summary>
        /// Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyCollectionChanged"/>
        /// </remarks>
        public virtual event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// raise CollectionChanged event to any listeners
        /// </summary>
        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        #endregion INotifyCollectionChanged

        #region INotifyPropertyChanged

        /// <summary>
        /// PropertyChanged event (per <see cref="INotifyPropertyChanged" />).
        /// </summary>
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        /// <summary>
        /// Occurs when a property changes.
        /// </summary>
        /// <remarks>
        /// see <seealso cref="INotifyPropertyChanged"/>
        /// </remarks>
        public virtual event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// raise PropertyChanged event to any listeners
        /// </summary>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        #endregion INotifyPropertyChanged

        #endregion Interfaces

        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        // forward CollectionChanged events from the base list to our listeners
        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }

        // forward PropertyChanged events from the base list to our listeners
        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e);
        }
        #endregion Private Methods

        #region Private Fields

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #endregion Private Fields
    }
}

