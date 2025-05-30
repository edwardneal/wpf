// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//


namespace System.Windows.Media
{
    /// <summary>
    ///     TextHintingMode - Enum used for specifying how text should be rendered with respect 
    ///     to animated or static text
    /// </summary>
    public enum TextHintingMode
    {
        /// <summary>
        ///     Auto - Rendering engine will automatically determine whether to draw text with 
        ///     quality settings appropriate to animated or static text
        /// </summary>
        Auto = 0,

        /// <summary>
        ///     Fixed - Rendering engine will render text for highest static quality
        /// </summary>
        Fixed = 1,

        /// <summary>
        ///     Animated - Rendering engine will render text for highest animated quality
        /// </summary>
        Animated = 2,
    }
}
