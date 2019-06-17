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

namespace Trinity.Exceptions.DBError
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
    /// LevelDB error base defination.
    /// </summary>
    /// Error Type: 
    ///     0 -- Common Error;
    ///     1 -- ChannelModel Error;
    ///     2 -- TransactionModel Error;
    ///     3 -- BlockModel Error;
    ///     4 ~ F -- Be reserved currently.
    public enum EnumLevelDBErrorBase : uint
    {
        COMMON = 0xE0000000,

        /// <summary>
        /// LevelDB error code base definition
        /// </summary>
        /// Message Type or Function Type Definition is mapping to class -- ModelPrefix
        ///     0x01 -- Channel Summary error;
        ///     0x10 -- Channel Details error;
        ///     0x20 -- Transaction Details error;
        ///     0x21 -- Transaction Id error;
        ///     0x22 -- Transaction Htlc HashR-R lock pair error;
        ///     0x40 -- Latest Block height record error;
        ///     Others -- Reserved.
        CHANNEL_TABLE_SUMMARY = 0xE0101000,
        CHANNEL_TABLE_DETAIL = 0xE0110000,
        TRANSACTION_TABLE_DETAIL = 0xE0220000,
        TRANSACTION_TABLE_ID = 0xE0221000,
        TRANSACTION_TABLE_HLOCK = 0xE0222000,
        BLOCK_TABLE_HEIGHT = 0xE0340000,
    }

    /// <summary>
    /// Defines the Database error code here.
    /// </summary>
    public enum EnumLevelDBErrorCode : uint
    {
        Ok = 0,

        // Error Definitions
        // 0x1 ~ 0x3F Common error definitions
        Unknown_Error = 0x1,
        Not_Found = 0x2,

        // 0x40 ~ 0x7F: Channel table related error
        Channel_Not_Found = 0x40,

        // 0x80 ~ 0xFF: Transaction related error
        Transaction_Contents_Not_Found = 0x80,

        // 0x100 ~ 0x13F : Block Information related error
        Block_Height_Info_Not_Found = 0x100,
    }
}
