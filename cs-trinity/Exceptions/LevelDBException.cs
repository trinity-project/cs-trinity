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

using Trinity.Exceptions;
using Trinity.Exceptions.DBError;

namespace Trinity
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
    ///             User Defined. Refer to error code enum definitions.
    ///         
    ///         ErrorCode :
    ///             000 -- means Unknown Error;
    ///             001 ~ F00 -- User defined.
    ///         
    /// For example: wallets error starts from 0xE2000000.
    /////////////////////////////////////////////////////////////////////////////////////
    //// <summary>
    /// 
    /// </summary>
    public class TrinityLevelDBException : TrinityExceptionGeneric<EnumLevelDBErrorCode, EnumLevelDBErrorBase>
    {
        public TrinityLevelDBException(EnumLevelDBErrorCode innerError, string errorType = "COMMON")
            : base(innerError, errorType)
        { }

        public TrinityLevelDBException(EnumLevelDBErrorCode innerError, string message, string errorType = "COMMON")
            : base(innerError, message, errorType)
        { }

    }
}
