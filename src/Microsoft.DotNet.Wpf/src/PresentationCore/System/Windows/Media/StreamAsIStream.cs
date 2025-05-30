﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MS.Internal;
using MS.Win32;
using System.Runtime.InteropServices;

using UnsafeNativeMethods = MS.Win32.PresentationCore.UnsafeNativeMethods;

namespace System.Windows.Media
{
    #region StreamDescriptor
    [StructLayout(LayoutKind.Sequential)]
    internal struct StreamDescriptor
    {
        internal delegate void Dispose(ref StreamDescriptor pSD);
        internal delegate int Read(ref StreamDescriptor pSD, IntPtr buffer, uint cb, out uint cbRead);

        internal unsafe delegate int Seek(ref StreamDescriptor pSD, long offset, uint origin, long* plibNewPostion);
        internal delegate int Stat(ref StreamDescriptor pSD, out System.Runtime.InteropServices.ComTypes.STATSTG statstg, uint grfStatFlag);
        internal delegate int Write(ref StreamDescriptor pSD, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] buffer, uint cb, out uint cbWritten);

        internal delegate int CopyTo(ref StreamDescriptor pSD, IntPtr pstm, long cb, out long cbRead, out long cbWritten);
        internal delegate int SetSize(ref StreamDescriptor pSD, long value);
        internal delegate int Revert(ref StreamDescriptor pSD);
        internal delegate int Commit(ref StreamDescriptor pSD, UInt32 grfCommitFlags);
        internal delegate int LockRegion(ref StreamDescriptor pSD, long libOffset, long cb, uint dwLockType);
        internal delegate int UnlockRegion(ref StreamDescriptor pSD, long libOffset, long cb, uint dwLockType);
        internal delegate int Clone(ref StreamDescriptor pSD, out IntPtr stream);
        internal delegate int CanWrite(ref StreamDescriptor pSD, out bool canWrite);
        internal delegate int CanSeek(ref StreamDescriptor pSD, out bool canSeek);

        internal Dispose pfnDispose;
        internal Read pfnRead;
        
        internal Seek pfnSeek;
        
        internal Stat pfnStat;
        internal Write pfnWrite;
        
        internal CopyTo pfnCopyTo;
        
        internal SetSize pfnSetSize;
        internal Commit pfnCommit;
        internal Revert pfnRevert;
        internal LockRegion pfnLockRegion;
        internal UnlockRegion pfnUnlockRegion;
        internal Clone pfnClone;
        internal CanWrite pfnCanWrite;
        internal CanSeek pfnCanSeek;
        internal static void StaticDispose(ref StreamDescriptor pSD)
        {
            Debug.Assert(((IntPtr)pSD.m_handle) != IntPtr.Zero, "If this asserts fires: why is it firing. It might be legal in future.");
            StreamAsIStream sais = (StreamAsIStream)(pSD.m_handle.Target);
            ((System.Runtime.InteropServices.GCHandle)(pSD.m_handle)).Free();
        }

