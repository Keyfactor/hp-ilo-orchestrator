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
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Extensions;
using Keyfactor.Orchestrators.Extensions.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Jobs
{
    public abstract class OrchestratorJob<T> : IOrchestratorJobExtension
    {
        // The IPAMSecretResolver is an interface that allows the orchestrator to retrieve secrets from secure external sources.  Refer to the documentation on how to configure a PAM provider.
        protected internal IPAMSecretResolver PamSecretResolver { get; set; }

        protected internal ILogger logger { get; set; }

        protected internal HPiLOJobConfig JobConfig { get; set; }

        // An instance of the FileStoreClient; which is used to encapsulate interactions with the file system.
        protected internal ServiceProvider APIService { get; set; }
        // This is the base class that the specific job classes will inherit.
        // Put actions common to all implemented jobs here.

        // ExtensionName is required by IManagementJobExtension but value not used.  Will be removed in a later update to the interface.
        public string ExtensionName => "HPiLO";

        // This method is called for each job except reenrollment and stores the values passed by the platform for subsequent use
        public void Initialize(dynamic config)
        {
            JobConfig = new HPiLOJobConfig();
            logger = LogHandler.GetClassLogger(GetType());
            logger.MethodEntry(LogLevel.Trace);

            //logger.LogTrace("values received from command: ");
            //logger.LogTrace($"{JsonConvert.SerializeObject(config)}\n\"----------------------\\n");
            string storepath = "";
            try
            {
                string capability = Convert.ToString(config.Capability);
                capability = capability.Split('.').ToList().Last();
                switch (capability)
                {
                    case "Discovery": // Capture the properties specific to discovery jobs
                        storepath = "";
                        break; // no other properties are shared for a discovery job
                    case "Management":
                        goto case "Inventory"; // continue collecting base properties
                    case "Reenrollment":
                        goto case "Inventory"; // continue collecting base properties
                    case "Inventory":
                        if (config.CertificateStoreDetails == null)
                        {
                            throw new MissingFieldException(
                                "No value for CertificateStoreDetails for non-Discovery job.");
                        }

                        JobConfig.CertificateStoreDetails = new StoreDetails(config.CertificateStoreDetails);
                        storepath = JobConfig.CertificateStoreDetails.StorePath;
                        break;
                    default:
                        throw new InvalidOperationException($"{capability} is not a recognized job type.");
                }

                // resolve secrets using the PAM settings configured on the orchestrator (if any)
                // if PAM is not configured, the resolved values will be the ones passed by the orchestrator, rather than looked up via PAM provider extension.
                if (config.ServerUsername != null)
                {
                    JobConfig.ServerUsername = PamResolver.ResolvePAMField(PamSecretResolver, logger, "Server UserName",
                        config.ServerUsername);
                }

                if (config.ServerPassword != null)
                {
                    JobConfig.ServerPassword = PamResolver.ResolvePAMField(PamSecretResolver, logger, "Server Password",
                        config.ServerPassword);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Error evaluating the job parameters: {ex.Message}");
                throw;
            }

            ServiceCollection serviceCollection = new();
            serviceCollection.AddHttpClient();
            serviceCollection.AddSingleton(sp =>
                new HPiLOClient(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    JobConfig.CertificateStoreDetails.StorePath,
                    JobConfig.ServerUsername,
                    JobConfig.ServerPassword,
                    inventoryall: Convert.ToBoolean(JobConfig.CertificateStoreDetails.Properties.InventoryAll),
                    ignorevalidation: Convert.ToBoolean(JobConfig.CertificateStoreDetails.Properties.IgnoreValidation),
                    waittime: JobConfig.CertificateStoreDetails.Properties.HTTPSCertWaitTime,
                    inputlogger: logger
                ));
            APIService = serviceCollection.BuildServiceProvider();
        }
    }
}