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
using org.herbal3d.mblue.Session;

namespace org.herbal3d.mblue {

    public partial class MBlueTestMain {

        public static IHost? MBlueHost { get; private set; } = default!;

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

                     // Read the grids configuration from Grids.json for OpenSim grids.
                     // This should really be in the mblue-comm-os module but not sure how to add.
                     config.AddJsonFile("Grids.json", optional: false, reloadOnChange: true);

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
                     services.Configure<MBlueConfig>(context.Configuration.GetSection(MBlueConfig.subSectionName))

                        // The global cancellation token source that can be used to signal shutdown across the app.
                        .AddSingleton<GlobalControl>()

                        // Version information for the application and MBlue.Common assembly.
                        .AddSingleton<MBVersions>()

                        // Logger and MBLogger wrapper for base logger
                        .Configure<MBLoggerConfig>(context.Configuration.GetSection(MBLoggerConfig.subSectionName))
                        .AddTransient(typeof(MBLogger<>))

                        // The test routine has a REST interface for interaction
                        .AddTransient<RestHandlerDumpable>()
                        .AddTransient<RestHandlerStatic>()
                        .AddTransient<RestHandlerStats>()
                        .AddTransient<RestHandlerUI>()
                        .AddSingleton<RestHandlerFactory>()
                        .AddSingleton<RestManager>()
                        .AddHostedService(sp => sp.GetRequiredService<RestManager>())
                        .AddSingleton<SessionManager>()
                        .AddHostedService(sp => sp.GetRequiredService<SessionManager>());


                     // Add MBlue ECM services
                     MBlueECMServiceSetup.AddServices(services, context.Configuration);

                     // Add MBlue Communication services.
                     // This also adds the actual underlying communication services.
                     MBlueCommServiceSetup.AddServices(services, context.Configuration);

                     // For the moment, just stuff the comm-os services into the DI container.
                     // This should eventually be moved into the mblue-comm service setup.
                     MBlueCommOSServiceSetup.AddServices(services, context.Configuration);

                     // TODO: add more

                 })
                 .Build();

            m_log = MBlueTestMain.GetService<MBLogger<MBlueTestMain>>();

            CollectAndDisplayVersions(m_log);

            LogConfigurationComplete(m_log);

            await MBlueHost.RunAsync(GetService<GlobalControl>().GlobalCTS.Token);

            LogShutdown(m_log);
        }

        // The following are examples of source-generated logging methods.
        [LoggerMessage(0, LogLevel.Information, "MBlue application starting up.")]
        static partial void LogStartup(ILogger logger);
        [LoggerMessage(0, LogLevel.Information, "MBlue application configuration complete.")]
        static partial void LogConfigurationComplete(ILogger logger);
        [LoggerMessage(0, LogLevel.Information, "MBlue application shutting down.")]
        static partial void LogShutdown(ILogger logger);

        // Collects and displays the versions of the application and its modules.
        // Fills the MBVersions object with the application and module versions.
        private static void CollectAndDisplayVersions(MBLogger<MBlueTestMain> m_log) {
            IOptions<MBlueConfig> mblueConfig = GetMBlueConfig;
            MBVersions mbVersion = MBlueTestMain.GetService<MBVersions>();

            mbVersion.AppName = mblueConfig.Value.AppName;
            mbVersion.AppVersion = ThisAssembly.AssemblyInformationalVersion;
            mbVersion.AddOtherVersion("MBlue.Test", ThisAssembly.AssemblyInformationalVersion);

            // Get the version of MBlue.Common from its assembly attribute
            string mblue_common_version = typeof(MBException).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            mbVersion.AddOtherVersion("MBlue.Common", mblue_common_version);

            // Get the version of MBlue.ECM from its assembly attribute
            string mblue_ecm_version = typeof(MBlueECMServiceSetup).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            mbVersion.AddOtherVersion("MBlue.ECM", mblue_ecm_version);

            // Get the version of MBlue.comm from its assembly attribute
            string mblue_comm_version = typeof(MBlueCommServiceSetup).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            mbVersion.AddOtherVersion("MBlue.comm", mblue_comm_version);

            // Get the version of MBlue.comm-os from its assembly attribute
            string mblue_comm_os_version = typeof(MBlueCommOSServiceSetup).Assembly
                .GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
            mbVersion.AddOtherVersion("MBlue.comm-os", mblue_comm_os_version);

            m_log.LogInformation($"MBlue application {mbVersion.AppName} version {mbVersion.AppVersion}");
            mbVersion.OtherVersions.ToList().ForEach(v => m_log.LogInformation($"Module {v.Key} version {v.Value}"));
        }

    }
}
