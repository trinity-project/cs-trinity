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
    /// 
    /// </summary>
    public class ChannelModel : BaseModel
    {
        private readonly byte[] group;
        // private readonly byte[] peerGroup;

        public SliceBuilder summary => SliceBuilder.Begin(ModelPrefix.MPChannelSummary);
        public SliceBuilder keyword
        {
            get
            {
                if (null != this.group)
                {
                    return SliceBuilder.Begin(ModelPrefix.MPChannel).Add(this.group);
                }
                else
                {
                    return SliceBuilder.Begin(ModelPrefix.MPChannel);
                }
            }
        }

        public SliceBuilder bothKeyword
        {
            get
            {
                if (null != this.group)
                {
                    return SliceBuilder.Begin(ModelPrefix.MPChannel).Add(this.group);
                }
                else
                {
                    return SliceBuilder.Begin(ModelPrefix.MPChannel);
                }
            }
        }

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="path"></param>
        /// 
        public ChannelModel(string path, string uri, string peerUri=null) : base(path)
        {
            this.group = uri?.ToHashBytes();

            //if (null != peerUri)
            //{
            //    this.peerGroup = uri.ToHashBytes();
            //}
        }
    }
}
