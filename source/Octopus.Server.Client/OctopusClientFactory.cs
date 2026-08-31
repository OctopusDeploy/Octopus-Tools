using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Octopus.Client.Model;

namespace Octopus.Client
{
    /// <summary>
    ///     Creates instances of the <see cref="IOctopusAsyncClient" />.
    /// </summary>
    public class OctopusClientFactory : IOctopusClientFactory
    {
        /// <summary>
        ///     Creates an instance of the client.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint.</param>
        /// <returns>The <see cref="IOctopusClient" /> instance.</returns>
        /// <param name="options">The configuration options for this client instance.</param>
        public IOctopusClient CreateClient(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options = default)
        {
            options ??= new OctopusClientOptions();
            var requestingTool = DetermineRequestingTool();
            return new OctopusClient(serverEndpoint, requestingTool, options);
        }

        /// <summary>
        ///     Creates an instance of the client.
        /// </summary>
        /// <param name="serverEndpoint">The server endpoint.</param>
        /// <param name="options">The configuration options for this client instance.</param>
        /// <returns>The <see cref="IOctopusAsyncClient" /> instance.</returns>
        public Task<IOctopusAsyncClient> CreateAsyncClient(OctopusServerEndpoint serverEndpoint, OctopusClientOptions options = default)
        {
            options ??= new OctopusClientOptions();
            var requestingTool = DetermineRequestingTool();
            return OctopusAsyncClient.Create(serverEndpoint, options, requestingTool);
        }

        internal static HttpClient BuildHttpClient(HttpMessageHandler handler, OctopusClientOptions clientOptions, string requestingTool, bool disposeHandler = true)
            => BuildHttpClient(handler, clientOptions, new OctopusCustomHeaders(requestingTool), disposeHandler);

        internal static HttpClient BuildHttpClient(HttpMessageHandler handler, OctopusClientOptions clientOptions, OctopusCustomHeaders octopusCustomHeaders, bool disposeHandler = true)
        {
            // Retry HTTP 429s from the server's rate limiter before anything else in the chain sees them.
            var httpClient = new HttpClient(new RateLimitRetryHandler(handler, clientOptions), disposeHandler);
            httpClient.Timeout = clientOptions.Timeout;
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", octopusCustomHeaders.UserAgent);
            return httpClient;
        }

        private string DetermineRequestingTool()
        {
            var launchAssembly = Assembly.GetEntryAssembly();

            if (launchAssembly != null && launchAssembly.GetTypes()
                .Any(x => x.FullName == "Octo.Program" || x.FullName == "Octopus.DotNet.Cli.Program"))
            {
                var octoExtensionVersion = Environment.GetEnvironmentVariable("OCTOEXTENSION");
                if (!string.IsNullOrWhiteSpace(octoExtensionVersion))
                    return $"octo plugin/{octoExtensionVersion}";

                return "octo";
            }

            return null;
        }
    }
}
