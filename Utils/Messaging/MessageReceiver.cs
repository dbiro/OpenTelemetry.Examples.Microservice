// <copyright file="MessageReceiver.cs" company="OpenTelemetry Authors">
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Utils.Tracing;

namespace Utils.Messaging
{
    public class MessageReceiver : IDisposable
    {
        private static readonly ActivitySource ActivitySource = NexogenActivitySource.Default;
        private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

        private readonly ILogger<MessageReceiver> logger;
        private readonly IConnection connection;
        private readonly IModel channel;

        public MessageReceiver(ILogger<MessageReceiver> logger)
        {
            this.logger = logger;
            this.connection = RabbitMqHelper.CreateConnection();
            this.channel = RabbitMqHelper.CreateModelAndDeclareTestQueue(this.connection);
        }

        public void Dispose()
        {
            this.channel.Dispose();
            this.connection.Dispose();
        }

        public void StartConsumer()
        {
            RabbitMqHelper.StartConsumer(this.channel, this.ReceiveMessage);
        }

        public void ReceiveMessage(BasicDeliverEventArgs ea)
        {
            // Extract the PropagationContext of the upstream parent from the message headers.
            var parentContext = Propagator.Extract(default, ea.BasicProperties, this.ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;
            
            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{ea.RoutingKey} receive";
            
            using (var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext))
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.Span.ToArray());

                    this.logger.LogInformation($"Messagea received: [{message}]");

                    activity?.SetTag("nexogen.message", message);
                    activity?.SetTag("nexogen.baggage.items.count", Baggage.Current.Count);
                    activity?.SetTag("nexogen.message.timestamp", Baggage.Current.GetBaggage("nexogen.message.timestamp") ?? "anyám");

                    // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                    RabbitMqHelper.AddMessagingTags(activity);

                    using (var workActivity = ActivitySource.StartActivity("Do some work"))
                    {
                        // Simulate some work
                        Thread.Sleep(500);
                        //throw new Exception("baszki");
                        activity.AddEvent(new ActivityEvent("fasza lett!"));
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Message processing failed.");
                    activity.RecordException(ex);
                    activity.SetStatus(Status.Error.WithDescription("Message processing failed."));
                }
            }
        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
        {
            try
            {
                if (props.Headers.TryGetValue(key, out var value))
                {
                    var bytes = value as byte[];
                    return new[] { Encoding.UTF8.GetString(bytes) };
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Failed to extract trace context: {ex}");
            }

            return Enumerable.Empty<string>();
        }
    }
}
