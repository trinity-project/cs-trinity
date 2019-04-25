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
using System.Security.Cryptography;

using Neo.IO.Data.LevelDB;
using Trinity.TrinityWallet;

namespace Trinity.TrinityDB
{
    public static class Utils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>String with 32 characters</returns>
        public static string ToHashString(this byte[] origin)
        {
            return BitConverter.ToString(origin).Replace("-", "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="origin"></param>
        /// <returns>32-bytes result</returns>
        public static byte[] ToHashBytes(this string origin)
        {
            byte[] sourcebytes = Encoding.Default.GetBytes(origin);
            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(sourcebytes);
        }

        public static byte[] ToBytesUtf8(this string origin)
        {
            return Encoding.UTF8.GetBytes(origin);
        }

        public static string ToBytesUtf8(this byte[] origin)
        {
            return Encoding.UTF8.GetString(origin);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="slice"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Slice Get(this DB db, SliceBuilder slice, string item)
        {
            if (null != item)
            {
                return db.Get(ReadOptions.Default, slice.Add(item.ToBytesUtf8()));
            }

            return default;
        }

        public static List<TValue> FuzzyGet<TValue>(this DB db, SliceBuilder slice)
        {
            List<TValue> channelList = new List<TValue>();

            // Fuzzy get
            foreach (var channel in db.Find(ReadOptions.Default, slice, (k, v) => new
            {
                name = k.ToArray(),
                value = v.ToString(),
            }))
            {
                // add the channel to the list
                channelList.Add(channel.value.Deserialize<TValue>());
            }

            return channelList;
        }

        public static void Add<TValue>(this DB db, SliceBuilder slice, string item, TValue value)
        {
            if (default != Get(db, slice, item))
            {
                // TODO: add logs here
                Console.WriteLine("Items {0} already exists in database", item);
                return;
            }

            Update(db, slice, item, value);
        }

        public static void Update<TValue>(this DB db, SliceBuilder slice, string item, TValue value)
        {
            WriteBatch batch = new WriteBatch();
            batch.Put(slice.Add(item.ToBytesUtf8()), value.Serialize());
            db.Write(WriteOptions.Default, batch);
        }

        public static void Delete(this DB db, SliceBuilder slice, string item)
        {
            WriteBatch batch = new WriteBatch();
            batch.Delete(slice.Add(item.ToBytesUtf8()));
            db.Write(WriteOptions.Default, batch);
        }
    }
}
