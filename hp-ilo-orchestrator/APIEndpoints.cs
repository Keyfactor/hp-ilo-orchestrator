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

namespace Keyfactor.Extensions.Orchestrator.HPiLO
{
    /// <summary>
    ///     Contains all API endpoints used in the HP iLO orchestrator solution.
    /// </summary>
    public static class APIEndpoints
    {
        // Certificate-related endpoints

        /// <summary>
        ///     Used in Reenrollment.cs (HTTPSCert CSR generation), HPiLOClient.cs (GenerateCSR)
        /// </summary>
        public const string GenerateCSRHTTPSCert =
            "/redfish/v1/Managers/1/SecurityService/HttpsCert/Actions/HpeHttpsCert.GenerateCSR";

        /// <summary>
        ///     Used in Reenrollment.cs (iLOLDevID CSR generation), HPiLOClient.cs (GenerateCSR)
        /// </summary>
        public const string GenerateCSRiLOLDevID =
            "/redfish/v1/CertificateService/Actions/CertificateService.GenerateCSR";

        /// <summary>
        ///     Used in Reenrollment.cs (HTTPSCert import), HPiLOClient.cs (ImportCertificate)
        /// </summary>
        public const string ImportCertificateHTTPSCert =
            "/redfish/v1/Managers/1/SecurityService/HttpsCert/Actions/HpeHttpsCert.ImportCertificate";

        /// <summary>
        ///     Used in Reenrollment.cs (iLOLDevID import), HPiLOClient.cs (ImportCertificate, GenerateCSR)
        /// </summary>
        public const string ImportCertificateiLOLDevID =
            "/redfish/v1/Managers/1/SecurityService/iLOLDevID/Certificates/";

        /// <summary>
        ///     Used in HPiLOClient.cs (DeleteCertificate)
        /// </summary>
        public const string DeleteCertificateiLOLDevID =
            "/redfish/v1/Managers/1/SecurityService/iLOLDevID/Certificates/1";

        /// <summary>
        ///     Used in HPiLOClient.cs (DeleteCertificate)
        /// </summary>
        public const string DeleteCertificateHTTPSCert = "/redfish/v1/Managers/1/SecurityService/HttpsCert/";

        // Manager-related endpoints

        /// <summary>
        ///     Used in HPiLOClient.cs (GetAllCertificates)
        /// </summary>
        public const string ManagersCollection = "/redfish/v1/Managers/";
    }
}