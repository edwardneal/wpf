// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Xps.Serialization.RCW
{
    /// <summary>
    /// RCW for xpsobjectmodel.idl found in Windows SDK
    /// This is generated code with minor manual edits. 
    /// i.  Generate TLB
    ///      MIDL /TLB xpsobjectmodel.tlb xpsobjectmodel.IDL //xpsobjectmodel.IDL found in Windows SDK
    /// ii. Generate RCW in a DLL
    ///      TLBIMP xpsobjectmodel.tlb // Generates xpsobjectmodel.dll
    /// iii.Decompile the DLL and copy out the RCW by hand.
    ///      ILDASM xpsobjectmodel.dll
    /// </summary>

    internal struct XPS_MATRIX
    {
        internal float m11;

        internal float m12;

        internal float m21;

        internal float m22;

        internal float m31;

        internal float m32;
    }
}