using System;
using Sample;
using System.Security.Principal;

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
}
