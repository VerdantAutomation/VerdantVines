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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics;
using System.Threading;

namespace Verdant.Vines.XBee
{
    /// <summary>
    /// Wrapper for low-level serial protocol access to an XBee device from a UWP program
    /// Note that your appsmanifest must include this, or something like it in the Capabilities section:
    ///     <DeviceCapability Name="serialcommunication">
    ///       <Device Id = "any" >
    ///         <Function Type="name:serialPort"/>
    ///       </Device>
    ///     </DeviceCapability>
    /// </summary>
    public partial class XBeeDevice : IDisposable
    {
        private readonly SerialDevice _serialDevice;
        private readonly IInputStream _input;
        private readonly IOutputStream _output;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public XBeeDevice(SerialDevice serialDevice)
        {
            _serialDevice = serialDevice;
            _serialDevice.ErrorReceived += _serialDevice_ErrorReceived;
            _input = _serialDevice.InputStream;
            _output = _serialDevice.OutputStream;
            Task.Run(() => ReadLoop(_cts.Token));
        }

        private void _serialDevice_ErrorReceived(SerialDevice sender, ErrorReceivedEventArgs args)
        {
        }

        public void Dispose()
        {
            _cts.Cancel();
            _input.Dispose();
            _output.Dispose();
            _serialDevice.Dispose();
        }

        public static async Task<IReadOnlyList<XBeeDevice>> Discover()
        {
            var result = new List<XBeeDevice>();

            string serialSelector = SerialDevice.GetDeviceSelector();
            var devices = await DeviceInformation.FindAllAsync(serialSelector);
            if (devices != null && devices.Count > 0)
            {
                foreach (var device in devices)
                {
                    var baudrates = new uint[] { 9600, 19200, 57600, 115200 };
                    bool success = false;
                    XBeeDevice candidate = null;
                    foreach (var baud in baudrates)
                    {
                        var serport = await SerialDevice.FromIdAsync(device.Id);
                        if (serport != null)
                        {
                            serport.BaudRate = baud;
                            candidate = new XBeeDevice(serport);
                            try
                            {
                                var hardwareVersion = candidate.GetHardwareVersion();
                                if ((hardwareVersion >> 8) != 0x19 &&
                                    (hardwareVersion >> 8) != 0x1e &&  // undocumented but valid
                                    (hardwareVersion >> 8) != 0x1a)
                                    break; // unsupported hardware
                                var firmwareVersion = candidate.GetFirmwareVersion();
                                if (((firmwareVersion >> 8) & 0xf0) != 0x20)
                                    break; // unsupported firmware
                                var ni = candidate.GetNodeIdentifier();
                                if (ni != null)
                                    success = true;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Exception during discovery: " + ex);
                                success = false;
                            }
                        }
                        if (success)
                            break;
                    }
                    if (success && candidate != null)
                    {
                        result.Add(candidate);
                    }
                }
            }

            return result.AsReadOnly();
        }

        private async void ReadLoop(CancellationToken ct)
        {
            _serialDevice.ReadTimeout = TimeSpan.FromSeconds(1.0);
            while (true)
            {
                try
                {
                    uint length;

                    var intro = new byte[1].AsBuffer();
                    var lenBuf = new byte[2].AsBuffer();
                    await _input.ReadAsync(intro, 1, InputStreamOptions.Partial);
                    if (ct.IsCancellationRequested)
                        break;
                    if (intro.Length > 0 && (char)intro.ToArray()[0] == 0x7e)
                    {
                        await _input.ReadAsync(lenBuf, 2, InputStreamOptions.None);
                        if (ct.IsCancellationRequested)
                            break;
                        if (lenBuf.Length == 2)
                        {
                            var lenArray = lenBuf.ToArray();
                            length = (uint)(lenArray[0] << 8 | lenArray[1]);

                            var dataBuffer = new byte[length + 1].AsBuffer();
                            await _input.ReadAsync(dataBuffer, length + 1, InputStreamOptions.None);
                            if (ct.IsCancellationRequested)
                                break;
                            if (dataBuffer.Length == length + 1)
                            {
                                var dataArray = dataBuffer.ToArray();

                                // compute checksum
                                int sum = 0;
                                for (int i = 0; i < dataArray.Length - 1; ++i)
                                {
                                    sum += dataArray[i];
                                }
                                sum = 0xff - (sum & 0xff);
                                if ((byte)sum != dataArray[dataArray.Length - 1])
                                {
                                    Debug.WriteLine("Checksum failure");
                                }
                                else
                                {
                                    ProcessReceivedFrame(dataArray, 0, (int)length);
                                    if (ct.IsCancellationRequested)
                                        break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception during serial read: " + ex);
                    if (ct.IsCancellationRequested)
                        return;
                }
            }
        }

        private uint Send(byte[] data, int offset, int length)
        {
            var buffer = data.AsBuffer(offset, length);
            var cbSent = _output.WriteAsync(buffer).AsTask().Result;
            return cbSent;
        }

    }
}
