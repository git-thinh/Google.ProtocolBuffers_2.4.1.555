#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.IO;
using System.Net;
using CSharpTest.Net.RpcLibrary;

namespace Google.ProtocolBuffers.Rpc.Win32Rpc
{
    public class Win32RpcClient : RpcClient
    {
        private readonly RpcClientApi _client;

        public Win32RpcClient(Guid iid, string protocol, string server, string endpoint)
        {
            _client = new RpcClientApi(iid, Parse(protocol), server, endpoint);
        }

        protected override void AuthenticateClient(RpcAuthenticationType type, NetworkCredential credentials)
        {
            switch (type)
            {
                case RpcAuthenticationType.User:
                    _client.AuthenticateAs(ServerPrincipalName, credentials);
                    break;
                case RpcAuthenticationType.Self:
                    _client.AuthenticateAs(ServerPrincipalName, RpcClientApi.Self);
                    break;
                default:
                    _client.AuthenticateAs(ServerPrincipalName, RpcClientApi.Anonymous);
                    break;
            }
        }

        protected override Stream Execute(byte[] requestBytes)
        {
            return new MemoryStream(_client.Execute(requestBytes), false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _client != null)
            {
                _client.Dispose();
            }
        }

        internal static RpcProtseq Parse(string protocol)
        {
            try
            {
                return (RpcProtseq) Enum.Parse(typeof (RpcProtseq), protocol, true);
            }
            catch
            {
                throw new ArgumentException("Unknown protocol: {0}", protocol);
            }
        }
    }
}