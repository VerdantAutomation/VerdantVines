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
        }

    }
}
