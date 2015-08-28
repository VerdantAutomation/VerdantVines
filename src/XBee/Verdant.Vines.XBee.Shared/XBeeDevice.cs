using System;
using System.IO.Ports;
using System.Text;

namespace Verdant.Vines.XBee
{
    public partial class XBeeDevice
    {
        private SerialPort _port;

        private XBeeDevice(SerialPort port)
        {
            _port = port;
        }
    }
}
