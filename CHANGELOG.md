# Changelog
## V1.1.2
### Fixes
- Improved docs.
- Fixed issue with credentials getting logged during reenrollment jobs. 
- Changed certificate store type - CustomAlias and PrivateKeyHandling now set to `Required`.

## V1.1.1
### Fixes
- Fixed issue with Management Job not functioning correctly on older versions of Command.

## v1.1
### ⚠️ Important Notice
**Cert Store Type has been changed from version 1.0. Please update existing stores to include the new entry parameters, enable Add job functionality and make sure the default values for custom fields and entry parameters align, then run an inventory job afterwards. See store type documentation for reference.**

### New Features
- Added support for HTTPSCert certificate "Add" job.
- Added detailed walkthrough of addition, deletion, and reenrollment/ODKG of certificates to the documentation.
- Added support to include IP in HTTPSCert CSR during reenrollment/ODKG.

### Cert Store Type Changes
- Management Add job is now supported and enabled.
- New entry parameters included: `IncludeIP`. This parameter is used during HTTPSCert reenrollment/ODKG to include the iLO IP in the CSR. It should be set to `False` for all other operations.

### Fixes
- Fixed an issue where the Platform Cert was not being inventoried correctly.
- Improved client logging and robustness.
- Fixed an issue with iLOLDevID deletion.
- Certificates now properly store their chain during inventory.
- Fixed an issue where the orchestrator would fail inventory after reenrollment/ODKG.

## v1.0
### Initial Release

- Initial extension release.

