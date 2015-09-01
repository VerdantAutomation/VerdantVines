using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.IO.Ports;

using Verdant.Vines.XBee;
using System.Threading;

namespace VinesXBeeMFTest
{
    public class Program
    {
        public static void Main()
        {
            var port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
            port.Open();

            var xbee = new XBeeDevice(port, (Cpu.Pin)17, (Cpu.Pin)45);

            Debug.Print("MAC address : " + DumpU64(xbee.GetSerialNumber()));

            Debug.Print("Waiting for association...");
            while (xbee.GetAssociationState() != XBeeDevice.AssociationIndication.Success)
            {
                Thread.Sleep(100);
            }

            Debug.Print("Associated - Sending test packet");
            xbee.Send(0UL, "Hello there coordinator");

            xbee.Dispose();
        }

        private static string DumpU64(UInt64 ull)
        {
            return
                ToHex((byte)(ull >> 56) & 0xff) + " " +
                ToHex((byte)(ull >> 48) & 0xff) + " " +
                ToHex((byte)(ull >> 40) & 0xff) + " " +
                ToHex((byte)(ull >> 32) & 0xff) + " " +
                ToHex((byte)(ull >> 24) & 0xff) + " " +
                ToHex((byte)(ull >> 16) & 0xff) + " " +
                ToHex((byte)(ull >>  8) & 0xff) + " " +
                ToHex((byte)(ull      ) & 0xff);
        }

        private static string _hex = "0123456789abcdef";
        private static string ToHex(int value)
        {
            return _hex[value >> 4 & 0x0f].ToString() + _hex[value & 0x0f];
        }
    }
}
