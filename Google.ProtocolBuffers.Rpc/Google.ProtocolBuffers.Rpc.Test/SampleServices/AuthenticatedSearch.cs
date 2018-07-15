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
using Google.ProtocolBuffers.Rpc.Messages;
using Google.ProtocolBuffers.TestProtos;

namespace Google.ProtocolBuffers.SampleServices
{
    /// <summary>
    ///   A sample implementation of the ISearchService for testing
    /// </summary>
    internal class AuthenticatedSearch : ISearchService
    {
        SearchResponse ISearchService.Search(SearchRequest searchRequest)
        {
            using (RpcCallContext.Current.Impersonate())
            {
                if (searchRequest.CriteriaCount == 0)
                {
                    throw new ArgumentException("No criteria specified.", new InvalidOperationException());
                }
                SearchResponse.Builder resp = SearchResponse.CreateBuilder();
                foreach (string criteria in searchRequest.CriteriaList)
                {
                    resp.AddResults(
                        SearchResponse.Types.ResultItem.CreateBuilder().SetName(criteria).SetUrl("http://whatever.com").
                            Build());
                }
                return resp.Build();
            }
        }

        SearchResponse ISearchService.RefineSearch(RefineSearchRequest refineSearchRequest)
        {
            using (RpcCallContext.Current.Impersonate())
            {
                SearchResponse.Builder resp = refineSearchRequest.PreviousResults.ToBuilder();
                foreach (string criteria in refineSearchRequest.CriteriaList)
                {
                    resp.AddResults(
                        SearchResponse.Types.ResultItem.CreateBuilder().SetName(criteria).SetUrl("http://whatever.com").
                            Build());
                }
                return resp.Build();
            }
        }
    }
}