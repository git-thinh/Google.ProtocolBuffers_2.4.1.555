using Google.ProtocolBuffers;
using Google.ProtocolBuffers.Rpc.Messages;

namespace SampleProtoRpc
{
    class Impersonation : IRpcServerStub
    {
        private readonly IRpcServerStub _stub;

        public Impersonation(IRpcServerStub stub)
        {
            _stub = stub;
        }

        public IMessageLite CallMethod(string methodName, ICodedInputStream input, ExtensionRegistry registry)
        {
            using(RpcCallContext.Current.Impersonate())
            {
                return _stub.CallMethod(methodName, input, registry);
            }
        }

        public void Dispose()
        {
            _stub.Dispose();
        }
    }
}
