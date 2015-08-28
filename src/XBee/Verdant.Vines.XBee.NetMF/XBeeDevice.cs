using System;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;

namespace Verdant.Vines.XBee
{
    public partial class XBeeDevice
    {
        private OutputPort _resetPort;
        private OutputPort _sleepPort;

        public XBeeDevice(SerialPort port, Cpu.Pin resetPin, Cpu.Pin sleepPin)
            : this(port)
        {
        }

    }
}
