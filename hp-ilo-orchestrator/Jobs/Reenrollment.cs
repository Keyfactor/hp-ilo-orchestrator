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

#nullable enable

using Keyfactor.Extensions.Orchestrator.HPiLO.Models;
using Keyfactor.Logging;
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Jobs
{
    // The Reenrollment class implements IAgentJobExtension and it's purpose is to:
    //  1) Create a CSR for re-enrollment, based on the provided parameters
    //  2) Submit the CSR to KF Command to enroll the certificate and retrieve the certificate back
    //  4) Replace the existing certificatDeploy the newly re-enrolled certificate to a certificate store


    public class Reenrollment : OrchestratorJob<Reenrollment>, IReenrollmentJobExtension
    {
        //Job Entry Point
        public JobResult ProcessJob(ReenrollmentJobConfiguration config, SubmitReenrollmentCSR submitReenrollment)
        {
            //METHOD ARGUMENTS...
            //config - contains context information passed from KF Command to this job run:
            //
            // config.Server.Username, config.Server.Password - credentials for orchestrated server - use to authenticate to certificate store server.
            //
            // config.ServerUsername, config.ServerPassword - credentials for orchestrated server - use to authenticate to certificate store server.
            // config.CertificateStoreDetails.ClientMachine - server name or IP address of orchestrated server
            // config.CertificateStoreDetails.StorePath - location path of certificate store on orchestrated server
            // config.CertificateStoreDetails.StorePassword - if the certificate store has a password, it would be passed here
            // config.CertificateStoreDetails.StoreProperties - JSON string containing custom store properties for this specific store type
            //
            // config.CertificateJobProperties = Dictionary of custom parameters to use in building CSR and placing enrolled certiciate in a the proper certificate store


            ReenrollmentInitialize(config);

            logger.LogDebug("Begin Reenrollment...");

            // the default response
            JobResult result = new()
            {
                Result = OrchestratorJobStatusJobResult.Failure,
                JobHistoryId = JobConfig.JobHistoryId ?? 0,
                FailureMessage = "Reenrollment failed."
            };
            string CSRstring = "";

            IHPiLOClient APIclient = APIService.GetRequiredService<IHPiLOClient>();
            // Need to check if CN includes serial number.
            JobConfig.JobProperties.TryGetValue("subjectText", out object? subjectTextObj);
            iLOCertType CertType = APIclient.CheckType(JobConfig.ReenrollmentAlias);
            // step 1 - generate CSR via HP iLO
            try
            {
                string address = "";
                // Reenrolling HTTPS Cert:
                if (CertType == iLOCertType.HTTPSCert)
                {
                    logger.LogDebug("Reenrolling HTTPS Cert");
                    address = APIEndpoints.GenerateCSRHTTPSCert;
                }
                // Reenrolling iLOLDev Cert
                else if (CertType == iLOCertType.iLOLDevID)
                {
                    logger.LogDebug("Reenrolling iLOLDev Cert");
                    // if this cert exists it needs to be removed first. 
                    APIclient.DeleteCertificate(CertType);
                    // generating the actual csr
                    address = APIEndpoints.GenerateCSRiLOLDevID;
                }
                else
                {
                    throw new Exception("Reenrollment for this certificate is not supported by HP iLO.");
                }

                CSRstring = APIclient.GenerateCSR(address, subjectTextObj?.ToString() ?? string.Empty, CertType,
                    Convert.ToBoolean(JobConfig.JobProperties["IncludeIP"] ?? false));
                CSRstring = Regex.Replace(CSRstring, @"[\p{C}\p{Cf}]+", string.Empty);
            }
            catch (Exception ex)
            {
                result.FailureMessage = $"Error generating CSR: {ex.Message}";
                throw;
            }

            //step 2) submit the CSR to Keyfactor Command
            X509Certificate2? returnCert = null;
            try
            {
                returnCert = submitReenrollment.Invoke(CSRstring);
                if (returnCert == null)
                {
                    throw new InvalidOperationException(
                        "The returned certificate is null. Please check your configuration.");
                }
            }
            catch (Exception ex)
            {
                result.FailureMessage = $"Error submitting the CSR to Command: {ex.Message}";
                throw;
            }

            // step 3) write the returned certificate to the local store
            try
            {
                // Check for private key
                if (returnCert.HasPrivateKey)
                {
                    logger.LogDebug("Importing certificate with private key");
                    APIclient.AddCertificate(
                        ExportCertificateWithDetails(returnCert), CertType);
                }
                else
                {
                    logger.LogDebug("Importing certificate without private key");
                    APIclient.AddCertificate(
                        ToPem(returnCert.RawData), CertType);
                }

                result.Result = OrchestratorJobStatusJobResult.Success;
                result.FailureMessage =
                    $"Successfully performed reenrollment on certificate with subject `{returnCert.SubjectName.Name}`";
            }
            catch (Exception ex)
            {
                result.FailureMessage = $"Error adding the returned certificate to the local store: {ex.Message}";
                throw;
            }

            return result;
        }


        /// <summary>
        /// Simply provides a PEM formatted cert without a private key or extra text
        /// </summary>
        /// <param name="certBytes"></param>
        /// <returns></returns>
        public static string ToPem(byte[] certBytes)
        {
            return "-----BEGIN CERTIFICATE-----\n" +
                   Convert.ToBase64String(certBytes, Base64FormattingOptions.InsertLineBreaks) +
                   "\n-----END CERTIFICATE-----";
        }
        /// <summary>
        /// Preps an x509Certificate2 for export with private key to HPiLO
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string ExportCertificateWithDetails(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            StringBuilder builder = new();

            // Get a detailed text dump of the certificate.
            // This is similar to the output of "openssl x509 -text -noout".
            builder.AppendLine("Certificate:");
            builder.AppendLine(certificate.ToString(true));

            // Export certificate in DER format and convert to PEM.
            byte[] certBytes = certificate.Export(X509ContentType.Cert);
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(certBytes, Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            // If the certificate has an associated RSA private key, export it.
            using (RSA? rsa = certificate.GetRSAPrivateKey())
            {
                if (rsa != null)
                {
                    byte[] privateKeyBytes = rsa.ExportRSAPrivateKey();
                    builder.AppendLine("-----BEGIN PRIVATE KEY-----");
                    builder.AppendLine(
                        Convert.ToBase64String(privateKeyBytes, Base64FormattingOptions.InsertLineBreaks));
                    builder.AppendLine("-----END PRIVATE KEY-----");
                }
            }

            return builder.ToString();
        }


        private void ReenrollmentInitialize(ReenrollmentJobConfiguration rnrConfig)
        {
            logger = LogHandler.GetClassLogger(GetType());

            logger.MethodEntry();
            logger.LogTrace("values received from command: ");
            logger.LogTrace($"{JsonConvert.SerializeObject(rnrConfig)}\n\"----------------------\\n");
            try
            {
                JobConfig = new HPiLOJobConfig();
                JobConfig.Capability = rnrConfig.Capability;
                JobConfig.JobHistoryId = rnrConfig.JobHistoryId;
                JobConfig.JobCancelled = rnrConfig.JobCancelled;
                JobConfig.RequestStatus = rnrConfig.RequestStatus;
                JobConfig.UseSSL = rnrConfig.UseSSL;
                JobConfig.JobProperties = rnrConfig.JobProperties;
                JobConfig.ServerError = rnrConfig.ServerError;
                JobConfig.CertificateStoreDetails = new StoreDetails(rnrConfig.CertificateStoreDetails);
                JobConfig.ReenrollmentAlias = rnrConfig.Alias;
                // resolve secrets using the PAM settings configured on the orchestrator (if any)
                // if PAM is not configured, the resolved values will be the ones passed by the orchestrator, rather than looked up via PAM provider extension.
                if (rnrConfig.ServerUsername != null)
                {
                    JobConfig.ServerUsername = PamResolver.ResolvePAMField(PamSecretResolver, logger, "Server UserName",
                        rnrConfig.ServerUsername);
                }

                if (rnrConfig.ServerPassword != null)
                {
                    JobConfig.ServerPassword = PamResolver.ResolvePAMField(PamSecretResolver, logger, "Server Password",
                        rnrConfig.ServerPassword);
                }
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