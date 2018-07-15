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

namespace Google.ProtocolBuffers.Rpc.Messages
{
    partial class RpcCallContext
    {
        [ThreadStatic] internal static RpcCallContext g_current;

        public static RpcCallContext Current
        {
            get { return g_current ?? DefaultInstance; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (g_current == null)
                {
                    throw new InvalidOperationException();
                }
                value._client = g_current._client;
                g_current = value;
            }
        }

        public static RpcSession Session
        {
            get
            {
                if (g_current == null)
                {
                    throw new InvalidOperationException();
                }
                if (g_current.hasSessionId == false)
                {
                    Current =
                        Current.ToBuilder().SetSessionId(ByteString.CopyFrom(Guid.NewGuid().ToByteArray())).Build();
                }
                return Current._session ??
                       (Current._session = RpcSession.GetSession(new Guid(Current.SessionId.ToByteArray())));
            }
        }

        private IClientContext _client;
        private RpcSession _session;

        public IClientContext Client
        {
            get
            {
                if (_client == null)
                {
                    throw new InvalidOperationException();
                }
                return _client;
            }
            set { _client = value; }
        }

        public bool IsClientLocal
        {
            get { return Client.IsClientLocal; }
        }

        public byte[] ClientAddress
        {
            get { return Client.ClientAddress; }
        }

        public int ClientPid
        {
            get { return Client.ClientPid; }
        }

        public bool IsImpersonating
        {
            get { return Client.IsImpersonating; }
        }

        public IDisposable Impersonate()
        {
            return Client.Impersonate();
        }

        public IIdentity ClientUser
        {
            get { return Client.ClientUser; }
        }
    }
}