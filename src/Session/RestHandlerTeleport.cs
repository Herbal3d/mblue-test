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
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Options;

using org.herbal3d.mblue.comm;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.Rest;

namespace org.herbal3d.mblue.Session {

    public class RestHandlerTeleport : RestHandler {

        private readonly MBLogger<RestHandlerTeleport> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;

        /// <summary>
        /// </summary>

        public RestHandlerTeleport(MBLogger<RestHandlerTeleport> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager,
                                ICommProvider pCommProvider
                                ) : base(pRestManager,
                                    Utilities.JoinFilePieces(pRestManager.APIBase, "session/teleport")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commProvider = pCommProvider;
        }

        public override async Task ProcessPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            m_log.Log(MBLogLevel.DRESTDETAIL, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

            string strBody = "";
            if (pRequest is not null) {
                using (StreamReader rdr = new StreamReader(pRequest.InputStream)) {
                    strBody = rdr.ReadToEnd();
                    // m_log.Log(MBLogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
                }
            }
            try {
                JsonNode? jsonBody = JsonNode.Parse(strBody);

                if (jsonBody?.AsObject().ContainsKey("DESTINATION") ?? false) {
                    string destination = jsonBody["DESTINATION"]?.ToString() ?? "";
                    m_log.Log(MBLogLevel.DRESTDETAIL, "Teleport request to " + destination);

                    bool result = await m_commProvider.StartTeleport(destination, pCancelToken).ConfigureAwait(false);

                    JsonNode respJson = new JsonObject();
                    if (result) {
                        respJson["result"] = "success";
                        respJson["message"] = "Teleport initiated";
                    } else {
                        respJson["result"] = "failure";
                        respJson["message"] = "Teleport failed";
                    }
                    byte[] respBytes = Encoding.UTF8.GetBytes(respJson.ToString());
                    m_RestManager.DoSimpleResponse(pResponse, "application/json", () => respBytes);
                } else {
                    m_log.Log(MBLogLevel.Error, "RestHandlerTeleport: No DESTINATION in request");
                    m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.BadRequest,
                                    () => Encoding.UTF8.GetBytes("No DESTINATION specified"));
                }
            } catch (Exception e) {
                m_log.Log(MBLogLevel.Error, "RestHandlerTeleport: Exception {0} trying to teleport", e.Message);
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError,
                                    () => Encoding.UTF8.GetBytes("Exception during teleport request"));
            }
        }
    }
}
