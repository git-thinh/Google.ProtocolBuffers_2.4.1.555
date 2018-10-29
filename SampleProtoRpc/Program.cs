using System;
using Sample;
using System.Runtime.InteropServices;
using Google.ProtocolBuffers.Rpc;
using helloworld;
using books;

namespace SampleProtoRpc
{
    class Program
    {
        static readonly Guid IID = Marshal.GenerateGuidForType(typeof(IMyService));

        static void Main(string[] args)
        {
            args = new[] { "" };
            args[0] = "listen";

            switch (args[0].ToLower())
            {
                case "listen":
                    {
                        ////using (RpcServer.CreateRpc(IID, new Impersonation(new MyService.ServerStub(new Implementation())))
                        //using (RpcServer.CreateRpc(IID, new Impersonation(new Greeter.ServerStub(new Implementation_Greeter())))
                        //    //.AddAuthentication(CSharpTest.Net.RpcLibrary.RpcAuthentication.RPC_C_AUTHN_NONE)
                        //    //.AddAuthNegotiate()
                        //    .AddProtocol("ncacn_ip_tcp", "50051")
                        //    //.AddProtocol("ncacn_np", @"\pipe\Greeter")
                        //    ////.AddProtocol("ncalrpc", "MyService")
                        //    //.AddProtocol("ncalrpc", "Greeter")
                        //    .StartListening())
                        //{
                        //    Console.WriteLine("Waiting for connections...");
                        //    Console.ReadLine();
                        //}


                        Guid iid = Marshal.GenerateGuidForType(typeof(IGreeter));
                        using (RpcServer.CreateRpc(iid, new Greeter.ServerStub(new Anonymous_Greeter()))
                            //.AddAuthNegotiate()
                            .AddAuthentication(CSharpTest.Net.RpcLibrary.RpcAuthentication.RPC_C_AUTHN_NONE)
                            .AddAuthNegotiate()
                            .AddProtocol("ncacn_ip_tcp", "50051")
                            //.AddProtocol("ncalrpc", "Greeter")
                            .StartListening())
                        {

                            Console.WriteLine("Waiting for connections...");
                            string name = "123"; // Console.ReadLine();


                            using (Greeter client = new Greeter(RpcClient
                                .ConnectRpc(iid, "ncacn_ip_tcp", @"localhost", "50051")
                                .Authenticate(RpcAuthenticationType.Self)
                                //.Authenticate(RpcAuthenticationType.None)
                                ))
                            {
                                HelloReply response = client.SayHello(HelloRequest.CreateBuilder().SetName(name).Build());
                                Console.WriteLine("OK: " + response.Message);
                            }
                            Console.ReadLine();
                        }

                        //Guid iid = Marshal.GenerateGuidForType(typeof(IBookService));
                        //using (RpcServer.CreateRpc(iid, new BookService.ServerStub(new Anonymous_BookService()))
                        //    .AddProtocol("ncacn_ip_tcp", "50051")
                        //    .AddProtocol("ncalrpc", "BookService")
                        //    .StartListening())
                        //{
                        //    Console.WriteLine("Waiting for connections...");
                        //    Console.ReadLine();
                        //}


                        break;
                    }
                case "send-lrpc":
                    {
                        using (MyService client = new MyService(
                            RpcClient.ConnectRpc(IID, "ncalrpc", null, "MyService")
                            .Authenticate(RpcAuthenticationType.Self)))
                        {
                            MyResponse response = client.Send(
                                MyRequest.CreateBuilder().SetMessage("Hello via LRPC!").Build());
                        }
                        break;
                    }
                case "send-tcp":
                    {
                        using (MyService client = new MyService(
                            RpcClient.ConnectRpc(IID, "ncacn_ip_tcp", @"localhost", "8080")
                            .Authenticate(RpcAuthenticationType.Self)))
                        {
                            MyResponse response = client.Send(
                                MyRequest.CreateBuilder().SetMessage("Hello via Tcp/Ip!").Build());
                        }
                        break;
                    }
                case "send-np":
                    {
                        using (MyService client = new MyService(
                            RpcClient.ConnectRpc(IID, "ncacn_np", @"\\localhost", @"\pipe\MyService")
                            .Authenticate(RpcAuthenticationType.Self)))
                        {
                            MyResponse response = client.Send(
                                MyRequest.CreateBuilder().SetMessage("Hello via Named Pipe!").Build());
                        }
                        break;
                    }
                case "send-anon":
                    {
                        using (MyService client = new MyService(
                            RpcClient.ConnectRpc(IID, "ncacn_np", @"\\localhost", @"\pipe\MyService")
                            .Authenticate(RpcAuthenticationType.Anonymous)))
                        {
                            try
                            {
                                MyResponse response = client.Send(
                                    MyRequest.CreateBuilder().SetMessage("Hello from Anonymous!").Build());
                            }
                            catch (Exception e)
                            {
                                Console.Error.WriteLine(e);
                            }
                        }
                        break;
                    }
            }
        }
    }
}
