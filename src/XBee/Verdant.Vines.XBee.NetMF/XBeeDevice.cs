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
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT;
using System.Threading;

namespace Verdant.Vines.XBee
{
    public partial class XBeeDevice : IDisposable
    {
        private readonly OutputPort _resetPort;
        private readonly OutputPort _sleepPort;
        private readonly SerialPort _serialPort;

        public XBeeDevice(SerialPort port, Cpu.Pin resetPin, Cpu.Pin sleepPin)
        {
            _resetPort = new OutputPort(resetPin, true);
            _sleepPort = new OutputPort(sleepPin, false);
            _serialPort = port;
            new Thread(ReadThread).Start();
        }

        public void Dispose()
        {

        }

        private uint WriteFrame(byte[] data, int offset, int length)
        {
            _serialPort.Write(data, offset, length);
            return (uint)length;
        }

        private void ReadThread()
        {
            var buffer = new byte[2];

            while (true)
            {
                try
                {
                    var intro = _serialPort.ReadByte();
                    if (intro == -1)
                        break;

                    if (intro == 0x7e)
                    {
                        _serialPort.Read(buffer, 0, 2);
                        var length = buffer[0] << 8 | buffer[1];
                        var frameBuffer = new byte[length + 1];
                        _serialPort.Read(frameBuffer, 0, length + 1);

                        int sum = 0;
                        for (int i = 0; i < frameBuffer.Length - 1; ++i)
                        {
                            sum += frameBuffer[i];
                        }
                        sum = 0xff - (sum & 0xff);
                        if ((byte)sum != frameBuffer[frameBuffer.Length - 1])
                        {
                            Debug.Print("Checksum failure");
                        }
                        else
                        {
                            ProcessReceivedFrame(frameBuffer, length);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("Exception in XBee read loop : " + ex.Message);
                }
            }
        }
    }
}
