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
using System.Runtime.InteropServices;
using System.Security.Principal;
using Google.ProtocolBuffers.Rpc;
using Google.ProtocolBuffers.Rpc.Messages;
using Google.ProtocolBuffers.Rpc.Win32Rpc;
using Google.ProtocolBuffers.SampleServices;
using Google.ProtocolBuffers.TestProtos;
using NUnit.Framework;

namespace Google.ProtocolBuffers
{
    [TestFixture]
    public class RpcLibraryTest
    {
        private readonly Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));

        [Test]
        public void AuthNegotiateTest()
        {
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddAuthNegotiate()
                .AddProtocol("ncacn_ip_tcp", "12345")
                .StartListening())
            {
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "127.0.0.1", "12345").Authenticate(
                                RpcAuthenticationType.Self)))
                {
                    SearchResponse results =
                        client.Search(SearchRequest.CreateBuilder().AddCriteria("Test Criteria").Build());
                    Assert.AreEqual(1, results.ResultsCount);
                }
            }
        }

        [Test]
        public void AuthWinNTTest()
        {
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddAuthWinNT()
                .AddProtocol("ncacn_ip_tcp", "12345")
                .StartListening())
            {
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "127.0.0.1", "12345").Authenticate(
                                RpcAuthenticationType.Self)))
                {
                    SearchResponse results =
                        client.Search(SearchRequest.CreateBuilder().AddCriteria("Test Criteria").Build());
                    Assert.AreEqual(1, results.ResultsCount);
                }
            }
        }

        [Test]
        public void SimplePingTest()
        {
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddProtocol("ncacn_ip_tcp", "12345")
                .AddAuthNegotiate()
                .StartListening())
            {
                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "127.0.0.1", "12345").Authenticate(
                            RpcAuthenticationType.Self))
                {
                    client.Ping();
                }
            }
        }

        [Test]
        public void PingWithExtensionTest()
        {
            using (RpcServer server = RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddProtocol("ncacn_np", @"\pipe\p1"))
            {
                UnitTestRpcInterop.RegisterAllExtensions(server.ExtensionRegistry);
                server.Ping += delegate(RpcPingRequest r)
                                   {
                                       if (r.HasExtension(UnitTestRpcInterop.CustomPingDataIn))
                                       {
                                           return RpcPingResponse.CreateBuilder()
                                               .SetExtension(UnitTestRpcInterop.CustomPingDataOut,
                                                             r.GetExtension(UnitTestRpcInterop.CustomPingDataIn))
                                               .Build();
                                       }
                                       return RpcPingResponse.DefaultInstance;
                                   };
                server.StartListening();

                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_np", null, @"\pipe\p1").Authenticate(
                            RpcAuthenticationType.Anonymous))
                {
                    UnitTestRpcInterop.RegisterAllExtensions(client.ExtensionRegistry);

                    RpcPingRequest r = RpcPingRequest.CreateBuilder()
                        .SetExtension(UnitTestRpcInterop.CustomPingDataIn, "ping-request-data")
                        .Build();
                    RpcPingResponse response = client.Ping(r);
                    Assert.IsTrue(response.HasExtension(UnitTestRpcInterop.CustomPingDataOut));
                    Assert.AreEqual("ping-request-data", response.GetExtension(UnitTestRpcInterop.CustomPingDataOut));
                }
            }
        }

        [Test]
        public void RoundTripCallContextExtensionsTest()
        {
            using (RpcServer server = RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddProtocol("ncacn_np", @"\pipe\p1"))
            {
                UnitTestRpcInterop.RegisterAllExtensions(server.ExtensionRegistry);
                server.Ping += delegate
                                   {
                                       String strValue =
                                           RpcCallContext.Current.GetExtension(UnitTestRpcInterop.CustomContextString);
                                       UInt64 intValue =
                                           RpcCallContext.Current.GetExtension(UnitTestRpcInterop.CustomContextNumber);

                                       char[] tmp = strValue.ToCharArray();
                                       Array.Reverse(tmp);
                                       strValue = new string(tmp);
                                       intValue = ~intValue;

                                       RpcCallContext.Current = RpcCallContext.Current.ToBuilder()
                                           .SetExtension(UnitTestRpcInterop.CustomContextString, strValue)
                                           .SetExtension(UnitTestRpcInterop.CustomContextNumber, intValue)
                                           .Build();

                                       return RpcPingResponse.DefaultInstance;
                                   };
                server.StartListening();

                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_np", null, @"\pipe\p1").Authenticate(
                            RpcAuthenticationType.Anonymous))
                {
                    UnitTestRpcInterop.RegisterAllExtensions(client.ExtensionRegistry);

                    client.CallContext = client.CallContext.ToBuilder()
                        .SetExtension(UnitTestRpcInterop.CustomContextString, "abc")
                        .SetExtension(UnitTestRpcInterop.CustomContextNumber, 0x70000000FFFFFFFFUL)
                        .Build();
                    client.Ping();

                    Assert.AreEqual("cba", client.CallContext.GetExtension(UnitTestRpcInterop.CustomContextString));
                    Assert.AreEqual(~0x70000000FFFFFFFFUL,
                                    client.CallContext.GetExtension(UnitTestRpcInterop.CustomContextNumber));
                }
            }
        }

        [Test]
        public void MultiPartMessageTest()
        {
            //Notice that both client and server must enable multi-part messages...
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddAuthNegotiate()
                .AddProtocol("ncacn_ip_tcp", "12345")
                .EnableMultiPart()
                .StartListening())
            {
                using (
                    SearchService client = new SearchService(RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "::1", "12345")
                                                                 .Authenticate(RpcAuthenticationType.Self).
                                                                 EnableMultiPart(1000000)))
                {
                    // Non-LRPC channels have limitations on message sizes, we use multiple calls to forward large messages
                    // and store state on the server in the RpcSession associated with this client.  This is all transparent
                    // to the caller, but this 7 meg message will produce several rpc-calls.
                    SearchRequest.Builder criteria = SearchRequest.CreateBuilder();
                    byte[] bytes = new byte[2500];
                    Random r = new Random();
                    for (int i = 0; i < 2500; i++)
                    {
                        r.NextBytes(bytes);
                        criteria.AddCriteria(Convert.ToBase64String(bytes));
                    }
                    SearchResponse results = client.Search(criteria.Build());
                    Assert.AreEqual(2500, results.ResultsCount);
                }
            }
        }

        [Test, ExpectedException(typeof (MissingMethodException))]
        public void MultiPartMessageFailsWithoutHandler()
        {
            //Notice that both client and server must enable multi-part messages...
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddAuthNegotiate()
                .AddProtocol("ncacn_ip_tcp", "12345")
//Omit server response:        .EnableMultiPart()
                .StartListening())
            {
                using (
                    SearchService client = new SearchService(RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "::1", "12345")
                                                                 .Authenticate(RpcAuthenticationType.Self).
                                                                 EnableMultiPart(1000000)))
                {
                    SearchRequest.Builder criteria = SearchRequest.CreateBuilder();
                    byte[] bytes = new byte[1000000];
                    new Random().NextBytes(bytes);
                    SearchResponse results = client.Search(criteria.AddCriteria(Convert.ToBase64String(bytes)).Build());
                    Assert.AreEqual(2500, results.ResultsCount);
                }
            }
        }

        [Test]
        public void MultiPartMessageCancel()
        {
            //Notice that both client and server must enable multi-part messages...
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddAuthNegotiate()
                .AddProtocol("ncacn_ip_tcp", "12345")
                .EnableMultiPart()
                .StartListening())
            {
                ByteString transaction = ByteString.CopyFrom(Guid.NewGuid().ToByteArray());
                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "::1", "12345").Authenticate(
                            RpcAuthenticationType.Self))
                {
                    RpcMultiPartResponse response = client.CallMethod(".multi",
                                                                      RpcMultiPartRequest.CreateBuilder()
                                                                          .SetTransactionId(transaction)
                                                                          .SetMessageStatus(
                                                                              RpcMultiPartRequest.Types.RpcMessageStatus
                                                                                  .CONTINUE)
                                                                          .SetMethodName("Whatever")
                                                                          .SetCurrentPosition(0)
                                                                          .SetBytesSent(1000)
                                                                          .SetTotalBytes(2000)
                                                                          .SetPayloadBytes(
                                                                              ByteString.CopyFrom(new byte[1000]))
                                                                          .Build(),
                                                                      RpcMultiPartResponse.CreateBuilder());

                    Assert.IsTrue(response.Continue);

                    response = client.CallMethod(".multi",
                                                 RpcMultiPartRequest.CreateBuilder()
                                                     .SetTransactionId(transaction)
                                                     .SetMessageStatus(RpcMultiPartRequest.Types.RpcMessageStatus.CANCEL)
                                                     .Build(),
                                                 RpcMultiPartResponse.CreateBuilder());
                    Assert.IsFalse(response.Continue);
                }
            }
        }

        [Test]
        public void LrpcPerformanceTest()
        {
            Win32RpcServer.VerboseLogging = false;
            try
            {
                using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                    .AddAuthNegotiate()
                    .AddProtocol("ncalrpc", "lrpctest")
                    .StartListening())
                {
                    using (
                        SearchService client =
                            new SearchService(
                                RpcClient.ConnectRpc(iid, "ncalrpc", null, "lrpctest").Authenticate(
                                    RpcAuthenticationType.Self)))
                    {
                        SearchResponse previous = client.Search(SearchRequest.CreateBuilder()
                                                                    .AddCriteria("one").AddCriteria("two").AddCriteria(
                                                                        "three").Build());
                        RefineSearchRequest req = RefineSearchRequest.CreateBuilder()
                            .AddCriteria("four").SetPreviousResults(previous).Build();

                        Stopwatch w = new Stopwatch();
                        w.Start();
                        for (int i = 0; i < 1000; i++)
                        {
                            client.RefineSearch(req);
                        }
                        w.Stop();
                        Trace.TraceInformation("Lrpc Performance = {0}", w.ElapsedMilliseconds);
                    }
                }
            }
            finally
            {
                Win32RpcServer.VerboseLogging = true;
            }
        }

        #region InprocRpcClient

        private class InprocRpcClient : RpcClient
        {
            private readonly InprocRpcServer server;

            public InprocRpcClient(IRpcServerStub stub)
            {
                server = new InprocRpcServer(stub);
            }

            private class ClientContext : IClientContext, IDisposable
            {
                public bool IsClientLocal
                {
                    get { return false; }
                }

                public byte[] ClientAddress
                {
                    get { return new byte[0]; }
                }

                public int ClientPid
                {
                    get { return 0; }
                }

                public bool IsAuthenticated
                {
                    get { return true; }
                }

                public bool IsImpersonating
                {
                    get { return true; }
                }

                public IIdentity ClientUser
                {
                    get { return WindowsIdentity.GetCurrent(); }
                }

                public IDisposable Impersonate()
                {
                    return this;
                }

                void IDisposable.Dispose()
                {
                }
            }

            private class InprocRpcServer : RpcServer
            {
                public InprocRpcServer(IRpcServerStub stub) : base(stub)
                {
                }

                public override RpcServer StartListening()
                {
                    return this;
                }

                public override void StopListening()
                {
                }

                public void Execute(Stream input, Stream output)
                {
                    base.OnExecute(new ClientContext(), input, output);
                }
            }

            protected override void AuthenticateClient(RpcAuthenticationType type, NetworkCredential credentials)
            {
            }

            protected override Stream Execute(byte[] requestBytes)
            {
                using (MemoryStream input = new MemoryStream(requestBytes, false))
                {
                    MemoryStream output = new MemoryStream();
                    server.Execute(input, output);
                    output.Position = 0;
                    return output;
                }
            }
        }

        #endregion InprocRpcClient

        /// <summary>
        ///   Tests all the packaging/serializaiton without the wire transport.
        /// </summary>
        [Test]
        public void InprocPerformanceTest()
        {
            using (
                SearchService client =
                    new SearchService(new InprocRpcClient(new SearchService.ServerStub(new AuthenticatedSearch()))))
            {
                SearchResponse previous = client.Search(SearchRequest.CreateBuilder()
                                                            .AddCriteria("one").AddCriteria("two").AddCriteria("three").
                                                            Build());
                RefineSearchRequest req = RefineSearchRequest.CreateBuilder()
                    .AddCriteria("four").SetPreviousResults(previous).Build();

                Stopwatch w = new Stopwatch();
                w.Start();
                for (int i = 0; i < 1000; i++)
                {
                    client.RefineSearch(req);
                }
                w.Stop();
                Trace.TraceInformation("Inproc Performance = {0}", w.ElapsedMilliseconds);
            }
        }

        [Test]
        public void ClientInformationTest()
        {
            //Create the server with a stub pointing to our implementation
            using (Win32RpcServer server = RpcServer.CreateRpc(iid, new ClientInformationFilter())
                .AddAuthWinNT()
                .AddProtocol("ncalrpc", "lrpctest")
                .AddProtocol("ncacn_ip_tcp", "12345")
                .AddProtocol("ncacn_np", @"\pipe\p1"))
            {
                server.StartListening();

                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncalrpc", null, "lrpctest").Authenticate(RpcAuthenticationType.Self))
                    client.CallMethod("ncalrpc", RpcVoid.DefaultInstance, RpcVoid.CreateBuilder());

                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "127.0.0.1", "12345").Authenticate(
                            RpcAuthenticationType.Self))
                    client.CallMethod("ncacn_ip_tcp", RpcVoid.DefaultInstance, RpcVoid.CreateBuilder());

                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_np", @"\\localhost", @"\pipe\p1").Authenticate(
                            RpcAuthenticationType.Anonymous))
                    client.CallMethod("ncacn_np-Anonymous", RpcVoid.DefaultInstance, RpcVoid.CreateBuilder());

                server.AddAuthNegotiate(); //winnt authentication not supported over pipes... need to allow nego
                using (
                    RpcClient client =
                        RpcClient.ConnectRpc(iid, "ncacn_np", @"\\localhost", @"\pipe\p1").Authenticate(
                            RpcAuthenticationType.Self))
                    client.CallMethod("ncacn_np", RpcVoid.DefaultInstance, RpcVoid.CreateBuilder());
            }
        }

        #region ClientInformationFilter

        private class ClientInformationFilter : IRpcServerStub
        {
            void IDisposable.Dispose()
            {
            }

            public IMessageLite CallMethod(string methodName, ICodedInputStream input, ExtensionRegistry registry)
            {
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                if (currentUser == null)
                {
                    throw new ArgumentNullException();
                }

                IClientContext ctx = RpcCallContext.Current.Client;
                switch (methodName)
                {
                    case "ncalrpc":
                        {
                            Assert.AreEqual(new byte[0], ctx.ClientAddress);
                            Assert.AreEqual(Process.GetCurrentProcess().Id, ctx.ClientPid);
                            Assert.AreEqual(true, ctx.ClientUser.IsAuthenticated);
                            Assert.IsTrue(ctx.ClientUser.AuthenticationType == "NTLM"
                                          || ctx.ClientUser.AuthenticationType == "Negotiate"
                                          || ctx.ClientUser.AuthenticationType == "Kerberos");
                            Assert.AreEqual(currentUser.Name, ctx.ClientUser.Name);
                            Assert.AreEqual(true, ctx.IsClientLocal);
                            Assert.AreEqual(true, ctx.IsAuthenticated);
                            Assert.AreEqual(false, ctx.IsImpersonating);
                            using (ctx.Impersonate())
                                Assert.AreEqual(true, ctx.IsImpersonating);
                            break;
                        }
                    case "ncacn_ip_tcp":
                        {
                            Assert.AreEqual(16, ctx.ClientAddress.Length);
                            Assert.AreEqual(true, ctx.ClientUser.IsAuthenticated);
                            Assert.IsTrue(ctx.ClientUser.AuthenticationType == "NTLM"
                                          || ctx.ClientUser.AuthenticationType == "Negotiate"
                                          || ctx.ClientUser.AuthenticationType == "Kerberos");
                            Assert.AreEqual(currentUser.Name, ctx.ClientUser.Name);
                            Assert.AreEqual(true, ctx.IsAuthenticated);
                            Assert.AreEqual(false, ctx.IsImpersonating);
                            using (ctx.Impersonate())
                                Assert.AreEqual(true, ctx.IsImpersonating);
                            break;
                        }
                    case "ncacn_np":
                        {
                            Assert.AreEqual(new byte[0], ctx.ClientAddress);
                            Assert.AreEqual(true, ctx.ClientUser.IsAuthenticated);
                            Assert.IsTrue(ctx.ClientUser.AuthenticationType == "NTLM"
                                          || ctx.ClientUser.AuthenticationType == "Negotiate"
                                          || ctx.ClientUser.AuthenticationType == "Kerberos");
                            Assert.AreEqual(currentUser.Name, ctx.ClientUser.Name);
                            Assert.AreEqual(true, ctx.IsAuthenticated);
                            Assert.AreEqual(false, ctx.IsImpersonating);
                            using (ctx.Impersonate())
                                Assert.AreEqual(true, ctx.IsImpersonating);
                            break;
                        }
                    case "ncacn_np-Anonymous":
                        {
                            Assert.AreEqual(new byte[0], ctx.ClientAddress);
                            Assert.AreEqual(false, ctx.ClientUser.IsAuthenticated);
                            Assert.AreEqual("", ctx.ClientUser.AuthenticationType);
                            Assert.AreEqual("", ctx.ClientUser.Name);
                            Assert.AreEqual(false, ctx.IsAuthenticated);
                            Assert.AreEqual(false, ctx.IsImpersonating);
                            try
                            {
                                // impersonation not allowed when no credentials were provided, however, you can use ctx.ClientUser.Impersonate
                                ctx.Impersonate();
                            }
                            catch (UnauthorizedAccessException)
                            {
                            }
                            Assert.AreEqual(false, ctx.IsImpersonating);
                            break;
                        }
                }
                return RpcVoid.DefaultInstance;
            }
        }

        #endregion
    }
}