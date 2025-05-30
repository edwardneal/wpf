﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Windows.Media;

namespace System.Windows.Input.StylusPlugIns
{
    /////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// [TBS]
    /// </summary>
    public class RawStylusInput
    {
        /////////////////////////////////////////////////////////////////////
        /// <summary>
        ///     [TBS]
        /// </summary>
        /// <param name="report">[TBS]</param>
        /// <param name="tabletToElementTransform">[TBS]</param>
        /// <param name="targetPlugInCollection">[TBS]</param>
        internal RawStylusInput(
            RawStylusInputReport    report,
            GeneralTransform        tabletToElementTransform,
            StylusPlugInCollection targetPlugInCollection)
        {
            ArgumentNullException.ThrowIfNull(report);
            if (tabletToElementTransform.Inverse == null)
            {
                throw new ArgumentException(SR.Stylus_MatrixNotInvertable, nameof(tabletToElementTransform));
            }
            ArgumentNullException.ThrowIfNull(targetPlugInCollection);

            // We should always see this GeneralTransform is frozen since we access this from multiple threads.
            System.Diagnostics.Debug.Assert(tabletToElementTransform.IsFrozen);
            _report                 = report;
            _tabletToElementTransform  = tabletToElementTransform;
            _targetPlugInCollection = targetPlugInCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        public int StylusDeviceId { get { return _report.StylusDeviceId; } }    

        /// <summary>
        /// 
        /// </summary>
        public int TabletDeviceId { get { return _report.TabletDeviceId; } }

        /// <summary>
        /// 
        /// </summary>
        public int Timestamp { get { return _report.Timestamp; } }    

        /// <summary>
        /// Returns a copy of the StylusPoints
        /// </summary>
        public StylusPointCollection GetStylusPoints()
        {
            return GetStylusPoints(Transform.Identity);
        }

        /// <summary>
        /// Internal method called by StylusDevice to prevent two copies
        /// </summary>
        internal StylusPointCollection GetStylusPoints(GeneralTransform transform)
        {
            if (_stylusPoints == null)
            {
                GeneralTransformGroup group = new GeneralTransformGroup();
                if ( StylusDeviceId == 0)
                {
                    // Only do this for the Mouse
                    group.Children.Add(new MatrixTransform(_report.InputSource.CompositionTarget.TransformFromDevice));
                }
                group.Children.Add(_tabletToElementTransform);
                if(transform != null)
                {
                    group.Children.Add(transform);
                }
                return new StylusPointCollection(_report.StylusPointDescription, _report.GetRawPacketData(), group, Matrix.Identity);
            }
            else
            {
                return _stylusPoints.Clone(transform, _stylusPoints.Description);
            }
        }

        /// <summary>
        /// Replaces the StylusPoints.
        /// </summary>
        /// <remarks>
        ///     Callers must have Unmanaged code permission to call this API.
        /// </remarks>
        /// <param name="stylusPoints">stylusPoints</param>
        public void SetStylusPoints(StylusPointCollection stylusPoints)
        {
            ArgumentNullException.ThrowIfNull(stylusPoints);

            if (!StylusPointDescription.AreCompatible(  stylusPoints.Description,
                                                        _report.StylusPointDescription))
            {
                throw new ArgumentException(SR.IncompatibleStylusPointDescriptions, nameof(stylusPoints));
            }
            if (stylusPoints.Count == 0)
            {
                throw new ArgumentException(SR.Stylus_StylusPointsCantBeEmpty, nameof(stylusPoints));
            }

            _stylusPoints = stylusPoints.Clone();
        }

        /// <summary>
        /// Returns the RawStylusInputCustomDataList used to notify plugins before  
        /// PreviewStylus event has been processed by application.
        /// </summary>
        public void NotifyWhenProcessed(object callbackData)
        {
            if (_currentNotifyPlugIn == null)
            {
                throw new InvalidOperationException(SR.Stylus_CanOnlyCallForDownMoveOrUp);
            }
            if (_customData == null)
            {
                _customData = new RawStylusInputCustomDataList();
            }
            _customData.Add(new RawStylusInputCustomData(_currentNotifyPlugIn, callbackData));
        }

        /// <summary>
        /// True if a StylusPlugIn has modifiedthe StylusPoints.
        /// </summary>
        internal bool StylusPointsModified
        {
            get 
            {
                return _stylusPoints != null;
            }
        }

        /// <summary>
        /// Target StylusPlugInCollection that real time pen input sent to.
        /// </summary>
        internal StylusPlugInCollection Target
        {
            get 
            {
                return _targetPlugInCollection;
            }
        }

        /// <summary>
        /// Real RawStylusInputReport that this report is generated from.
        /// </summary>
        internal RawStylusInputReport Report
        {
            get 
            {
                return _report;
            }
        }

        /// <summary>
        /// Matrix that was used for rawstylusinput packets.
        /// </summary>
        internal GeneralTransform ElementTransform
        {
            get 
            {
                return _tabletToElementTransform;
            }
        }

        /// <summary>
        /// Retrieves the RawStylusInputCustomDataList associated with this input.
        /// </summary>
        internal RawStylusInputCustomDataList CustomDataList
        {
            get 
            {
                if (_customData == null)
                {
                    _customData = new RawStylusInputCustomDataList();
                }
                return _customData;
            }
        }

        /// <summary>
        /// StylusPlugIn that is adding a notify event.
        /// </summary>
        internal StylusPlugIn CurrentNotifyPlugIn
        {
            get 
            {
                return _currentNotifyPlugIn;
            }
            set
            {
                _currentNotifyPlugIn = value;
            }
        }

        /////////////////////////////////////////////////////////////////////

        private RawStylusInputReport    _report;
        private GeneralTransform        _tabletToElementTransform;
        private StylusPlugInCollection  _targetPlugInCollection;
        private StylusPointCollection   _stylusPoints;
        private StylusPlugIn            _currentNotifyPlugIn;
        private RawStylusInputCustomDataList _customData;
    }
}
