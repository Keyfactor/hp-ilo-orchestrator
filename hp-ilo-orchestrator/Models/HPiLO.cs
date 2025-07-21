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
using System.Collections.Generic;

namespace Keyfactor.Extensions.Orchestrator.HPiLO.Models
{
    public class SecurityServiceResource
    {
        [JsonProperty("Links")] public SecurityServiceLinks? Links { get; set; }

        [JsonProperty("iLOLDevID")] public iLOLDevId? iLOLDevID { get; set; }

        [JsonProperty("iLOIDevID")] public iLOIDevID? iLOIDevID { get; set; }

        [JsonProperty("SystemIDevID")] public SystemIDevID? SystemIDevID { get; set; }

        [JsonProperty("SystemIAK")] public SystemIAK? SystemIAK { get; set; }

        [JsonProperty("BMCIDevIDPCA")] public SystemIAK? BMCIDevIDPCA { get; set; }

        [JsonProperty("PlatformCert")] public PlatformCert? PlatformCert { get; set; }
    }

    public class SecurityServiceLinks
    {
        [JsonProperty("HttpsCert")] public ODataIdRef? HttpsCert { get; set; }
    }

    public class iLOLDevId
    {
        [JsonProperty("Certificates")] public ODataIdRef? Certificates { get; set; }
    }

    public class iLOIDevID
    {
        [JsonProperty("Certificates")] public ODataIdRef? Certificates { get; set; }
    }

    public class BMCIDevIDPCA
    {
        [JsonProperty("Certificates")] public ODataIdRef? Certificates { get; set; }
    }

    public class PlatformCert
    {
        [JsonProperty("Certificates")] public ODataIdRef? Certificates { get; set; }
    }

    public class SystemIAK
    {
        [JsonProperty("Certificates")] public ODataIdRef? Certificates { get; set; }
    }

    public class SystemIDevID
    {
        [JsonProperty("Certificates")] public ODataIdRef? Certificates { get; set; }
    }

    public class ODataIdRef
    {
        [JsonProperty("@odata.id")] public string? ODataId { get; set; }
    }

    public class CertificateCollection
    {
        [JsonProperty("Members")] public List<ODataIdRef>? Members { get; set; }
    }

    public class iLOCertificateInfo
    {
        [JsonProperty("Id")] public string? Id { get; set; }

        [JsonProperty("Name")] public string? Name { get; set; }

        [JsonProperty("CertificateString")] public string? CertificateString { get; set; }

        [JsonProperty("CertificateType")] public string? CertificateType { get; set; }

        [JsonProperty("Issuer")] public CertEntity? Issuer { get; set; }

        [JsonProperty("Subject")] public CertEntity? Subject { get; set; }

        [JsonProperty("ValidNotBefore")] public string? ValidNotBefore { get; set; }

        [JsonProperty("ValidNotAfter")] public string? ValidNotAfter { get; set; }
    }

    public class CertEntity
    {
        [JsonProperty("CommonName")] public string? CommonName { get; set; }
    }

    public class ManagerCollection
    {
        [JsonProperty("@odata.context")] public string? OdataContext { get; set; }

        [JsonProperty("@odata.etag")] public string? OdataEtag { get; set; }

        [JsonProperty("@odata.id")] public string? OdataId { get; set; }

        [JsonProperty("@odata.type")] public string? OdataType { get; set; }

        [JsonProperty("Description")] public string? Description { get; set; }

        [JsonProperty("Name")] public string? Name { get; set; }

        [JsonProperty("Members")] public List<ManagerReference>? Members { get; set; }

        [JsonProperty("Members@odata.count")] public int? MembersCount { get; set; }
    }

    public class ManagerReference
    {
        [JsonProperty("@odata.id")] public string? OdataId { get; set; }
    }

    public class CSRRequest
    {
        public string Action { get; set; } = "CertificateService.GenerateCSR";
        public CertificateCollectionCSR CertificateCollection { get; set; } = new();
    }

    public class CertificateCollectionCSR
    {
        public string odataid { get; set; } = string.Empty;
    }

    public class GenerateCsrRequestDevID
    {
        [JsonProperty("Action")] public string Action { get; set; } = "CertificateService.GenerateCSR";

