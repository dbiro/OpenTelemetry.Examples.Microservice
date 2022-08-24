// <copyright file="Program.cs" company="OpenTelemetry Authors">
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
using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Utils.Messaging;
using Utils.Tracing;

namespace WorkerService
{
    public class Program
    {
        private class EnvironmentVariablesCarrier
        {
            public string GetValue(string name)
            {
                return Environment.GetEnvironmentVariable(name);
            }

            public void SetValue(string name, string value)
            {
                Console.WriteLine($"{name}={value}");
            }
        }

        static Program()
        {
            var _ = Sdk.SuppressInstrumentation;
        }

        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            //var propagationContext = Propagators.DefaultTextMapPropagator.Extract(
            //    default,
            //    new EnvironmentVariablesCarrier(),
            //    (env, name) => new string[] { env.GetValue(name) });

            //Propagators.DefaultTextMapPropagator.Inject(
            //    new PropagationContext(propagationContext.ActivityContext, propagationContext.Baggage),
            //    new EnvironmentVariablesCarrier(),
            //    (env, name, value) => env.SetValue(name, value));

            //Console.WriteLine(Environment.GetEnvironmentVariable("traceparent"));

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => logging.AddOpenTelemetry(ops =>
                {
                    ops.IncludeFormattedMessage = true;
                    ops.IncludeScopes = true;
                    ops.ParseStateValues = true;
                }))
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();

                    services.AddSingleton<MessageReceiver>();
                    
                    services.AddOpenTelemetryTracing((builder) =>
                    {
                        builder
                            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName: "worker", serviceNamespace: "nexogen", serviceVersion: "0.1"))
                            .AddSource(NexogenActivitySource.Default.Name)
                            .SetSampler(new ParentBasedSampler(new AlwaysOffSampler()))
                            .AddZipkinExporter(b =>
                            {
                                var zipkinHostName = Environment.GetEnvironmentVariable("ZIPKIN_HOSTNAME") ?? "localhost";
                                b.Endpoint = new Uri($"http://{zipkinHostName}:9411/api/v2/spans");
                            })
                            .AddJaegerExporter(b =>
                            {
                                b.AgentHost = Environment.GetEnvironmentVariable("JAEGER_AGENT_HOST") ?? "localhost";
                                b.AgentPort = Convert.ToInt32(Environment.GetEnvironmentVariable("JAEGER_AGENT_PORT") ?? "6831");
                            });
                            //.AddConsoleExporter()
                            //.AddAzureMonitorTraceExporter(ops => ops.ConnectionString = "InstrumentationKey=f31bece5-2772-4e20-b2e0-7185ef0c67b8;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/");
                });
                });
    }
}
