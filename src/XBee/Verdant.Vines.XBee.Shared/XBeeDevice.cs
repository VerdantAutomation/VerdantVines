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
using System.Collections;
using System.Text;
using System.Threading;

#if MF_FRAMEWORK_VERSION_V4_3
using S = Verdant.Vines.XBee.StringExtensions;
#else
using S = System.String;
#endif

namespace Verdant.Vines.XBee
{
    public partial class XBeeDevice : IDisposable
    {
        private enum Api
        {
            Tx64 = 0x00,
            Tx16 = 0x01,
            ATCommand = 0x08,
            ATCommandQRV = 0x09,
            TransmitRequest = 0x10,
            ExplicitAddressingCommandFrame = 0x11,
            RemoteATCommand = 0x17,
            TxIPv4 = 0x20,
            CreateSourceRoute = 0x21,
            RegisterJoiningDevice = 0x24,
            Rx64Indicator = 0x80,
            Rx16Indicator = 0x81,
            IoRx64 = 0x82,
            IoRx16 = 0x83,
            OneWireRead64 = 0x84,
            NDResponseIndicator = 0x85,
            RemoteATResponse = 0x87,
            ATResponse = 0x88,
            TxStatus = 0x89,
            ModemStatus = 0x8a,
            TxStatus2 = 0x8b,
            RouteInformation = 0x8d,
            AggregateAddressingUpdate = 0x8e,
            IODataSampleReceiveIndicator = 0x8f,
            Receive = 0x90,
            ExplicitRxIndicator = 0x91,
            IODataSampleReceiveIndicator2 = 0x92,
            XBeeSensorReadIndicator = 0x94,
            NodeIdentificationIndicator = 0x95,
            RemoteCommandResponse = 0x97,
            OTAFirmwareUpdateStatus = 0xa0,
            RouteRecordIndicator = 0xa1,
            DeviceAuthenticatedIndicator = 0xa2,
            ManyToOneRouteRequestIndicator = 0xa3,
            RegisterJoiningDeviceStatus = 0xa4,
            JoinNotificationStatus = 0xa5,
            RxIPv4 = 0xb0,
        }

        public enum AssociationIndication : byte
        {
            Success = 0x00, // Successfully formed or joined a network. (Coordinators form a network, routers and end devices join a network.)
            NoPANFound = 0x21, // Scan found no PANs
            NoValidPANFound = 0x22, // Scan found no valid PANs based on current SC and ID settings
            JoinDenied = 0x23, // Valid Coordinator or Routers found, but they are not allowing joining(NJ expired)
            NoJoinableBeaconsFound = 0x24, // No joinable beacons were found
            UnexpectedState = 0x25, // Unexpected state, node should not be attempting to join at this time
            JoinFailed = 0x27, // Node Joining attempt failed (typically due to incompatible security settings)
            CoordinatorStartFailed = 0x2A, // Coordinator Start attempt failed‘
            CheckingForExistingCoord = 0x2B, // Checking for an existing coordinator
            LeaveFailed = 0x2C, // Attempt to leave the network failed
            NoResponse = 0xAB, // Attempted to join a device that did not respond.
            SJEUnsecuredKey = 0xAC, // Secure join error - network security key received unsecured
            SJEKeyNotReceived = 0xAD, // Secure join error - network security key not received
            SJEConfigError = 0xAF, // Secure join error - joining device does not have the right preconfigured link key
            Scanning = 0xFF, // Scanning for a ZigBee network (routers and end devices)        }

        private const int DefaultTimeout = 50000; // in mS

        // Avoid calling System.Text.Encoding repeatedly by just keeping a table of commands
        private static byte[] AC = { 0x41, 0x43 };  // apply changes
        private static byte[] AI = { 0x41, 0x49 };  // association indication
        private static byte[] AP = { 0x41, 0x50 };  // api mode
        private static byte[] FR = { 0x46, 0x52 };  // factory reset
        private static byte[] HV = { 0x48, 0x56 };  // hardware version
        private static byte[] NI = { 0x4E, 0x49 };  // node identifier
        private static byte[] NP = { 0x4E, 0x50 };  // max unicast payload size
        private static byte[] NR = { 0x4E, 0x51 };  // network reset
        private static byte[] SH = { 0x53, 0x48 };  // serial number high
        private static byte[] SI = { 0x53, 0x49 };  // sleep immediate
        private static byte[] SL = { 0x53, 0x4C };  // serial number low
        private static byte[] VR = { 0x56, 0x52 };  // firmware version
        private static byte[] WR = { 0x57, 0x52 };  // write changes

        private readonly Hashtable _responseRecords = new Hashtable();
        private byte _frameId = 0x00;
        private byte[] _sendBuffer = new byte[2048];

        public AssociationIndication GetAssociationState()
        {
            return (AssociationIndication)GetByteValue(AI);
        }

        public byte GetApiMode()
        {
            return GetByteValue(AP);
        }

        public void SetApiMode(byte mode)
        {
            SetByteValue(AP, mode);
        }

        public UInt64 GetSerialNumber()
        {
            var high = GetUIntValue(SH);
            var low = GetUIntValue(SL);
            return (UInt64)high << 32 | low;
        }

        public string GetNodeIdentifier()
        {
            return GetStringValue(NI);
        }

        public void SetNodeIdentifier(string ident)
        {
            if (S.IsNullOrEmpty(ident))
                ident = "";
            if (ident.Length > 20)
                throw new ArgumentException("ident must be 0-20 characters in length");
            SetStringValue(NI, ident + '\r');
        }

