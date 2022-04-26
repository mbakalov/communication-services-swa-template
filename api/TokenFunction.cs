using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using System.Linq;
using Azure.Communication.Identity;
using System.Net;

namespace API.Function
{
    public static class TokenFunction
    {
        /// <summary>
        /// /api/token function that creates a new Azure Communication Services
        /// <a href="https://docs.microsoft.com/azure/communication-services/concepts/identity-model#identity">user</a> and
        /// <a href="https://docs.microsoft.com/azure/communication-services/concepts/identity-model#access-tokens">access token</a>.
        /// </summary>
        /// <returns>
        /// An instance of CommunicationUserIdentifierAndToken, containing both Communication Services user id
        /// and their token.
        /// See <a href="https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Communication.Identity/1.0.1/api/Azure.Communication.Identity/Azure.Communication.Identity.CommunicationUserIdentifierAndToken.html">
        /// SDK docs</a> for details.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Note that this function will create a new ephemeral Azure Communication Services user every time it is called.
        /// This is not recommended.
        /// </para>
        /// <para>
        /// Instead, you should maintain a mapping between user ids in your system and Communication Services users.
        /// </para>
        /// </remarks>
        [FunctionName("token")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var principal = GetPrincipal(req);

            // If our app user is authenticated, use Azure Communication Services Identity SDK
            // to create a Communication Services user and access token for them.
            // See: https://docs.microsoft.com/azure/static-web-apps/authentication-authorization
            if (principal.IsInRole("authenticated"))
            {
                var client = CreateIdentityClient();

                var userAndTokenResponse = await client.CreateUserAndTokenAsync(
                    scopes: new [] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP });
                
                return new OkObjectResult(userAndTokenResponse.Value);
            }

            // If not authenticated - return 401
            return new StatusCodeResult((int)HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Creates an instance of Azure Communication Services IdentityClient.
        /// </summary>
        /// <see cref="https://docs.microsoft.com/azure/communication-services/quickstarts/access-tokens?pivots=programming-language-csharp"/>
        private static CommunicationIdentityClient CreateIdentityClient()
        {
            string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ApplicationException("The environment variable 'COMMUNICATION_SERVICES_CONNECTION_STRING' is not set.");
            }
            
            return new CommunicationIdentityClient(connectionString);
        }

        /// <summary>
        /// Extracts user information from the 'x-ms-client-principal' header that is provided by the 
        /// Static Web Apps hosting platform.
        /// </summary>
        /// <see cref="https://docs.microsoft.com/azure/static-web-apps/user-information?tabs=csharp#api-functions"/>
        /// <param name="req">Current HttpRequest instance.</param>
        /// <returns>ClaimsPrincipal object, populated with user details and roles for non-anonymous users.</returns>
        private static ClaimsPrincipal GetPrincipal(HttpRequest req)
        {
            var principal = new ClientPrincipal();

            if (req.Headers.TryGetValue("x-ms-client-principal", out var header))
            {
                var data = header[0];
                var decoded = Convert.FromBase64String(data);
                var json = Encoding.UTF8.GetString(decoded);
                principal = JsonSerializer.Deserialize<ClientPrincipal>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            principal.UserRoles = principal.UserRoles?.Except(new string[] { "anonymous" }, StringComparer.CurrentCultureIgnoreCase);

            if (!principal.UserRoles?.Any() ?? true)
            {
                return new ClaimsPrincipal();
            }

            var identity = new ClaimsIdentity(principal.IdentityProvider);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
            identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
            identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));

            return new ClaimsPrincipal(identity);
        }

        private class ClientPrincipal
        {
            public string IdentityProvider { get; set; }
            public string UserId { get; set; }
            public string UserDetails { get; set; }
            public IEnumerable<string> UserRoles { get; set; }
        }
    }

    
}
