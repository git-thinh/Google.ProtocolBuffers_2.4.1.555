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
using System.Security.Principal;
using CSharpTest.Net.RpcLibrary;

namespace Google.ProtocolBuffers.Rpc.Win32Rpc
{
    internal class RpcClientCallInfo : IClientContext
    {
        internal readonly IRpcClientInfo Client;

        public RpcClientCallInfo(IRpcClientInfo client)
        {
            Client = client;
        }

        bool IClientContext.IsClientLocal
        {
            get { return Client.IsClientLocal; }
        }

        bool IClientContext.IsAuthenticated
        {
            get { return Client.IsAuthenticated; }
        }

        byte[] IClientContext.ClientAddress
        {
            get { return Client.ClientAddress; }
        }

        int IClientContext.ClientPid
        {
            get { return Client.ClientPid.ToInt32(); }
        }

        bool IClientContext.IsImpersonating
        {
            get { return Client.IsImpersonating; }
        }

        IDisposable IClientContext.Impersonate()
        {
            return Client.Impersonate();
        }

        IIdentity IClientContext.ClientUser
        {
            get { return Client.ClientUser; }
        }
    }
}