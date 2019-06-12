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

namespace Trinity.TrinityDB.Definitions
{
    public enum EnumTxType : byte
    {
        // For creating channel
        FUNDING = 0,

        // For RSMC transaction
        RSMC = 0x10,
        COMMITMENT = 0x11,
        REVOCABLE = 0x12,
        BREACHREMEDY = 0x13,

        // For HTLC Transaction
        HTLC = 0x40,
        HCTX = 0x41,
        HDTX = 0x42,
        HRDTX = 0x43,
        HETX = 0x48,
        HEDTX = 0x49,
        HERDTX = 0x4a,
        HTTX = 0x50,
        HTDTX = 0x51,
        HTRDTX = 0x52,

        // For closing channel
        SETTLE =0xFF
    }
}
