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
using Keyfactor.Orchestrators.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Keyfactor.PKI.PKIConstants.Microsoft;

namespace Keyfactor.Extensions.Orchestrator.HPiLO
{
    public interface IHPiLOClient
    {
    }

    public class HPiLOClient : IHPiLOClient
    {
        private readonly string _baseUrl;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly bool _ignorevalidation;
        private readonly bool _inventoryall;
        private readonly string _password;
        private readonly string _username;
        private readonly ILogger logger;
        private readonly string _httpscertwaittime;
        // The client is configured with the iLO base URL and credentials.
        public HPiLOClient(IHttpClientFactory httpClientFactory, string baseUrl,
            string username, string password, bool ignorevalidation, bool inventoryall, string waittime, ILogger inputlogger)
        {
            _httpClientFactory = httpClientFactory;
            _baseUrl = baseUrl;
            _username = username;
            _password = password;
            _ignorevalidation = ignorevalidation;
            _inventoryall = inventoryall;
            _httpscertwaittime = waittime;
            logger = inputlogger;
        }

        // Sends a delete request to given endpoint
        public bool DeleteCertificate(string alias)
        {
            // The endpoint for deleting the certificate
            string deleteUrl = "null";
            if (alias.IndexOf("iLOLDevID", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int managerNum = ExtractManagerID(alias);
                deleteUrl = "/redfish/v1/Managers/" + managerNum + "/SecurityService/iLOLDevID/";
            }
            else if (alias.IndexOf("HTTPSCert", StringComparison.OrdinalIgnoreCase) >= 0)

            {
                deleteUrl = "/redfish/v1/managers/{item}/securityservice/httpscert/";
            }
            else
            {
                throw new Exception("Invalid alias");
            }

            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = client.DeleteAsync(deleteUrl).GetAwaiter().GetResult();
                // Optionally, you can inspect response.StatusCode or response.Content here.
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                throw new Exception("Deletion failed.");
            }
        }

        // Retrieves the CSR, works for HTTPS cert and iLOLDevID certs
        public string GenerateCSR(
            string actionApiAddress,
            string DN, CSRType type)
        {
            string jsonPayload = "";
            if (type == CSRType.iLOLDevID)
            {
                GenerateCsrRequestDevID requestObj = new()
                {
                    CertificateCollection = new CertificateCollectionReference { ODataId = actionApiAddress }
                };
                jsonPayload = JsonConvert.SerializeObject(requestObj);
            }
            else if (type == CSRType.HTTPSCert)
            {
                GenerateCSRHTTPSCert requestObj = new();
                requestObj = ParseSubjectText(DN);
                jsonPayload = JsonConvert.SerializeObject(requestObj);
            }
            else
            {
                throw new Exception("Invalid CSR type");
            }


            using (HttpClient client = CreateClient())
            {
                HttpRequestMessage request = new(HttpMethod.Post, actionApiAddress)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                string responseJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (type == CSRType.iLOLDevID)
                {
                    iLOCertificateInfo result = JsonConvert.DeserializeObject<iLOCertificateInfo>(responseJson);
                    return result.CertificateString;
                }

                int waittime = 60;
                int.TryParse(_httpscertwaittime, out waittime);
                if (type == CSRType.HTTPSCert)
                {
                    // As per HP docs, we need to allow time for the CSR to be generated.
                    Task.Delay(TimeSpan.FromSeconds(waittime)).Wait();
                }
                else
                {
                    throw new Exception("Invalid CSR type");
                }
            }

            // Retrieve the generated CSR from the HTTPSCert endpoint in Manager 1.
            string csrAddress = "/redfish/v1/Managers/1/SecurityService/HttpsCert/";
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = client.GetAsync(csrAddress).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // Deserialize the JSON into an iLOCertificateInfo object.
                HpeHttpsCert certInfo = JsonConvert.DeserializeObject<HpeHttpsCert>(json);

                // Return the CertificateString property (which should contain the CSR)
                return certInfo?.CertificateSigningRequest ?? string.Empty;
            }
        }