        public ushort GetFirmwareVersion()
        {
            return GetUShortValue(VR);
        }

        public ushort GetHardwareVersion()
        {
            return GetUShortValue(HV);
        }

        public ushort GetPayloadSize()
        {
            return GetUShortValue(NP);
        }

        public void ApplyQueuedChanges()
        {
            SendATCommand(AC);
        }

        public void SaveChanges()
        {
            SendATCommand(WR);
        }

        public void FactoryReset()
        {
            SendATCommand(FR);
        }

        public void NetworkReset(bool globalReset)
        {
            SendATCommand(NR,
                new byte[] { (byte)(globalReset ? 1 : 0) });
        }

        public void Sleep()
        {
            SendATCommand(SI);
        }

        private uint GetUIntValue(byte[] command)
        {
            var reply = SendATCommand(command);
            return (uint)(reply[5] << 24 | reply[6] << 16 | reply[7] << 8 | reply[8]);
        }

        private uint SetUIntValue(byte[] command, uint value)
        {
            var reply = SendATCommand(command,
                new byte[] {
                    (byte)(value >> 24),
                    (byte)(value >> 16),
                    (byte)(value >>  8),
                    (byte)(value & 0xff)
                });
            return (uint)(reply[5] << 24 | reply[6] << 16 | reply[7] << 8 | reply[8]);
        }

        private ushort GetUShortValue(byte[] command)
        {
            var reply = SendATCommand(command);
            return (ushort)(reply[5] << 8 | reply[6]);
        }

        private ushort SetUShortValue(byte[] command, ushort value)
        {
            var reply = SendATCommand(command, 
                new byte[] { (byte)(value >> 8), (byte)(value & 0xff) });
            return (ushort)(reply[5] << 8 | reply[6]);
        }

        private byte GetByteValue(byte[] command)
        {
            var reply = SendATCommand(command);
            return reply[5];
        }

        private byte SetByteValue(byte[] command, byte value)
        {
            var reply = SendATCommand(command, new byte[] { value });
            return reply[5];
        }

        private string GetStringValue(byte[] command)
        {
            var reply = SendATCommand(command);
            var replyLen = reply.Length - 6;
            if (replyLen == 0)
                return "";
            else
                return new string(Encoding.UTF8.GetChars(reply, 5, replyLen));
        }

        private void SetStringValue(byte[] command, string value)
        {
            var data = Encoding.UTF8.GetBytes(value);
            var buffer = new byte[data.Length + 1];
            Array.Copy(data, buffer, data.Length);
            buffer[buffer.Length - 1] = (byte)0x00;
            SendATCommand(command, data);
        }

        private byte[] SendATCommand(byte[] command, byte[] arguments =  null, int timeout = DefaultTimeout)
        {
            byte[] reply = null;
            int payloadLength;
            if (arguments == null)
                payloadLength = command.Length;
            else
                payloadLength = command.Length + arguments.Length;
            var payload = new byte[payloadLength];
            Array.Copy(command, payload, command.Length);
            if (arguments != null && arguments.Length > 0)
                Array.Copy(arguments, 0, payload, command.Length, arguments.Length);
            var response = SendFrame(Api.ATCommand, payload);
#if MF_FRAMEWORK_VERSION_V4_3
            if (response.ResponseEvent.WaitOne(timeout, false))
#else
        if (response.ResponseEvent.WaitOne(timeout))
#endif
            {
                reply = response.ResponseData;
                if ((reply[4] & 0x0f) != 0)
                    throw new XBeeCommandFailedException((uint)(reply[4] & 0x0f));
            }
            else
                throw new XBeeCommunicationTimeoutException();

            return reply;
        }

        private ResponseRecord SendFrame(Api api, byte[] payload)
        {
            var frameId = GetNextFrameId();

            var payloadLen = payload.Length;
            var len = (payloadLen + 2) & 0xffff;
            _sendBuffer[0] = 0x7e;
            _sendBuffer[1] = (byte)(len >> 8);
            _sendBuffer[2] = (byte)(len & 0xff);
            _sendBuffer[3] = (byte)api;
            _sendBuffer[4] = frameId;
            Array.Copy(payload, 0, _sendBuffer, 5, payloadLen);

            int sum = 0;
            for (int i = 3 ; i<payloadLen+5; ++i)
            {
                sum += _sendBuffer[i];
            }
            _sendBuffer[5 + payloadLen] = (byte)(0xff - (byte)(sum & 0xff));

            Send(_sendBuffer, 0, payloadLen + 6);

            var rr = new ResponseRecord(frameId);
            _responseRecords.Add(frameId, rr);
            return rr;
        }

        private byte GetNextFrameId()
        {
            if (++_frameId == 0xff)
                _frameId = 0x01;
            return _frameId;
        }

        private void ProcessReceivedFrame(byte[] frame, int offset, int length)
        {
            var api = frame[0];

            switch (api)
            {
                case (byte)Api.ATResponse:
                    lock (_responseRecords)
                    {
                        var frameId = frame[1];
                        if (_responseRecords.Contains(frameId))
                        {
                            var rr = (ResponseRecord)_responseRecords[frameId];
                            rr.ResponseData = frame;
                            _responseRecords.Remove(frameId);
                            rr.ResponseEvent.Set();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private class ResponseRecord
        {
            public ResponseRecord(byte frameId)
            {
                this.FrameId = frameId;
            }
            public byte FrameId { get; private set; }
            public readonly ManualResetEvent ResponseEvent = new ManualResetEvent(false);
            public byte[] ResponseData { get; set; }
        }
    }
}
