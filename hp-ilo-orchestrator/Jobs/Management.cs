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
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Jobs
{
    public class Management : OrchestratorJob<Management>, IManagementJobExtension
    {
        // The Management job class handles the following operations:
        // Add (certificate)
        // Remove (certificate)
        // Create (certificate store)

        //Job Entry Point
        public JobResult ProcessJob(ManagementJobConfiguration config)
        {
            #region sample config json

            //          Sample Management > Create job configuration:
            //
            //          {
            //              "LastInventory": [],
            //              "CertificateStoreDetails": {
            //                  "ClientMachine": "localmachine",
            //                  "StorePath": "c:\\tempSOS\\mystore.json",
            //                  "StorePassword": null,
            //                  "Properties": "{\"StoreNameString\":\"my sample store\",\"ForTestingOnlyBool\":\"true\",\"CollectionNameMultipleChoice\":\"internal\",\"PrivateDetailsSecret\":\"my secret\",\"ServerUsername\":\"joe\",\"ServerPassword\":\"v\",\"ServerUseSsl\":\"true\"}",
            //                  "Type": 105
            //              },
            //              "OperationType": 4,
            //              "Overwrite": false,
            //              "JobCertificate": {
            //                  "Thumbprint": null,
            //                  "Contents": null,
            //                  "Alias": null,
            //                  "PrivateKeyPassword": null
            //              },
            //              "JobCancelled": false,
            //              "ServerError": null,
            //              "JobHistoryId": 15,
            //              "RequestStatus": 1,
            //              "ServerUsername": "joe",
            //              "ServerPassword": "v",
            //              "UseSSL": true,
            //              "JobProperties": { },
            //              "JobTypeId": "00000000-0000-0000-0000-000000000000",
            //              "JobId": "ac24fcd0-af79-49f7-bad8-8b2acc2ddbb6",
            //              "Capability": "CertStores.SOS.Management"
            //          }

            //  Sample Management > Add job configuration
            //
            //            {
            //                "LastInventory": [],
            //                "CertificateStoreDetails": {
            //                    "ClientMachine": "localmachine",
            //                    "StorePath": "c:\\tempSOS\\mystore.json",
            //                    "StorePassword": null,
            //                    "Properties": {
            //                        "StoreNameString": "my sample store",
            //                        "ForTestingOnlyBool": "true",
            //                        "CollectionNameMultipleChoice": "internal",
            //                        "PrivateDetailsSecret": "my secret",
            //                        "ServerUsername": "joe",
            //                        "ServerPassword": "v",
            //                        "ServerUseSsl": "true"
            //                    },
            //                "Type": 105
            //                },
            //                "OperationType": 2,
            //                "Overwrite": false,
            //                "JobCertificate": {
            //                    "Thumbprint": null,
            //                    "Contents": "",
            //                    "Alias": "testcert",
            //                    "PrivateKeyPassword": "..."
            //                },
            //                "JobCancelled": false,
            //                "ServerError": null,
            //                "JobHistoryId": 28,
            //                "RequestStatus": 1,
            //                "ServerUsername": "joe",
            //                "ServerPassword": "v",
            //                "UseSSL": true,
            //                "JobProperties": {
            //                    "CommaSeparatedSansString": "testwritecert.keyfactor.lab,testcert.keyfactor.lab",
            //                    "CertColorMultipleChoice": "red",
            //                    "ForTestingOnlyBool": true,
            //                    "PrivateCertDetailsSecret": "secretcert"
            //                },
            //                "JobTypeId": "00000000-0000-0000-0000-000000000000",
            //                "JobId": "4234344b-a254-45b5-b233-d70aedd187ea",
            //                "Capability": "CertStores.SOS.Management"
            //          }

            #endregion

            Initialize(config);

            logger.LogDebug("Begin Management job...");

            // default response
            JobResult result = new()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                JobHistoryId = config.JobHistoryId,
                FailureMessage =
                    "Custom message you want to show to show up as the error message in Job History in KF Command"
            };
            HPiLOClient APIclient = APIService.GetRequiredService<HPiLOClient>();
            try
            {
                //Management jobs, unlike Discovery, Inventory, and Reenrollment jobs can have 3 different purposes:
                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        //Add a certificate to the certificate store passed in the config object
                        break;

                    case CertStoreOperationType.Remove:
                        //Delete a certificate from the certificate store passed in the config object
                        logger.LogTrace("Beginning management > remove operation.");

                        if (APIclient.DeleteCertificate(JobConfig.JobCertificate.Alias))
                        {
                            result.Result = OrchestratorJobStatusJobResult.Success;
                            result.FailureMessage = string.Empty;
                        }

                        logger.LogTrace("Completed management > remove operation.");
                        break;

                    case CertStoreOperationType.Create:
                        //Create an empty certificate store in the provided location
                        logger.LogTrace("Beginning management > create (certificate store) operation.");
                        break;

                    default:
                        //Invalid OperationType.  Return error.  Should never happen
                        logger.LogError("Invalid management operation.");
                        result.FailureMessage =
                            $"Site {config.CertificateStoreDetails.StorePath} on server {config.CertificateStoreDetails.ClientMachine}: Unsupported operation: {config.OperationType}";
                        return result;
                }
            }
            catch (Exception ex)
            {
                //Status: 2=Success, 3=Warning, 4=Error
                string errormessage = $"Error executing job: {ex.Message}";
                logger.LogError(ex, errormessage);
                result.FailureMessage = errormessage;
                return result;
            }

            //Status: 2=Success, 3=Warning, 4=Error
            return result;
        }

        public void Initialize(ManagementJobConfiguration mgmtConfig)
        {
            logger = LogHandler.GetClassLogger(GetType());

            logger.MethodEntry();
            logger.LogTrace("values received from command: ");
            logger.LogTrace($"{JsonConvert.SerializeObject(mgmtConfig)}\n\"----------------------\\n");

            JobConfig = new HPiLOJobConfig();
            JobConfig.Capability = mgmtConfig.Capability;
            JobConfig.JobHistoryId = mgmtConfig.JobHistoryId;
            JobConfig.JobCancelled = mgmtConfig.JobCancelled;
            JobConfig.OperationType = mgmtConfig.OperationType;
            JobConfig.RequestStatus = mgmtConfig.RequestStatus;
            JobConfig.UseSSL = mgmtConfig.UseSSL;
            JobConfig.JobProperties = mgmtConfig.JobProperties;
            JobConfig.ServerError = mgmtConfig.ServerError;
            JobConfig.Overwrite = mgmtConfig.Overwrite;

            JobConfig.JobCertificate = new HPiLOJobCertificate(mgmtConfig.JobCertificate, mgmtConfig.JobProperties);
            JobConfig.CertificateStoreDetails = new StoreDetails(mgmtConfig.CertificateStoreDetails);

            // resolve secrets using the PAM settings configured on the orchestrator (if any)
            // if PAM is not configured, the resolved values will be the ones passed by the orchestrator, rather than looked up via PAM provider extension.
            JobConfig.ServerUsername = PamResolver.ResolvePAMField(PamSecretResolver, logger, "Server Username",
                mgmtConfig.ServerUsername);
            JobConfig.ServerPassword = PamResolver.ResolvePAMField(PamSecretResolver, logger, "Server Password",
                mgmtConfig.ServerPassword);

            // Generate the ServiceProvider for the HPiLOClient
            ServiceCollection serviceCollection = new();
            serviceCollection.AddHttpClient();
            serviceCollection.AddSingleton(sp =>
                new HPiLOClient(
                    sp.GetRequiredService<IHttpClientFactory>(),
                    JobConfig.CertificateStoreDetails.StorePath,
                    JobConfig.ServerUsername,
                    JobConfig.ServerPassword,
                    inventoryall: false,
                    ignorevalidation: Convert.ToBoolean(JobConfig.CertificateStoreDetails.Properties.IgnoreValidation),
                    waittime:JobConfig.CertificateStoreDetails.Properties.HTTPSCertWaitTime,
                    inputlogger: logger
                ));
            APIService = serviceCollection.BuildServiceProvider();
            ;
        }

        // Extracts Manager ID from certificate alias
        public static int ExtractManager(string input)
        {
            // Regex pattern: colon, then capture one or more digits, then a slash.
            Regex regex = new(@":(\d+)/");
            Match match = regex.Match(input);
            if (match.Success)
            {
                string numberStr = match.Groups[1].Value;
                if (int.TryParse(numberStr, out int result))
                {
                    return result;
                }
            }

            throw new ArgumentException("The input string does not match the expected format.");
        }
    }
}