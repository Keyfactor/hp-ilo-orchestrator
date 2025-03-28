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

using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Models
{
    //public class JobConfig
    //{
    //    public string ClientMachine { get; set; } // only defined for Discovery Jobs
    //    public string StorePath { get; set; } // only null for Discovery Jobs
    //    public string ServerUsername { get; set; }
    //    public string ServerPassword { get; set; }
    //    public string JobHistoryId { get; set; }
    //    public DiscoveryJobProperties DiscoveryJobProperties { get; set; } // only populated for Discovery Jobs
    //    public SampleStoreCustomProperties StoreProperties { get; set; } // collection of custom store properties defined on the store type in Command
    //    public CertificateJobProperties CertificateJobProperties { get; set; }
    //}

    public class HPiLOJobConfig
    {
        public StoreDetails CertificateStoreDetails { get; set; }
        public bool IgnoreValidation { get; set; }
        public CertStoreOperationType? OperationType { get; set; }
        public bool? Overwrite { get; set; }
        public HPiLOJobCertificate JobCertificate { get; set; }
        public bool? JobCancelled { get; set; }
        public ServerFault? ServerError { get; set; }
        public long? JobHistoryId { get; set; }
        public int? RequestStatus { get; set; }
        public string ServerUsername { get; set; }
        public string ServerPassword { get; set; }
        public bool? UseSSL { get; set; }
        public Dictionary<string, object> JobProperties { get; set; }
        public string JobTypeId { get; set; }
        public string JobId { get; set; }
        public string Capability { get; set; }
    }

    public class StoreDetails
    {
        public StoreDetails(CertificateStore storeDetails)
        {
            ClientMachine = storeDetails.ClientMachine;
            StorePath = storeDetails.StorePath;
            StorePassword = storeDetails.StorePassword;
            Properties = JsonConvert.DeserializeObject<HPiLOCertStoreProperties>(storeDetails.Properties);
            Type = storeDetails.Type;
        }

        public string ClientMachine { get; set; }
        public string StorePath { get; set; }
        public string StorePassword { get; set; }
        public HPiLOCertStoreProperties Properties { get; set; }
        public int Type { get; set; }
    }

    public class HPiLOCertStoreProperties
    {
        public string StoreNameString { get; set; }
        public string ServerUsername { get; set; }
        public string ServerPassword { get; set; }
        public string IgnoreValidation { get; set; }
        public string InventoryAll { get; set; }
        public string HTTPSCertWaitTime { get; set; }
    }

    public class HPiLOJobCertificate
    {
        public HPiLOJobCertificate(ManagementJobCertificate jobCertificate,
            Dictionary<string, object> customEntryParameters)
        {
            Thumbprint = jobCertificate.Thumbprint ?? string.Empty;
            Contents = jobCertificate.Contents ?? string.Empty;
            Alias = jobCertificate.Alias ?? string.Empty;
            PrivateKeyPassword = jobCertificate.PrivateKeyPassword ?? string.Empty;
            customEntryParameters.TryGetValue("CommaSeparatedSansString", out object commaSeparatedSansString);
            CommaSeparatedSansString = commaSeparatedSansString?.ToString() ?? string.Empty;

            customEntryParameters.TryGetValue("CertColorMultipleChoice", out object certColorMultipleChoice);
            CertColorMultipleChoice = certColorMultipleChoice?.ToString() ?? string.Empty;
            bool ForTestingOnlyBool;
            if (customEntryParameters.TryGetValue("ForTestingOnlyBool", out object forTestingOnlyBoolValue) &&
                bool.TryParse(forTestingOnlyBoolValue?.ToString(), out bool parsedBool))
            {
                ForTestingOnlyBool = parsedBool;
            }
            else
            {
                ForTestingOnlyBool = false;
            }

            customEntryParameters.TryGetValue("PrivateCertDetailsSecret", out object privateCertDetailsSecretValue);
            PrivateCertDetailsSecret = privateCertDetailsSecretValue?.ToString() ?? string.Empty;
        }

        public string Thumbprint { get; set; }
        public string Contents { get; set; }
        public string Alias { get; set; }
        public string PrivateKeyPassword { get; set; }
        public bool? Overwrite { get; set; }
        public string CommaSeparatedSansString { get; set; }
        public string CertColorMultipleChoice { get; set; }
        public bool? ForTestingOnlyBool { get; set; }
        public string PrivateCertDetailsSecret { get; set; }
        public string SubjectText { get; set; }
    }

    public class DiscoveryJobProperties
    {
        // This class contains the Job Properties that are passed during a Discovery Job   

        [JsonProperty("dirs")] public string Directories { get; set; }

        [JsonProperty("ignoreddirs")] public string IgnoredDirectories { get; set; }

        [JsonProperty("patterns")] public string RegexPatterns { get; set; }
    }
}