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

using Microsoft.Extensions.Options;

using org.herbal3d.mblue.comm;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.Rest;

namespace org.herbal3d.mblue.Session {

    public class RestHandlerLogout : RestHandler {

        private readonly MBLogger<RestHandlerLogout> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;
        private readonly ICommProvider m_commProvider;

        /// <summary>
        /// </summary>

        public RestHandlerLogout(MBLogger<RestHandlerLogout> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager,
                                ICommProvider pCommProvider
                                ) : base(pRestManager,
                                    Utilities.JoinFilePieces(pRestManager.APIBase, "session/logout")) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            m_commProvider = pCommProvider;
        }

        public override async Task ProcessPostRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            if (pRequest?.HttpMethod.ToUpper().Equals("POST") ?? false) {
                m_log.Log(MBLogLevel.DRESTDETAIL, "POST: " + (pRequest?.Url?.ToString() ?? "UNKNOWN"));

                try {
                    _ = m_commProvider.StartLogout(pCancelToken);
                } catch (Exception e) {
                    m_log.Log(MBLogLevel.DRESTDETAIL, "Logout exception: " + e.ToString());
                }
            }
        }
    }
}
