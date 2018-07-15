#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.ServiceModel;

namespace ProtocolBuffers.Rpc.Benchmarks.TestData
{
    [ServiceContract]
    public interface IWcfSampleService
    {
        [OperationContract]
        WcfSampleResponse Test(WcfSampleRequest searchRequest);
    }

    class WcfSampleServiceAuth : WcfSampleService
    {
        public WcfSampleServiceAuth(int responseSize) : base(responseSize)
        { }

        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public override WcfSampleResponse Test(WcfSampleRequest searchRequest)
        {
            return base.Test(searchRequest);
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    class WcfSampleService : IWcfSampleService
    {
        private readonly int _responseSize;

        public WcfSampleService(int responseSize)
        {
            _responseSize = responseSize;
        }

        public virtual WcfSampleResponse Test(WcfSampleRequest searchRequest)
        {
            return new WcfSampleResponse
                       {
                           Data = SampleData.Generate(
                               _responseSize,
                               d => new SampleDataContract
                                        {
                                            Bytes = (byte[]) d.Bytes.Clone(),
                                            Text = d.Text,
                                            Number = d.Number,
                                            Float = d.Float,
                                            Time = d.Time,
                                        })
                               .ToArray()
                       };
        }
    }
}
