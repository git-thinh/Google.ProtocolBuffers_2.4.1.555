using System;
using Sample;
using System.Runtime.InteropServices;
using Google.ProtocolBuffers.Rpc;

namespace SampleProtoRpc
{
    class Program
    {
        static readonly Guid IID = Marshal.GenerateGuidForType(typeof(IMyService));

        static void Main(string[] args)
        {
            switch (args[0].ToLower())
            {
                case "listen":
                    {
                        using (RpcServer.CreateRpc(IID, new Impersonation(new MyService.ServerStub(new Implementation())))
                            .AddAuthNegotiate()
                            .AddProtocol("ncacn_ip_tcp", "8080")
                            .AddProtocol("ncacn_np", @"\pipe\MyService")
                            .AddProtocol("ncalrpc", "MyService")
                            .StartListening())
                        {
                            Console.WriteLine("Waiting for connections...");
                            Console.ReadLine();
                        }
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
                            catch(Exception e)
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
