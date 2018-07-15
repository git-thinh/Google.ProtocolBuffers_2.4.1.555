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
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ProtocolBuffers.Rpc.Benchmarks.TestData;

namespace ProtocolBuffers.Rpc.Benchmarks.TestSuites
{
    abstract class WcfBase : BaseTestSuite
    {
        protected virtual string BindingName { get { return GetType().Name; } }

        protected abstract Uri UriBase { get; }
        protected abstract Binding GetBinding(string name);
        protected abstract IWcfSampleService CreateService(int responseSize);

        protected abstract IWcfSampleService CreateChannel();

        protected override IDisposable StartServer(int responseSize)
        {
            ServiceHost host = new ServiceHost(CreateService(responseSize), UriBase);
            host.AddServiceEndpoint(typeof(IWcfSampleService), GetBinding(BindingName), BindingName);
            host.Open();
            return host;
        }

        protected override void RunClient(int repeatedCount, ref bool bStop, int responseSize, out int successful)
        {
            successful = 0;
            IWcfSampleService client = CreateChannel();

            for (int count = 0; !bStop && count < repeatedCount; count++)
            {
                WcfSampleResponse response = client.Test(
                    new WcfSampleRequest
                    {
                        Data = SampleData.Generate(
                            responseSize,
                            d => new SampleDataContract
                            {
                                Bytes = (byte[])d.Bytes.Clone(),
                                Text = d.Text,
                                Number = d.Number,
                                Float = d.Float,
                                Time = d.Time,
                            })
                            .ToArray()
                    }
                    );

                GC.KeepAlive(response);
                successful++;
            }
        }
    }
}
