using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace AmpLeadGen2HubSpot
{
    public static class AmpLeadGen2HubSpot
    {
        [FunctionName("AmpLeadGen2HubSpot")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"1. New Lead generated from Azure Marketplace!. Request Raw body {requestBody}");

            var ampLeadData = JsonConvert.DeserializeObject<AmpLeadData>(requestBody);
            log.LogInformation($"2. Deserialized request payload to C# Lead object.");

            var hubSpotContactStringObject = GetHubSpotRequestBody(ampLeadData);
            log.LogInformation($"3. Converted C# Lead object to Hubspot contact object json string. Hubspot Json string {hubSpotContactStringObject}");

            try
            {
                var hApiKey = Environment.GetEnvironmentVariable("hapikey");                
                var Content = new StringContent(hubSpotContactStringObject, Encoding.UTF8, "application/json");
                
                log.LogInformation("4. Sending Hubspot contact object json string to Hub Spot contact api");
                HttpClient newClient = new HttpClient();
                HttpResponseMessage response = await newClient.PostAsync($"https://api.hubapi.com/contacts/v1/contact?hapikey={hApiKey}", Content);

                if(response.IsSuccessStatusCode)
                    return new OkObjectResult($"Request processed successfully! Hubspot Call Result: {response.StatusCode} : Hubspot body: {hubSpotContactStringObject}");
                else
                    return new BadRequestObjectResult($"Error reponse from HUBSpot API Result: {response.StatusCode} : Reason: {response.ReasonPhrase}");

            }
            catch (Exception ex)
            {

                return new BadRequestObjectResult($"Exception calling HUBSpot API: Exception: {ex.Message} Hubspot body: {hubSpotContactStringObject}");
            }
        }

        public class UserDetails
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string country { get; set; }
            public string company { get; set; }
            public string title { get; set; }
        }

        public class AmpLeadData
        {
            public UserDetails userDetails { get; set; }
            public string leadSource { get; set; }
            public string actionCode { get; set; }
            public string offerTitle { get; set; }
            public string description { get; set; }
        }      


        static string GetHubSpotRequestBody(AmpLeadData ampLeadData)
        {
            StringBuilder hubSpotContactStringObject = new StringBuilder("{\"properties\": [");
            hubSpotContactStringObject.Append($"{{\"property\": \"firstname\", \"value\": \"{ampLeadData.userDetails.firstName}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"lastname\", \"value\": \"{ampLeadData.userDetails.lastName}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"email\", \"value\": \"{ampLeadData.userDetails.email}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"phone\", \"value\": \"{ampLeadData.userDetails.phone}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"company\", \"value\": \"{ampLeadData.userDetails.company}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"country\", \"value\": \"{ampLeadData.userDetails.country}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"jobtitle\", \"value\": \"{ampLeadData.userDetails.title}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"leadSource\", \"value\": \"{ampLeadData.leadSource}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"actionCode\", \"value\": \"{ampLeadData.actionCode}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"offerTitle\", \"value\": \"{ampLeadData.offerTitle}\"}},");
            hubSpotContactStringObject.Append($"{{\"property\": \"description\", \"value\": \"{ampLeadData.description}\"}}");
            hubSpotContactStringObject.Append($"]}}");

            return hubSpotContactStringObject.ToString();
        }
    }
}
