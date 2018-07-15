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
using System.ServiceModel;
using System.ServiceModel.Channels;
using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Rpc.Win32Rpc;
using ProtocolBuffers.Rpc.Benchmarks.TestData;
using System.IO;

namespace ProtocolBuffers.Rpc.Benchmarks.TestSuites
{
    class ProtoBuf_Wcf : ProtoBufRpcBase
    {
        protected virtual string BindingName { get { return GetType().Name; } }

        protected virtual Uri UriBase
        {
            get { return new Uri("net.tcp://localhost:12345", UriKind.Absolute); }
        }

        protected virtual Binding GetBinding(string name)
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.HostNameComparisonMode = HostNameComparisonMode.Exact;
            binding.Name = name;
            binding.MaxConnections = 255;
            binding.TransferMode = TransferMode.Streamed;
            return binding;
        }

        protected override void PrepareService(Win32RpcServer service, Guid iid)
        { throw new NotSupportedException(); }

        protected override IDisposable StartServer(int responseSize)
        {
            ServiceHost host = new ServiceHost(new WcfTransportInvoke(CreateStub(responseSize)), UriBase);
            host.AddServiceEndpoint(typeof(IWcfTransportInvoke), GetBinding(BindingName), BindingName);
            host.Open();
            return host;
        }

        protected virtual IWcfTransportInvoke CreateChannel()
        {
            return new ChannelFactory<IWcfTransportInvoke>(
                GetBinding(BindingName), new EndpointAddress(new Uri(UriBase, "/" + BindingName)))
                .CreateChannel();
        }

        protected override IRpcDispatch Connect(Guid iid)
        {
            return new WcfProxy(CreateChannel());
        }

        private class WcfProxy : IRpcDispatch
        {
            private readonly IWcfTransportInvoke _channel;

            public WcfProxy(IWcfTransportInvoke channel)
            {
                _channel = channel;
            }

            #region IRpcDispatch Members

            public TMessage CallMethod<TMessage, TBuilder>(string method, IMessageLite request, IBuilderLite<TMessage, TBuilder> response)
                where TMessage : IMessageLite<TMessage, TBuilder>
                where TBuilder : IBuilderLite<TMessage, TBuilder>
            {
                Stream stream = new MemoryStream();
                CodedOutputStream output = CodedOutputStream.CreateInstance(stream);
                output.WriteStringNoTag(method);
                request.WriteTo(output);
                output.Flush();

                stream.Position = 0;
                stream = _channel.Invoke(stream);
                CodedInputStream input = CodedInputStream.CreateInstance(stream);
                response.MergeFrom(input);
                return response.Build();
            }

            #endregion
        }
    }
}
