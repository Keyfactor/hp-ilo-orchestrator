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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Models
{
    public class HPiLOCertificate
    {
        // Constructor to initialize required properties
        public HPiLOCertificate(
            string odataId,
            string odataType,
            string certificateString,
            CertificateType certificateType,
            string id,
            string name)
        {
            OdataId = odataId ?? throw new ArgumentNullException(nameof(odataId));
            OdataType = odataType ?? throw new ArgumentNullException(nameof(odataType));
            CertificateString = certificateString ?? throw new ArgumentNullException(nameof(certificateString));
            CertificateType = certificateType;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        [JsonProperty("@odata.context")] public string? OdataContext { get; set; } // Optional

        [JsonProperty("@odata.etag")] public string? OdataEtag { get; set; } // Optional

        [JsonProperty("@odata.id")] [Required] public string OdataId { get; set; } // Required property

        [JsonProperty("@odata.type")]
        [Required]
        public string OdataType { get; set; } // Required property

        [JsonProperty("CertificateString")]
        [Required]
        public string CertificateString { get; set; } // Required on create

        [JsonProperty("CertificateType")]
        [Required]
        [JsonConverter(typeof(StringEnumConverter))]
        public CertificateType CertificateType { get; set; } // Required on create

        [JsonProperty("Id")] [Required] public string Id { get; set; } // Required property

        [JsonProperty("Issuer")] public CertificateIssuer? Issuer { get; set; } // Optional

        [JsonProperty("Name")] [Required] public string Name { get; set; } // Required property

        [JsonProperty("SerialNumber")] public string? SerialNumber { get; set; } // Optional

        [JsonProperty("Subject")] public CertificateSubject? Subject { get; set; } // Optional

        [JsonProperty("ValidNotAfter")] public DateTime? ValidNotAfter { get; set; } // Optional

        [JsonProperty("ValidNotBefore")] public DateTime? ValidNotBefore { get; set; } // Optional

        [JsonProperty("UefiSignatureOwner")] public Guid? UefiSignatureOwner { get; set; } // Optional
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CertificateType
    {
        [EnumMember(Value = "PEM")] PEM,

        [EnumMember(Value = "PKCS7")] PKCS7
    }

    public class CertificateIssuer
    {
        [JsonProperty("City")] public string? City { get; set; } // Optional

        [JsonProperty("CommonName")] public string? CommonName { get; set; } // Optional

        [JsonProperty("Country")] public string? Country { get; set; } // Optional

        [JsonProperty("DisplayString")] public string? DisplayString { get; set; } // Optional

        [JsonProperty("Email")] public string? Email { get; set; } // Optional

        [JsonProperty("Organization")] public string? Organization { get; set; } // Optional

        [JsonProperty("OrganizationalUnit")] public string? OrganizationalUnit { get; set; } // Optional

        [JsonProperty("State")] public string? State { get; set; } // Optional
    }

    public class CertificateSubject
    {
        [JsonProperty("City")] public string? City { get; set; } // Optional

        [JsonProperty("CommonName")] public string? CommonName { get; set; } // Optional

        [JsonProperty("Country")] public string? Country { get; set; } // Optional

        [JsonProperty("DisplayString")] public string? DisplayString { get; set; } // Optional

        [JsonProperty("Email")] public string? Email { get; set; } // Optional

        [JsonProperty("Organization")] public string? Organization { get; set; } // Optional

        [JsonProperty("OrganizationalUnit")] public string? OrganizationalUnit { get; set; } // Optional

        [JsonProperty("State")] public string? State { get; set; } // Optional
    }
}