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

namespace VinesXBeeUWPTest
{
    /// <summary>
    /// These tests require that you have two XBee devices - one coordinator and one router - attached to the 
    /// computer running these tests
    /// </summary>
    [TestClass]
    public class DiscoveryTests
    {
        [TestMethod]
        public async Task Discover()
        {
            var devices = await XBeeDevice.Discover();
            Assert.AreEqual(2, devices.Count);
            foreach (var device in devices)
            {
                device.Dispose();
            }
        }

        [TestMethod]
        public async Task SetApiMode()
        {
            var devices = await XBeeDevice.Discover();
            Assert.IsNotNull(devices);
            Assert.IsTrue(devices.Count > 0);

            var mode = devices[0].GetApiMode();
            Assert.AreEqual((byte)1, mode);

            devices[0].SetApiMode((byte)2);
            mode = devices[0].GetApiMode();
            Assert.AreEqual((byte)2, mode);

            devices[0].SetApiMode((byte)1);
            mode = devices[0].GetApiMode();
            Assert.AreEqual((byte)1, mode);
        }
    }
}
