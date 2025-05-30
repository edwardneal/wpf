// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System.IO;
using System.ComponentModel;
using System.Net.Cache;

namespace System.Windows.Media.Imaging
{
    public sealed partial class CroppedBitmap : BitmapSource
    {
        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        ///     Shadows inherited Clone() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new CroppedBitmap Clone()
        {
            return (CroppedBitmap)base.Clone();
        }

        /// <summary>
        ///     Shadows inherited CloneCurrentValue() with a strongly typed
        ///     version for convenience.
        /// </summary>
        public new CroppedBitmap CloneCurrentValue()
        {
            return (CroppedBitmap)base.CloneCurrentValue();
        }




        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            CroppedBitmap target = ((CroppedBitmap) d);


            target.SourcePropertyChangedHook(e);



            // The first change to the default value of a mutable collection property (e.g. GeometryGroup.Children) 
            // will promote the property value from a default value to a local value. This is technically a sub-property 
            // change because the collection was changed and not a new collection set (GeometryGroup.Children.
            // Add versus GeometryGroup.Children = myNewChildrenCollection). However, we never marshalled 
            // the default value to the compositor. If the property changes from a default value, the new local value 
            // needs to be marshalled to the compositor. We detect this scenario with the second condition 
            // e.OldValueSource != e.NewValueSource. Specifically in this scenario the OldValueSource will be 
            // Default and the NewValueSource will be Local.
            if (e.IsASubPropertyChange && 
               (e.OldValueSource == e.NewValueSource))
            {
                return;
            }



            target.PropertyChanged(SourceProperty);
        }
        private static void SourceRectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CroppedBitmap target = ((CroppedBitmap) d);


            target.SourceRectPropertyChangedHook(e);

            target.PropertyChanged(SourceRectProperty);
        }


        #region Public Properties

        /// <summary>
        ///     Source - BitmapSource.  Default value is null.
        /// </summary>
        public BitmapSource Source
        {
            get
            {
                return (BitmapSource)GetValue(SourceProperty);
            }
            set
            {
                SetValueInternal(SourceProperty, value);
            }
        }

        /// <summary>
        ///     SourceRect - Int32Rect.  Default value is Int32Rect.Empty.
        /// </summary>
        public Int32Rect SourceRect
        {
            get
            {
                return (Int32Rect)GetValue(SourceRectProperty);
            }
            set
            {
                SetValueInternal(SourceRectProperty, value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Implementation of <see cref="System.Windows.Freezable.CreateInstanceCore">Freezable.CreateInstanceCore</see>.
        /// </summary>
        /// <returns>The new Freezable.</returns>
        protected override Freezable CreateInstanceCore()
        {
            return new CroppedBitmap();
        }
        /// <summary>
        /// Implementation of Freezable.CloneCore()
        /// </summary>
        protected override void CloneCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceCroppedBitmap);

            base.CloneCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceCroppedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.CloneCurrentValueCore()
        /// </summary>
        protected override void CloneCurrentValueCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceCroppedBitmap);

            base.CloneCurrentValueCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceCroppedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetAsFrozenCore()
        /// </summary>
        protected override void GetAsFrozenCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceCroppedBitmap);

            base.GetAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceCroppedBitmap);
        }
        /// <summary>
        /// Implementation of Freezable.GetCurrentValueAsFrozenCore()
        /// </summary>
        protected override void GetCurrentValueAsFrozenCore(Freezable source)
        {
            CroppedBitmap sourceCroppedBitmap = (CroppedBitmap)source;

            // Set any state required before actual clone happens
            ClonePrequel(sourceCroppedBitmap);

            base.GetCurrentValueAsFrozenCore(source);



            // Set state once clone has finished
            ClonePostscript(sourceCroppedBitmap);
        }


        #endregion ProtectedMethods

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods









        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties





        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Dependency Properties
        //
        //------------------------------------------------------

        #region Dependency Properties

        /// <summary>
        ///     The DependencyProperty for the CroppedBitmap.Source property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty;
        /// <summary>
        ///     The DependencyProperty for the CroppedBitmap.SourceRect property.
        /// </summary>
        public static readonly DependencyProperty SourceRectProperty;

        #endregion Dependency Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields





        internal static BitmapSource s_Source = null;
        internal static Int32Rect s_SourceRect = Int32Rect.Empty;

        #endregion Internal Fields



        #region Constructors

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        static CroppedBitmap()
        {
            // We check our static default fields which are of type Freezable
            // to make sure that they are not mutable, otherwise we will throw
            // if these get touched by more than one thread in the lifetime
            // of your app.
            Debug.Assert(s_Source == null || s_Source.IsFrozen,
                "Detected context bound default value CroppedBitmap.s_Source (See OS Bug #947272).");


            // Initializations
            Type typeofThis = typeof(CroppedBitmap);
            SourceProperty =
                  RegisterProperty("Source",
                                   typeof(BitmapSource),
                                   typeofThis,
                                   null,
                                   new PropertyChangedCallback(SourcePropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSource));
            SourceRectProperty =
                  RegisterProperty("SourceRect",
                                   typeof(Int32Rect),
                                   typeofThis,
                                   Int32Rect.Empty,
                                   new PropertyChangedCallback(SourceRectPropertyChanged),
                                   null,
                                   /* isIndependentlyAnimated  = */ false,
                                   /* coerceValueCallback */ new CoerceValueCallback(CoerceSourceRect));
        }



        #endregion Constructors
    }
}
