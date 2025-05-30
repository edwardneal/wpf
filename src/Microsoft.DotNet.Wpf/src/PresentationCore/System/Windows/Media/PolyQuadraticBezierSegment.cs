// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

namespace System.Windows.Media
{
    /// <summary>
    /// PolyQuadraticBezierSegment
    /// </summary>
    public sealed partial class PolyQuadraticBezierSegment : PathSegment
    {
        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal override string ConvertToString(string format, IFormatProvider provider)
        {
            return (Points != null) ? "Q" + Points.ConvertToString(format, provider) : "";
        }
    }
}

