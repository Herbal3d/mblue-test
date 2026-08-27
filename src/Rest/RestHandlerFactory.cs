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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.Logging;

namespace org.herbal3d.mblue.Rest {

    public class RestHandlerFactory {
        private readonly IServiceProvider m_serviceProvider;

        public RestHandlerFactory(IServiceProvider pServiceProvider) {
            m_serviceProvider = pServiceProvider;
        }

        public RestHandler CreateHandler<T>(params object[] parameters) where T : RestHandler {
            return ActivatorUtilities.CreateInstance<T>(m_serviceProvider, parameters);
        }

        // Convenience method for creating a RestHandlerDumpable with the required parameters.
        // Not sure this is needed as CreateInstance should match the parameters correctly.
        public RestHandlerDumpable CreateHandlerDumpable(string pPrefix, IDumpable pDumpableSource) {
            return (RestHandlerDumpable)CreateHandler<RestHandlerDumpable>(
                m_serviceProvider.GetRequiredService<MBLogger<RestHandlerDumpable>>(),
                m_serviceProvider.GetRequiredService<IOptions<RestManagerConfig>>(),
                m_serviceProvider.GetRequiredService<RestManager>(),
                pPrefix, pDumpableSource);
        }
    }
}
