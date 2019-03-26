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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IO.Data.LevelDB;
using System.Runtime.InteropServices;

namespace Trinity.DB
{
    class Iterators : LevelDBBase
    { 
        internal Iterators(IntPtr Handler)
        {
            this.Handler = Handler;
        }

        /// An iterator is either positioned at a key/value pair, or not valid.  
        public bool IsValid()
        {
            return Native.leveldb_iter_valid(this.Handler) != true;
        }

        /// Position at the first key in the source
        public void SeekToFirst()
        {
            Native.leveldb_iter_seek_to_first(this.Handler);
            Throw();
        }

        /// Position at the last key in the source.
        public void SeekToLast()
        {
            Native.leveldb_iter_seek_to_last(this.Handler);
            Throw();
        }


        public void Seek(byte[] key)
        {
            Native.leveldb_iter_seek(this.Handler, key, (UIntPtr)key.Length);
            Throw();
        }

        public void Seek(string key)
        {
            Seek(Encoding.ASCII.GetBytes(key));
        }
    
        public void Next()
        {
            Native.leveldb_iter_next(this.Handler);
            Throw();
        }

        public void Prev()
        {
            Native.leveldb_iter_prev(this.Handler);
            Throw();
        }

        public string KeyAsString()
        {
            return Encoding.ASCII.GetString(this.Key());
        }

        /// <summary>
        /// Return the key for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Key()
        {
            UIntPtr length;
            var key = Native.leveldb_iter_key(this.Handler, out length);
            Throw();

            var bytes = new byte[(int)length];
            Marshal.Copy(key, bytes, 0, (int)length);
            return bytes;
        }

        public string ValueAsString()
        {
            return Encoding.ASCII.GetString(this.Value());
        }

        /// <summary>
        /// Return the value for the current entry.  
        /// REQUIRES: Valid()
        /// </summary>
        public byte[] Value()
        {
            UIntPtr length;
            var value = Native.leveldb_iter_value(this.Handler, out length);
            Throw();

            var bytes = new byte[(int)length];
            Marshal.Copy(value, bytes, 0, (int)length);
            return bytes;
        }

        /// <summary>
        /// If an error has occurred, throw it.  
        /// </summary>
        void Throw()
        {
            Throw(msg => new Exception(msg));
        }

        /// <summary>
        /// If an error has occurred, throw it.  
        /// </summary>
        void Throw(Func<string, Exception> exception)
        {
            IntPtr error;
            Native.leveldb_iter_get_error(this.Handler, out error);
            if (error != IntPtr.Zero) throw exception(Marshal.PtrToStringAnsi(error));
        }

        protected override void FreeUnDisposedObject()
        {
            Native.leveldb_iter_destroy(this.Handler);
        }
    }
}