        // Retrieves all available certificates from the SecurityService in each available manager. 
        // First retrieves the https one, then proceeds to factory certs.
        // Also retrieves the BIOS certs.
        public List<CurrentInventoryItem> GetAllCertificates()
        {
            List<CurrentInventoryItem> processed_certificates = new();
            // Retrieve the TLS Cert. There is only one per connection/iLO instance.
            X509Certificate2 HTTPSCert = GetCertificate(_baseUrl);
            CurrentInventoryItem TLSCert = new();
            TLSCert.Alias = "HTTPSCert";
            byte[] rawdata = HTTPSCert.RawData;
            List<string> Cert = new();
            Cert.Add(Convert.ToBase64String(rawdata));
            TLSCert.Certificates = Cert;
            processed_certificates.Add(TLSCert);

            // Iterate over the number of managers and get all managers stored on the server. 
            int managerNum = 0;
            List<int> managers = new();
            string urlManagers = "/redfish/v1/Managers/";
            ManagerCollection managersCollection = GetSync<ManagerCollection>(urlManagers);
            // For each manager
            foreach (ManagerReference manager in managersCollection.Members)
            {
                string managerPath = manager.OdataId;
                // 1. Get the SecurityService resource (which contains links to certificates)
                string url = managerPath + "SecurityService";
                SecurityServiceResource secService =
                    GetSync<SecurityServiceResource>(url);
                // 2. Retrieve the ILoLDevID cert. This one enables user configured 802.1x access and should be inventoried regardless. Refer to iLO Docs.
                int managerId = ExtractManagerID(manager.OdataId);
                processed_certificates.AddRange(
                    ProcessCert("Manager" + managerId + "/iLOLDevID", secService.iLOLDevID.Certificates));
                // 3. If _fullinventory, retrieve and process all the baked-in certs, place them into a list. These are the baked in certificates we cant re-enroll.
                if (_inventoryall)
                {
                    // TPM 2.0 Cert
                    processed_certificates.AddRange(ProcessCert(managerId + "/PlatformCert",
                        secService.PlatformCert.Certificates));
                    // These are factory installed certs used for 802.1x authentication. Can only have one or the other.
                    if (secService.iLOIDevID != null)
                    {
                        processed_certificates.AddRange(ProcessCert(managerId + "/iLOIDevID",
                            secService.iLOIDevID.Certificates));
                    }
                    else
                    {
                        processed_certificates.AddRange(ProcessCert(managerId + "/BMCIDevIDPCA",
                            secService.BMCIDevIDPCA.Certificates));
                    }

                    // Another TPM 2.0 cert
                    processed_certificates.AddRange(ProcessCert(managerId + "/SystemIAK",
                        secService.SystemIAK.Certificates));
                }
            }

            /*
            if (_inventoryall)
            {
                // Retrieve the BIOS HTTPS Certs.
                SystemCollection systems = GetSync<SystemCollection>("/redfish/v1/Systems");
                List<BiosCertificateInfo> biosCerts = new();
                if (systems.Members != null)
                {
                    foreach (SystemReference systemRef in systems.Members)
                    {
                        if (!string.IsNullOrEmpty(systemRef.OdataId))
                        {
                            // Construct the URL for the BIOS TLS configuration for this system.
                            string biosCertUrl = systemRef.OdataId.TrimEnd('/') + "/bios/tlsconfig/";

                            // Retrieve the BIOS certificate information.
                            BiosCertificateInfo certInfo = GetSync<BiosCertificateInfo>(biosCertUrl);
                            biosCerts.Add(certInfo);
                        }
                    }
                }

                // Process the BIOS HTTPS Certs.
                foreach (BiosCertificateInfo biosCertificateInfo in biosCerts)
                {
                    CurrentInventoryItem newCert = new();
                    newCert.Alias = "Boot TLS Certificate";
                    List<string> tempList = new();
                    tempList.Add(biosCertificateInfo.CertificateString);
                    newCert.Certificates = tempList;
                    processed_certificates.Add(newCert);
                }
            }
            */
            return processed_certificates;
        }

        // Extracts the manager ID from the OData ID.
        private int ExtractManagerID(string input)
        {
            // Pattern: look for "/Managers/" followed by one or more digits, optionally followed by a trailing slash.
            Regex regex = new(@"/redfish/v1/Managers/(\d+)/?");
            Match match = regex.Match(input);
            if (match.Success)
            {
                string idStr = match.Groups[1].Value;
                return int.Parse(idStr);
            }

            Console.WriteLine("ID not found.");
            return 0;
        }

        // Converts a cert retrieved from the endpoint to a CurrentInventoryItem
        private List<CurrentInventoryItem> ProcessCert(string alias, ODataIdRef certaddress)
        {
            List<CurrentInventoryItem> processedCerts = new();
            List<iLOCertificateInfo> unprocessedCerts = retrieveCert(certaddress.ODataId);
            foreach (iLOCertificateInfo unprocessedCert in unprocessedCerts)
            {
                CurrentInventoryItem processedCert = new();
                List<string> Cert = new();
                Cert.Add(unprocessedCert.CertificateString);
                processedCert.Certificates = Cert;
                processedCert.Alias = alias;
                processedCerts.Add(processedCert);
            }

            return processedCerts;
        }

        // Retrieve cert from endpoint, works for all certs except TLS
        private List<iLOCertificateInfo> retrieveCert(string certlink)
        {
            List<iLOCertificateInfo> returnedCerts = new();
            if (!string.IsNullOrEmpty(certlink))
            {
                // Fetch the collection of certificates (which may contain multiple entries)
                CertificateCollection collection = GetSync<CertificateCollection>(certlink);
                if (collection.Members != null)
                {
                    foreach (ODataIdRef certRef in collection.Members)
                    {
                        // Fetch each certificate in the collection
                        if (!string.IsNullOrEmpty(certRef.ODataId))
                        {
                            iLOCertificateInfo cert = GetSync<iLOCertificateInfo>(certRef.ODataId);
                            returnedCerts.Add(cert);
                        }
                    }

                    return returnedCerts;
                }
            }

            return returnedCerts;
        }

