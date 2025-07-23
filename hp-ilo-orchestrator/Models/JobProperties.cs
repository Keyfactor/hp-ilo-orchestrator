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
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Models
{
    public class HPiLOJobConfig
    {
        public StoreDetails CertificateStoreDetails { get; set; } = new();
        public bool IgnoreValidation { get; set; }
        public CertStoreOperationType? OperationType { get; set; }
        public bool? Overwrite { get; set; }
        public HPiLOJobCertificate? JobCertificate { get; set; }
        public bool? JobCancelled { get; set; }
        public ServerFault? ServerError { get; set; }
        public long? JobHistoryId { get; set; }
        public int? RequestStatus { get; set; }
        public string ServerUsername { get; set; } = string.Empty;
        public string ServerPassword { get; set; } = string.Empty;
        public bool? UseSSL { get; set; }
        public Dictionary<string, object> JobProperties { get; set; } = new();
        public string JobTypeId { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string Capability { get; set; } = string.Empty;
        public string ReenrollmentAlias { get; set; } = string.Empty;
    }

    public class StoreDetails
    {
        public StoreDetails() { }

        public StoreDetails(CertificateStore storeDetails)
        {
            ClientMachine = storeDetails.ClientMachine;
            StorePath = storeDetails.StorePath;
            StorePassword = storeDetails.StorePassword;
            Properties = JsonConvert.DeserializeObject<HPiLOCertStoreProperties>(storeDetails.Properties) ??
                         new HPiLOCertStoreProperties();
            Type = storeDetails.Type;
        }

        public string ClientMachine { get; set; } = string.Empty;
        public string StorePath { get; set; } = string.Empty;
        public string StorePassword { get; set; } = string.Empty;
        public HPiLOCertStoreProperties Properties { get; set; } = new();
        public int Type { get; set; }
    }

    // HPiLOCertStoreProperties already has default values for all properties.

    public class HPiLOCertStoreProperties
    {
        public string StoreNameString { get; set; } = string.Empty;
        public string ServerUsername { get; set; } = string.Empty;
        public string ServerPassword { get; set; } = string.Empty;
        public string IgnoreValidation { get; set; } = string.Empty;
        public string InventoryAll { get; set; } = string.Empty;
        public string HTTPSCertWaitTime { get; set; } = string.Empty;
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
            ContentsFormat = jobCertificate.ContentsFormat ?? string.Empty;
        }

        public string Thumbprint { get; set; } = string.Empty;
        public string Contents { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string PrivateKeyPassword { get; set; } = string.Empty;
        public string ContentsFormat { get; set; } = string.Empty;
        public bool? Overwrite { get; set; } = true;
        public string SubjectText { get; set; } = string.Empty;
    }
}