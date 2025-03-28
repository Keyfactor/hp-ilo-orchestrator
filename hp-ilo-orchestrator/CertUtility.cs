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

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;

namespace Keyfactor.Extensions.Orchestrator.HPiLO
{
    // This class contains methods for working with certificate objects

    public class CertUtility
    {
        public static string GenerateCSR(string subjectText, List<string> sans, int keySize = 4096)
        {
            //Code logic to:
            //  1) Generate a new CSR
            //  2) Include the provided subject text
            //  3) Include the list of SANs
            //  3) Include the OID corresponding to a Time Stamping request, so Command recognizes this as a request for re-enrollment
            //  4) Return the base64 encoded CSR.

            // this approach relies on the Bouncy Castle Crypto libraries

            KeyGenerationParameters keyGenParams = new(new SecureRandom(new CryptoApiRandomGenerator()), keySize);

            RsaKeyPairGenerator keyPairGenerator = new();

            keyPairGenerator.Init(keyGenParams);

            AsymmetricCipherKeyPair keyPair = keyPairGenerator.GenerateKeyPair();
            X509Name subject = new(subjectText);

            // Add SAN entries
            List<GeneralName> subAltNameList = new();
            sans.ForEach(san => subAltNameList.Add(new GeneralName(GeneralName.DnsName, san.Trim())));
            GeneralNames generalSubAltNames = new(subAltNameList.ToArray());

            // Create Key Usage attribute
            int keyUsage = KeyUsage.DigitalSignature | KeyUsage.NonRepudiation;
            KeyUsage keyUsageExtension = new(keyUsage);

            // Create extensions
            X509ExtensionsGenerator extensionsGenerator = new();
            extensionsGenerator.AddExtension(X509Extensions.SubjectAlternativeName, false, generalSubAltNames);
            extensionsGenerator.AddExtension(X509Extensions.KeyUsage, true, keyUsageExtension);
            X509Extensions extensions = extensionsGenerator.Generate();

            // Create attribute set with extensions
            AttributePkcs attributeSet = new(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(extensions));

            // Include the attributes in the request
            Pkcs10CertificationRequest csr = new(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest.Id, subject,
                keyPair.Public, new DerSet(attributeSet), keyPair.Private);

            // encode the CSR as base64
            string encodedCsr = Convert.ToBase64String(csr.GetEncoded());

            return encodedCsr;
        }
    }
}