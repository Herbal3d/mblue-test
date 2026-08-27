// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Net;
using System.Text.Json.Nodes;

using org.herbal3d.mblue.comm;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.ecm;

using Microsoft.Extensions.Options;

namespace org.herbal3d.mblue.Rest {

    public class RestHandlerStats : RestHandler {

        private readonly MBLogger<RestHandlerStats> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;
        private readonly ECMFactory m_ECMFactory;

        /// <summary>
        /// </summary>

        public RestHandlerStats(MBLogger<RestHandlerStats> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager,
                                ECMFactory pECMFactory,
                                ICommProvider pCommProvider
                                ) : base(pRestManager,
                                Utilities.JoinFilePieces(pRestManager.APIBase, "/stats")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commConfig = pCommConfig;
            m_ECMFactory = pECMFactory;
            m_commProvider = pCommProvider;
        }

        public override async Task ProcessGetRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            JsonObject responseMap = new JsonObject {
                ["status"] = "success",
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["commprovider"] = m_commProvider.GetType().Name,
                ["isconnected"] = m_commProvider.IsConnected,
                ["isloggedin"] = m_commProvider.IsLoggedIn
            };

            responseMap["components"] = m_ECMFactory.GetDump() ?? new JsonObject();

            /* Sample code on how configuration parameters can be added to the response.
            // Add in the comm config parameters
            JsonObject commConfig = new JsonObject();
            foreach (var param in m_commConfig.Value.GetType().GetProperties()) {
                var val = param.GetValue(m_commConfig.Value);
                if (val != null) {
                    commConfig[param.Name.ToLower()] = val.ToString() ?? "";
                }
            }
            responseMap["commconfig"] = commConfig;
            */

            // Send the response
            m_RestManager.DoSimpleResponse(pResponse, "application/json", () => {
                return Utilities.StringToBytes(responseMap.ToString());
            });

            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(MBLogLevel.DRESTDETAIL, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NotImplemented, null);

            }
            ;
        }
    }
}

