using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Identity.Client;

namespace APIManagementTemplate
{
    public class AzureResourceCollector : IResourceCollector
    {

        public string DebugOutputFolder = "";
        public string token;
		private readonly IPublicClientApplication _publicClientApp;

		public AzureResourceCollector()  
        {
			_publicClientApp = PublicClientApplicationBuilder.Create(Constants.ClientId)
			.WithRedirectUri(Constants.RedirectUrl)
			.Build();
		}

		// Asynchronous Login method
		public string Login(string tenantName)
		{
			if (string.IsNullOrEmpty(tenantName))
			{
				tenantName = "common";
			}

			// Define the scopes your app needs
			var scopes = new[] { "https://management.azure.com/.default" }; // Example for Microsoft Azure Management API

			// Try to get accounts from cache and use AcquireTokenSilent for a cached token
			var accounts = _publicClientApp.GetAccountsAsync();
			var test = accounts.Result;
			var test2 = test.Count();

			if (accounts.Result.Count() != 0)
			{
				var firstAccount = accounts.Result.FirstOrDefault();

				var result = _publicClientApp.AcquireTokenSilent(scopes, firstAccount)
				.WithTenantId(tenantName) // Use WithTenantId to set the tenant
				.ExecuteAsync();

				token = result.Result.AccessToken;
			}
			else
			{
				var result = _publicClientApp.AcquireTokenInteractive(scopes)
				.WithTenantId(tenantName) // Use WithTenantId to set the tenant
				.WithPrompt(Prompt.SelectAccount)
				.ExecuteAsync();

				token = result.Result.AccessToken;
			}

			return token;
		}

		private static HttpClient client = new HttpClient() { BaseAddress = new Uri("https://management.azure.com") };

        public async Task<JObject> GetResource(string resourceId, string suffix = "",string apiversion = "2019-01-01")
        {
            string url = resourceId + $"{GetSeparatorCharacter(resourceId)}api-version={apiversion}" + (string.IsNullOrEmpty(suffix) ? "" : $"&{suffix}");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            HttpResponseMessage response;
            string responseContent;
            JArray finalJArray =  new JArray();
            JObject finalResponse = new JObject();
            var mergeSettings = new JsonMergeSettings
            {
                MergeArrayHandling = MergeArrayHandling.Union
            };
            JToken nextLinkJToken;

            do
            {
                response = await client.GetAsync(url);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                responseContent = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new UnauthorizedAccessException(responseContent);
                }

                finalResponse = JObject.Parse(responseContent);

                var valueArr = JObject.Parse(responseContent).SelectToken("value");
                if (valueArr != null && valueArr.Type == JTokenType.Array)
                {
                    finalJArray.Merge(JObject.Parse(responseContent).SelectToken("value") as JArray, mergeSettings);
                    finalResponse.SelectToken("value").Replace(finalJArray);
                }

                //Check if nextLink
                nextLinkJToken = JObject.Parse(responseContent).SelectToken("nextLink");
                if (nextLinkJToken != null)
                {
                    //Update URL with nextlink
                    url = nextLinkJToken.ToString();
                }

            } while (nextLinkJToken != null);

            if (!string.IsNullOrEmpty(DebugOutputFolder))
            {
                var path = DebugOutputFolder + "\\" + EscapeString(resourceId.Split('/').SkipWhile( (a) => { return a != "service" && a != "workflows" && a != "sites"; }).Aggregate<string>((b,c) => { return b +"-" +c; })  + ".json");
                System.IO.File.WriteAllText(path, finalResponse.ToString());
            }
            return finalResponse;

        }

        private static string GetSeparatorCharacter(string resourceId)
        {
            return resourceId.Contains("?") ? "&" : "?";
        }

        public async Task<JObject> GetResourceByURL(string url)
        {
            var response = await new HttpClient().GetAsync(url);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            var responseContent = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(DebugOutputFolder))
            {
                var uri = new Uri(url);
                var path = EscapeString(uri.AbsolutePath);
                System.IO.File.WriteAllText($"{DebugOutputFolder}\\{uri.Host}{path}", responseContent);
            }
            return JObject.Parse(responseContent);

        }

        public static string EscapeString(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return value;
            return value.Replace("/", "-").Replace(" ", "-").Replace("=","-").Replace("&", "-").Replace("?", "-");
        }
    }
}
