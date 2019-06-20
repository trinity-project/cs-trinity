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

namespace Trinity.Exceptions.WalletError
{
    /////////////////////////////////////////////////////////////////////////////////////
    /// For distinguashing from the system error code,
    /// error codes of Trinity starts from 0xE0000000 except Ok(equals to zero).
    /// Each Bits means:
    /// ---------------------------------------------------------------------------------
    /// |    E    |    0    |    0    |        00        |             000              | 
    /// ---------------------------------------------------------------------------------
    /// | 4-bits  | 4-bits  | 4-bits  |      8-bits      |           12-bits            | 
    /// ---------------------------------------------------------------------------------
    /// | Trinity | Trinity | Error   | Message Type     |            ErrorCode         |
    /// | Error   | Module  | Type    | or Function type |                              |
    /// ---------------------------------------------------------------------------------
    ///     Details:
    ///         Trinity Module :
    ///             0 -- LevelDB error;
    ///             1 -- Network error;
    ///             2 -- Wallets error;
    ///             3 ~ F -- Be reserved currently.
    ///             
    ///         Error Type :
    ///             0 -- Common Error;
    ///             1 ~ F -- User Defined. Refer to error code enum definitions.
    ///             
    ///         Message Type of Function Type :
    ///             0 -- Common Error;
    ///             1 ~ FF -- User Defined. Refer to error code enum definitions.
    ///         
    ///         ErrorCode :
    ///             000 -- means Unknown Error;
    ///             001 ~ F00 -- User defined.
    ///         
    /// For example: wallets error starts from 0xE2000000.
    /////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Transaction error base defination.
    /// </summary>
    /// Error Type: 
    ///     0 -- Common Error;
    ///     1 -- Control Plane Error;
    ///     2 -- Transaction Plane Error;
    ///     3 ~ F -- Be reserved currently.
    ///
    ////
    public enum EnumTransactionErrorBase : uint
    {
        COMMON = 0xE2000000,

        /// <summary>
        /// Transaction plane Error base
        /// </summary>
        /// Message Type or Function Type
        ///     01 -- RegisterChannel
        ///     02 -- Reserved for RegisterChannelFail or RegisterChannel response
        ///     03 -- Founder
        ///     04 -- FounderSign
        ///     05 -- Settle
        ///     06 -- SettleSign
        ///     07 -- Rsmc
        ///     08 -- RsmcSign
        ///     09 -- Htlc
        ///     0a -- HtlcSign
        ///     0b -- RResponse
        ///     0d ~ FF -- Reserved.
        REGISTERCHANNEL = 0xE2201000,
        FOUNDER = 0xE2203000,
        FOUNDERSIGN = 0xE2204000,
        SETTLE = 0xE2205000,
        SETTLESIGN = 0xE2206000,
        RSMC = 0xE2207000,
        RSMCSIGN = 0xE2208000,
        HTLC = 0xE2209000,
        HTLCSIGN = 0xE220A000,
        RRESPONSE = 0xE220B000,
    }

    /// <summary>
    /// Defines the wallets error code here.
    /// </summary>
    public enum EnumTransactionErrorCode : uint
    {
        Ok = 0,

        // Error code
        Unknown_Error = 0x1,
        Invalid_Url = 0x2,
        Transaction_With_Wrong_Signature = 0x3,

        // Asset Type related error
        NullReferrence_Asset_Type_Or_ID = 0x40,
        Unsupported_Asset_Type_Or_ID = 0x41,
        Incompatible_Asset_Type_Or_ID = 0x42,

        // Net magic related error
        NullReferrence_Net_Magic_ID = 0x50,
        Incompatible_Net_Magic_ID = 0x51,

        // Nonce Related Error
        Incompatible_Nonce = 0x58,
        Transaction_Nonce_Should_Be_largger_Than_Zero = 0x59,

        // Role related error
        NullReferrence_Role_Index = 0x70,
        Role_Index_Out_Of_Range = 0x71,

        // Channel related error
        NullReferrence_ChannelName_Or_ID = 0x100,
        Channel_Not_Found = 0x101,
        Channel_Not_Opened = 0x102,
        Channel_With_Incompatible_Balance = 0x108,
        Channel_Without_Enough_Balance_For_Payment = 0x109,
    }
}
