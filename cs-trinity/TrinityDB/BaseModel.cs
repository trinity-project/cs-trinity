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

namespace Trinity.TrinityDB
{
    public class BaseModel
    {
        private readonly DBManager dbManager;

        public BaseModel(string path)
        {
            this.dbManager = DBManager.GetInstance(path);
        }

        public DB Db => this.dbManager.GetDbHandler();

        //public virtual Slice Get(ReadOptions options, Slice key)
        //{
        //    return this.db.Get(options, key);
        //}

        //public Slice TryGet(ReadOptions options, Slice key, out Slice value)
        //{
        //    value = default;
        //    return this.db.TryGet(options, key, out value);
        //}

        //public void FuzzyGet(ReadOptions options, Slice key, out Dictionary<byte[], Slice> value)
        //{
        //    value = new Dictionary<byte[], Slice>();

        //    foreach (var tableItem in db.Find(ReadOptions.Default, key, (k, v) => new
        //    {
        //        name = k.ToArray(),
        //        value = v.ToString()
        //    }))
        //    {
        //        value.Add(tableItem.name, tableItem.value);
        //    }
        //}

        //public void Add(WriteOptions options, WriteBatch write_batch)
        //{
        //    this.db.Write(options, write_batch);
        //}

        //public void Update(WriteOptions options, WriteBatch write_batch)
        //{
        //    this.db.Write(options, write_batch);
        //}

        //public void Delete(WriteOptions options, Slice key)
        //{
        //    this.db.Delete(options, key);

        //}
    }
}
