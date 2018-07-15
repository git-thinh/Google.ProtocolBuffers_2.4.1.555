#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.IO;
using Google.ProtocolBuffers;

namespace ProtocolBuffers.Rpc.Benchmarks.TestData
{
    [ServiceContract]
    interface IWcfTransportInvoke
    {
        [OperationContract]
        Stream Invoke(Stream request);
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    class WcfTransportInvoke : IWcfTransportInvoke
    {
        private readonly IRpcServerStub _createStub;

        public WcfTransportInvoke(IRpcServerStub createStub)
        {
            _createStub = createStub;
        }

        public virtual Stream Invoke(Stream request)
        {
            ICodedInputStream input = CodedInputStream.CreateInstance(request);
            string method = null;
            input.ReadString(ref method);
            IMessageLite response = _createStub.CallMethod(method, input, ExtensionRegistry.Empty);
            return new MemoryStream(response.ToByteArray());
        }
    }
}
