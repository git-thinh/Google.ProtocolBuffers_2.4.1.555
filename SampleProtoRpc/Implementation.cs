using System;
using Sample;
using System.Security.Principal;
using helloworld;
using books;

namespace SampleProtoRpc
{
    class Implementation : IMyService
    {
        #region IMyService Members

        public MyResponse Send(MyRequest myRequest)
        {
            using (WindowsIdentity user = WindowsIdentity.GetCurrent())
            {
                Console.WriteLine("{0} says: {1}", user.Name, myRequest.Message);
                return MyResponse.DefaultInstance;
            }
        }

        #endregion
    }

    internal class Anonymous_BookService : books.IBookService
    {
        public BookList List(Empty empty)
        {
            Console.WriteLine("-> client send: ");
            return BookList.DefaultInstance;
        }         
    }

    internal class Anonymous_Greeter : IGreeter
    {
        public HelloReply SayHello(HelloRequest helloRequest)
        {
            Console.WriteLine("-> client send: {0}", helloRequest.Name);
            //return HelloReply.DefaultInstance;
            return HelloReply.CreateBuilder().SetMessage("Server: " + helloRequest.Name).Build();
        }
    }

    class Implementation_Greeter : IGreeter
    {
        public HelloReply SayHello(HelloRequest helloRequest)
        {
            using (WindowsIdentity user = WindowsIdentity.GetCurrent())
            {
                Console.WriteLine("{0} says: {1}", user.Name, helloRequest.Name);
                return HelloReply.DefaultInstance;
            }
        } 
    }

}
