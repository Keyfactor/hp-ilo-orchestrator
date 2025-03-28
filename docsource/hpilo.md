## Overview
This orchestrator extension supports the following operations:

### Inventory:
- The HTTPS Cert used for connection to this instance of HPiLO\
Note: 
At present, only the HTTPS certificate used for connection to the HPiLO system/API can be inventoried, due to the limitations of the HP iLO API.
- iLOLDevID (Certificate used for 802.1x authentication)


This extension also supports inventory of the following factory-installed certificates (with the InventoryAll custom field set to True in Certificate Store Type):
- Platform Cert
- SystemIAK
- SystemIDevID
- iLOIDevID/BMCIDevIDPCA

### Management (delete):
- HTTPS Cert
- iLOLDevID (802.1x Cert)

### Reenrollment:
- HTTPS Cert
- iLOLDevID (802.1x Cert)\
Note:
Reenrollment is only supported for certificates hosted on internal manager 1 (a scenario typical for an HPiLO deployment). Please see [HP iLO API Reference](https://servermanagementportal.ext.hpe.com/docs/redfishservices/ilos/ilo6/ilo6_158/ilo6_manager_resourcedefns158/#manager) for reference on managers. \
Due to the way the HPiLO API is set up, to perform reenrollment, the CN field must be set to the FQDN of the HPiLO instance. The FQDN typically follows a pattern of "ILOXXXXXXXXXX". If reenrolling the HTTPS Certificate, the CN must be set to include the full FQDN string, including the "ILO" characters, as "ILOXXXXXXXXXX". For reenrollment of the iLOLDevID certificate, it should be just the remaining characters of the FQDN string, without the "ILO", as "XXXXXXXXXX". 

## Certificate Store Type Custom Fields
- InventoryAll\
Allows for inventory of factory-installed certificates as listed above.

- IgnoreValidation\
WARNING: Only enable if testing. Used to disable certificate validation checks at the API endpoint. 

- HTTPS Cert Wait Time\
The HPiLO API requires the user to wait while the HTTPS Cert CSR is generated. HP suggests a time of 60 seconds, as is the default setting, but it can be adjusted.