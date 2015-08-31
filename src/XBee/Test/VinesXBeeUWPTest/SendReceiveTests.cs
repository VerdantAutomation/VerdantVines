//
// Copyright 2015 Pervasive Digital LLC
//
// Licensed for non-commercial use only, under the Apache License, 
// Version 2.0 (the "License"); you may not use this file except 
// for non-commercial purposes in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Commercial-use licenses are available. Contact licensing@verdant.io
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;

using Verdant.Vines.XBee;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;

namespace VinesXBeeUWPTest
{
    /// <summary>
    /// These tests require that you have two XBee devices - one coordinator and one router - attached to the 
    /// computer running these tests
    /// </summary>
    [TestClass]
    public class SendReceiveTests
    {
        private IReadOnlyList<XBeeDevice> _devices;
        private UInt64[] _mac = new UInt64[2];
        private ConcurrentQueue<string> _responseQueue = new ConcurrentQueue<string>();

        [TestInitialize]
        public async Task Setup()
        {
            _devices = await XBeeDevice.Discover();
            Assert.IsTrue(_devices.Count > 1, "These tests require two or more nodes attached to the test computer!");
            int retries = 10;
            bool success = false;
            do
            {
                int count = 0;
                foreach (var device in _devices)
                {
                    if (device.GetAssociationState() == XBeeDevice.AssociationIndication.Success)
                        ++count;
                }
                if (count == _devices.Count)
                {
                    success = true;
                    break;
                }
                else
                {
                    await Task.Delay(300);
                }
            } while (!success && --retries > 0);
            Assert.IsTrue(success, "Not all devices are associated yet.");
            if (success)
            {
                _mac[0] = _devices[0].GetSerialNumber();
                _mac[1] = _devices[1].GetSerialNumber();

                _devices[0].OnPacketReceived += SendReceiveTests_OnPacketReceived;
                _devices[1].OnPacketReceived += SendReceiveTests_OnPacketReceived;
            }
        }

        private void SendReceiveTests_OnPacketReceived(object sender, ulong address, ushort addr16, byte options, byte[] data)
        {
            var message = Encoding.UTF8.GetString(data);
            Debug.WriteLine("Data received : " + message);
            _responseQueue.Enqueue(message);
        }

        [TestCleanup]
        public void CleanUp()
        {
            foreach (var device in _devices)
            {
                device.Dispose();
            }
            _devices = null;
        }

        [TestMethod]
        public async Task SendTest()
        {
            var sent = "Hello World";
            _devices[0].Send(_mac[1], sent);
            await CheckResponse(sent);

            sent = "dlroW olleH";
            _devices[1].Send(_mac[0], sent);
            await CheckResponse(sent);
        }

        private async Task CheckResponse(string sent)
        { 
            string received;
            int retries = 50;
            do
            {
                if (_responseQueue.TryDequeue(out received))
                {
                    Assert.AreEqual(sent, received);
                    break;
                }
                else
                {
                    await Task.Delay(200);
                }
            } while (--retries > 0);
            Assert.IsTrue(retries > 0, "retries exhausted without receiving the sent message");
        }
    }
}
