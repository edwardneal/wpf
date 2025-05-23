﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MS.Internal;
using MS.Internal.Automation;

namespace System.Windows.Automation.Peers
{
    /// 
    public class UIElement3DAutomationPeer: AutomationPeer
    {
        ///
        public UIElement3DAutomationPeer(UIElement3D owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            _owner = owner;
        }

        ///
        public UIElement3D Owner
        {
            get
            {
                return _owner;
            }
        }

        ///<summary>
        /// This static helper creates an AutomationPeer for the specified element and 
        /// caches it - that means the created peer is going to live long and shadow the
        /// element for its lifetime. The peer will be used by Automation to proxy the element, and
        /// to fire events to the Automation when something happens with the element.
        /// The created peer is returned from this method and also from subsequent calls to this method
        /// and <seealso cref="FromElement"/>. The type of the peer is determined by the 
        /// <seealso cref="UIElement3D.OnCreateAutomationPeer"/> virtual callback. If UIElement3D does not
        /// implement the callback, there will be no peer and this method will return 'null' (in other
        /// words, there is no such thing as a 'default peer').
        ///</summary>
        public static AutomationPeer CreatePeerForElement(UIElement3D element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return element.CreateAutomationPeer();
        }

        ///
        public static AutomationPeer FromElement(UIElement3D element)
        {
            ArgumentNullException.ThrowIfNull(element);

            return element.GetAutomationPeer();
        }
       
        /// 
        protected override List<AutomationPeer> GetChildrenCore()
        {
            List<AutomationPeer> children = null;

            iterate(_owner,
                    (IteratorCallback)delegate(AutomationPeer peer)
                    {
                        if (children == null)
                            children = new List<AutomationPeer>();

                        children.Add(peer);
                        return (false);
                    });

            return children;
        }

        private delegate bool IteratorCallback(AutomationPeer peer);
        
        //
        private static bool iterate(DependencyObject parent, IteratorCallback callback)
        {
            bool done = false;

            if(parent != null)
            {
                AutomationPeer peer = null;
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count && !done; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                    
                    if(     child != null
                        &&  child is UIElement 
                        &&  (peer = UIElementAutomationPeer.CreatePeerForElement((UIElement)child)) != null  )
                    {
                        done = callback(peer);
                    }
                    else if ( child != null
                        &&    child is UIElement3D
                        &&    (peer = CreatePeerForElement(((UIElement3D)child))) != null )
                    {
                        done = callback(peer);
                    }
                    else
                    {
                        done = iterate(child, callback);
                    }
                }
            }
            
            return done;
        }

        /// 
        public override object GetPattern(PatternInterface patternInterface)
        {
            //Support synchronized input
            if (patternInterface == PatternInterface.SynchronizedInput)
            {
                // Adaptor object is used here to avoid loading UIA assemblies in non-UIA scenarios.
                if (_synchronizedInputPattern == null)
                    _synchronizedInputPattern = new SynchronizedInputAdaptor(_owner);
                return _synchronizedInputPattern;
            }
            return null;
        }

        //
        // P R O P E R T I E S 
        //

        ///
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        ///
        protected override string GetAutomationIdCore() 
        { 
            return (AutomationProperties.GetAutomationId(_owner));
        }

        ///
        protected override string GetNameCore()
        {
            return (AutomationProperties.GetName(_owner));
        }

        ///
        protected override string GetHelpTextCore()
        {
            return (AutomationProperties.GetHelpText(_owner));
        }

        /// <summary>
        /// <see cref="AutomationPeer.GetBoundingRectangleCore"/>
        /// </summary>
        protected override Rect GetBoundingRectangleCore()
        {
            Rect rectScreen;

            if (!ComputeBoundingRectangle(out rectScreen))
            {
                rectScreen = Rect.Empty;
            }

            return rectScreen;
        }

        ///
        private bool ComputeBoundingRectangle(out Rect rect)
        {
            rect = Rect.Empty;
            
            PresentationSource presentationSource = PresentationSource.CriticalFromVisual(_owner);

            // If there's no source, the element is not visible, return empty rect
            if(presentationSource == null)
                return false;

            HwndSource hwndSource = presentationSource as HwndSource;

            // If the source isn't an HwndSource, there's not much we can do, return empty rect
            if(hwndSource == null)
                return false;

            Rect rectElement    =  _owner.Visual2DContentBounds;            
            // we use VisualTreeHelper.GetContainingVisual2D to transform from the containing Viewport3DVisual
            Rect rectRoot       = PointUtil.ElementToRoot(rectElement, VisualTreeHelper.GetContainingVisual2D(_owner), presentationSource);
            Rect rectClient     = PointUtil.RootToClient(rectRoot, presentationSource);
            rect    = PointUtil.ClientToScreen(rectClient, hwndSource);

            return true;
        }