        internal System.Runtime.InteropServices.GCHandle m_handle;
    }
    #endregion

    #region StaticPtrs
    /// <summary>
    /// We need to keep the delegates alive.
    /// </summary>
    internal static class StaticPtrs
    {
        static StaticPtrs()
        {
            StaticPtrs.pfnDispose = new StreamDescriptor.Dispose(StreamDescriptor.StaticDispose);

            StaticPtrs.pfnClone = new StreamDescriptor.Clone(StreamAsIStream.Clone);
            StaticPtrs.pfnCommit = new StreamDescriptor.Commit(StreamAsIStream.Commit);
            StaticPtrs.pfnCopyTo = new StreamDescriptor.CopyTo(StreamAsIStream.CopyTo);
            StaticPtrs.pfnLockRegion = new StreamDescriptor.LockRegion(StreamAsIStream.LockRegion);
            StaticPtrs.pfnRead = new StreamDescriptor.Read(StreamAsIStream.Read);
            StaticPtrs.pfnRevert = new StreamDescriptor.Revert(StreamAsIStream.Revert);
            unsafe
            {
                StaticPtrs.pfnSeek = new StreamDescriptor.Seek(StreamAsIStream.Seek);
            }
            StaticPtrs.pfnSetSize = new StreamDescriptor.SetSize(StreamAsIStream.SetSize);
            StaticPtrs.pfnStat = new StreamDescriptor.Stat(StreamAsIStream.Stat);
            StaticPtrs.pfnUnlockRegion = new StreamDescriptor.UnlockRegion(StreamAsIStream.UnlockRegion);
            StaticPtrs.pfnWrite = new StreamDescriptor.Write(StreamAsIStream.Write);
            StaticPtrs.pfnCanWrite = new StreamDescriptor.CanWrite(StreamAsIStream.CanWrite);
            StaticPtrs.pfnCanSeek = new StreamDescriptor.CanSeek(StreamAsIStream.CanSeek);
        }

        internal static StreamDescriptor.Dispose pfnDispose;
        internal static StreamDescriptor.Read pfnRead;
        
        internal static StreamDescriptor.Seek pfnSeek;
        
        internal static StreamDescriptor.Stat pfnStat;
        internal static StreamDescriptor.Write pfnWrite;
        
        internal static StreamDescriptor.CopyTo pfnCopyTo;
        
        internal static StreamDescriptor.SetSize pfnSetSize;
        internal static StreamDescriptor.Commit pfnCommit;
        internal static StreamDescriptor.Revert pfnRevert;
        internal static StreamDescriptor.LockRegion pfnLockRegion;
        internal static StreamDescriptor.UnlockRegion pfnUnlockRegion;
        internal static StreamDescriptor.Clone pfnClone;
        internal static StreamDescriptor.CanWrite pfnCanWrite;
        internal static StreamDescriptor.CanSeek pfnCanSeek;
    }
    #endregion

    #region StreamAsIStream
    internal class StreamAsIStream
    {
        #region Instance Data
        private const int STREAM_SEEK_SET = 0x0;
        private const int STREAM_SEEK_CUR = 0x1;
        private const int STREAM_SEEK_END = 0x2;

        protected System.IO.Stream dataStream;
        private Exception _lastException;

        // to support seeking ahead of the stream length...
        private long virtualPosition = -1;
        #endregion

        #region Constructor
        private StreamAsIStream(System.IO.Stream dataStream)
        {
            this.dataStream = dataStream;
        }
        #endregion

        #region Private Methods
        private void ActualizeVirtualPosition()
        {
            if (virtualPosition == -1)
            {
                return;
            }

            if (virtualPosition > dataStream.Length)
            {
                dataStream.SetLength(virtualPosition);
            }

            dataStream.Position = virtualPosition;

            virtualPosition = -1;
        }
        #endregion

        #region StreamFunctions
        public int Clone(out IntPtr stream)
        {
            stream = IntPtr.Zero;

            try
            {
                Verify();
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.E_NOTIMPL;
        }

        public int Commit(uint grfCommitFlags)
        {
            try
            {
                Verify();
                dataStream.Flush();
                // Extend the length of the file if needed.
                ActualizeVirtualPosition();
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int CopyTo(IntPtr /* IStream */ pstm, long cb, out long cbRead, out long cbWritten)
        {
            int hr = NativeMethods.S_OK;

            uint bufsize = 4096; // one page

            byte[] buffer = new byte[bufsize];

            cbWritten = 0;
            cbRead = 0;

            try
            {
                Verify();

                while (cbWritten < cb)
                {
                    uint toRead = bufsize;

                    if (cbWritten + toRead > cb)
                    {
                        toRead  = (uint) (cb - cbWritten);
                    }

                    uint read = 0;

                    hr = Read(buffer.AsSpan(0, (int) toRead), out read);

                    if (read == 0)
                    {
                        break;
                    }

                    cbRead += read;

                    uint written = 0;

                    hr = MILIStreamWrite(pstm, buffer, read, out written);

                    if (written != read)
                    {
                        return hr;
                    }

                    cbWritten += read;
                }
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return hr;
        }

        public int LockRegion(long libOffset, long cb, uint dwLockType)
        {
            try
            {
                Verify();
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.E_NOTIMPL;
        }

        public int Read(Span<byte> buffer, out uint cbRead)
        {
            cbRead = 0;

            try
            {
                Verify();
                ActualizeVirtualPosition();

                cbRead = (uint) dataStream.Read(buffer);
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int Revert()
        {
            try
            {
                Verify();
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.E_NOTIMPL;
        }

        public unsafe int Seek(long offset, uint origin, long * plibNewPostion)
        {
            try
            {
                Verify();
                long pos = virtualPosition;

                if (virtualPosition == -1)
                {
                    pos = dataStream.Position;
                }
                long len = dataStream.Length;

                switch (origin)
                {
                    case STREAM_SEEK_SET:
                        if (offset <= len)
                        {
                            dataStream.Position = offset;
                            virtualPosition = -1;
                        }
                        else
                        {
                            virtualPosition = offset;
                        }
                        break;
                    case STREAM_SEEK_END:
                        if (offset <= 0)
                        {
                            dataStream.Position = len + offset;
                            virtualPosition = -1;
                        }
                        else
                        {
                            virtualPosition = len + offset;
                        }
                        break;
                    case STREAM_SEEK_CUR:
                        if (offset+pos <= len)
                        {
                            dataStream.Position = pos + offset;
                            virtualPosition = -1;
                        }
                        else
                        {
                            virtualPosition = offset + pos;
                        }
                        break;
                }

                if (plibNewPostion!=null)
                {
                    if (virtualPosition != -1)
                    {
                        *plibNewPostion = virtualPosition;
                    }
                    else
                    {
                        *plibNewPostion = dataStream.Position;
                    }
                }
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int SetSize(long value)
        {
            try
            {
                Verify();

                dataStream.SetLength(value);
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int Stat(out System.Runtime.InteropServices.ComTypes.STATSTG statstg, uint grfStatFlag)
        {
            System.Runtime.InteropServices.ComTypes.STATSTG statstgOut = new System.Runtime.InteropServices.ComTypes.STATSTG();
            statstg = statstgOut;

            try
            {
                Verify();

                statstgOut.type = 2; // STGTY_STREAM
                statstgOut.cbSize = dataStream.Length;
                statstgOut.grfLocksSupported = 2; //LOCK_EXCLUSIVE
                statstgOut.pwcsName = null;
                statstg = statstgOut;
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int UnlockRegion(long libOffset, long cb, uint dwLockType)
        {
            try
            {
                Verify();
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.E_NOTIMPL;
        }

        public int Write(byte[] buffer, uint cb, out uint cbWritten)
        {
            cbWritten = 0;

            try
            {
                Verify();

                ActualizeVirtualPosition();

                dataStream.Write(buffer, 0, (int) cb);

                cbWritten = cb;
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int CanWrite(out bool canWrite)
        {
            canWrite = false;

            try
            {
                Verify();

                canWrite = dataStream.CanWrite;
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }

        public int CanSeek(out bool canSeek)
        {
            canSeek = false;

            try
            {
                Verify();

                canSeek = dataStream.CanSeek;
            }
            catch (Exception e)
            {
                // store the last exception
                _lastException = e;
                return SecurityHelper.GetHRForException(e);
            }

            return NativeMethods.S_OK;
        }
        #endregion

        #region Verify
        private void Verify()
        {
            if (this.dataStream == null)
            {
                throw new System.ObjectDisposedException(SR.Media_StreamClosed);
            }
        }
        #endregion

        #region Delegate Implemetations
        internal static StreamAsIStream FromSD(ref StreamDescriptor sd)
        {
            Debug.Assert(((IntPtr)sd.m_handle) != IntPtr.Zero, "Stream is disposed.");
            System.Runtime.InteropServices.GCHandle handle = (System.Runtime.InteropServices.GCHandle)(sd.m_handle);
            return (StreamAsIStream)(handle.Target);
        }

        internal static int Clone(ref StreamDescriptor pSD, out IntPtr stream)
        {
            return (StreamAsIStream.FromSD(ref pSD)).Clone(out stream);
        }

        internal static int Commit(ref StreamDescriptor pSD, UInt32 grfCommitFlags)
        {
            return (StreamAsIStream.FromSD(ref pSD)).Commit(grfCommitFlags);
        }

        internal static int CopyTo(ref StreamDescriptor pSD, IntPtr pstm, long cb, out long cbRead, out long cbWritten)
        {
            return (StreamAsIStream.FromSD(ref pSD)).CopyTo(pstm, cb, out cbRead, out cbWritten);
        }

        internal static int LockRegion(ref StreamDescriptor pSD, long libOffset, long cb, uint dwLockType)
        {
            return (StreamAsIStream.FromSD(ref pSD)).LockRegion(libOffset, cb, dwLockType);
        }

        internal static unsafe int Read(ref StreamDescriptor pSD, IntPtr buffer, uint cb, out uint cbRead)
        {
            var span = new Span<byte>(buffer.ToPointer(), (int) cb);
            return (StreamAsIStream.FromSD(ref pSD)).Read(span, out cbRead);
        }

        internal static int Revert(ref StreamDescriptor pSD)
        {
            return (StreamAsIStream.FromSD(ref pSD)).Revert();
        }

        internal static unsafe int Seek(ref StreamDescriptor pSD, long offset, uint origin, long* plibNewPostion)
        {
            return (StreamAsIStream.FromSD(ref pSD)).Seek(offset, origin, plibNewPostion);
        }

        internal static int SetSize(ref StreamDescriptor pSD, long value)
        {
            return (StreamAsIStream.FromSD(ref pSD)).SetSize(value);
        }

        internal static int Stat(ref StreamDescriptor pSD, out System.Runtime.InteropServices.ComTypes.STATSTG statstg, uint grfStatFlag)
        {
            return (StreamAsIStream.FromSD(ref pSD)).Stat(out statstg, grfStatFlag);
        }

        internal static int UnlockRegion(ref StreamDescriptor pSD, long libOffset, long cb, uint dwLockType)
        {
            return (StreamAsIStream.FromSD(ref pSD)).UnlockRegion(libOffset, cb, dwLockType);
        }

        internal static int Write(ref StreamDescriptor pSD, byte[] buffer, uint cb, out uint cbWritten)
        {
            return (StreamAsIStream.FromSD(ref pSD)).Write(buffer, cb, out cbWritten);
        }

        internal static int CanWrite(ref StreamDescriptor pSD, out bool canWrite)
        {
            return (StreamAsIStream.FromSD(ref pSD)).CanWrite(out canWrite);
        }

        internal static int CanSeek(ref StreamDescriptor pSD, out bool canSeek)
        {
            return (StreamAsIStream.FromSD(ref pSD)).CanSeek(out canSeek);
        }
        #endregion

        // Takes an IStream that is potentially not seekable and returns
        // a seekable memory stream that is a copy of it.
        internal static IntPtr IStreamMemoryFrom(IntPtr comStream)
        {
            IntPtr pIStream = IntPtr.Zero;

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                if (HRESULT.Failed(UnsafeNativeMethods.WICImagingFactory.CreateStream(myFactory.ImagingFactoryPtr, out pIStream)))
                {
                    return IntPtr.Zero;
                }

                if (HRESULT.Failed(UnsafeNativeMethods.WICStream.InitializeFromIStream(pIStream, comStream)))
                {
                    UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref pIStream);

                    return IntPtr.Zero;
                }
            }

            return pIStream;
        }

        internal static IntPtr IStreamFrom(IntPtr memoryBuffer, int bufferSize)
        {
            IntPtr pIStream = IntPtr.Zero;

            using (FactoryMaker myFactory = new FactoryMaker())
            {
                if (HRESULT.Failed(UnsafeNativeMethods.WICImagingFactory.CreateStream(myFactory.ImagingFactoryPtr, out pIStream)))
                {
                    return IntPtr.Zero;
                }

                if (HRESULT.Failed(UnsafeNativeMethods.WICStream.InitializeFromMemory(pIStream, memoryBuffer, (uint) bufferSize)))
                {
                    UnsafeNativeMethods.MILUnknown.ReleaseInterface(ref pIStream);

                    return IntPtr.Zero;
                }
            }

            return pIStream;
        }

        #region IStreamFrom System.IO.Stream
        internal static IntPtr IStreamFrom(System.IO.Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            IntPtr pStream = IntPtr.Zero;

            StreamAsIStream sais = new StreamAsIStream(stream);
            StreamDescriptor sd = new StreamDescriptor
            {
                pfnDispose = StaticPtrs.pfnDispose,

                pfnClone = StaticPtrs.pfnClone,
                pfnCommit = StaticPtrs.pfnCommit,
                pfnCopyTo = StaticPtrs.pfnCopyTo,
                pfnLockRegion = StaticPtrs.pfnLockRegion,
                pfnRead = StaticPtrs.pfnRead,
                pfnRevert = StaticPtrs.pfnRevert
            };
            unsafe
            {
                sd.pfnSeek = StaticPtrs.pfnSeek;
            }
            sd.pfnSetSize = StaticPtrs.pfnSetSize;
            sd.pfnStat = StaticPtrs.pfnStat;
            sd.pfnUnlockRegion = StaticPtrs.pfnUnlockRegion;
            sd.pfnWrite = StaticPtrs.pfnWrite;
            sd.pfnCanWrite = StaticPtrs.pfnCanWrite;
            sd.pfnCanSeek = StaticPtrs.pfnCanSeek;

            sd.m_handle = System.Runtime.InteropServices.GCHandle.Alloc(sais, System.Runtime.InteropServices.GCHandleType.Normal);

            HRESULT.Check(UnsafeNativeMethods.MilCoreApi.MILCreateStreamFromStreamDescriptor(ref sd, out pStream));

            return pStream;
        }
        #endregion

        [DllImport(DllImport.MilCore)]//CASRemoval:
        private static extern int /* HRESULT */ MILIStreamWrite(IntPtr pStream, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]byte[] buffer, uint cb, out uint cbWritten);
    }
    #endregion
}
