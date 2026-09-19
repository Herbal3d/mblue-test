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

using Microsoft.Extensions.Options;

using org.herbal3d.mblue.comm;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.Rest;

using LMVSD = LibreMetaverse.StructuredData;

namespace org.herbal3d.mblue.Session {

    public class RestHandlerExit : RestHandler {

        private readonly MBLogger<RestHandlerExit> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;
        private readonly CancellationTokenSource m_cancelToken;

        /// <summary>
        /// </summary>

        public RestHandlerExit(MBLogger<RestHandlerExit> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager,
                                ICommProvider pCommProvider,
                                CancellationTokenSource pCancelToken
                                ) : base(pRestManager,
                                    Utilities.JoinFilePieces(pRestManager.APIBase, "Session/exit")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commProvider = pCommProvider;
            m_cancelToken = pCancelToken;
        }

        public override async Task ProcessPostRequest(HttpListenerContext pContext,
                                            HttpListenerRequest pRequest,
                                            HttpListenerResponse pResponse,
                                            CancellationToken pCancelToken) {

            m_log.Log(MBLogLevel.DRESTDETAIL, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

            try {
                // try a logout
                m_commProvider.StartLogout();
                // Send a simple response back to the client before exiting.
                m_RestManager.DoSimpleResponse(pResponse, null, null);
                // Also force the main loop to exit, which will cause the app to close.
                m_cancelToken.Cancel();
            } catch (Exception e) {
                m_log.Log(MBLogLevel.DRESTDETAIL, "Exit exception: " + e.ToString());
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError,
                                    () => Encoding.UTF8.GetBytes(e.Message));
            }
        }
    }
}

