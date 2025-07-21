/*
 *  Copyright © 2024 Keyfactor
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 */

using Keyfactor.Extensions.Orchestrator.HPiLO.Models;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Jobs
{
    // Everything common to all HPiLO jobs, this class implements the IOrchestratorJobExtension interface.
    public abstract class OrchestratorJob<T> : IOrchestratorJobExtension
    {
        // The IPAMSecretResolver is an interface that allows the orchestrator to retrieve secrets from secure external sources.  Refer to the documentation on how to configure a PAM provider.
        protected internal IPAMSecretResolver PamSecretResolver { get; set; }

        protected internal ILogger logger { get; set; }

        // Shared Job Configuration
        protected internal HPiLOJobConfig JobConfig { get; set; }

        protected internal ServiceProvider APIService { get; set; }
        // This is the base class that the specific job classes will inherit.
        // Put actions common to all implemented jobs here.

        // ExtensionName is required by IManagementJobExtension but value not used.  Will be removed in a later update to the interface.
        public string ExtensionName => "HPiLO";

        // Shared Client Initialization Method
        protected void InitializeHttpClient(HPiLOJobConfig jobConfig)
        {
            // 1) pull out the ignore‐validation flag so we can close over it
            bool ignoreValidation =
                Convert.ToBoolean(jobConfig.CertificateStoreDetails.Properties.IgnoreValidation);

            // 2) setup our DI container
            ServiceCollection services = new();

            // 2a) make *your* job’s ILogger available for injection
            services.AddSingleton(logger);

            // 2b) (optional) register generic logging if any other component
            //     needs an ILogger<T>
            services.AddLogging();

            // 2c) register your options
            services.AddOptions();
            services.Configure<ClientOptions>(opts =>
            {
                opts.BaseUrl = jobConfig.CertificateStoreDetails.StorePath.TrimEnd('/');
                opts.Username = jobConfig.ServerUsername;
                opts.Password = jobConfig.ServerPassword;
                opts.HttpsCertWaitSeconds = int.TryParse(
                    jobConfig.CertificateStoreDetails.Properties.HTTPSCertWaitTime,
                    out int sec)
                    ? sec
                    : 0;
                opts.InventoryAll = Convert.ToBoolean(
                    jobConfig.CertificateStoreDetails.Properties.InventoryAll);
            });

            // 2d) register the *concrete* HPiLOClient as a typed HTTP client,
            //     wiring up its HttpClient + your ILogger instance
            services.AddHttpClient<HPiLOClient>((sp, http) =>
                {
                    ClientOptions opts = sp.GetRequiredService<IOptions<ClientOptions>>().Value;
                    http.BaseAddress = new Uri(opts.BaseUrl);
                    http.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                    string basic = Convert.ToBase64String(
                        Encoding.ASCII.GetBytes($"{opts.Username}:{opts.Password}"));
                    http.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", basic);
                })
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    HttpClientHandler handler = new();
                    if (ignoreValidation)
                    {
                        handler.ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }

                    return handler;
                });

            // 2e) if elsewhere you ask for IHPiLOClient, map it back to the concrete
            services.AddTransient<IHPiLOClient>(sp => sp.GetRequiredService<HPiLOClient>());

            // 3) build the provider
            APIService = services.BuildServiceProvider();
        }
    }
}