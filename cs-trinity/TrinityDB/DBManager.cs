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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neo.IO.Data.LevelDB;

namespace Trinity.TrinityDB
{
    /// <summary>
    /// Database interface: Singleton Class to keep the DB write or read
    /// </summary>
    internal sealed class DBManager
    {
        private static volatile DBManager singletonInstance;
        private static readonly object newInstancelocker = new object();
        private readonly DB dbHandler;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="path"></param>
        private DBManager(string path)
        {
            path = Path.GetFullPath(path);
            Directory.CreateDirectory(path);
            this.dbHandler = DB.Open(path, new Options { CreateIfMissing = true });
        }
  
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static DBManager GetInstance(string path)
        {
            // To avoid the locker is implemented multi-times.
            if (null == singletonInstance)
            {
                lock (newInstancelocker)
                {
                    // create new instance
                    if (null == singletonInstance)
                    {
                        singletonInstance = new DBManager(path);
                    }
                }
            }
            return singletonInstance;
        }

        public DB GetDbHandler()
        {
            return this.dbHandler;
        }
        //public void Write(WriteOptions options, WriteBatch batch)
        //{
        //    singletonInstance?.dbHandler.Write(options, batch);
        //}

        //public void Put(WriteOptions options, Slice key, Slice value)
        //{
        //    singletonInstance?.dbHandler.Put(options, key, value);
        //}

        //public Slice Get(ReadOptions options, Slice key)
        //{
        //    if (null == singletonInstance) {
        //        return default;
        //    }
        //    return singletonInstance.dbHandler.Get(options, key);
        //}

        //public bool TryGet(ReadOptions options, Slice key, out Slice value)
        //{
        //    if (null == singletonInstance)
        //    {
        //        return default;
        //    }
        //    return singletonInstance.dbHandler.TryGet(options, key, out value);
        //}
    }
}
