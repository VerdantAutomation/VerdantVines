using System;
using System.Text;
using System.Threading;

namespace Verdant.Vines.XBee
{
    public partial class XBeeDevice
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

        // Avoid calling System.Text.Encoding repeatedly by just keeping a table of commands
        private static byte[] NI = { 0x4E, 0x49 };

        private byte _frameId = 0x00;
        private byte[] _sendBuffer = new byte[2048];


        public string GetNodeIdentifier()
        {
            return GetParameterValue(NI);
        }

        private ManualResetEvent _foo = new ManualResetEvent(false);
        private string GetParameterValue(byte[] command)
        {
            SendATCommand(command);
            // stall here for debugging purposes
            _foo.WaitOne();
            return "";
        }

        private void SendATCommand(byte[] command)
        {
            //TODO: this should return a wait handle of some sort
            SendFrame(Api.ATCommand, command);
            //TODO: wait for a response
        }

        private byte SendFrame(Api api, byte[] payload)
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

            return frameId;
        }

        private byte GetNextFrameId()
        {
            if (++_frameId == 0xff)
                _frameId = 0x01;
            return _frameId;
        }

        private void ProcessReceivedFrame(byte[] frame, int offset, int length)
        {

        }
    }
}
