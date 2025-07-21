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
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using System;
using System.IO;
using System.Linq;
using System.Text;

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

            ManagementInitialize(config);

            logger.LogDebug("Begin Management job...");

            // default response
            JobResult result = new()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                JobHistoryId = config.JobHistoryId,
                FailureMessage =
                    "Custom message you want to show to show up as the error message in Job History in KF Command"
            };
            try
            {
                HPiLOClient APIclient = APIService.GetRequiredService<HPiLOClient>();
                //Management jobs, unlike Discovery, Inventory, and Reenrollment jobs can have 3 different purposes:
                switch (config.OperationType)
                {
                    case CertStoreOperationType.Add:
                        //Add a certificate to the certificate store passed in the config object - only HTTPSCert addition is supported.
                        iLOCertType certTypeAdd =
                            APIclient.CheckType(JobConfig.JobProperties["CertificateType"].ToString());
                        if (JobConfig.JobCertificate.ContentsFormat != "PFX")
                        {
                            throw new Exception(
                                "Only enrollment of PFX certificates with included password is supported.");
                        }

                        if (certTypeAdd == iLOCertType.HTTPSCert)
                        {
                            if (APIclient.AddCertificate(
                                    ConvertBase64Pkcs12ToPem_BC(JobConfig.JobCertificate?.Contents,
                                        JobConfig.JobCertificate.PrivateKeyPassword),
                                    certTypeAdd))
                            {
                                result.Result = OrchestratorJobStatusJobResult.Success;
                                result.FailureMessage = $"Successfully installed {certTypeAdd}";
                            }
                        }
                        else
                        {
                            //Unsupported addition.
                            logger.LogError(
                                $"Unsupported certificate type for certification addition:{certTypeAdd}. Only HTTPSCert addition is supported.");
                        }

                        break;

                    case CertStoreOperationType.Remove:
                        //Delete a certificate from the certificate store passed in the config object
                        logger.LogTrace("Beginning management > remove operation.");
                        iLOCertType certTypeDelete =
                            APIclient.CheckType(JobConfig.JobProperties["CertificateType"].ToString());
                        if (APIclient.DeleteCertificate(certTypeDelete))
                        {
                            result.Result = OrchestratorJobStatusJobResult.Success;
                            result.FailureMessage = $"Successfully deleted {certTypeDelete}";
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

        /// <summary>
        ///     This converts the passed down PKCS12 (PFX) certificate in Base64 format to PEM format using BouncyCastle.
        ///     BouncyCastle is used here due to the MS Library refusing to export the key in PKCS#1 format, which is required by
        ///     iLO.
        /// </summary>
        /// <param name="base64Pfx"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ConvertBase64Pkcs12ToPem_BC(string base64Pfx, string password)
        {
            if (string.IsNullOrWhiteSpace(base64Pfx))
            {
                throw new ArgumentNullException(nameof(base64Pfx));
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            // 1) Decode and load the PFX
            byte[] pfxData = Convert.FromBase64String(base64Pfx);
            using MemoryStream stream = new(pfxData, false);
            Pkcs12Store store = new Pkcs12StoreBuilder().Build();
            store.Load(stream, password.ToCharArray());

            // 2) Extract the leaf cert + key
            string alias = store.Aliases.First(a => store.IsKeyEntry(a));
            AsymmetricKeyEntry keyEntry = store.GetKey(alias);
            X509CertificateEntry certEntry = store.GetCertificate(alias);

            // 3) Helper to emit PEM with '\n' only
            static string ToPem(string label, byte[] der)
            {
                StringBuilder sb = new();
                sb.Append($"-----BEGIN {label}-----\n");
                string b64 = Convert.ToBase64String(der);
                const int lineLen = 64;
                for (int i = 0; i < b64.Length; i += lineLen)
                {
                    sb.Append(b64, i, Math.Min(lineLen, b64.Length - i))
                        .Append('\n');
                }

                sb.Append($"-----END {label}-----\n");
                return sb.ToString();
            }

            // 4) Build the PEM blocks
            string certPem = ToPem("CERTIFICATE", certEntry.Certificate.GetEncoded());

            string keyPem;
            if (keyEntry.Key is RsaPrivateCrtKeyParameters rsaParams)
            {
                // RSA PRIVATE KEY (PKCS#1)
                RsaPrivateKeyStructure rsaStruct = new(
                    rsaParams.Modulus,
                    rsaParams.PublicExponent,
                    rsaParams.Exponent,
                    rsaParams.P,
                    rsaParams.Q,
                    rsaParams.DP,
                    rsaParams.DQ,
                    rsaParams.QInv);
                keyPem = ToPem("RSA PRIVATE KEY", rsaStruct.ToAsn1Object().GetEncoded());
            }
            else
            {
                // PRIVATE KEY (PKCS#8)
                PrivateKeyInfo pkInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyEntry.Key);
                keyPem = ToPem("PRIVATE KEY", pkInfo.ToAsn1Object().GetEncoded());
            }

            // 5) Combine and strip any '\r', ensuring only '\n'
            return (certPem + keyPem).Replace("\r", "");
        }

        public void ManagementInitialize(ManagementJobConfiguration mgmtConfig)
        {
            logger = LogHandler.GetClassLogger(GetType());

            logger.MethodEntry();
            try
            {
                if (!mgmtConfig.JobProperties.ContainsKey("CertificateType"))
                {
                    throw new Exception(
                        "CertificateType entry parameter has not been detected. Have you recently updated from version 1 of this extension? Please review the documentation and the changelog to learn more about the new entry parameters.");
                }

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
            }
            catch (Exception ex)
            {
                logger.LogError($"Error evaluating the job parameters: {ex.Message}");
                throw;
            }

            InitializeHttpClient(JobConfig);
        }
    }
}