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
using System.Diagnostics;
using System.IO;
using System.Net;
using Google.ProtocolBuffers.Rpc.Messages;
using Google.ProtocolBuffers.Rpc.Win32Rpc;

namespace Google.ProtocolBuffers.Rpc
{
    public enum RpcAuthenticationType
    {
        None,
        Anonymous,
        Self,
        User
    };

    public abstract class RpcClient : IRpcDispatch, IDisposable
    {
        private RpcErrorTypeBehavior _exceptionTypeResolution;
        private readonly ExtensionRegistry _extensions;
        private readonly RpcCallContext.Builder _callContext;
        protected RpcAuthenticationType _authenticatedAs;
        private string _serverPrincipalName;

        public static RpcClient ConnectRpc(Guid iid, string protocol, string server, string endpoint)
        {
            return new Win32RpcClient(iid, protocol, server, endpoint);
        }

        protected RpcClient()
        {
            _exceptionTypeResolution = RpcErrorTypeBehavior.OnlyUseLoadedAssemblies;
            _extensions = ExtensionRegistry.CreateInstance();
            _callContext = RpcCallContext.CreateBuilder();
            _authenticatedAs = RpcAuthenticationType.None;
            _serverPrincipalName = null;
        }

        ~RpcClient()
        {
            Dispose(false);
        }

        public RpcClient SetServerPrincipalName(string value)
        {
            _serverPrincipalName = value;
            return this;
        }

        public string ServerPrincipalName
        {
            get { return _serverPrincipalName; }
            set { _serverPrincipalName = value; }
        }

        public RpcClient Authenticate(RpcAuthenticationType type)
        {
            AuthenticateClient(type, null);
            _authenticatedAs = type;
            return this;
        }

        public RpcClient Authenticate(RpcAuthenticationType type, NetworkCredential credentials)
        {
            AuthenticateClient(type, credentials);
            _authenticatedAs = type;
            return this;
        }

        protected abstract void AuthenticateClient(RpcAuthenticationType type, NetworkCredential credentials);

        /// <summary>
        ///   Controls how (and if) clients resolve exception types returned by the server, by default
        ///   the client will not perform Assembly.Load() to resolve exceptions.
        /// </summary>
        public RpcErrorTypeBehavior ExceptionTypeResolution
        {
            get { return _exceptionTypeResolution; }
            set { _exceptionTypeResolution = value; }
        }

        public IRpcDispatch EnableMultiPart(int maxBytesThreshold)
        {
            return new RpcMultiPartClientFilter(this, _extensions, maxBytesThreshold);
        }

        public ExtensionRegistry ExtensionRegistry
        {
            get { return _extensions; }
        }

        public RpcCallContext CallContext
        {
            get { return _callContext.Clone().Build(); }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                _callContext.Clear().MergeFrom(value);
            }
        }

        public void Ping()
        {
            Ping(RpcPingRequest.DefaultInstance);
        }

        public RpcPingResponse Ping(RpcPingRequest request)
        {
            return CallMethod(".ping", request, RpcPingResponse.CreateBuilder());
        }

        public virtual TMessage CallMethod<TMessage, TBuilder>(string method, IMessageLite request,
                                                               IBuilderLite<TMessage, TBuilder> response)
            where TMessage : IMessageLite<TMessage, TBuilder>
            where TBuilder : IBuilderLite<TMessage, TBuilder>
        {
            CallService(method, request, response);
            return response.Build();
        }

        protected virtual void CallService(string method, IMessageLite request, IBuilderLite response)
        {
            Guid messageId = Guid.NewGuid();
            RpcRequestHeader reqHdr = RpcRequestHeader.CreateBuilder()
                .SetVersion(RpcRequestHeader.DefaultInstance.Version)
                .SetMessageId(ByteString.CopyFrom(messageId.ToByteArray()))
                .SetMethodName(method)
                .SetCallContext(_callContext.Clone().Build())
                .Build();

            RpcResponseHeader responseHeader;
            Stream responseBody;
            CallService(reqHdr, request, out responseHeader, out responseBody);
            try
            {
                RpcCommunicationException.Assert(responseHeader != null &&
                                                 messageId.Equals(new Guid(responseHeader.MessageId.ToByteArray())));
                if (responseHeader.HasCallContext)
                {
                    _callContext.Clear().MergeFrom(responseHeader.CallContext);
                }

                if (responseHeader.HasException)
                {
                    responseHeader.Exception.ReThrow(_exceptionTypeResolution);
                }

                RpcCommunicationException.Assert(responseHeader.Success && responseBody != null);

                response.WeakMergeFrom(CodedInputStream.CreateInstance(responseBody), _extensions);
            }
            finally
            {
                if (responseBody != null)
                {
                    responseBody.Dispose();
                }
            }
        }

        protected virtual void CallService(RpcRequestHeader requestHeader, IMessageLite requestBody,
                                           out RpcResponseHeader responseHeader, out Stream responseBody)
        {
            byte[] requestBytes;
            using (MemoryStream input = new MemoryStream(1024))
            {
                requestHeader.WriteDelimitedTo(input);
                requestBody.WriteTo(input);
                requestBytes = input.ToArray();
            }

            Stream output = Execute(requestBytes);

            try
            {
                responseHeader = RpcResponseHeader.ParseDelimitedFrom(output, ExtensionRegistry);
                responseBody = output;
            }
            catch
            {
                output.Dispose();
                throw;
            }
        }

        protected abstract Stream Execute(byte[] requestBytes);

        protected virtual void Close()
        {
            try
            {
                if (_callContext.HasSessionId)
                {
                    CallMethod(".close", RpcVoid.DefaultInstance, RpcVoid.CreateBuilder());
                    _callContext.ClearSessionId();
                }
            }
            catch (Exception e)
            {
                Trace.TraceWarning("Exception on close connection: {0}", e);
            }
        }

        public void Dispose()
        {
            Close();
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}