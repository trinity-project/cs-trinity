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
using Trinity.TrinityWallet.Templates.Definitions;
using Trinity.TrinityWallet.TransferHandler;

namespace Trinity
{
    public class Program
    {
        public static void Main()
        {
            RegisterKeepAlive Request = new RegisterKeepAlive();

            RegisterWallet MsgHandler = new RegisterWallet(Request);
            Console.WriteLine(MsgHandler.GetJsonMessage());

            using (RegisterWallet msgHandler = new RegisterWallet("test", "port"))
            {
                Console.WriteLine(msgHandler.ToJson());
            }

                Console.ReadKey();
        }
    }
}

#if false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using Neo;
using Neo.VM;
using Neo.Wallets;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.SmartContract;
using Neo.Network.P2P;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Trinity
{
    class result
    {
        public string txData;
        public string addressFunding;
        public string txId;
        public string scriptFunding;
        public string witness;
    }

    class Program
    {
        //scriptHash转Address
        //暂用，因无法获取到gui里的参数
        public static string ToAddress1(UInt160 scriptHash)
        {
            byte[] data = new byte[21];
            data[0] = 23;           //此参数无法获取
            Buffer.BlockCopy(scriptHash.ToArray(), 0, data, 1, 20);
            return data.Base58CheckEncode();
        }

        //scriptHash转Address
        //暂用，因无法获取到gui里的参数
        public static UInt160 ToScriptHash1(string address)
        {
            byte[] data = address.Base58CheckDecode();
            if (data.Length != 21)
                throw new FormatException();
            return new UInt160(data.Skip(1).ToArray());
        }

        //long转Bytes数组方法
        public static byte[] longToBytes(long n)
        {
            byte[] b = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                b[i] = (byte)(n >> (24 - i * 8));
            }
            return b;
        }

        //string转HexString
        public static string StringToHexString(string s, Encoding encode)
        {
            byte[] b = encode.GetBytes(s);//按照指定编码将string编程字节数组
            string result = string.Empty;
            for (int i = 0; i < b.Length; i++)//逐字节变为16进制字符
            {
                result += Convert.ToString(b[i], 16);
            }
            return result;
        }

        //int转HexString
        public static string intToHex(int value)
        {
            return BigInteger.Parse(value.ToString()).ToByteArray().ToHexString();
        }

        //Hex反转
        //public static string hex_reverse(int value)
        //{
        //    tmp = binascii.unhexlify(input[2:])；
        //    be = bytearray(tmp)；
        //    be.reverse()；
        //    output = binascii.hexlify(be).decode()；
        //    return output
        //}

        //创建opdata方法
        public static string createOpdata(string address_from, string address_to, string value, string contract_hash)
        {
            string op_data = "";
            string value1 = BigInteger.Parse(value).ToByteArray().ToHexString();
            //int value1 = binascii.hexlify(BigInteger(value).ToByteArray()).decode();
            Console.WriteLine("value1:");
            Console.WriteLine(value1);
            //Console.WriteLine(ToScriptHash1(address_from));
            string scripthash_from = ToScriptHash1(address_from).ToArray().ToHexString();
            Console.WriteLine("scripthash_from:");
            Console.WriteLine(scripthash_from);
            string scripthash_to = ToScriptHash1(address_to).ToArray().ToHexString();
            Console.WriteLine("scripthash_to:");
            Console.WriteLine(scripthash_to);
            string method = StringToHexString("transfer", System.Text.Encoding.UTF8);            //7472616e73666572
            Console.WriteLine("method:");
            Console.WriteLine(method);
            string[] invoke_args = { value1, scripthash_to, scripthash_from };
            foreach (var item in invoke_args)
            {
                string[] args = { intToHex(item.Length / 2), item };
                op_data += string.Join("", args);
            }
            Console.WriteLine("op_data:");
            Console.WriteLine(op_data);
            op_data += "53";  // PUSH3
            op_data += "c1";  // PACK
            op_data += intToHex(method.Length / 2);
            op_data += method;
            op_data += "67";  // APPCALL
            Console.WriteLine("asset_id:");
            Console.WriteLine(contract_hash.Substring(2).HexToBytes().Reverse().ToArray().ToHexString());
            op_data += contract_hash.Substring(2).HexToBytes().Reverse().ToArray().ToHexString();                   //好像多去了2位  ？？？
            op_data += "f1";  // maybe THROWIFNOT

            return op_data;
        }

        //创建txData
        public static string get_tx_data()
        {
            //构造MemoryStream实例，并输出初始分配容量及使用大小 
            MemoryStream mem = new MemoryStream();
            Console.WriteLine("初始分配容量:{0}", mem.Capacity);
            Console.WriteLine("初始使用量：{0}", mem.Length);
            //将待写入的数据从字符串转换为字节数组 
            UnicodeEncoding encoder = new UnicodeEncoding();
            byte[] bytes = encoder.GetBytes("新增数据");
            //向内存流中写入数据 
            for (int i = 1; i < 4; i++)
            {
                Console.WriteLine("第{0}次写入新数据", i);
                mem.Write(bytes, 0, bytes.Length);
            }
            //写入数据后 MemoryStream 实例的容量和使用大小 
            Console.WriteLine("当前分配容量：{0}", mem.Capacity);
            Console.WriteLine("当前使用量：{0}", mem.Length);

            return "1";
        }

        static void Main(string[] args)
        {

            ECPoint[] publicKeys;
            string pk1 = "02ea3b68aa765c9af9dfa89eeb39dde03d1816493d9e11bb827940ee47ce2536cc";
            string address1 = "Acwk9RhThDhn6x47GpKmUV9SS8qmGHSimC";
            string pk2 = "02d62f3a5e56ae9e20e0803d735465e88019ef9e7545a14c611ba72bb6fdab5d52";
            string address2 = "AcnJGoFrRe5QNKjSadk7yZpw8wQuTWmiRE";
            string deposit = "10";
            string asset_id = "5e7fb71d90044445caf77c0c36df0901fda8340c";


            List<System.String> listS = new List<System.String>();
            listS.Add(pk1);
            listS.Add(pk2);                //AcnJGoFrRe5QNKjSadk7yZpw8wQuTWmiRE
            Console.WriteLine(listS);
            publicKeys = listS.Select(p => ECPoint.DecodePoint(p.HexToBytes(), ECCurve.Secp256r1)).ToArray();
            //publicKeys = listS.ToArray();
            Contract contract = Contract.CreateMultiSigContract(2, publicKeys);
            Console.WriteLine("contract：");
            Console.WriteLine(contract.GetHashCode());
            Console.WriteLine(contract.Script.ToHexString());
            Console.WriteLine("地址hash：");
            Console.WriteLine(contract.ScriptHash);
            Console.WriteLine("合约地址：");
            string contractAddress = ToAddress1(contract.ScriptHash);
            Console.WriteLine(ToAddress1(contract.ScriptHash));


            //时间戳
            TimeSpan cha = DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            long t = (long)cha.TotalSeconds;

            long t1 = 1554171549;
            //UInt160 time_stamp = new UInt160(longToBytes(t));                              //需转Uint160  ？？
            //byte[] time_stamp = longToBytes(t1).ToHexString().Substring(2).HexToBytes();
            //UInt160 time_stamp = new UInt160(longToBytes(t1).ToHexString().Substring(2).HexToBytes().ToArray());
            UInt160 time_stamp = UInt160.Parse(longToBytes(t1).ToHexString().Substring(2).PadLeft(40, '0'));

            Console.WriteLine("时间戳：");
            Console.WriteLine(t1);

            // 双方的地址hash
            ECPoint pk_1 = ECPoint.DecodePoint(pk1.HexToBytes(), ECCurve.Secp256r1);
            ECPoint pk_2 = ECPoint.DecodePoint(pk2.HexToBytes(), ECCurve.Secp256r1);
            UInt160 script_hash1 = Contract.CreateSignatureRedeemScript(pk_1).ToScriptHash();
            Console.WriteLine("script_hash1：");
            Console.WriteLine(script_hash1);
            UInt160 script_hash2 = Contract.CreateSignatureRedeemScript(pk_2).ToScriptHash();
            Console.WriteLine(Contract.CreateSignatureRedeemScript(pk_1).ToString());
            Console.WriteLine(script_hash1);
            string address = ToAddress1(script_hash1);
            Console.WriteLine(address);
            //string address = script_hash.ToAddress();       //无法获取gui的参数，暂不使用             
            byte[] addresshash = Encoding.ASCII.GetBytes(address).Sha256().Sha256().Take(4).ToArray();
            Console.WriteLine(addresshash.ToHexString());

            //string a11 = createOpdata("Acwk9RhThDhn6x47GpKmUV9SS8qmGHSimC", "AJn3EwEbxEmMVvbga5PXzVXu7PrVF1sQqU", deposit, asset_id);
            //Console.WriteLine("createOpdata：");
            //Console.WriteLine(a11);

            string op_dataSelf = createOpdata(address1, contractAddress, deposit, asset_id);
            Console.WriteLine("op_dataSelf:");
            Console.WriteLine(op_dataSelf);
            string op_dataOther = createOpdata(address2, contractAddress, deposit, asset_id);
            Console.WriteLine("op_dataOther:");
            Console.WriteLine(op_dataOther);

            List<UInt160> list1 = new List<UInt160>();
            list1.Add(script_hash1);
            list1.Add(script_hash2);
            list1.Add(time_stamp);
            UInt160[] txAttributes;
            txAttributes = list1.Select(p => p).ToArray();
            Transaction tx = new InvocationTransaction
            {
                Version = 1,
                Script = (op_dataSelf + op_dataOther).HexToBytes(),
            };
            tx.Inputs = new CoinReference[0];
            tx.Outputs = new TransactionOutput[0];
            //tx.Witnesses = new Witness[0];

            Console.WriteLine("-----11111-----");
            Console.WriteLine(tx.Version);
            
            HashSet<UInt160> sAttributes = new HashSet<UInt160>(txAttributes);
            //sAttributes.UnionWith(balances.Select(p => p.Account));

            List<TransactionAttribute> attributes = new List<TransactionAttribute>();
            attributes.AddRange(sAttributes.Select(p => new TransactionAttribute
            {
                Usage = TransactionAttributeUsage.Remark,
                Data = p.ToArray()
            }));
            tx.Attributes = attributes.ToArray();
            //Console.WriteLine("Script:");
            //Console.WriteLine((op_dataSelf + op_dataOther).HexToBytes());
            //Console.WriteLine((op_dataSelf + op_dataOther).HexToBytes().ToString());
            Console.WriteLine("----------");

            string witness;
            if (script_hash1 > script_hash2)
            {
                witness = "024140{signOther}2321" + pk2 + "ac" + "4140{signSelf}2321" + pk1 + "ac";
            }
            else
            {
                witness = "024140{signSelf}2321" + pk1 + "ac" + "4140{signOther}2321" + pk2 + "ac";
            }

            result result = new result();
            Console.WriteLine(tx.Hash);
            Console.WriteLine("Finished");
            //byte[] bb = tx.GetHashData();
            //result.txData = tx.get_tx_data();
            //result.addressFunding = contractAddress;
            //result.txId = createTxid(tx.get_tx_data());
            //result.scriptFunding = contract.Script.ToHexString();
            //result.witness = witness;

            Console.ReadLine();
        }
    }
}
#endif