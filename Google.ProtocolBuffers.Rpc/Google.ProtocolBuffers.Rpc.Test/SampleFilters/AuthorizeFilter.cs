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
using System.Security.Principal;
using Google.ProtocolBuffers.Rpc.Messages;
using NUnit.Framework;

namespace Google.ProtocolBuffers.SampleFilters
{
    internal class AuthorizeFilter : IRpcDispatch
    {
        private readonly IRpcDispatch _next;

        public AuthorizeFilter(IRpcDispatch next)
        {
            _next = next;
        }

        public TMessage CallMethod<TMessage, TBuilder>(string method, IMessageLite request,
                                                       IBuilderLite<TMessage, TBuilder> response)
            where TMessage : IMessageLite<TMessage, TBuilder>
            where TBuilder : IBuilderLite<TMessage, TBuilder>
        {
            //obviously you could do anything here... create a logging adapter, forward messages to another server, whatever.
            using (RpcCallContext.Current.Impersonate())
            {
                //do some test to see if the use is allowed...
                WindowsIdentity userId = (WindowsIdentity) RpcCallContext.Current.ClientUser;
                Assert.IsFalse(userId.IsAnonymous);
                Assert.IsFalse(userId.IsGuest);
                //then, call the final target:
                return _next.CallMethod(method, request, response);
            }
        }
    }
}