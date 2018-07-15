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
using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Rpc;
using Google.ProtocolBuffers.Rpc.Win32Rpc;
using ProtocolBuffers.Rpc.Benchmarks.TestData;
using Google.ProtocolBuffers.Rpc.Messages;

namespace ProtocolBuffers.Rpc.Benchmarks.TestSuites
{
    abstract class ProtoBufRpcBase : BaseTestSuite
    {
        private static readonly Guid Iid = new Guid("{A003AF91-87ED-42fc-BE1C-5D62E86DB8B4}");
        private Win32RpcServer _service;

        protected abstract void PrepareService(Win32RpcServer service, Guid iid);
        protected abstract IRpcDispatch Connect(Guid iid);

        protected virtual IRpcServerStub CreateStub(int responseSize)
        {
            return new SampleService.ServerStub(new ServiceImpl(responseSize));
        }

        protected override IDisposable StartServer(int responseSize)
        {
            _service = RpcServer.CreateRpc(Iid, CreateStub(responseSize));
            PrepareService(_service, Iid);
            _service.StartListening();
            return _service;
        }

        protected override void RunClient(int repeatedCount, ref bool bStop, int responseSize, out int successful)
        {
            successful = 0;
            using (SampleService client = new SampleService(Connect(Iid)))
            {
                for(int count = 0; !bStop && count < repeatedCount; count++)
                {
                    SampleResponse response = client.Test(
                        SampleRequest.CreateBuilder()
                            .AddRangeData(CreateSampleData(responseSize))
                            .Build()
                        );
                    
                    GC.KeepAlive(response);
                    successful++;
                }
            }
        }

        static IEnumerable<SampleProtoData> CreateSampleData(int count)
        {
            return new List<SampleProtoData>(
                SampleData.Generate(
                    count,
                    value =>
                    SampleProtoData.CreateBuilder()
                        .SetBytes(ByteString.CopyFrom(value.Bytes))
                        .SetText(value.Text)
                        .SetNumber(value.Number)
                        .SetFloat(value.Float)
                        .SetTime(value.Time.Ticks)
                        .Build()
                    ));
        }

        class ServiceImpl : ISampleService
        {
            private readonly int _responseSize;

            public ServiceImpl(int responseSize)
            {
                _responseSize = responseSize;
            }

            public SampleResponse Test(SampleRequest sampleRequest)
            {
                return SampleResponse.CreateBuilder()
                    .AddRangeData(CreateSampleData(_responseSize))
                    .Build();
            }
        }

        protected class ImpersonatingServerStub : IRpcServerStub
        {
            private readonly IRpcServerStub _next;

            public ImpersonatingServerStub(IRpcServerStub next)
            {
                _next = next;
            }

            public IMessageLite CallMethod(string methodName, ICodedInputStream input, ExtensionRegistry registry)
            {
                using (RpcCallContext.Current.Impersonate())
                    return _next.CallMethod(methodName, input, registry);
            }

            public void Dispose()
            {
                _next.Dispose();
            }
        }
    }
}