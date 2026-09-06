// Copyright 2026 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Extensions.Logging;

using org.herbal3d.mblue.Config;
using org.herbal3d.mblue.comm;
using org.herbal3d.mblue.Common;
using org.herbal3d.mblue.ecm;
using org.herbal3d.mblue.Logging;
using org.herbal3d.mblue.Rest;

namespace org.herbal3d.mblue {

    public partial class MBlueTestMain {

        public static IHost? MBlueHost { get; private set; } = default!;

        public static CancellationTokenSource GlobalCTS { get; } = new CancellationTokenSource();

        // Way to get the logger for those isolated routines that need to log errors
        private static MBLogger<MBlueTestMain>? m_log;
        public static MBLogger<MBlueTestMain> Log {
            get {
                if (m_log is null) {
                    throw new ApplicationException("MBlueMain.Log accessed before initialization.");
                }
                return m_log;
            }
        }

        // Static way to get to options
        public static IOptions<MBlueConfig> GetMBlueConfig {
            get {
                return GetService<IOptions<MBlueConfig>>();
            }
        }

        /// <summary>
        /// Static way to get an instance of <typeparamref name="T"/>
        /// Will throw if its unable to provide a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static T GetService<T>() where T : notnull {
            return MBlueHost!.Services.GetRequiredService<T>()
                ?? throw new ApplicationException($"There requested type {typeof(T).FullName} could not be provided.");
        }

        /// <summary>
        /// Testing setup for loading and testing the libraries that
        /// will be used in the eventual application.
        /// The main functionality here is to create the dependency injection (DI)
        /// services and call the other libraries to add themselves to the
        /// services collection and then test that it all worked.
        /// </summary>
        public static async Task Main(string[] args) {

            MBlueHost = Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration((context, config) => {
                     // CreateDefaultBuilder already adds 'appsettings.json',
                     //     'appsettings.Development.json', and environment variables.

                     // Add the default logging config first so other configs can override it.
                     config.AddJsonFile("MBlue.json", optional: false, reloadOnChange: true);
#if DEBUG
                     config.AddJsonFile("MBlue.Debug.json", optional: true, reloadOnChange: true);
#endif
                     // This reads MBlue.{Environment}.json, which can be used to override settings
                     //       for specific environments (e.g. Development, Staging, Production).
                     config.AddJsonFile($"MBlue.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                     // config.AddJsonFile("Grids.json", optional: false, reloadOnChange: true);
                     config.AddEnvironmentVariables("MBlue_");
                     // re-add command line args so they override other settings
                     config.AddCommandLine(Environment.GetCommandLineArgs());
                 })
                 .ConfigureLogging(logging => {
                     // Remove all the MS stuff and use NLog
                     logging.ClearProviders();
                     logging.AddNLog();
                     // See
                     // https://github.com/NLog/NLog.Extensions.Logging/wiki/NLog-configuration-with-appsettings.json
                     // for more details on NLog configuration using appsettings.json.
                 })
                 .ConfigureServices((context, services) => {
                     services.Configure<MBlueConfig>(context.Configuration.GetSection(MBlueConfig.subSectionName));

                     // The global cancellation token source that can be used to signal shutdown across the app.
                     services.AddSingleton(GlobalCTS);

                     // Logger and MBLogger wrapper for base logger
                     services.Configure<MBLoggerConfig>(context.Configuration.GetSection(MBLoggerConfig.subSectionName));
                     services.AddTransient(typeof(MBLogger<>));

                     // The test routine has a REST interface for interaction
                     services.AddTransient<RestHandlerDumpable>();
                     services.AddTransient<RestHandlerStatic>();
                     services.AddTransient<RestHandlerStats>();
                     services.AddTransient<RestHandlerUI>();
                     services.AddSingleton<RestHandlerFactory>();
                     services.AddSingleton<RestManager>();
                     services.AddHostedService(sp => sp.GetRequiredService<RestManager>());

                     MBlueECMServiceSetup.AddServices(services, context.Configuration);

                     MBlueCommServiceSetup.AddServices(services, context.Configuration);

                     // TODO: add more

                 })
                 .Build();

            m_log = MBlueTestMain.GetService<MBLogger<MBlueTestMain>>();

            IOptions<MBlueConfig> mblueConfig = GetMBlueConfig;

            m_log.LogInformation("MBlue Version: {version}", ThisAssembly.AssemblyInformationalVersion);

            // Get the version of MBlue.Common from its assembly attribute
            string mblue_common_version = typeof(MBException).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            m_log.LogInformation("MBlue.Common Version: {version}", mblue_common_version);

            // Get the version of MBlue.ECM from its assembly attribute
            string mblue_ecm_version = typeof(MBlueECMServiceSetup).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            m_log.LogInformation("MBlue.ECM Version: {version}", mblue_ecm_version);

            // Get the version of MBlue.comm from its assembly attribute
            string mblue_comm_version = typeof(MBlueCommServiceSetup).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            m_log.LogInformation("MBlue.comm Version: {version}", mblue_comm_version);

            LogConfigurationComplete(m_log);

            await MBlueHost.RunAsync(GlobalCTS.Token);

            LogShutdown(m_log);
        }

        // The following are examples of source-generated logging methods.
        [LoggerMessage(0, LogLevel.Information, "MBlue application starting up.")]
        static partial void LogStartup(ILogger logger);
        [LoggerMessage(0, LogLevel.Information, "MBlue application configuration complete.")]
        static partial void LogConfigurationComplete(ILogger logger);
        [LoggerMessage(0, LogLevel.Information, "MBlue application shutting down.")]
        static partial void LogShutdown(ILogger logger);

    }
}
