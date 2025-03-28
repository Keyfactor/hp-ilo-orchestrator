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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Models
{
    public class HPiLOCertificate
    {
        [JsonProperty("@odata.context")] public string OdataContext { get; set; }

        [JsonProperty("@odata.etag")] public string OdataEtag { get; set; }

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

        [JsonProperty("Issuer")] public CertificateIssuer Issuer { get; set; }

        [JsonProperty("Name")] [Required] public string Name { get; set; } // Required property

        [JsonProperty("SerialNumber")] public string SerialNumber { get; set; }

        [JsonProperty("Subject")] public CertificateSubject Subject { get; set; }

        [JsonProperty("ValidNotAfter")] public DateTime? ValidNotAfter { get; set; }

        [JsonProperty("ValidNotBefore")] public DateTime? ValidNotBefore { get; set; }

        [JsonProperty("UefiSignatureOwner")] public Guid? UefiSignatureOwner { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CertificateType
    {
        [EnumMember(Value = "PEM")] PEM,

        [EnumMember(Value = "PKCS7")] PKCS7
    }

    public class CertificateIssuer
    {
        [JsonProperty("City")] public string City { get; set; }

        [JsonProperty("CommonName")] public string CommonName { get; set; }

        [JsonProperty("Country")] public string Country { get; set; }

        [JsonProperty("DisplayString")] public string DisplayString { get; set; } // May be null

        [JsonProperty("Email")] public string Email { get; set; } // May be null

        [JsonProperty("Organization")] public string Organization { get; set; }

        [JsonProperty("OrganizationalUnit")] public string OrganizationalUnit { get; set; }

        [JsonProperty("State")] public string State { get; set; }
    }

    public class CertificateSubject
    {
        [JsonProperty("City")] public string City { get; set; }

        [JsonProperty("CommonName")] public string CommonName { get; set; }

        [JsonProperty("Country")] public string Country { get; set; }

        [JsonProperty("DisplayString")] public string DisplayString { get; set; } // May be null

        [JsonProperty("Email")] public string Email { get; set; } // May be null

        [JsonProperty("Organization")] public string Organization { get; set; }

        [JsonProperty("OrganizationalUnit")] public string OrganizationalUnit { get; set; }

        [JsonProperty("State")] public string State { get; set; }
    }
}