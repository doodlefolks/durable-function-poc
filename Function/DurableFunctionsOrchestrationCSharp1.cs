using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DurableFunctionPOC.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DurableFunctionPOC.Function
{
    public class DurableFunctionsOrchestrationCSharp1
    {
        private AppConfig _config;
        public DurableFunctionsOrchestrationCSharp1(IOptions<AppConfig> config, HttpClient httpClient)
        {
            _config = config.Value;
        }

        [FunctionName("DurableFunctionsOrchestrationCSharp1")]
        public async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Fan out/fan in
            var tasks = new List<Task<string>>();
            tasks.Add(context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", "Tokyo"));
            tasks.Add(context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", "Seattle"));
            tasks.Add(context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", "London"));

            await Task.WhenAll(tasks);
            outputs = tasks.Select(x => x.Result).ToList();

            // Synchronous
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", "Tokyo"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", "Seattle"));
            // outputs.Add(await context.CallActivityAsync<string>("DurableFunctionsOrchestrationCSharp1_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("DurableFunctionsOrchestrationCSharp1_Hello")]
        public async Task<string> SayHello([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("DurableFunctionsOrchestrationCSharp1_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableClient starter,
            ILogger log)
        {
            string instanceId = await starter.StartNewAsync("DurableFunctionsOrchestrationCSharp1", null);
            // string instanceId = await starter.StartNewAsync(nameof(LongRunningFunction), null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName(nameof(LongRunningFunction))]
        public async Task LongRunningFunction([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log.LogInformation("Starting LongRunningFunction");
            await context.CallHttpAsync(HttpMethod.Get, new System.Uri($"{_config.LongRunningJobUrl}/{context.InstanceId}"));
            await context.WaitForExternalEvent("LongRunningJobDone");
            log.LogInformation("LongRunningFunction is done");

        }

        [FunctionName(nameof(NotifyLongRunningProcessDone))]
        public async Task NotifyLongRunningProcessDone(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "JobDone/{instanceId}")] HttpRequestMessage req,
            [DurableClient] IDurableClient client,
            string instanceId)
        {
            await client.RaiseEventAsync(instanceId, "LongRunningJobDone");
        }
    }
}