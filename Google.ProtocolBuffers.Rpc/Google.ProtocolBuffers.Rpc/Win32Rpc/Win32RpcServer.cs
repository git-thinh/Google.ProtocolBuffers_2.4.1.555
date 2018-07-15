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
using CSharpTest.Net.RpcLibrary;

namespace Google.ProtocolBuffers.Rpc.Win32Rpc
{
    public class Win32RpcServer : RpcServer
    {
        protected readonly RpcServerApi _server;

        public static bool VerboseLogging
        {
            get { return RpcServerApi.VerboseLogging; }
            set { RpcServerApi.VerboseLogging = value; }
        }

        public Win32RpcServer(Guid iid, IRpcServerStub implementation)
            : base(implementation)
        {
            _server = new RpcServerApi(iid);
        }

        public Win32RpcServer AddProtocol(string protocol, string endpoint)
        {
            return AddProtocol(protocol, endpoint, RpcServerApi.MAX_CALL_LIMIT);
        }

        public Win32RpcServer AddProtocol(string protocol, string endpoint, uint maxCallLimit)
        {
            _server.AddProtocol(Win32RpcClient.Parse(protocol), endpoint, maxCallLimit);
            return this;
        }

        public Win32RpcServer AddAuthNegotiate()
        {
            return AddAuthentication(RpcAuthentication.RPC_C_AUTHN_GSS_NEGOTIATE);
        }

        public Win32RpcServer AddAuthWinNT()
        {
            return AddAuthentication(RpcAuthentication.RPC_C_AUTHN_WINNT);
        }

        public Win32RpcServer AddAuthentication(RpcAuthentication auth)
        {
            _server.AddAuthentication(auth, ServerPrincipalName);
            return this;
        }

        public override RpcServer StartListening()
        {
            _server.OnExecute += OnExecute;
            _server.StartListening();
            return this;
        }

        public override void StopListening()
        {
            _server.OnExecute -= OnExecute;
            _server.StopListening();
        }

        private byte[] OnExecute(IRpcClientInfo client, byte[] bytesIn)
        {
            using (MemoryStream input = new MemoryStream(bytesIn, false))
            using (MemoryStream output = new MemoryStream(1024))
            {
                base.OnExecute(new RpcClientCallInfo(client), input, output);
                return output.ToArray();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _server != null)
            {
                _server.OnExecute -= OnExecute;
                _server.Dispose();
            }
        }
    }
}