        public bool ImportCertificate(string certificate, string endpoint, CSRType certType)
        {
            // Build the payload with the certificate string.
            string jsonPayload = "";

            if (certType == CSRType.HTTPSCert)
            {
                var payload = new { Certificate = certificate };
                jsonPayload = JsonConvert.SerializeObject(payload);
            }
            else if (certType == CSRType.iLOLDevID)
            {
                ImportCertificateiLOLDevID payload = new();
                payload.CertificateString = certificate;
                payload.CertificateType = "PEM";
                jsonPayload = JsonConvert.SerializeObject(payload);
            }
            else
            {
                throw new Exception("Invalid CSR type");
            }

            using (HttpClient client = CreateClient())
            {
                // Create a POST request.
                HttpRequestMessage request = new(new HttpMethod("POST"), endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                // Send the request synchronously.
                HttpResponseMessage response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
        }

        // Create an HttpClient that optionally bypasses certificate errors.
        private HttpClient CreateClient()
        {
            HttpClient client;
            if (!_ignorevalidation)
            {
                client = _httpClientFactory.CreateClient();
            }
            else
            {
                // Create a custom handler that bypasses certificate errors.
                HttpClientHandler handler = new();
                handler.ServerCertificateCustomValidationCallback =
                    (httpRequestMessage, certificate, chain, sslPolicyErrors) =>
                    {
                        // Bypass specific errors such as name mismatch and chain errors.
                        if (sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch) ||
                            sslPolicyErrors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors) ||
                            sslPolicyErrors != SslPolicyErrors.None)
                        {
                            return true;
                        }

                        return true;
                    };

                client = new HttpClient(handler);
            }

            client.BaseAddress = new Uri(_baseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            string authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);

            return client;
        }

        // Synchronously performs a GET request and deserializes the JSON response to type T.
        private T GetSync<T>(string relativeUrl)
        {
            using (HttpClient client = CreateClient())
            {
                HttpResponseMessage response = client.GetAsync(relativeUrl).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                return JsonConvert.DeserializeObject<T>(json);
            }
        }

        //  Connects to the specified host and port to retrieve its TLS certificate.
        //  Retries the operation up to MaxRetries in case of failure.
        //  Returns the certificate as an X509Certificate2.
        public static X509Certificate2 GetCertificate(string host_entry, int port = 443)
        {
            string host = StripScheme(host_entry);
            using (TcpClient client = new(host, port))
            {
                using (SslStream sslStream = new(
                           client.GetStream(),
                           false,
                           ValidateServerCertificate,
                           null))
                {
                    // Initiate the SSL handshake.
                    sslStream.AuthenticateAsClient(host);

                    // Extract the remote certificate.
                    X509Certificate remoteCertificate = sslStream.RemoteCertificate;
                    if (remoteCertificate == null)
                    {
                        throw new Exception("No certificate was provided by the remote server.");
                    }

                    return new X509Certificate2(remoteCertificate);
                }
            }
        }


        // Parses subject text.
        public static GenerateCSRHTTPSCert ParseSubjectText(string distinguishedName)
        {
            // Create an instance with default values.
            GenerateCSRHTTPSCert requestBody = new()
            {
                IncludeIP = false // default value if not provided
            };

            if (string.IsNullOrWhiteSpace(distinguishedName))
            {
                return requestBody;
            }

            // Split by commas (DN parts are separated by commas)
            string[] parts = distinguishedName.Split(',');

            foreach (string part in parts)
            {
                // Trim whitespace and split key/value by '='.
                string trimmedPart = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmedPart))
                {
                    continue;
                }

                // Use a split limit of 2 in case the value contains '='.
                string[] keyValue = trimmedPart.Split(new[] { '=' }, 2);
                if (keyValue.Length != 2)
                {
                    continue;
                }

                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim();

                // Map DN attributes to our request body properties.
                switch (key.ToUpperInvariant())
                {
                    case "C":
                        requestBody.Country = value;
                        break;
                    case "ST":
                        requestBody.State = value;
                        break;
                    case "L":
                        requestBody.City = value;
                        break;
                    case "O":
                        requestBody.OrgName = value;
                        break;
                    case "OU":
                        requestBody.OrgUnit = value;
                        break;
                    case "CN":
                        requestBody.CommonName = value;
                        break;
                    case "INCLUDEIP":
                    case "IP": // Optionally, allow "IP" as an alias.
                        if (bool.TryParse(value, out bool includeIp))
                        {
                            requestBody.IncludeIP = includeIp;
                        }

                        break;
                }
            }

            return requestBody;
        }


        // A simple certificate validation callback that accepts all certificates.
        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        // Strip ip
        public static string StripScheme(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                return uri.Host;
            }

            // Fallback for non-standard inputs: manually remove "https://" if present.
            const string httpsPrefix = "https://";
            if (url.StartsWith(httpsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(httpsPrefix.Length);
            }

            return url;
        }
    }
}