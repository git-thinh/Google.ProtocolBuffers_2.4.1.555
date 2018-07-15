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
using System.Runtime.InteropServices;
using Google.ProtocolBuffers.Rpc;
using Google.ProtocolBuffers.Rpc.Win32Rpc;
using Google.ProtocolBuffers.SampleFilters;
using Google.ProtocolBuffers.SampleServices;
using Google.ProtocolBuffers.TestProtos;
using NUnit.Framework;

namespace Google.ProtocolBuffers
{
    /// <summary>
    ///   This test fixture demonstrates the usage of the RpcClient and RpcServer classes
    /// </summary>
    [TestFixture]
    public class DemoRpcLibrary
    {
        [TestFixtureSetUp]
        public void EnableVerboseLogging()
        {
            Win32RpcServer.VerboseLogging = true;
        }

        [Test]
        public void DemoRpcOverLrpc()
        {
            //obtain the interface id for rpc registration
            Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
            //Create the server with a stub pointing to our implementation
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                //allow GSS_NEGOTIATE
                .AddAuthNegotiate()
                //LRPC named 'lrpctest'
                .AddProtocol("ncalrpc", "lrpctest")
                //Begin responding
                .StartListening())
            {
                //Create the rpc client connection and give it to the new SearchService
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncalrpc", null, "lrpctest").Authenticate(
                                RpcAuthenticationType.Self)))
                {
                    //party on!
                    SearchResponse results =
                        client.Search(SearchRequest.CreateBuilder().AddCriteria("Test Criteria").Build());
                    Assert.AreEqual(1, results.ResultsCount);
                    Assert.AreEqual("Test Criteria", results.ResultsList[0].Name);
                    Assert.AreEqual("http://whatever.com", results.ResultsList[0].Url);
                }
            }
        }

        [Test]
        public void DemoRpcOverTcpIp()
        {
            //obtain the interface id for rpc registration
            Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
            //Create the server with a stub pointing to our implementation
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                //allow GSS_NEGOTIATE
                .AddAuthNegotiate()
                //tcp/ip port 12345
                .AddProtocol("ncacn_ip_tcp", "12345")
                //Begin responding
                .StartListening())
            {
                //Create the rpc client connection and give it to the new SearchService
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncacn_ip_tcp", "127.0.0.1", "12345").Authenticate(
                                RpcAuthenticationType.Self)))
                {
                    //party on!
                    SearchResponse results =
                        client.Search(SearchRequest.CreateBuilder().AddCriteria("Test Criteria").Build());
                    Assert.AreEqual(1, results.ResultsCount);
                    Assert.AreEqual("Test Criteria", results.ResultsList[0].Name);
                    Assert.AreEqual("http://whatever.com", results.ResultsList[0].Url);
                }
            }
        }

        [Test]
        public void DemoRpcOverNamedPipe()
        {
            //obtain the interface id for rpc registration
            Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
            //Create the server with a stub pointing to our implementation
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AuthenticatedSearch()))
                //allow GSS_NEGOTIATE
                .AddAuthNegotiate()
                //pipes start with '\pipe\'
                .AddProtocol("ncacn_np", @"\pipe\p1")
                //Begin responding
                .StartListening())
            {
                //Create the rpc client connection and give it to the new SearchService
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncacn_np", @"\\localhost", @"\pipe\p1").Authenticate(
                                RpcAuthenticationType.Self)))
                {
                    //party on!
                    SearchResponse results =
                        client.Search(SearchRequest.CreateBuilder().AddCriteria("Test Criteria").Build());
                    Assert.AreEqual(1, results.ResultsCount);
                    Assert.AreEqual("Test Criteria", results.ResultsList[0].Name);
                    Assert.AreEqual("http://whatever.com", results.ResultsList[0].Url);
                }
            }
        }

        [Test]
        public void DemoRpcOverAnonymousNamedPipe()
        {
            //obtain the interface id for rpc registration
            Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
            //Create the server with a stub pointing to our implementation
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(new AnonymousSearch()))
                //pipes start with '\pipe\'
                .AddProtocol("ncacn_np", @"\pipe\p1")
                //Begin responding
                .StartListening())
            {
                //using the anonId interface and AnonymousSearch implementation we can call without authentication
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncacn_np", @"\\localhost", @"\pipe\p1").Authenticate(
                                RpcAuthenticationType.Anonymous)))
                    client.Search(SearchRequest.CreateBuilder().Build());
            }
        }

        [Test]
        public void DemoCustomAuthorization()
        {
            //obtain the interface id for rpc registration
            Guid iid = Marshal.GenerateGuidForType(typeof (ISearchService));
            //create the implementation and wrap our autthorization around it
            IRpcDispatch impl = new AuthorizeFilter(new SearchService.Dispatch(new AuthenticatedSearch()));
            //Create the server with a stub pointing to our implementation
            using (RpcServer.CreateRpc(iid, new SearchService.ServerStub(impl))
                .AddProtocol("ncalrpc", @"lrpctest")
                .StartListening())
            {
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid, "ncalrpc", null, @"lrpctest").Authenticate(
                                RpcAuthenticationType.Self)))
                    client.Search(SearchRequest.CreateBuilder().AddCriteria(String.Empty).Build());
            }
        }

        [Test]
        public void DemoClientProxyChain()
        {
            Guid iid1 = Guid.NewGuid();
            Guid iid2 = Guid.NewGuid();
            //forward reuests from iid1 to service iid2:
            using (
                RpcServer.CreateRpc(iid1,
                                    new SearchService.ServerStub(
                                        RpcClient.ConnectRpc(iid2, "ncalrpc", null, @"lrpctest").Authenticate(
                                            RpcAuthenticationType.Self)))
                    .AddProtocol("ncalrpc", @"lrpctest")
                    .AddAuthNegotiate()
                    .StartListening())
                //iid calls the implementation
            using (RpcServer.CreateRpc(iid2, new SearchService.ServerStub(new AuthenticatedSearch()))
                .AddProtocol("ncalrpc", @"lrpctest")
                .AddAuthNegotiate()
                .StartListening())
            {
                using (
                    SearchService client =
                        new SearchService(
                            RpcClient.ConnectRpc(iid1, "ncalrpc", null, @"lrpctest").Authenticate(
                                RpcAuthenticationType.Self)))
                    Assert.AreEqual(1,
                                    client.Search(SearchRequest.CreateBuilder().AddCriteria(String.Empty).Build()).
                                        ResultsCount);
            }
        }
    }
}