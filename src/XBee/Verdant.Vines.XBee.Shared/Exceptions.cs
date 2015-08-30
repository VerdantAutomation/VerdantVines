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

namespace Verdant.Vines.XBee
{
    public class XBeeCommunicationTimeoutException : Exception
    {
        public XBeeCommunicationTimeoutException()
        {

        }
    }

    public class XBeeCommandFailedException : Exception
    {
        public XBeeCommandFailedException(uint errCode)
        {
            this.ErrorCode = errCode;
        }

        public uint ErrorCode { get; private set; }
    }
}
