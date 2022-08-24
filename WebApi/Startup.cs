// <copyright file="Startup.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Linq;
using System.Net.Http;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RestEase.HttpClientFactory;
using Utils.Messaging;
using Utils.Tracing;
using WebApi.Controllers;
using WebApi.GitHub;

namespace WebApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<MessageSender>();

            services.AddOpenTelemetryTracing((builder) => builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "webapi", serviceNamespace: "nexogen", serviceVersion: "0.1"))
                .AddAspNetCoreInstrumentation(c =>
                {
                    c.RecordException = true;
                    //c.Filter = httpcontext => false;
                })
                .AddHttpClientInstrumentation(o =>
                {
                    //o.Filter = msg => false;
                    o.RecordException = true;
                    //o.Enrich = (activity, evt, obj) =>
                    //{
                    //    switch (evt)
                    //    {
                    //        case "OnStartActivity":
                    //            var msg = obj as HttpRequestMessage;
                    //            if (msg != null)
                    //            {
                    //                activity.DisplayName = $"{activity.DisplayName} {msg.RequestUri.PathAndQuery}";
                    //            }
                    //            break;
                    //        default:
                    //            break;
                    //    }

                    //};
                })
                //.AddSource(nameof(MessageSender))
                .AddSource(nameof(SendMessageController))
                .AddSource(NexogenActivitySource.Default.Name)
                .AddZipkinExporter(b =>
                {
                    var zipkinHostName = Environment.GetEnvironmentVariable("ZIPKIN_HOSTNAME") ?? "localhost";
                    b.Endpoint = new Uri($"http://{zipkinHostName}:9411/api/v2/spans");
                })
                .AddJaegerExporter(b =>
                {
                    b.AgentHost = Environment.GetEnvironmentVariable("JAEGER_AGENT_HOST") ?? "localhost";
                    b.AgentPort = Convert.ToInt32(Environment.GetEnvironmentVariable("JAEGER_AGENT_PORT") ?? "6831");
                }));
                //.AddConsoleExporter()
                //.AddAzureMonitorTraceExporter(ops => ops.ConnectionString = "InstrumentationKey=f31bece5-2772-4e20-b2e0-7185ef0c67b8;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/"));

            services.AddRestEaseClient<IGitHubApi>("https://api.github.com");

            //services.AddHttpClient("github")
            //    .ConfigureHttpClient(x => x.BaseAddress = new Uri("https://api.github.com"))
            //    .UseWithRestEaseClient<IGitHubApi>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
