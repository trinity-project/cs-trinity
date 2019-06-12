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

namespace Trinity.Wallets.Templates.Definitions
{
    public enum TransactionErrorCode : UInt16
    {
        Ok = 0,

        // Common Error code for all messages
        Invalid_Url = 0x10,
        Invalid_Asset_Type_Or_ID = 0x11,
        Invalid_ChannelName_Or_ID = 0x12,
        Invalid_NetMagic_ID = 0x13,
        Invalid_Nonce = 0x14,
        Invalid_Role = 0x15,

        // Error Code for RegisterChannel Message
        RegisterChannel_Unkown_Error = 0x100,
        RegisterChannel_Invalid_Url = 0x101,
        RegisterChannel_Invalid_Deposit = 0x102,

        // Error Code for Founder Message
        FounderSign_Unkown_Error = 0x200,

        // Error Code for RSMC Message
        RsmcSign_Unkown_Error = 0x400,

        // Error Code for HTLC Message
        HtlcSign_Unkown_Error = 0x600,

        // Error Code for Settle Message
        SettleSign_Unkown_Error = 0x800,
        SettleSign_Balance_Not_Compatible = 0x801,

        Fail = 0xFFFF
    }
}
