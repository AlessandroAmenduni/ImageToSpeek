using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Description
{
    public static class Function1
    { 

        [FunctionName("Function1")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
          
            var url = context.GetInput<string>();
            if (string.IsNullOrWhiteSpace(url))
                return null;

          
            var descriptionEN = await context.CallActivityAsync<string>("GetDescription", url);
            var descriptionIT = await context.CallActivityAsync<string>("Translate", descriptionEN);
            var speech = await context.CallActivityAsync<string>("Speech", descriptionIT);
       
            return speech;
        }

        [FunctionName("GetDescription")]
        public static async Task<string> GetDescription([ActivityTrigger] string Url, ILogger log)
        {
            //log.LogInformation("entrato");
            string subscriptionKey = "6aac1fcb8c1040bbac31315711752a16";
            string endpoint = "https://its-amendunia.cognitiveservices.azure.com/";

            ComputerVisionClient client =
              new ComputerVisionClient(new ApiKeyServiceClientCredentials(subscriptionKey))
              { Endpoint = endpoint };

            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
            {
                    VisualFeatureTypes.Categories, VisualFeatureTypes.Description,
                    VisualFeatureTypes.Faces, VisualFeatureTypes.ImageType,
                    VisualFeatureTypes.Tags, VisualFeatureTypes.Adult,
                    VisualFeatureTypes.Color, VisualFeatureTypes.Brands,
                    VisualFeatureTypes.Objects
            };

            ImageAnalysis results = await client.AnalyzeImageAsync(Url, visualFeatures: features);
            string description = results.Description.Captions[0].Text;
            log.LogInformation($"Description: {description}");
            return description;
        }


        [FunctionName("Translate")]
        public static async Task<string> Translate([ActivityTrigger] string message, ILogger log)
        {
            string result;
            //log.LogInformation("entrato");
            string subscriptionKey1 = "858463d44aaf410ba956635cd60f6203";
            string endpoint1 = "https://api.cognitive.microsofttranslator.com/";

            string route = "/translate?api-version=3.0&from=en&to=it";

            object[] body = new object[] { new { Text = message } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint1 + route);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey1);
                request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                result = await response.Content.ReadAsStringAsync();
            }
            return result;
        }


        [FunctionName("Speech")]
        public static async Task Speech([ActivityTrigger] string description, ILogger log)
        {
            var config = SpeechConfig.FromSubscription("4aafd292f34f4ea997600464d954f211", "westeurope"); 
            config.SpeechSynthesisLanguage = "it-IT";
            using var audioConfig = AudioConfig.FromWavFileOutput("D:/ITS KENNEDY/ANNO 2/azure/ITS.AmenduniA.Translate/file.wav");
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);
            await synthesizer.SpeakTextAsync(description);
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var url = "https://moderatorsampleimages.blob.core.windows.net/samples/sample16.png";
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null, url);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}