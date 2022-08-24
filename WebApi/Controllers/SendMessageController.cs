// <copyright file="SendMessageController.cs" company="OpenTelemetry Authors">
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using Utils.Messaging;
using WebApi.GitHub;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SendMessageController : ControllerBase
    {
        private readonly ILogger<SendMessageController> logger;
        private readonly MessageSender messageSender;

        private static readonly ActivitySource ActivitySource = new ActivitySource(nameof(SendMessageController));

        private readonly IGitHubApi gitHubApi;

        public SendMessageController(ILogger<SendMessageController> logger, MessageSender messageSender, IGitHubApi gitHubApi)
        {
            this.logger = logger;
            this.messageSender = messageSender;
            this.gitHubApi = gitHubApi ?? throw new System.ArgumentNullException(nameof(gitHubApi));
        }

        [HttpGet]
        public async Task<ActionResult<string>> Get()
        {            
            using var _ = ActivitySource.StartActivity("Sending a message");
            this.logger.LogInformation("Sending a message");

            Baggage.Current.SetBaggage("nexogen.message.timestamp", DateTime.UtcNow.ToLongDateString());
            Baggage.Current.SetBaggage("nexogen.message.dani", "dani");

            var user = await this.gitHubApi.GetUserAsync("dbiro");
            
            return this.messageSender.SendMessage(user.Name);
        }
    }
}
