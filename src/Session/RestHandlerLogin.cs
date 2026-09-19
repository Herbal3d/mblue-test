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

using org.herbal3d.mblue.comm;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.Rest;

using Microsoft.Extensions.Options;

namespace org.herbal3d.mblue.Session {

    public class RestHandlerLogin : RestHandler {

        private readonly MBLogger<RestHandlerLogin> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;
        private readonly SessionManager m_sessionManager;

        /// <summary>
        /// </summary>
        public RestHandlerLogin(MBLogger<RestHandlerLogin> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager,
                                SessionManager pSessionManager,
                                ICommProvider pCommProvider
                                ) : base(pRestManager,
                                    Utilities.JoinFilePieces(pRestManager.APIBase, "Session/login")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commProvider = pCommProvider;
            m_sessionManager = pSessionManager;
        }

        public override async Task ProcessPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            m_log.Log(MBLogLevel.DRESTDETAIL, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

            string strBody = "";
            using (StreamReader rdr = new StreamReader(pRequest.InputStream)) {
                strBody = rdr.ReadToEnd();
                // m_log.Log(MBLogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
            }
            try {
                JsonNode? body = JsonNode.Parse(strBody);
                LoginParams loginParams = new LoginParams();
                loginParams.FromJson(body);

                LoginResponse? result = await m_commProvider.StartLogin(loginParams);

                JsonObject respMap = new JsonObject();
                if (result is not null && result.Success) {
                    respMap.Add("result", "success");
                    respMap.Add("message", result.Message);
                    respMap.Add("session_id", result.SessionID.ToString());
                    respMap.Add("loginResp", result.ToString());
                } else {
                    respMap.Add("result", "failure");
                    respMap.Add("message", "Login failed: " + (result?.Message ?? "Unknown reason"));
                }
                byte[] respBytes = Encoding.UTF8.GetBytes(respMap.ToString());
                m_RestManager.DoSimpleResponse(pResponse, "application/json", () => respBytes);
            } catch (Exception e) {
                m_log.Log(MBLogLevel.Error, "RestHandlerLogin: Exception {0} trying to do login", e.Message);
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, () => {
                    byte[] respBytes = Encoding.UTF8.GetBytes("Internal Server Error: " + e.Message);
                    return respBytes;
                });
            }
        }
    }
}
