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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using Trinity;
using Trinity.ChannelSet;
using Trinity.ChannelSet.Definitions;
using Trinity.TrinityDB.Definitions;



namespace TestTrinity.UT.Tests
{
    internal class ChannelMock : Channel
    {
        public ChannelMock(string channel, string asset, string uri, string peerUri = null)
            : base(channel, asset, uri, peerUri)
        {
        }

        public override string dbPath()
        {
            string path = Directory.GetCurrentDirectory();
            return @".\trinity\UTLeveldb";
        }

        public void Destroy()
        {
            string path = Path.GetFullPath( this.dbPath() );

            if (Directory.Exists(path))
            {
                Log.Debug("Remove all files in UTLeveldb");
                Directory.Delete(path, true);
            }
        }
    }

    [TestFixture]
    public class UTestChannel
    {

        private const string channelNameExample = "8BD3D4DF65A23449ABB29543325FC215";
        private const string netMagic = "1234567890";

        private string assetType;
        private string uri;
        private string peerUri;
        private string channelName;
        
        
        private ChannelMock channelEntry;

        private void PrepareExampleChannel()
        {
            Assert.IsNotNull(channelEntry);

            Dictionary<string, long> deposit = new Dictionary<string, long>
            {
                { uri, 10}, { peerUri, 10}
            };


            ChannelTableContent channelContentItem = new ChannelTableContent
            {
                channel = channelNameExample,
                asset = assetType,
                uri = uri,
                peer = peerUri,
                magic = netMagic,
                role = EnumRole.FOUNDER.ToString(),
                state = EnumChannelState.INIT.ToString(),
                alive = 0,
                deposit = deposit,
                balance = deposit

            };
            channelEntry.AddChannel(channelNameExample, channelContentItem);
        }

        [SetUp]
        public void UTestChannelSetup()
        {
            assetType = "TNC";
            uri = "02614f837dd7025ce133312b11e70c0fac76db48bfa255eada5e0b89d0bbdc33d8@localhost:8089";
            peerUri = "0285593d596c6619694430d6b5e6ac18acecff83043329aae4fe408d3573d77317@localhost:8089";
            channelName = ChannelMock.NewChannel(uri, peerUri);

            channelEntry = new ChannelMock(channelName, assetType, uri, peerUri);
            channelEntry.Destroy();

            // prepare leveldb
            PrepareExampleChannel();

            Log.Debug("Test channel.cs: {0}", channelName);
        }

        [TearDown]
        public void UTestChannelTearDown()
        {
            Log.Debug("End Test Channel.cs");
        }

        [Test]
        public void TestChangeChannelState()
        {
            ChannelTableContent content = channelEntry.TryGetChannel(channelNameExample);
            Assert.IsNotNull(content);
            Assert.AreEqual(EnumChannelState.INIT.ToString(), content.state);

            // change channel state to opening
            content.state = EnumChannelState.OPENING.ToString();
            channelEntry.UpdateChannel(channelNameExample, content);
            content = channelEntry.TryGetChannel(channelNameExample);
            Assert.AreEqual(EnumChannelState.OPENING.ToString(), content.state);
        }

        [Test]
        public void TestCurrentBlockHeight()
        {
            uint expected = 123456;
            channelEntry.AddBlockHeight(this.uri, expected);
            uint blockHeight = channelEntry.TryGetBlockHeight(this.uri);
            Assert.AreEqual(expected, blockHeight);

            channelEntry.AddBlockHeight(this.uri, expected+1);
            blockHeight = channelEntry.TryGetBlockHeight(this.uri);
            Assert.AreEqual(expected+1, blockHeight);
        }

    }
}
