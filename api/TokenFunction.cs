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
        [FunctionName("token")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var principal = GetPrincipal(req);

            if (principal.IsInRole("authenticated"))
            {
                var client = CreateIdentityClient();

                var userAndTokenResponse = await client.CreateUserAndTokenAsync(
                    scopes: new [] { CommunicationTokenScope.Chat, CommunicationTokenScope.VoIP });
                
                return new OkObjectResult(userAndTokenResponse.Value);
            }

            return new StatusCodeResult((int)HttpStatusCode.Unauthorized);
        }

        private static CommunicationIdentityClient CreateIdentityClient()
        {
            string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ApplicationException("The environment variable 'COMMUNICATION_SERVICES_CONNECTION_STRING' is not set.");
            }
            
            return new CommunicationIdentityClient(connectionString);
        }

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
