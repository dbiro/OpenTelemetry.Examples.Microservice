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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace WebApi
{
    public class Program
    {
        static Program()
        {
            var _ = Sdk.SuppressInstrumentation;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine(Activity.DefaultIdFormat);


            var builder = CreateHostBuilder(args);

            Console.WriteLine(Activity.DefaultIdFormat);

            var host = builder.Build();

            Console.WriteLine(Activity.DefaultIdFormat);

            var hostTask = host.RunAsync();

            Console.WriteLine(Activity.DefaultIdFormat);

            hostTask.Wait();
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
