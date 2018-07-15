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
using Google.ProtocolBuffers.Rpc.Messages;
using Google.ProtocolBuffers.Rpc.Win32Rpc;

namespace Google.ProtocolBuffers.Rpc
{
    public abstract class RpcServer : IDisposable
    {
        private RpcErrorDetailBehavior _exceptionDetails;
        protected IRpcServerStub _implementation;
        private readonly ExtensionRegistry _extensions;
        private string _serverPrincipalName;

        protected RpcServer(IRpcServerStub implementation)
        {
            _exceptionDetails = RpcErrorDetailBehavior.FullDetails;
            _extensions = ExtensionRegistry.CreateInstance();
            _serverPrincipalName = null;
            _implementation = implementation;
        }

        ~RpcServer()
        {
            Dispose(false);
        }

        public static Win32RpcServer CreateRpc(Guid iid, IRpcServerStub implementation)
        {
            return new Win32RpcServer(iid, implementation);
        }

        /// <summary>
        ///   Controls the amount of details about excpetions that will be returned to the client. 
        ///   Default is FullDetails (trusted subsystem)
        /// </summary>
        public RpcErrorDetailBehavior ExceptionDetails
        {
            get { return _exceptionDetails; }
            set { _exceptionDetails = value; }
        }

        public ExtensionRegistry ExtensionRegistry
        {
            get { return _extensions; }
        }

        public RpcServer SetServerPrincipalName(string value)
        {
            _serverPrincipalName = value;
            return this;
        }

        public string ServerPrincipalName
        {
            get { return _serverPrincipalName; }
            set { _serverPrincipalName = value; }
        }

        public RpcServer EnableMultiPart()
        {
            if (!(_implementation is RpcMultiPartServerFilter))
            {
                _implementation = new RpcMultiPartServerFilter(_implementation);
            }
            return this;
        }

        public abstract RpcServer StartListening();
        public abstract void StopListening();

        protected virtual void OnExecute(IClientContext client, Stream input, Stream output)
        {
            RpcRequestHeader requestHeader = RpcRequestHeader.DefaultInstance;
            RpcResponseHeader.Builder responseHeader = RpcResponseHeader.CreateBuilder();
            try
            {
                requestHeader = RpcRequestHeader.ParseDelimitedFrom(input, ExtensionRegistry);
                responseHeader.SetMessageId(requestHeader.MessageId);

                IMessageLite responseBody = OnExecute(client, requestHeader, CodedInputStream.CreateInstance(input),
                                                      responseHeader);

                responseHeader.Build().WriteDelimitedTo(output);
                responseBody.WriteTo(output);
            }
            catch (Exception ex)
            {
                OnException(requestHeader, responseHeader, ex);
                responseHeader.Build().WriteDelimitedTo(output);
            }
        }

        public virtual IMessageLite OnExecute(IClientContext client, RpcRequestHeader requestHeader,
                                              CodedInputStream input, RpcResponseHeader.Builder responseHeader)
        {
            RpcCallContext previous = RpcCallContext.g_current;
            try
            {
                responseHeader.SetMessageId(requestHeader.MessageId);
                requestHeader.CallContext.Client = client;
                RpcCallContext.g_current = requestHeader.CallContext;

                IMessageLite responseBody = CallMethod(requestHeader, input);

                if (RpcCallContext.g_current != null && !requestHeader.CallContext.Equals(RpcCallContext.g_current))
                {
                    responseHeader.SetCallContext(RpcCallContext.g_current);
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                OnException(requestHeader, responseHeader, ex);
                return RpcVoid.DefaultInstance;
            }
            finally
            {
                RpcCallContext.g_current = previous;
            }
        }

        public virtual IMessageLite CallMethod(RpcRequestHeader requestHeader, CodedInputStream input)
        {
            if (requestHeader.MethodName == ".close")
            {
                Close();
                return RpcVoid.DefaultInstance;
            }
            else if (requestHeader.MethodName == ".ping")
            {
                return OnPing(RpcPingRequest.ParseFrom(input, ExtensionRegistry));
            }
            else
            {
                return _implementation.CallMethod(requestHeader.MethodName, input, ExtensionRegistry);
            }
        }

        protected virtual void OnException(RpcRequestHeader requestHeader, RpcResponseHeader.Builder responseHeader,
                                           Exception exception)
        {
            responseHeader.SetSuccess(false);
            responseHeader.ClearContentLength();
            responseHeader.SetException(RpcExceptionInfo.Create(exception, _exceptionDetails));
        }

        public event Converter<RpcPingRequest, RpcPingResponse> Ping;

        protected virtual RpcPingResponse OnPing(RpcPingRequest request)
        {
            Converter<RpcPingRequest, RpcPingResponse> ping = Ping;

            if (ping != null)
            {
                RpcPingResponse.Builder response = RpcPingResponse.CreateBuilder();
                foreach (Converter<RpcPingRequest, RpcPingResponse> del in ping.GetInvocationList())
                {
                    response.MergeFrom(del(request));
                }
                return response.Build();
            }
            return RpcPingResponse.DefaultInstance;
        }

        protected virtual void Close()
        {
            if (RpcCallContext.Current.HasSessionId)
            {
                RpcSession.KillSession(new Guid(RpcCallContext.Current.SessionId.ToByteArray()));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            if (_implementation is IDisposable)
            {
                (_implementation).Dispose();
            }
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}