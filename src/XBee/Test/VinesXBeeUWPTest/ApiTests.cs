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
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using System.Threading.Tasks;

using Verdant.Vines.XBee;
using System.Collections.Generic;

namespace VinesXBeeUWPTest
{
    /// <summary>
    /// These tests require that you have two XBee devices - one coordinator and one router - attached to the 
    /// computer running these tests
    /// </summary>
    [TestClass]
    public class ApiTests
    {
        private IReadOnlyList<XBeeDevice> _devices;

        [TestInitialize]
        public async Task Setup()
        {
            _devices = await XBeeDevice.Discover();
            Assert.IsTrue(_devices.Count > 0, "No devices found!");
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
        public void SetApiMode()
        {
            var mode = _devices[0].GetApiMode();
            Assert.AreEqual((byte)1, mode);

            _devices[0].SetApiMode((byte)2);
            mode = _devices[0].GetApiMode();
            Assert.AreEqual((byte)2, mode);

            _devices[0].SetApiMode((byte)1);
            mode = _devices[0].GetApiMode();
            Assert.AreEqual((byte)1, mode);
        }

        [TestMethod]
        public void GetPayloadSize()
        {
            var payloadsize = _devices[0].GetPayloadSize();
            Assert.IsTrue(payloadsize > 0, "payload size is inexplicably zero");
        }

        [TestMethod]
        public void GetSerialNumber()
        {
            var serno = _devices[0].GetSerialNumber();
            Assert.IsTrue(serno > 0);
        }
    }
}
