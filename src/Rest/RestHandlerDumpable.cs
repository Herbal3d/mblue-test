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

using Microsoft.Extensions.Options;

using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.Rest {

    /// <summary>
    /// REST handler that serves displayable data from an IDumpable source.
    /// The prefix must be set before use so invocation is to create with
    /// the factory and then set the prefix and the IDumpable source.
    /// </summary>
    public class RestHandlerDumpable : RestHandler {

        private readonly MBLogger<RestHandlerDumpable> m_log;
        private readonly IOptions<RestManagerConfig> m_restConfig;

        public IDumpable? DumpableSource { get; set; } = null;

        public RestHandlerDumpable(MBLogger<RestHandlerDumpable> pLogger,
                                IOptions<RestManagerConfig> pRestConfig,
                                RestManager pRestManager,
                                string pPrefix,
                                IDumpable? pDumpableSource
                                ) : base(pRestManager, pPrefix) {
            m_log = pLogger;
            m_restConfig = pRestConfig;
            DumpableSource = pDumpableSource;
        }

        public override async Task ProcessGetRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            string absURL = pRequest.Url?.AbsolutePath ?? "";
            string afterString = absURL.Substring(Prefix.Length);

            // remove any query string
            int qPos = afterString.IndexOf("?");
            if (qPos >= 0) {
                afterString = afterString.Substring(0, qPos);
            }

            try {
                if (DumpableSource != null) {
                    JsonNode? displayMap = DumpableSource.GetDump();
                    if (displayMap != null) {
                        m_RestManager.DoSimpleResponse(pResponse, "application/json", () => {
                            return Utilities.StringToBytes(displayMap.ToJsonString());
                        });
                    } else {
                        m_log.Log(MBLogLevel.DRESTDETAIL, "No displayable data from source");
                        m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NoContent, null);
                    }
                } else {
                    m_log.Log(MBLogLevel.DRESTDETAIL, "No displayable source set");
                    m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.NoContent, null);
                }
            } catch (Exception e) {
                m_log.Log(MBLogLevel.Error, "Exception {0} getting displayable data", e.Message);
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, null);
            }
        }

        public override void Dispose() {
            base.Dispose();
            // m_RestManager.UnregisterListener(this);
        }
    }
}

