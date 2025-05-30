﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using MS.Internal;

namespace System.Windows
{
    /// <summary>
    /// CornerRadiusConverter - Converter class for converting instances of other types to and from CornerRadius instances.
    /// </summary> 
    public class CornerRadiusConverter : TypeConverter
    {
        #region Public Methods

        /// <summary>
        /// CanConvertFrom - Returns whether or not this class can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if thie converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext typeDescriptorContext, Type sourceType)
        {
            // We can only handle strings, integral and floating types
            TypeCode tc = Type.GetTypeCode(sourceType);
            switch (tc)
            {
                case TypeCode.String:
                case TypeCode.Decimal:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// CanConvertTo - Returns whether or not this class can convert to a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext typeDescriptorContext, Type destinationType)
        {
            // We can convert to an InstanceDescriptor or to a string.
            if (    destinationType == typeof(InstanceDescriptor) 
                ||  destinationType == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// ConvertFrom - Attempt to convert to a CornerRadius from the given object
        /// </summary>
        /// <returns>
        /// The CornerRadius which was constructed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the example object is not null and is not a valid type
        /// which can be converted to a CornerRadius.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="source"> The object to convert to a CornerRadius. </param>
        public override object ConvertFrom(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object source)
        {
            if (source != null)
            {
                if (source is string) { return FromString((string)source, cultureInfo); }
                else                  { return new CornerRadius(Convert.ToDouble(source, cultureInfo)); }
            }
            throw GetConvertFromException(source);
        }

        /// <summary>
        /// ConvertTo - Attempt to convert a CornerRadius to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// An ArgumentNullException is thrown if the example object is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// An ArgumentException is thrown if the object is not null and is not a CornerRadius,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="typeDescriptorContext"> The ITypeDescriptorContext for this call. </param>
        /// <param name="cultureInfo"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The CornerRadius to convert. </param>
        /// <param name="destinationType">The type to which to convert the CornerRadius instance. </param>
        public override object ConvertTo(ITypeDescriptorContext typeDescriptorContext, CultureInfo cultureInfo, object value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(value);

            ArgumentNullException.ThrowIfNull(destinationType);

            if (!(value is CornerRadius))
            {
                throw new ArgumentException(SR.Format(SR.UnexpectedParameterType, value.GetType(), typeof(CornerRadius)), nameof(value));
            }

            CornerRadius cr = (CornerRadius)value;
            if (destinationType == typeof(string)) { return ToString(cr, cultureInfo); }
            if (destinationType == typeof(InstanceDescriptor))
            {
                ConstructorInfo ci = typeof(CornerRadius).GetConstructor(new Type[] { typeof(double), typeof(double), typeof(double), typeof(double) });
                return new InstanceDescriptor(ci, new object[] { cr.TopLeft, cr.TopRight, cr.BottomRight, cr.BottomLeft });
            }

            throw new ArgumentException(SR.Format(SR.CannotConvertType, typeof(CornerRadius), destinationType.FullName));
        }

        #endregion Public Methods

        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        internal static string ToString(CornerRadius cr, CultureInfo cultureInfo)
        {
            char listSeparator = TokenizerHelper.GetNumericListSeparator(cultureInfo);

            return string.Create(cultureInfo, stackalloc char[64], $"{cr.TopLeft}{listSeparator}{cr.TopRight}{listSeparator}{cr.BottomRight}{listSeparator}{cr.BottomLeft}");
        }

        internal static CornerRadius FromString(string s, CultureInfo cultureInfo)
        {
            TokenizerHelper th = new TokenizerHelper(s, cultureInfo);
            double[] radii = new double[4];
            int i = 0;

            // Peel off each Length in the delimited list.
            while (th.NextToken())
            {
                if (i >= 4)
                {
                    i = 5;    // Set i to a bad value. 
                    break;
                }

                radii[i] = double.Parse(th.GetCurrentToken(), cultureInfo);
                i++;
            }

            // We have a reasonable interpreation for one value (all four edges)
            // and four values (left, top, right, bottom).
            switch (i)
            {
                case 1:
                    return (new CornerRadius(radii[0]));

                case 4:
                    return (new CornerRadius(radii[0], radii[1], radii[2], radii[3]));
            }

            throw new FormatException(SR.Format(SR.InvalidStringCornerRadius, s));
        }
        #endregion
    }
}
