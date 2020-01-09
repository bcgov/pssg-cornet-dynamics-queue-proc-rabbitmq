using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QueueProcessingService.Util;
using RabbitMQ.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QueueProcessingService.Service
{
    public class HealthCheckService
    {
        private readonly RequestDelegate _next;
        private readonly string _path;
        private readonly string username = ConfigurationManager.FetchConfig("QUEUE_USERNAME");
        private readonly string password = ConfigurationManager.FetchConfig("QUEUE_PASSWORD");
        private readonly string endpoint = ConfigurationManager.FetchConfig("RABBIT_HEALTH");

        public HealthCheckService(RequestDelegate next, string path)
        {
            _next = next;
            _path = path;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.Value == _path)
            {
                try
                {
                    HttpResponseMessage data = DataClient.GetAsync(endpoint, true, username, password).Result;
                    
                    if (data.IsSuccessStatusCode)
                    {
                        JObject jsonData = JsonConvert.DeserializeObject<JObject>(data.Content.ReadAsStringAsync().Result);
                        if (jsonData["status"].ToString() == "ok") {
                            context.Response.StatusCode = 200;
                            context.Response.ContentLength = 2;
                            await context.Response.WriteAsync("UP");
                        }
                    }
                    await ReturnError(context);
                }
                catch (Exception e)
                {
                    await ReturnError(context);
                }
            }
            else
            {
                await this._next(context);
            }
        }
        private async Task ReturnError(HttpContext context)
        {
            context.Response.StatusCode = 400;
            context.Response.ContentLength = 23;
            await context.Response.WriteAsync("Rabbit Connection Error");
        }
    }

    public static class HealthCheckMiddlewareExtensions
    {
        public static IApplicationBuilder UseHealthCheck(this IApplicationBuilder builder, string path)
        {
            return builder.UseMiddleware<HealthCheckService>(path);
        }
    }
}

