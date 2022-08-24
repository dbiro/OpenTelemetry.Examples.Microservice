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
using System.Diagnostics.Tracing;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;

namespace WebApi
{
    public sealed class EventSourceCreatedListener : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            Console.WriteLine($"New event source: {eventSource.Name}");
        }
    }

    sealed class EventSourceListener : EventListener
    {
        private readonly string _eventSourceName;
        private readonly StringBuilder _messageBuilder = new StringBuilder();

        public EventSourceListener(string name)
        {
            _eventSourceName = name;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);

            if (eventSource.Name == _eventSourceName)
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            base.OnEventWritten(eventData);

            string message;
            lock (_messageBuilder)
            {
                _messageBuilder.Append("<- Event ");
                _messageBuilder.Append(eventData.EventSource.Name);
                _messageBuilder.Append(" - ");
                _messageBuilder.Append(eventData.EventName);
                _messageBuilder.Append(" : ");
                _messageBuilder.AppendJoin(',', eventData.Payload);
                _messageBuilder.AppendLine(" ->");
                message = _messageBuilder.ToString();
                _messageBuilder.Clear();
            }
            Console.WriteLine(message);
        }
    }


    public class Program
    {
        static Program()
        {
            var _ = Sdk.SuppressInstrumentation;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine(Activity.DefaultIdFormat);

            //Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            //Activity.ForceDefaultIdFormat = true;

            using (new EventSourceCreatedListener())
            using (new EventSourceListener("OpenTelemetry-Api"))
            using (new EventSourceListener("OpenTelemetry-SDK"))
            {
                var builder = CreateHostBuilder(args);

                Console.WriteLine(Activity.DefaultIdFormat);

                var host = builder.Build();

                Console.WriteLine(Activity.DefaultIdFormat);

                var hostTask = host.RunAsync();

                Console.WriteLine(Activity.DefaultIdFormat);

                hostTask.Wait();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    //logging.AddOpenTelemetry(options =>
                    //{
                    //    options.IncludeFormattedMessage = true;
                    //    options.IncludeScopes = true;
                    //    options.ParseStateValues = true;
                    //});
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseUrls("http://*:5000")
                        .UseStartup<Startup>();
                        //.ConfigureLogging(logging =>
                        //{
                        //    logging.AddOpenTelemetry(options =>
                        //    {
                        //        options.IncludeFormattedMessage = true;
                        //        options.IncludeScopes = true;
                        //        options.ParseStateValues = true;
                        //    });
                        //});
                });
    }
}