        [JsonProperty("CertificateCollection")]
        public CertificateCollectionReference CertificateCollection { get; set; } = new();
    }

    public class CertificateCollectionReference
    {
        [JsonProperty("@odata.id")] public string ODataId { get; set; } = string.Empty;
    }

    public class SystemCollection
    {
        [JsonProperty("Members")] public List<SystemReference>? Members { get; set; }
    }

    public class SystemReference
    {
        [JsonProperty("@odata.id")] public string? OdataId { get; set; }
    }

    public class BiosCertificateInfo
    {
        [JsonProperty("Id")] public string? Id { get; set; }

        [JsonProperty("Name")] public string? Name { get; set; }

        [JsonProperty("CertificateString")] public string? CertificateString { get; set; }

        [JsonProperty("CertificateType")] public string? CertificateType { get; set; }
    }

    public class GenerateCSRHTTPSCert
    {
        [JsonProperty("OrgName", Order = 1)] public string OrgName { get; set; } = string.Empty;

        [JsonProperty("OrgUnit", Order = 2)] public string OrgUnit { get; set; } = string.Empty;

        [JsonProperty("CommonName", Order = 3)]
        public string CommonName { get; set; } = string.Empty;

        [JsonProperty("Country", Order = 4)] public string Country { get; set; } = string.Empty;

        [JsonProperty("State", Order = 5)] public string State { get; set; } = string.Empty;

        [JsonProperty("City", Order = 6)] public string City { get; set; } = string.Empty;

        [JsonProperty("IncludeIP", Order = 7)] public bool IncludeIP { get; set; }
    }

    public enum iLOCertType
    {
        HTTPSCert,
        iLOLDevID,
        PlatformCert,
        iLOIDevID,
        BMCIDevIDPCA,
        SystemIAK
    }

    public class HpeHttpsCert
    {
        [JsonProperty("@odata.context")] public string OdataContext { get; set; } = string.Empty;

        [JsonProperty("@odata.etag")] public string OdataEtag { get; set; } = string.Empty;

        [JsonProperty("@odata.id")] public string OdataId { get; set; } = string.Empty;

        [JsonProperty("@odata.type")] public string OdataType { get; set; } = string.Empty;

        [JsonProperty("Id")] public string Id { get; set; } = string.Empty;

        [JsonProperty("Actions")] public HpeHttpsCertActions Actions { get; set; } = new();

        [JsonProperty("CertificateSigningRequest")]
        public string CertificateSigningRequest { get; set; } = string.Empty;

        [JsonProperty("X509CertificateInformation")]
        public X509CertificateInformation X509CertificateInformation { get; set; } = new();
    }

    public class HpeHttpsCertActions
    {
        [JsonProperty("#HpeHttpsCert.GenerateCSR")]
        public HpeHttpsCertAction GenerateCSR { get; set; } = new();

        [JsonProperty("#HpeHttpsCert.ImportCertificate")]
        public HpeHttpsCertAction ImportCertificate { get; set; } = new();
    }

    public class HpeHttpsCertAction
    {
        [JsonProperty("target")] public string Target { get; set; } = string.Empty;
    }

    public class X509CertificateInformation
    {
        [JsonProperty("Issuer")] public string Issuer { get; set; } = string.Empty;

        [JsonProperty("SerialNumber")] public string SerialNumber { get; set; } = string.Empty;

        [JsonProperty("Subject")] public string Subject { get; set; } = string.Empty;

        [JsonProperty("ValidNotAfter")] public string ValidNotAfter { get; set; } = string.Empty;

        [JsonProperty("ValidNotBefore")] public string ValidNotBefore { get; set; } = string.Empty;
    }

    public class ImportCertificateiLOLDevID
    {
        [JsonProperty("CertificateType")] public string CertificateType { get; set; } = string.Empty;

        [JsonProperty("CertificateString")] public string CertificateString { get; set; } = string.Empty;
    }

    public class iLOLDevCSR
    {
        public string CSRString { get; set; } = string.Empty;
        public Certificatecollection CertificateCollection { get; set; } = new();
    }

    public class Certificatecollection
    {
        public string odataid { get; set; } = string.Empty;
    }
}