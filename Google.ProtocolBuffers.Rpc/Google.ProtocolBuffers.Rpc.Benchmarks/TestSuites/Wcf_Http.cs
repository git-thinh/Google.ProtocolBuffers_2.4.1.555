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
using System.Net.Security;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ProtocolBuffers.Rpc.Benchmarks.TestData;

namespace ProtocolBuffers.Rpc.Benchmarks.TestSuites
{
    class Wcf_Http : WcfBase
    {
        protected override IWcfSampleService CreateService(int responseSize)
        { return new WcfSampleService(responseSize); }

        protected override Uri UriBase
        {
            get { return new Uri("http://localhost:12345", UriKind.Absolute); }
        }

        protected override IWcfSampleService CreateChannel()
        {
            return new ChannelFactory<IWcfSampleService>(
                GetBinding(BindingName), new EndpointAddress(new Uri(UriBase, "/" + BindingName)))
                .CreateChannel();
        }

        protected override Binding GetBinding(string name)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            binding.HostNameComparisonMode = HostNameComparisonMode.Exact;
            binding.Name = name;
            binding.TransferMode = TransferMode.Streamed;
            return binding;
        }
    }
}
