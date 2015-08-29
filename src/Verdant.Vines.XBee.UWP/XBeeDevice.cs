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
        private SerialDevice _serialDevice;
        private IInputStream _input;
        private IOutputStream _output;

        public XBeeDevice(SerialDevice serialDevice)
        {
            _serialDevice = serialDevice;
            _serialDevice.ErrorReceived += _serialDevice_ErrorReceived;
            _input = _serialDevice.InputStream;
            _output = _serialDevice.OutputStream;
            Task.Run(() => ReadLoop());
        }

        private void _serialDevice_ErrorReceived(SerialDevice sender, ErrorReceivedEventArgs args)
        {
        }

        public void Dispose()
        {
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

        private async void ReadLoop()
        {
            while (true)
            {
                try
                {
                    uint length;

                    var intro = new byte[1].AsBuffer();
                    var lenBuf = new byte[2].AsBuffer();
                    await _input.ReadAsync(intro, 1, InputStreamOptions.Partial);
                    if (intro.Length > 0 && (char)intro.ToArray()[0] == 0x7e)
                    {
                        await _input.ReadAsync(lenBuf, 2, InputStreamOptions.None);
                        if (lenBuf.Length == 2)
                        {
                            var lenArray = lenBuf.ToArray();
                            length = (uint)(lenArray[0] << 8 | lenArray[1]);

                            var dataBuffer = new byte[length + 1].AsBuffer();
                            await _input.ReadAsync(dataBuffer, length + 1, InputStreamOptions.None);
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
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception during serial read: " + ex);
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
