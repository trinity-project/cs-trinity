/*
Author: Trinity Core Team

MIT License

Copyright (c) 2018 Trinity

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Runtime.InteropServices;
using System.Text;
using Trinity.DB.NativePtr;
using System.Collections.Generic;
using Neo.IO.Data.LevelDB;

namespace Trinity.DB
{
    class Comparators : LevelDBBase
    {
        private sealed class Inner : IDisposable
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate void Destructor(IntPtr GCHandleThis);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate int Compare(IntPtr GCHandleThis,
                                         IntPtr data1, IntPtr size1,
                                         IntPtr data2, IntPtr size2);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            private delegate IntPtr Name(IntPtr GCHandleThis);


            private static Destructor destructor
                = (GCHandleThis) =>
                {
                    var h = GCHandle.FromIntPtr(GCHandleThis);
                    var This = (Inner)h.Target;

                    This.Dispose();
                    h.Free();
                };

            private static Compare compare =
                (GCHandleThis, data1, size1, data2, size2) =>
                {
                    var This = (Inner)GCHandle.FromIntPtr(GCHandleThis).Target;
                    return This.cmp(new NativeArray { baseAddr = data1, byteLength = size1 },
                                    new NativeArray { baseAddr = data2, byteLength = size2 });
                };

            private static Name nameAccessor =
                (GCHandleThis) =>
                {
                    var This = (Inner)GCHandle.FromIntPtr(GCHandleThis).Target;
                    return This.NameValue;
                };

            private Func<NativeArray, NativeArray, int> cmp;
            private GCHandle namePinned;

            public IntPtr Init(string name, Func<NativeArray, NativeArray, int> cmp)
            {
                // TODO: Complete member initialization
                this.cmp = cmp;

                this.namePinned = GCHandle.Alloc(
                    Encoding.ASCII.GetBytes(name),
                    GCHandleType.Pinned);

                var thisHandle = GCHandle.Alloc(this);

                var chandle = Native.leveldb_comparator_create(
                    GCHandle.ToIntPtr(thisHandle),
                    Marshal.GetFunctionPointerForDelegate(destructor),
                    Marshal.GetFunctionPointerForDelegate(compare),
                    Marshal.GetFunctionPointerForDelegate(nameAccessor)
                    );

                if (chandle == default(IntPtr))
                    thisHandle.Free();
                return chandle;
            }

            private unsafe IntPtr NameValue
            {
                get
                {
                    // TODO: this is probably not the most effective way to get a pinned string
                    var s = ((byte[])this.namePinned.Target);
                    fixed (byte* p = s)
                    {
                        // Note: pinning the GCHandle ensures this value should remain stable 
                        // Note:  outside of the 'fixed' block.
                        return (IntPtr)p;
                    }
                }
            }

            public void Dispose()
            {
                if (this.namePinned.IsAllocated)
                    this.namePinned.Free();
            }
        }

        private Comparators(string name, Func<NativeArray, NativeArray, int> cmp)
        {
            var inner = new Inner();
            try
            {
                this.Handler = inner.Init(name, cmp);
            }
            finally
            {
                if (this.Handler == default(IntPtr))
                    inner.Dispose();
            }
        }

        public static Comparators Create(string name, Func<NativeArray, NativeArray, int> cmp)
        {
            return new Comparators(name, cmp);
        }
        public static Comparators Create(string name, IComparer<NativeArray> cmp)
        {
            return new Comparators(name, (a, b) => cmp.Compare(a, b));
        }

        protected override void FreeUnDisposedObject()
        {
            if (this.Handler != default(IntPtr))
            {
                // indirectly invoked CleanupInner
                Native.leveldb_comparator_destroy(this.Handler);
            }
        }
    }
}
