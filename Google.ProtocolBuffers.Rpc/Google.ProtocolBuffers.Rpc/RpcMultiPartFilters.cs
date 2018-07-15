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
using Google.ProtocolBuffers.Rpc.Messages;

namespace Google.ProtocolBuffers.Rpc
{
    internal class RpcMultiPartClientFilter : IRpcDispatch, IDisposable
    {
        private readonly IRpcDispatch next;
        private readonly int multiPartThreshold;
        private readonly ExtensionRegistry extensions;

        public RpcMultiPartClientFilter(IRpcDispatch next, ExtensionRegistry extensions, int threshold)
        {
            if (threshold < 2048)
            {
                throw new ArgumentOutOfRangeException();
            }
            this.next = next;
            this.extensions = extensions;
            multiPartThreshold = threshold;
        }

        public void Dispose()
        {
            if (next is IDisposable)
            {
                ((IDisposable) next).Dispose();
            }
        }

        TMessage IRpcDispatch.CallMethod<TMessage, TBuilder>(string method, IMessageLite request,
                                                             IBuilderLite<TMessage, TBuilder> response)
        {
            int size = request.SerializedSize;
            if (size < multiPartThreshold)
            {
                return next.CallMethod(method, request, response);
            }
            else
            {
                ByteString transaction = ByteString.CopyFrom(Guid.NewGuid().ToByteArray());
                byte[] requestBytes = request.ToByteArray();

                RpcMultiPartRequest.Types.RpcMessageStatus status = RpcMultiPartRequest.Types.RpcMessageStatus.CONTINUE;
                try
                {
                    int total = requestBytes.Length;
                    int amt = multiPartThreshold - 1024; //reserved for header
                    RpcMultiPartResponse mpr = RpcMultiPartResponse.DefaultInstance;

                    for (int pos = 0; pos < total; pos += amt)
                    {
                        amt = Math.Min(amt, total - pos);
                        status = (pos + amt) < total ? status : RpcMultiPartRequest.Types.RpcMessageStatus.COMPLETE;
                        mpr = next.CallMethod(".multi",
                                              RpcMultiPartRequest.CreateBuilder()
                                                  .SetTransactionId(transaction)
                                                  .SetMethodName(method)
                                                  .SetMessageStatus(status)
                                                  .SetCurrentPosition(pos)
                                                  .SetTotalBytes(total)
                                                  .SetBytesSent(amt)
                                                  .SetPayloadBytes(ByteString.CopyFrom(requestBytes, pos, amt))
                                                  .Build(),
                                              RpcMultiPartResponse.CreateBuilder()
                            );
                        if (!mpr.Continue)
                        {
                            throw new InvalidDataException("The operation was canceled by the server.");
                        }
                    }
                    if (!mpr.HasResponseBytes)
                    {
                        throw new InvalidDataException("The server did not provide a response.");
                    }

                    return response.MergeFrom(mpr.ResponseBytes.ToByteArray(), extensions).Build();
                }
                catch
                {
                    if (status == RpcMultiPartRequest.Types.RpcMessageStatus.CONTINUE)
                    {
                        try
                        {
                            next.CallMethod(".multi",
                                            RpcMultiPartRequest.CreateBuilder()
                                                .SetTransactionId(transaction)
                                                .SetMessageStatus(RpcMultiPartRequest.Types.RpcMessageStatus.CANCEL)
                                                .Build(),
                                            RpcVoid.CreateBuilder()
                                );
                        }
                        catch (Exception e)
                        {
                            Trace.TraceWarning("Unable to cancel multi-part message: {0}, error = {1}", transaction, e);
                        }
                    }
                    throw;
                }
            }
        }
    }

    internal class RpcMultiPartServerFilter : IRpcServerStub, IDisposable
    {
        private readonly IRpcServerStub next;

        public RpcMultiPartServerFilter(IRpcServerStub next)
        {
            this.next = next;
        }

        public void Dispose()
        {
            if (next is IDisposable)
            {
                (next).Dispose();
            }
        }

        public IMessageLite CallMethod(string method, ICodedInputStream input, ExtensionRegistry registry)
        {
            if (method == ".multi")
            {
                return MultiPartMessage(RpcMultiPartRequest.ParseFrom(input, registry), registry);
            }
            else
            {
                return next.CallMethod(method, input, registry);
            }
        }

        protected virtual RpcMultiPartResponse MultiPartMessage(RpcMultiPartRequest request, ExtensionRegistry registry)
        {
            RpcSession session = RpcCallContext.Session;
            string messageId = request.TransactionId.ToBase64();
            Stream message;

            if (!session.TryGetValue(messageId, out message))
            {
                if (request.CurrentPosition != 0)
                {
                    throw new InvalidDataException("The TransactionId is not valid.");
                }
                session.Add(messageId, message = CreateStream(request.TotalBytes));
                message.SetLength(request.TotalBytes);
            }
            if (request.MessageStatus == RpcMultiPartRequest.Types.RpcMessageStatus.CANCEL)
            {
                message.Dispose();
                session.Remove(messageId);
                return RpcMultiPartResponse.CreateBuilder().SetContinue(false).Build();
            }
            if (message.Position != request.CurrentPosition || message.Length != request.TotalBytes ||
                request.BytesSent != request.PayloadBytes.Length)
            {
                throw new InvalidDataException();
            }
            request.PayloadBytes.WriteTo(message);

            if (request.MessageStatus == RpcMultiPartRequest.Types.RpcMessageStatus.COMPLETE)
            {
                using (message)
                {
                    session.Remove(messageId);
                    if (message.Position != request.TotalBytes)
                    {
                        throw new InvalidDataException();
                    }
                    message.Position = 0;
                    byte[] response =
                        next.CallMethod(request.MethodName, CodedInputStream.CreateInstance(message), registry).
                            ToByteArray();
                    return RpcMultiPartResponse.CreateBuilder()
                        .SetResponseBytes(ByteString.CopyFrom(response))
                        .SetContinue(true)
                        .Build();
                }
            }
            return RpcMultiPartResponse.CreateBuilder()
                .SetContinue(true)
                .Build();
        }

        protected virtual Stream CreateStream(int totalBytes)
        {
            return new MemoryStream(totalBytes);
        }
    }
}