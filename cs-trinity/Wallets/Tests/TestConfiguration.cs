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

namespace Trinity.Wallets.Tests
{
    public static class TestConfiguration
    {
        // 
        public const string magic = "195378745719990331";
        public const string AssetType = "TNC";
        public const double deposit = 10;

        // TrinityTcpClient definitions
        public const string ip = "10.0.0.5";
        public const string peerIp = "10.0.0.5";
        public const string port = "8089";
        public const string peerPort = "8089";

        // one side wallet configuration
        public const string priKey = "b608e267c8e5f1316b2229504ea93fb0d3b1bfc01230fb71648b4b2823f25eda";
        public const string pubKey = "02614f837dd7025ce133312b11e70c0fac76db48bfa255eada5e0b89d0bbdc33d8";

        // other side wallet
        public const string peerPriKey = "2b70128c49b8164921947ed7e9e1a5234d265880d381b7e72d595e952984fd20";
        public const string peerPubKey = "03f082d73e5c1ed87c5d2b84ae726185ffc01b76490c1012f015b77162f5883333";

        // sender, receiver
        public static string uri = string.Format("{0}@{1}:{2}", pubKey, ip, port);
        public static string peerUri = string.Format("{0}@{1}:{2}", peerPubKey, peerIp, peerPort);
    }                                     
}
