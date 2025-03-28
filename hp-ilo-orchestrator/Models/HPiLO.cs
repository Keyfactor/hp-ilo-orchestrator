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
        // Note: Changed to a single object rather than an array
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
        public string Action { get; set; }
        public CertificateCollectionCSR CertificateCollection { get; set; }
    }

    public class CertificateCollectionCSR
    {
        public string odataid { get; set; }
    }

    // New model that holds the CSR subject properties.
    public class CertificateCSRProperties
    {
        [JsonProperty("CommonName")] public string CommonName { get; set; }

        [JsonProperty("Organization")] public string Organization { get; set; }

        [JsonProperty("OrganizationalUnit")] public string OrganizationalUnit { get; set; }

        [JsonProperty("Locality")] public string Locality { get; set; }

        [JsonProperty("State")] public string State { get; set; }

        [JsonProperty("Country")] public string Country { get; set; }
    }

    // Updated request model to include CSR properties.
    public class GenerateCsrRequestDevID
    {
        [JsonProperty("Action")] public string Action { get; set; } = "CertificateService.GenerateCSR";

        [JsonProperty("CertificateCollection")]
        public CertificateCollectionReference CertificateCollection { get; set; }
    }

    public class CertificateCollectionReference
    {
        [JsonProperty("@odata.id")] public string ODataId { get; set; }
    }

    // Supporting model for the Systems collection.
    public class SystemCollection
    {
        [JsonProperty("Members")] public List<SystemReference>? Members { get; set; }
    }

    public class SystemReference
    {
        [JsonProperty("@odata.id")] public string? OdataId { get; set; }
    }

    // Model for the BIOS certificate information.
    public class BiosCertificateInfo
    {
        [JsonProperty("Id")] public string? Id { get; set; }

        [JsonProperty("Name")] public string? Name { get; set; }

        [JsonProperty("CertificateString")] public string? CertificateString { get; set; }

        [JsonProperty("CertificateType")] public string? CertificateType { get; set; }

        // Additional properties such as Issuer, Subject, etc., can be added if provided by the API.
    }

    public class GenerateCSRHTTPSCert
    {
        [JsonProperty("OrgName", Order = 1)] public string OrgName { get; set; }

        [JsonProperty("OrgUnit", Order = 2)] public string OrgUnit { get; set; }

        [JsonProperty("CommonName", Order = 3)]
        public string CommonName { get; set; }

        [JsonProperty("Country", Order = 4)] public string Country { get; set; }

        [JsonProperty("State", Order = 5)] public string State { get; set; }

        [JsonProperty("City", Order = 6)] public string City { get; set; }

        [JsonProperty("IncludeIP", Order = 7)] public bool IncludeIP { get; set; }
    }

    public enum CSRType
    {
        HTTPSCert,
        iLOLDevID
    }

    public class HpeHttpsCert
    {
        [JsonProperty("@odata.context")] public string OdataContext { get; set; }

        [JsonProperty("@odata.etag")] public string OdataEtag { get; set; }

        [JsonProperty("@odata.id")] public string OdataId { get; set; }

        [JsonProperty("@odata.type")] public string OdataType { get; set; }

        [JsonProperty("Id")] public string Id { get; set; }

        [JsonProperty("Actions")] public HpeHttpsCertActions Actions { get; set; }

        [JsonProperty("CertificateSigningRequest")]
        public string CertificateSigningRequest { get; set; }

        [JsonProperty("X509CertificateInformation")]
        public X509CertificateInformation X509CertificateInformation { get; set; }
    }

    public class HpeHttpsCertActions
    {
        [JsonProperty("#HpeHttpsCert.GenerateCSR")]
        public HpeHttpsCertAction GenerateCSR { get; set; }

        [JsonProperty("#HpeHttpsCert.ImportCertificate")]
        public HpeHttpsCertAction ImportCertificate { get; set; }
    }

    public class HpeHttpsCertAction
    {
        [JsonProperty("target")] public string Target { get; set; }
    }

    public class X509CertificateInformation
    {
        [JsonProperty("Issuer")] public string Issuer { get; set; }

        [JsonProperty("SerialNumber")] public string SerialNumber { get; set; }

        [JsonProperty("Subject")] public string Subject { get; set; }

        [JsonProperty("ValidNotAfter")] public string ValidNotAfter { get; set; }

        [JsonProperty("ValidNotBefore")] public string ValidNotBefore { get; set; }
    }

    public class ImportCertificateiLOLDevID
    {
        [JsonProperty("CertificateType")] public string CertificateType { get; set; }

        [JsonProperty("CertificateString")] public string CertificateString { get; set; }
    }
}