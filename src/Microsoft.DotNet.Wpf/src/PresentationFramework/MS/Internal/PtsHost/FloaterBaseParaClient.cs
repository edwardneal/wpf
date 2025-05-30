﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


//
// Description: FloaterBaseParaClient class: Base para client class
//              for floaters and UIElements 
//

using MS.Internal.Documents;

using MS.Internal.PtsHost.UnsafeNativeMethods;

namespace MS.Internal.PtsHost
{
    // ----------------------------------------------------------------------
    // FloaterBaseParaClient class: base class for floater and UIElement
    // para clients
    // ----------------------------------------------------------------------
    internal abstract class FloaterBaseParaClient : BaseParaClient
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        #region Constructors

        // ------------------------------------------------------------------
        // Constructor.
        //
        //      paragraph - Paragraph associated with this object.
        // ------------------------------------------------------------------
        protected FloaterBaseParaClient(FloaterBaseParagraph paragraph)
            : base(paragraph)
        {
        }

        #endregion Constructors
        
        // ------------------------------------------------------------------
        // Arrange floater
        //
        //      rcFloater - rectangle of the floater
        //      rcHostPara - rectangle of the host text paragraph.
        //      fswdirParent- flow direction of parent
        //      pageContext - page context
        // ------------------------------------------------------------------
        internal virtual void ArrangeFloater(PTS.FSRECT rcFloater, PTS.FSRECT rcHostPara, uint fswdirParent, PageContext pageContext)
        {
        }
                 
        // ------------------------------------------------------------------
        // Return TextContentRange for the content of the paragraph.
        // ------------------------------------------------------------------
        internal abstract override TextContentRange GetTextContentRange();
    }
}
