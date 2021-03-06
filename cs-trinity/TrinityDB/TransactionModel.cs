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
using Trinity.TrinityDB.Definitions;

namespace Trinity.TrinityDB
{
    /// <summary>
    /// record the transaction record by channel
    /// </summary>
    public class TransactionModel : BaseModel
    {
        private readonly byte[] group;

        public SliceBuilder txHashPairGroup => SliceBuilder.Begin(ModelPrefix.MPTransactionHtlcLockPair);
        public SliceBuilder txIdGroup => SliceBuilder.Begin(ModelPrefix.MPTransactionTxId);
        public SliceBuilder record
        {
            get
            {
                if (null != this.group)
                {
                    return SliceBuilder.Begin(ModelPrefix.MPTransaction).Add(this.group);
                }
                else
                {
                    return SliceBuilder.Begin(ModelPrefix.MPTransaction);
                }
            }
        }
        

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="path"></param>
        public TransactionModel(string path, string channel) : base(path)
        {
            this.group = channel?.ToHashBytes();
        }
        
    }
}
