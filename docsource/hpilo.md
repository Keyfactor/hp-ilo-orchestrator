# HP iLO Certificate Operations

## Overview

This document details supported certificate operations for HP iLO.  

`IncludeIP` entry parameter should be set to false outside of HTTPSCert reenrollment/ODKG operation.

`CertType` entry parameter is required for all operations and should be set to the type of certificate the operation is carried out on.

Please review the description of each supported operation below to understand the requirements and limitations.

## Inventory
The following certificates can be inventoried:
- **HTTPS Certificate**
    
    The TLS certificate used for connections to this iLO instance.
	> **Note:** Only the HTTPS certificate used for iLO system/API connection via TLS can be inventoried, due to HP iLO API limitations.

- **iLOLDevID**

  Certificate used for 802.1X authentication. 

- **Additional Factory‑Installed Certificates**			  
    *(Require `InventoryAll = True` in Certificate Store Type)*
	- Platform Cert  
	- SystemIAK  
	- SystemIDevID  
	- iLOIDevID / BMCIDevIDPCA 

## Management Add (Addition to certificate store)
The following certificate supports addition to an HPiLO cert store:
- **HTTPS Certificate:**
    				
	Performing this operation will import a specified PFX certificate and store the certificate and its private key in HPiLO. Only PFX certificates can be added. After the operation is complete, HPiLO will reboot.
    HP iLO will take in a cert with an RSA 2084 based key, similarly to HTTPS Cert reenrollment/ODKG.
	- **Alias:** `HTTPSCert`
	- **Overwrite:** `Yes`
		- Overwrite needs to be enabled.
	- **CertType:** `HTTPSCert`
	- **IncludeIP:** `False`

## Management Remove (Removal from certificate store)
The following certificates can be deleted from an HPiLO cert store:
- **HTTPS Certificate**
	- **CertType:** `HTTPSCert`
	- **IncludeIP:** `false`
- **iLOLDevID (802.1X Certificate)** 
	- **CertType:** `iLOLDevID`
	- **IncludeIP:** `false`

## Reenrollment / ODKG
The following certificates can be undergo reenrollment/ODKG using an HPiLO cert store:
- **HTTPS Certificate**  
  Following the reenrollment of an HTTPS certificate, HP iLO will reboot. The CSR produced by HP iLO for this purpose will utilize a RSA 2084 key. Please ensure your CA supports this algorithm and has an appropriate template configured.
	- **Alias:** `HTTPSCert`  
	- **Subject String Format:**  
	  ```text
	  CN=test.demo.local, L=Example, ST=Example, C=Example, OU=Example, O=Example
	  ```  
	  - Supported attributes: `CN`, `O`, `OU`, `L`, `ST`, `C`.  
	  - The `CN` must match the iLO FQDN.
	  - If `IncludeIP` is set to true, iLO will automatically add its IPv4/IPv6 addresses as SANs. The CN is added as a SAN automatically.  
	  - **Note:** iLO will reboot after the certificate is installed. 
	- **CertType:** `HTTPSCert`  
- **iLOLDevID**  
  802.1X Certificate reenrollment. The CSR produced by HPiLO will utilize ECC 384 based key. Please setup an appropriate template and make sure your CA supports this algorithm. 
	- **Alias:** `1/iLOLDevID`
	- **CertType:** `iLOLDevID`  
	- **Subject String Format:**  
	The subject string contents depend on the version of HP iLO firmware you are using.
		##### Versions Below HPiLO 6 1.60 
		```text
		CN=CZJ3199GDZ, O=Hewlett Packard Enterprise, OU=Servers, L=Houston, ST=TX, C=US
		```
		- `CN` must match the iLO serial number, e.g. CZJ3199GDZ 
		- Other attributes must match exactly for Keyfactor to correlate the CSR and resulting certificate. 
		##### Versions HPiLO 6 1.60 and Above
		```text
		CN=P98765-B21, SURNAME=PYNXHC2ZGIE11U, GIVENNAME=P12345-001, SERIALNUMBER=CZJ3199GDZ, OU=Servers, O=Hewlett Packard Enterprise Development, L=Houston, ST=Texas, C=US
		```
		- For complete details on the contents of these attributes, please see the [HP iLO LDevID CSR documentation](https://servermanagementportal.ext.hpe.com/docs/redfishservices/ilos/supplementdocuments/securityservice/#ilo-ldevid-csr-format). There is a chart explaining the contents of these fields and their required values.

> **Manager Limitation:**  
> Reenrollment and deletion are only supported on internal manager 1 (common in typical HP iLO deployments).  
> See the [HP iLO API Reference](https://servermanagementportal.ext.hpe.com/docs/redfishservices/ilos/ilo6/ilo6_158/ilo6_manager_resourcedefns158/#manager) for details.