        ///
        protected override bool IsOffscreenCore()
        {
            IsOffscreenBehavior behavior = AutomationProperties.GetIsOffscreenBehavior(_owner);

            switch (behavior)
            {
                case IsOffscreenBehavior.Onscreen :
                    return false;

                case IsOffscreenBehavior.Offscreen :
                    return true;

                case IsOffscreenBehavior.FromClip:
                    {
                        bool isOffscreen = !_owner.IsVisible;
                        
                        if (!isOffscreen)
                        {
                            UIElement containingUIElement = GetContainingUIElement(_owner);
                            if (containingUIElement != null)
                            {
                                Rect boundingRect = UIElementAutomationPeer.CalculateVisibleBoundingRect(containingUIElement);
                                
                                isOffscreen = (DoubleUtil.AreClose(boundingRect, Rect.Empty) || 
                                               DoubleUtil.AreClose(boundingRect.Height, 0) || 
                                               DoubleUtil.AreClose(boundingRect.Width, 0));
                            }
                        }

                        return isOffscreen;
                    }

                default :
                    return !_owner.IsVisible;
            }
        }

        /// <summary>
        /// Returns the closest UIElement that contains the given DependencyObject
        /// </summary>
        private static UIElement GetContainingUIElement(DependencyObject reference)
        {
            UIElement element = null;

            while (reference != null)
            {
                element = reference as UIElement;

                if (element != null) break;

                reference = VisualTreeHelper.GetParent(reference);
            }

            return element;
        }

        ///
        protected override AutomationOrientation GetOrientationCore()
        {
            return (AutomationOrientation.None);
        }
        
        ///
        protected override string GetItemTypeCore()
        {
            return AutomationProperties.GetItemType(_owner);
        }

        ///
        protected override string GetClassNameCore()
        {
            return string.Empty;
        }

        ///
        protected override string GetItemStatusCore()
        {
            return AutomationProperties.GetItemStatus(_owner);
        }

        ///
        protected override bool IsRequiredForFormCore()
        {
            return AutomationProperties.GetIsRequiredForForm(_owner);
        }

        /// 
        protected override bool IsKeyboardFocusableCore()
        {
            return Keyboard.IsFocusable(_owner);
        }

        ///
        protected override bool HasKeyboardFocusCore()
        {
            return _owner.IsKeyboardFocused;
        }

        ///
        protected override bool IsEnabledCore()
        {
            return _owner.IsEnabled;
        }

        ///
        protected override bool IsDialogCore()
        {
            return AutomationProperties.GetIsDialog(_owner);
        }

        ///
        protected override bool IsPasswordCore()
        {
            return false;
        }

        ///
        protected override bool IsContentElementCore()
        {
            return true;
        }

        ///
        protected override bool IsControlElementCore()
        {
            // We only want this peer to show up in the Control view if it is visible
            // For compat we allow falling back to legacy behavior (returning true always)
            // based on AppContext flags, IncludeInvisibleElementsInControlView evaluates them.
            return IncludeInvisibleElementsInControlView || _owner.IsVisible;
        }

        ///
        protected override AutomationPeer GetLabeledByCore()
        {          
            UIElement element = AutomationProperties.GetLabeledBy(_owner);
            if (element != null)
                return element.GetAutomationPeer();

            return null;                        
        }

        ///
        protected override string GetAcceleratorKeyCore()
        {
            return AutomationProperties.GetAcceleratorKey(_owner);
        }

        ///
        protected override string GetAccessKeyCore()
        {
            string result = AutomationProperties.GetAccessKey(_owner);
            if (string.IsNullOrEmpty(result))
                return AccessKeyManager.InternalGetAccessKeyCharacter(_owner);

            return string.Empty;
        }

        protected override AutomationLiveSetting GetLiveSettingCore()
        {
            return AutomationProperties.GetLiveSetting(_owner);
        }

        /// <summary>
        /// Provides a value for UIAutomation's PositionInSet property
        /// Reads <see cref="AutomationProperties.PositionInSetProperty"/> and returns the value.
        /// </summary>
        protected override int GetPositionInSetCore()
        {
            return AutomationProperties.GetPositionInSet(_owner);
        }
        /// <summary>
        /// Provides a value for UIAutomation's SizeOfSet property
        /// Reads <see cref="AutomationProperties.SizeOfSetProperty"/> and returns the value.
        /// </summary>
        protected override int GetSizeOfSetCore()
        {
            return AutomationProperties.GetSizeOfSet(_owner);
        }

        /// <summary>
        /// Provides a value for UIAutomation's HeadingLevel property
        /// Reads <see cref="AutomationProperties.HeadingLevelProperty"/> and returns the value
        /// </summary>
        protected override AutomationHeadingLevel GetHeadingLevelCore()
        {
            return AutomationProperties.GetHeadingLevel(_owner);
        }

        //
        // M E T H O D S
        //

        /// <summary>
        /// <see cref="AutomationPeer.GetClickablePointCore"/>
        /// </summary>
        protected override Point GetClickablePointCore()
        {
            Rect rectScreen;
            Point pt = new Point(double.NaN, double.NaN);
    
            if (ComputeBoundingRectangle(out rectScreen))
            {
                pt = new Point(rectScreen.Left + rectScreen.Width * 0.5, rectScreen.Top + rectScreen.Height * 0.5);
            }
                        
            return pt;
        }

        ///
        protected override void SetFocusCore() 
        { 
            if (!_owner.Focus())
                throw new InvalidOperationException(SR.SetFocusFailed);
        }

        ///
        internal override Rect GetVisibleBoundingRectCore()
        {
            return GetBoundingRectangle();
        }

        private UIElement3D _owner;
        private SynchronizedInputAdaptor _synchronizedInputPattern;
    }
}

