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

using org.herbal3d.mblue.comm.os;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.Rest;

using Microsoft.Extensions.Options;

namespace org.herbal3d.mblue.Session {

    public class RestHandlerGrids : RestHandler {

        private readonly MBLogger<RestHandlerGrids> m_log;
        private readonly Grids m_grids;

        /// <summary>
        /// </summary>
        public RestHandlerGrids(MBLogger<RestHandlerGrids> pLogger,
                                RestManager pRestManager,
                                Grids pGrids
                                ) : base(pRestManager,
                                    Utilities.JoinFilePieces(pRestManager.APIBase, "Session/grids")) {
            m_log = pLogger;
            m_grids = pGrids;
        }

        public override async Task ProcessGetRequest(HttpListenerContext pContext,
                                           HttpListenerRequest pRequest,
                                           HttpListenerResponse pResponse,
                                           CancellationToken pCancelToken) {

            try {
                JsonObject respMap = new JsonObject();
                JsonArray gridArray = new JsonArray();
                m_grids.ForEach((gd) => {
                    JsonObject gridMap = new JsonObject {
                        { "GridNick", gd.GridNick },
                        { "GridName", gd.GridName },
                        { "LoginURI", gd.LoginURI }
                    };
                    gridArray.Add(gridMap);
                });
                respMap.Add("grids", gridArray);

                byte[] respBytes = Encoding.UTF8.GetBytes(respMap.ToString());
                m_RestManager.DoSimpleResponse(pResponse, "application/json", () => respBytes);
            } catch (Exception e) {
                m_log.Log(MBLogLevel.Error, "RestHandlerGrids: Exception {0} trying to do GET grids", e.Message);
                m_RestManager.DoErrorResponse(pResponse, HttpStatusCode.InternalServerError, null);
            }
        }
    }
}
