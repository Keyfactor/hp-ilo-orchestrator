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
using Keyfactor.Orchestrators.Common.Enums;
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Keyfactor.Extensions.Orchestrator.HPiLO
{
    public class ClientOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int HttpsCertWaitSeconds { get; set; }
        public bool InventoryAll { get; set; } = false;
    }

    public interface IHPiLOClient
    {
        bool DeleteCertificate(iLOCertType csrType);
        string GenerateCSR(string actionApiAddress, string distinguishedName, iLOCertType type, bool includeIP);
        bool AddCertificate(string PemCert, iLOCertType certType);
        iLOCertType CheckType(string input);
    }

    public class HPiLOClient : IHPiLOClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;
        private readonly ClientOptions _opts;

        public HPiLOClient(HttpClient client, IOptions<ClientOptions> options, ILogger logger)
        {
            _logger = logger;
            _logger.LogTrace("Constructing HPiLOClient with BaseUrl={Url}, User={User}", options.Value.BaseUrl,
                options.Value.Username);

            _client = client;
            _opts = options.Value;

            // Base setup
            _client.BaseAddress = new Uri(_opts.BaseUrl.TrimEnd('/'));
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            string basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_opts.Username}:{_opts.Password}"));
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

            _logger.LogDebug("HTTP client configured: BaseAddress={BaseAddress}", _client.BaseAddress);
        }

        /// <summary>
        ///     Determines what API Endpoint to hit based on the certificate type, used for both Reenrollment and Management Add.
        /// </summary>
        public bool AddCertificate(string PemCert, iLOCertType certType)
        {
            _logger.LogTrace("Entering DeleteCertificate: Type={Type}", certType);
            string rel = certType switch
            {
                iLOCertType.iLOLDevID => APIEndpoints.ImportCertificateiLOLDevID,
                iLOCertType.HTTPSCert => APIEndpoints.ImportCertificateHTTPSCert,
                _ => throw new ArgumentException("Invalid Certificate type.")
            };

            if (certType == iLOCertType.HTTPSCert)
            {
                _logger.LogDebug("Importing HTTPS certificate with private key");
                if (ImportCertificate(PemCert, rel, certType))
                {
                    return true;
                }
            }
            else if (certType == iLOCertType.iLOLDevID)
            {
                _logger.LogDebug("Importing iLOLDevID certificate");
                if (ImportCertificate(PemCert, rel, certType))
                {
                    return true;
                }
            }
            else
            {
                throw new Exception("Reenrollment for this certificate is not supported by HP iLO.");
            }

            return false;
        }

        /// <summary>
        ///     Deletes a certificate from the iLO based on the provided certificate type.
        /// </summary>
        public bool DeleteCertificate(iLOCertType csrType)
        {
            _logger.LogTrace("Entering DeleteCertificate: Type={Type}", csrType);

            string rel = csrType switch
            {
                iLOCertType.iLOLDevID => APIEndpoints.DeleteCertificateiLOLDevID,
                iLOCertType.HTTPSCert => APIEndpoints.DeleteCertificateHTTPSCert,
                _ => throw new ArgumentException(
                    "Invalid cert type. Only deletion of iLOLDevID and HTTPSCert is supported.")
            };

            Uri uri = BuildUri(rel);
            _logger.LogDebug("DELETE request URI: {Uri}", uri);
            HttpRequestMessage request = new(HttpMethod.Delete, uri);

            HttpResponseMessage resp = _client.Send(request, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogDebug("Received response: Status={StatusCode}", resp.StatusCode);

            if (resp.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully deleted certificate: {Type}", csrType);
                return true;
            }

            _logger.LogError("Deletion failed: Status={Status}", resp.StatusCode);
            throw new HttpRequestException($"Deletion failed: {resp.StatusCode}");
        }

        /// <summary>
        ///     Handles CSR generation for both iLOLDevID and HTTPSCert types for reenrollment.
        /// </summary>
        public string GenerateCSR(string actionApiAddress, string distinguishedName, iLOCertType type, bool includeIP)
        {
            _logger.LogTrace("Entering GenerateCSR: Action={Action}, DN={DN}, Type={Type}", actionApiAddress,
                distinguishedName, type);

            string payload = type switch
            {
                iLOCertType.iLOLDevID => JsonConvert.SerializeObject(new GenerateCsrRequestDevID
                {
                    CertificateCollection = new CertificateCollectionReference
                    {
                        ODataId = APIEndpoints.ImportCertificateiLOLDevID
                    }
                }),
                iLOCertType.HTTPSCert => JsonConvert.SerializeObject(ParseSubjectText(distinguishedName, includeIP)),
                _ => throw new ArgumentException("Invalid CSR type")
            };

            _logger.LogDebug("CSR payload: {Payload}", payload);
            HttpRequestMessage req = new(HttpMethod.Post, new Uri(actionApiAddress, UriKind.RelativeOrAbsolute))
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            _logger.LogDebug("POST request URI: {Uri}", req.RequestUri);
            HttpResponseMessage resp = _client.Send(req, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogDebug("Received response: Status={StatusCode}", resp.StatusCode);
            resp.EnsureSuccessStatusCode();
            string json1;
            using (Stream stream = resp.Content.ReadAsStream())
            using (StreamReader reader = new(stream, Encoding.UTF8))
            {
                json1 = reader.ReadToEnd();
            }

            _logger.LogTrace("CSR response payload length: {Length}", json1.Length);

            if (type == iLOCertType.HTTPSCert && _opts.HttpsCertWaitSeconds > 0)
            {
                _logger.LogDebug("Waiting {Seconds}s for HTTPS CSR re-enrollment", _opts.HttpsCertWaitSeconds);
                Thread.Sleep(_opts.HttpsCertWaitSeconds * 1000);
            }

            if (type == iLOCertType.HTTPSCert)
            {
                // Retrieve the generated CSR from the HTTPSCert endpoint in Manager 1.
                string csrAddress = "/redfish/v1/Managers/1/SecurityService/HttpsCert/";
                HttpRequestMessage req2 = new(HttpMethod.Get, csrAddress);
                HttpResponseMessage responseHTTPSCsr = _client.Send(req2, HttpCompletionOption.ResponseHeadersRead);
                resp.EnsureSuccessStatusCode();

                string json2;
                using (Stream stream = responseHTTPSCsr.Content.ReadAsStream())
                using (StreamReader reader = new(stream, Encoding.UTF8))
                {
                    json2 = reader.ReadToEnd();
                }

                // Deserialize the JSON into an HpeHttpsCert object.
                HpeHttpsCert certInfo = JsonConvert.DeserializeObject<HpeHttpsCert>(json2);

                // Return the HTTPSCert CSR 
                return certInfo?.CertificateSigningRequest ?? string.Empty;
            }

            //Return iLOLDevID CSR string
            string result = JsonConvert.DeserializeObject<iLOLDevCSR>(json1)!.CSRString;
            _logger.LogInformation("Generated CSR successfully for Type={Type}", type);
            return result;
        }

        /// <summary>
        ///     Determines the enum certificate type by checking if the input string contains any of the substrings.
        /// </summary>
        public iLOCertType CheckType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogError("Input is null or empty.  Please specify alias.");
                throw new ArgumentException("Input cannot be null or empty.  Please specify alias.");
            }

            foreach (iLOCertType certType in Enum.GetValues(typeof(iLOCertType)))
            {
                if (input.Contains(certType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return certType;
                }
            }

            _logger.LogError("Unknown CSR type for input: {Input}. Please specify alias.", input);
            throw new ArgumentException("Unknown CSR type based on alias input. Please specify alias.");
        }

        /// <summary>
        ///     Retrieves all certificates from the iLO, including HTTPSCert and iLOLDevID.
        /// </summary>
        public List<CurrentInventoryItem> GetAllCertificates()
        {
            _logger.LogTrace("Entering GetAllCertificates");
            _logger.LogDebug("Retrieving TLS certificate from BaseAddress={Base}", _client.BaseAddress);
            Dictionary<string, object> Paramaters = new();
            //This retrieves the TLS certificate chain from the iLO's BaseAddress. There is no way to inventory it 
            //through the Redfish API, so we do it manually.
            List<X509Certificate2> certs = GetCertificateChain(_client.BaseAddress!.ToString());
            List<CurrentInventoryItem> items = new()
            {
                new CurrentInventoryItem
                {
                    Alias = "HTTPSCert",
                    Certificates = ExportToPem(certs),
                    ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                    PrivateKeyEntry = true,
                    UseChainLevel = true,
                    Parameters = Paramaters
                }
            };
            //All the other certificates are retrieved through the Redfish API and can be iterated through per manager.
            ManagerCollection mgrColl = GetSync<ManagerCollection>(APIEndpoints.ManagersCollection);
            foreach (ManagerReference mgr in mgrColl.Members!)
            {
                int id = ExtractManagerID(mgr.OdataId);
                _logger.LogDebug("Processing Manager ID={Id}", id);

                SecurityServiceResource sec = GetSync<SecurityServiceResource>($"{mgr.OdataId}SecurityService");
                items.AddRange(ProcessCert($"{id}/iLOLDevID", sec.iLOLDevID.Certificates));


                if (_opts.InventoryAll)
                {
                    items.AddRange(ProcessCert($"{id}/PlatformCert", sec.PlatformCert.Certificates));
                    items.AddRange(ProcessCert(
                        $"{id}/iLOIDevID" + (sec.iLOIDevID != null ? string.Empty : "/BMCIDevIDPCA"),
                        sec.iLOIDevID?.Certificates ?? sec.BMCIDevIDPCA.Certificates));
                    items.AddRange(ProcessCert($"{id}/SystemIAK", sec.SystemIAK.Certificates));
                }
            }


            _logger.LogInformation("Total certificates retrieved: {Count}", items.Count);
            return items;
        }

        /// <summary>
        ///     Submits a certificate to iLO.
        /// </summary>
        public bool ImportCertificate(string certificate, string endpoint, iLOCertType certType)
        {
            _logger.LogTrace("Entering ImportCertificate: Endpoint={End}, Type={Type}", endpoint, certType);

            object body = certType switch
            {
                iLOCertType.HTTPSCert => new { Certificate = certificate },
                iLOCertType.iLOLDevID => new ImportCertificateiLOLDevID
                {
                    CertificateString = certificate, CertificateType = "PEM"
                },
                _ => throw new ArgumentException("Invalid CSR type")
            };

            string jsonBody = JsonConvert.SerializeObject(body);
            _logger.LogDebug("Import payload: CertificateType={Type}", certType);
            HttpRequestMessage req = new(HttpMethod.Post, new Uri(endpoint, UriKind.RelativeOrAbsolute))
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };

            _logger.LogDebug("POST import request URI: {Uri}", req.RequestUri);
            HttpResponseMessage resp = _client.Send(req, HttpCompletionOption.ResponseHeadersRead);
            _logger.LogDebug("Received import response: Status={StatusCode}", resp.StatusCode);
            resp.EnsureSuccessStatusCode();

            _logger.LogInformation("Imported certificate successfully: Type={Type}", certType);


            return true;
        }

        /// <summary>
        /// Determines the enum certificate type by checking if the input string contains any of the substrings.
        /// </summary>
        public iLOCertType CheckType(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                _logger.LogError("Input is null or empty.  Please specify alias.");
                throw new ArgumentException("Input cannot be null or empty.  Please specify alias.");
            }

            foreach (iLOCertType certType in Enum.GetValues(typeof(iLOCertType)))
            {
                if (input.Contains(certType.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return certType;
                }
            }

            _logger.LogError("Unknown CSR type for input: {Input}. Please specify alias.", input);
            throw new ArgumentException("Unknown CSR type based on alias input. Please specify alias.");
        }

        /// <summary>
        /// Concatenates a relative path with the base URI to build a full URI.
        /// </summary>
        private Uri BuildUri(string relativePath)
        {
            _logger.LogTrace("Building URI from relative path: {Path}", relativePath);
            UriBuilder builder = new(_client.BaseAddress!)
            {
                Path = $"{_client.BaseAddress.AbsolutePath.TrimEnd('/')}/{relativePath.TrimStart('/')}"
            };
            Uri uri = builder.Uri;
            _logger.LogDebug("Built URI: {Uri}", uri);
            return uri;
        }

        /// <summary>
        ///     Wrapper for all GET requests to the Redfish API. Contains retry logic.
        /// </summary>
        private T GetSync<T>(string relativePath, int maxRetries = 3)
        {
            _logger.LogTrace("Entering GetSync<{Type}>: Path={Path}", typeof(T).Name, relativePath);
            Uri uri = BuildUri(relativePath);
            Exception lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    HttpRequestMessage request = new(HttpMethod.Get, uri);
                    _logger.LogDebug("GET request URI: {Uri}", uri);

                    HttpResponseMessage resp = _client.Send(request, HttpCompletionOption.ResponseHeadersRead);
                    _logger.LogDebug("Received GET response: Status={StatusCode}", resp.StatusCode);
                    resp.EnsureSuccessStatusCode();

                    string json;
                    using (Stream stream = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (StreamReader reader = new(stream, Encoding.UTF8))
                    {
                        json = reader.ReadToEnd();
                    }

                    _logger.LogTrace("GET response payload length: {Length}", json.Length);

                    return JsonConvert.DeserializeObject<T>(json)!;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    _logger.LogWarning(ex, "Attempt {Attempt} to retrieve data from {Path} failed.", attempt,
                        relativePath);
                    if (attempt < maxRetries)
                    {
                        Thread.Sleep(60000); // Delay before retrying
                    }
                }
            }

            _logger.LogError(lastEx, "Failed to retrieve data from {Path} after {MaxRetries} attempts.", relativePath,
                maxRetries);
            throw new InvalidOperationException(
                $"Could not retrieve data from {relativePath} after {maxRetries} tries.", lastEx);
        }

        /// <summary>
        ///     Extracts the Manager ID from a Redfish path.
        /// </summary>
        private int ExtractManagerID(string path)
        {
            _logger.LogTrace("Extracting Manager ID from path: {Path}", path);
            Match match = Regex.Match(path, @"/redfish/v1/Managers/(\d+)/?");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                _logger.LogDebug("Extracted Manager ID: {Id}", id);
                return id;
            }

            _logger.LogWarning("Manager ID not found in path '{Path}'", path);
            return 0;
        }

        /// <summary>
        ///     Prepares a certificate retrieved from Redfish into a Keyfactor-supported format.
        /// </summary>
        private List<CurrentInventoryItem> ProcessCert(string alias, ODataIdRef certRef)
        {
            _logger.LogTrace("Processing certificates for alias: {Alias}", alias);
            List<iLOCertificateInfo> infos = RetrieveCert(certRef.ODataId);
            _logger.LogDebug("Retrieved {Count} cert infos for alias {Alias}", infos.Count, alias);
            Dictionary<string, object> Paramaters = new();
            return infos.Select(info => new CurrentInventoryItem
            {
                Alias = alias,
                Certificates =
                    ExtractCertificates(info.CertificateString?.Replace("\r", "").Replace("\n", "") ??
                                        string.Empty),
                ItemStatus = OrchestratorInventoryItemStatus.Unknown,
                UseChainLevel = true,
                PrivateKeyEntry = false,
                Parameters = Paramaters
            }).ToList();
        }

        /// <summary>
        ///     Extracts PEM-encoded certificates from a PEM blob, ignoring any other content and returning a chain in a list.
        /// </summary>
        public static List<string> ExtractCertificates(string pemBlob)
        {
            if (pemBlob is null)
            {
                throw new ArgumentNullException(nameof(pemBlob));
            }

            List<string> certList = new();

            // Regex to match exactly the CERTIFICATE blocks, non-greedy
            Regex certRegex = new(
                "-----BEGIN CERTIFICATE-----(?<body>.*?)-----END CERTIFICATE-----",
                RegexOptions.Singleline | RegexOptions.Compiled);

            foreach (Match match in certRegex.Matches(pemBlob))
            {
                // match.Value includes both the BEGIN/END lines and everything in between
                certList.Add(match.Value);
            }

            return certList;
        }

        /// <summary>
        ///     Basic function to handle parsing cert type from alias.
        /// </summary>
        private static string GetCertificateTypeFromAlias(string alias)
        {
            return alias.Split('/').Last();
        }

        /// <summary>
        ///     Retrieves certificates from the Redfish API using the provided link.
        /// </summary>
        private List<iLOCertificateInfo> RetrieveCert(string certLink)
        {
            _logger.LogTrace("Entering RetrieveCert: Link={Link}", certLink);
            if (string.IsNullOrEmpty(certLink))
            {
                _logger.LogDebug("No cert link provided, returning empty list.");
                return new List<iLOCertificateInfo>();
            }

            CertificateCollection coll = GetSync<CertificateCollection>(certLink);
            List<iLOCertificateInfo> list = new();
            if (coll.Members != null)
            {
                foreach (ODataIdRef member in coll.Members)
                {
                    _logger.LogDebug("Retrieving member cert at ODataId={ODataId}", member.ODataId);
                    if (!string.IsNullOrEmpty(member.ODataId))
                    {
                        list.Add(GetSync<iLOCertificateInfo>(member.ODataId));
                    }
                }
            }

            _logger.LogDebug("Total certificates retrieved from link: {Count}", list.Count);
            return list;
        }

        /// <summary>
        ///     Manually retrieves the TLS certificate chain from the iLO host.
        /// </summary>
        public List<X509Certificate2> GetCertificateChain(string hostEntry, int port = 443, int maxRetries = 3)
        {
            Exception lastEx = null;
            string host = StripScheme(hostEntry);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // 1) Establish TCP + TLS, grab the leaf cert
                    using TcpClient tcp = new(host, port);
                    using SslStream ssl = new(
                        tcp.GetStream(),
                        false,
                        (s, cert, chain, errors) => true,
                        null
                    );
                    ssl.AuthenticateAsClient(host);

                    X509Certificate2 leafCert = ssl.RemoteCertificate is X509Certificate2 c2
                        ? c2
                        : new X509Certificate2(ssl.RemoteCertificate);

                    // 2) Now build the full chain from that leaf
                    X509Chain chain = new()
                    {
                        // tweak policy so intermediates are fetched from AIA if needed
                        ChainPolicy =
                        {
                            RevocationMode = X509RevocationMode.NoCheck,
                            VerificationFlags = X509VerificationFlags.IgnoreWrongUsage,
                            UrlRetrievalTimeout = TimeSpan.FromSeconds(5)
                        }
                    };

                    bool built = chain.Build(leafCert);

                    // 3) Always include at least the leaf
                    List<X509Certificate2> certs = new() { leafCert };

                    if (built && chain.ChainElements.Count > 1)
                    {
                        // skip element 0 (the leaf we already have)
                        certs.AddRange(chain.ChainElements
                            .Skip(1)
                            .Select(e => new X509Certificate2(e.Certificate)));
                    }

                    return certs;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    _logger.LogWarning(ex,
                        "Attempt {Attempt} to retrieve cert chain failed. (Could be waiting for reboot after HTTPSCert reenrollment/ODKG?)",
                        attempt);
                    Thread.Sleep(60000);
                }
            }

            _logger.LogError(lastEx, "Failed to retrieve certificate chain after {MaxRetries} attempts.", maxRetries);
            throw new InvalidOperationException(
                $"Could not retrieve cert chain from {host}:{port} after {maxRetries} tries.",
                lastEx);
        }

        /// <summary>
        ///     Parses the distinguished name and IP inclusion flag into a GenerateCSRHTTPSCert object.
        /// </summary>
        public static GenerateCSRHTTPSCert ParseSubjectText(string distinguishedName, bool includeIP)
        {
            GenerateCSRHTTPSCert req = new() { IncludeIP = includeIP };
            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                return req;
            }

            foreach (string part in distinguishedName.Split(','))
            {
                string[] kv = part.Split('=', 2);
                if (kv.Length != 2)
                {
                    continue;
                }

                string key = kv[0].Trim().ToUpperInvariant();
                string val = kv[1].Trim();

                switch (key)
                {
                    case "C": req.Country = val; break;
                    case "ST": req.State = val; break;
                    case "L": req.City = val; break;
                    case "O": req.OrgName = val; break;
                    case "OU": req.OrgUnit = val; break;
                    case "CN": req.CommonName = val; break;
                    case "INCLUDEIP":
                    case "IP":
                        if (bool.TryParse(val, out bool incl))
                        {
                            req.IncludeIP = incl;
                        }

                        break;
                }
            }

            return req;
        }

        /// <summary>
        ///     Strips the scheme (http/https) from a URL.
        /// </summary>
        private static string StripScheme(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri u))
            {
                return u.Host;
            }

            const string https = "https://";
            const string http = "http://";
            if (url.StartsWith(https, StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(https.Length);
            }

            if (url.StartsWith(http, StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(http.Length);
            }

            return url;
        }

        /// <summary>
        ///     Processes the manually retrieved TLS certificates into PEM format for Keyfactor.
        /// </summary>
        private static string[] ExportToPem(IEnumerable<X509Certificate2> certificates)
        {
            if (certificates == null)
            {
                throw new ArgumentNullException(nameof(certificates));
            }

            // Materialize the list so we can check for null elements
            IList<X509Certificate2> certList = certificates as IList<X509Certificate2> ?? certificates.ToList();

            if (certList.Any(c => c == null))
            {
                throw new ArgumentException("Certificate collection contains a null element.", nameof(certificates));
            }

            return certList
                .Select(cert =>
                {
                    // Export the certificate in DER format
                    byte[] certBytes = cert.Export(X509ContentType.Cert);

                    // Build the PEM block
                    StringBuilder sb = new();
                    sb.AppendLine("-----BEGIN CERTIFICATE-----");
                    sb.AppendLine(Convert.ToBase64String(certBytes, Base64FormattingOptions.InsertLineBreaks));
                    sb.AppendLine("-----END CERTIFICATE-----");

                    return sb.ToString();
                })
                .ToArray();
        }
    }
}