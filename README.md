<h1 align="center" style="border-bottom: none">
    HP iLO Universal Orchestrator Extension
</h1>

<p align="center">
  <!-- Badges -->
<img src="https://img.shields.io/badge/integration_status-pilot-3D1973?style=flat-square" alt="Integration Status: pilot" />
<a href="https://github.com/Keyfactor/hp-ilo-orchestrator/releases"><img src="https://img.shields.io/github/v/release/Keyfactor/hp-ilo-orchestrator?style=flat-square" alt="Release" /></a>
<img src="https://img.shields.io/github/issues/Keyfactor/hp-ilo-orchestrator?style=flat-square" alt="Issues" />
<img src="https://img.shields.io/github/downloads/Keyfactor/hp-ilo-orchestrator/total?style=flat-square&label=downloads&color=28B905" alt="GitHub Downloads (all assets, all releases)" />
</p>

<p align="center">
  <!-- TOC -->
  <a href="#support">
    <b>Support</b>
  </a>
  ·
  <a href="#installation">
    <b>Installation</b>
  </a>
  ·
  <a href="#license">
    <b>License</b>
  </a>
  ·
  <a href="https://github.com/orgs/Keyfactor/repositories?q=orchestrator">
    <b>Related Integrations</b>
  </a>
</p>

## Overview

This is an HPiLO orchestrator extension.



### HPiLO

This orchestrator extension supports the following operations:

#### Inventory:
- The HTTPS Cert used for connection to this instance of HPiLO\
Note: 
At present, only the HTTPS certificate used for connection to the HPiLO system/API can be inventoried, due to the limitations of the HP iLO API.
- iLOLDevID (Certificate used for 802.1x authentication)


This extension also supports inventory of the following factory-installed certificates (with the InventoryAll custom field set to True in Certificate Store Type):
- Platform Cert
- SystemIAK
- SystemIDevID
- iLOIDevID/BMCIDevIDPCA

#### Management (delete):
- HTTPS Cert
- iLOLDevID (802.1x Cert)

#### Reenrollment:
- HTTPS Cert
- iLOLDevID (802.1x Cert)\
Note:
Reenrollment is only supported for certificates hosted on internal manager 1 (a scenario typical for an HPiLO deployment). Please see [HP iLO API Reference](https://servermanagementportal.ext.hpe.com/docs/redfishservices/ilos/ilo6/ilo6_158/ilo6_manager_resourcedefns158/#manager) for reference on managers. \
Due to the way the HPiLO API is set up, to perform reenrollment, the CN field must be set to the FQDN of the HPiLO instance. The FQDN typically follows a pattern of "ILOXXXXXXXXXX". If reenrolling the HTTPS Certificate, the CN must be set to include the full FQDN string, including the "ILO" characters, as "ILOXXXXXXXXXX". For reenrollment of the iLOLDevID certificate, it should be just the remaining characters of the FQDN string, without the "ILO", as "XXXXXXXXXX".

## Compatibility

This integration is compatible with Keyfactor Universal Orchestrator version 10.1 and later.

## Support
The HP iLO Universal Orchestrator extension If you have a support issue, please open a support ticket by either contacting your Keyfactor representative or via the Keyfactor Support Portal at https://support.keyfactor.com. 
 
> To report a problem or suggest a new feature, use the **[Issues](../../issues)** tab. If you want to contribute actual bug fixes or proposed enhancements, use the **[Pull requests](../../pulls)** tab.

## Requirements & Prerequisites

Before installing the HP iLO Universal Orchestrator extension, we recommend that you install [kfutil](https://github.com/Keyfactor/kfutil). Kfutil is a command-line tool that simplifies the process of creating store types, installing extensions, and instantiating certificate stores in Keyfactor Command.


The account the Universal Orchestrator is running under needs to have read/write access to the .json file.

This orchestrator extension was written to work with HP iLO 6.


## Create the HPiLO Certificate Store Type

To use the HP iLO Universal Orchestrator extension, you **must** create the HPiLO Certificate Store Type. This only needs to happen _once_ per Keyfactor Command instance.



* **Create HPiLO using kfutil**:

    ```shell
    # HPiLO
    kfutil store-types create HPiLO
    ```

* **Create HPiLO manually in the Command UI**:
    <details><summary>Create HPiLO manually in the Command UI</summary>

    Create a store type called `HPiLO` with the attributes in the tables below:

    #### Basic Tab
    | Attribute | Value | Description |
    | --------- | ----- | ----- |
    | Name | HPiLO | Display name for the store type (may be customized) |
    | Short Name | HPiLO | Short display name for the store type |
    | Capability | HPiLO | Store type name orchestrator will register with. Check the box to allow entry of value |
    | Supports Add | ✅ Checked | Check the box. Indicates that the Store Type supports Management Add |
    | Supports Remove | ✅ Checked | Check the box. Indicates that the Store Type supports Management Remove |
    | Supports Discovery | ✅ Checked | Check the box. Indicates that the Store Type supports Discovery |
    | Supports Reenrollment | ✅ Checked |  Indicates that the Store Type supports Reenrollment |
    | Supports Create | ✅ Checked | Check the box. Indicates that the Store Type supports store creation |
    | Needs Server | ✅ Checked | Determines if a target server name is required when creating store |
    | Blueprint Allowed | ✅ Checked | Determines if store type may be included in an Orchestrator blueprint |
    | Uses PowerShell | 🔲 Unchecked | Determines if underlying implementation is PowerShell |
    | Requires Store Password | 🔲 Unchecked | Enables users to optionally specify a store password when defining a Certificate Store. |
    | Supports Entry Password | ✅ Checked | Determines if an individual entry within a store can have a password. |

    The Basic tab should look like this:

    ![HPiLO Basic Tab](docsource/images/HPiLO-basic-store-type-dialog.png)

    #### Advanced Tab
    | Attribute | Value | Description |
    | --------- | ----- | ----- |
    | Supports Custom Alias | Optional | Determines if an individual entry within a store can have a custom Alias. |
    | Private Key Handling | Optional | This determines if Keyfactor can send the private key associated with a certificate to the store. Required because IIS certificates without private keys would be invalid. |
    | PFX Password Style | Default | 'Default' - PFX password is randomly generated, 'Custom' - PFX password may be specified when the enrollment job is created (Requires the Allow Custom Password application setting to be enabled.) |

    The Advanced tab should look like this:

    ![HPiLO Advanced Tab](docsource/images/HPiLO-advanced-store-type-dialog.png)

    > For Keyfactor **Command versions 24.4 and later**, a Certificate Format dropdown is available with PFX and PEM options. Ensure that **PFX** is selected, as this determines the format of new and renewed certificates sent to the Orchestrator during a Management job. Currently, all Keyfactor-supported Orchestrator extensions support only PFX.

    #### Custom Fields Tab
    Custom fields operate at the certificate store level and are used to control how the orchestrator connects to the remote target server containing the certificate store to be managed. The following custom fields should be added to the store type:

    | Name | Display Name | Description | Type | Default Value/Options | Required |
    | ---- | ------------ | ---- | --------------------- | -------- | ----------- |
    | StoreNameString | Store Name | The Store name for the particular SOS store. | String |  | 🔲 Unchecked |
    | ForTestingOnlyBool | For Testing Only | Test bool variable. | Bool | true | 🔲 Unchecked |
    | CollectionNameMultipleChoice | Collection Name | A test collection. | MultipleChoice | internal | ✅ Checked |
    | PrivateDetailsSecret | Private Details | A test secret. | Secret | test | 🔲 Unchecked |

    The Custom Fields tab should look like this:

    ![HPiLO Custom Fields Tab](docsource/images/HPiLO-custom-fields-store-type-dialog.png)



    #### Entry Parameters Tab

    | Name | Display Name | Description | Type | Default Value | Entry has a private key | Adding an entry | Removing an entry | Reenrolling an entry |
    | ---- | ------------ | ---- | ------------- | ----------------------- | ---------------- | ----------------- | ------------------- | ----------- |
    | CommaSeparatedSansString | SANs | SAN string. | String |  | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |
    | CertColorMultipleChoice | Certificate Color | A test variable with multiple choice. | MultipleChoice | red | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |
    | ForTestingOnlyBool | For Testing Only | Another test boolean. | Bool | true | ✅ Checked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |
    | PrivateCertDetailsSecret | Private Cert Details | A per cert secret. | Secret | test | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked | 🔲 Unchecked |

    The Entry Parameters tab should look like this:

    ![HPiLO Entry Parameters Tab](docsource/images/HPiLO-entry-parameters-store-type-dialog.png)



    </details>

## Installation

1. **Download the latest HP iLO Universal Orchestrator extension from GitHub.** 

    Navigate to the [HP iLO Universal Orchestrator extension GitHub version page](https://github.com/Keyfactor/hp-ilo-orchestrator/releases/latest). Refer to the compatibility matrix below to determine whether the `net6.0` or `net8.0` asset should be downloaded. Then, click the corresponding asset to download the zip archive.
    | Universal Orchestrator Version | Latest .NET version installed on the Universal Orchestrator server | `rollForward` condition in `Orchestrator.runtimeconfig.json` | `hp-ilo-orchestrator` .NET version to download |
    | --------- | ----------- | ----------- | ----------- |
    | Older than `11.0.0` | | | `net6.0` |
    | Between `11.0.0` and `11.5.1` (inclusive) | `net6.0` | | `net6.0` | 
    | Between `11.0.0` and `11.5.1` (inclusive) | `net8.0` | `Disable` | `net6.0` | 
    | Between `11.0.0` and `11.5.1` (inclusive) | `net8.0` | `LatestMajor` | `net8.0` | 
    | `11.6` _and_ newer | `net8.0` | | `net8.0` |

    Unzip the archive containing extension assemblies to a known location.

    > **Note** If you don't see an asset with a corresponding .NET version, you should always assume that it was compiled for `net6.0`.

2. **Locate the Universal Orchestrator extensions directory.**

    * **Default on Windows** - `C:\Program Files\Keyfactor\Keyfactor Orchestrator\extensions`
    * **Default on Linux** - `/opt/keyfactor/orchestrator/extensions`
    
3. **Create a new directory for the HP iLO Universal Orchestrator extension inside the extensions directory.**
        
    Create a new directory called `hp-ilo-orchestrator`.
    > The directory name does not need to match any names used elsewhere; it just has to be unique within the extensions directory.

4. **Copy the contents of the downloaded and unzipped assemblies from __step 2__ to the `hp-ilo-orchestrator` directory.**

5. **Restart the Universal Orchestrator service.**

    Refer to [Starting/Restarting the Universal Orchestrator service](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/StarttheService.htm).



> The above installation steps can be supplimented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/InstallingAgents/NetCoreOrchestrator/CustomExtensions.htm?Highlight=extensions).



## Defining Certificate Stores



* **Manually with the Command UI**

    <details><summary>Create Certificate Stores manually in the UI</summary>

    1. **Navigate to the _Certificate Stores_ page in Keyfactor Command.**

        Log into Keyfactor Command, toggle the _Locations_ dropdown, and click _Certificate Stores_.

    2. **Add a Certificate Store.**

        Click the Add button to add a new Certificate Store. Use the table below to populate the **Attributes** in the **Add** form.
        | Attribute | Description |
        | --------- | ----------- |
        | Category | Select "HPiLO" or the customized certificate store name from the previous step. |
        | Container | Optional container to associate certificate store with. |
        | Client Machine | Runs on a Windows based machine. |
        | Store Path | Path points to a local .json file. Orchestrator and its account should have read/write access. |
        | Orchestrator | Select an approved orchestrator capable of managing `HPiLO` certificates. Specifically, one with the `HPiLO` capability. |
        | StoreNameString | The Store name for the particular SOS store. |
        | ForTestingOnlyBool | Test bool variable. |
        | CollectionNameMultipleChoice | A test collection. |
        | PrivateDetailsSecret | A test secret. |


        

    </details>

* **Using kfutil**
    
    <details><summary>Create Certificate Stores with kfutil</summary>
    
    1. **Generate a CSV template for the HPiLO certificate store**

        ```shell
        kfutil stores import generate-template --store-type-name HPiLO --outpath HPiLO.csv
        ```
    2. **Populate the generated CSV file**

        Open the CSV file, and reference the table below to populate parameters for each **Attribute**.
        | Attribute | Description |
        | --------- | ----------- |
        | Category | Select "HPiLO" or the customized certificate store name from the previous step. |
        | Container | Optional container to associate certificate store with. |
        | Client Machine | Runs on a Windows based machine. |
        | Store Path | Path points to a local .json file. Orchestrator and its account should have read/write access. |
        | Orchestrator | Select an approved orchestrator capable of managing `HPiLO` certificates. Specifically, one with the `HPiLO` capability. |
        | StoreNameString | The Store name for the particular SOS store. |
        | ForTestingOnlyBool | Test bool variable. |
        | CollectionNameMultipleChoice | A test collection. |
        | PrivateDetailsSecret | A test secret. |


        

    3. **Import the CSV file to create the certificate stores** 

        ```shell
        kfutil stores import csv --store-type-name HPiLO --file HPiLO.csv
        ```
    </details>

> The content in this section can be supplimented by the [official Command documentation](https://software.keyfactor.com/Core-OnPrem/Current/Content/ReferenceGuide/Certificate%20Stores.htm?Highlight=certificate%20store).


### Certificate Store Type Custom Fields
- InventoryAll\
Allows for inventory of factory-installed certificates as listed above.

- IgnoreValidation\
WARNING: Only enable if testing. Used to disable certificate validation checks at the API endpoint. 

- HTTPS Cert Wait Time\
The HPiLO API requires the user to wait while the HTTPS Cert CSR is generated. HP suggests a time of 60 seconds, as is the default setting, but it can be adjusted.



## Installation

The compiled binaries can be deployed to the extensions folder at the location of the Universal Orchestrator installation. However, to get the most use out of this extension, it is recommended to use the Visual Studio project. You should either install Visual Studio on the machine you run the orchestrator or link the debugger remotely. In case you setup Visual Studio locally, you could use a symlink to link the Visual studio output directory to the extensions folder, specifically making a subfolder named "SOS". Once this is done and the code is compiled, you can attach the Visual Studio debugger to the Universal Orchestrator process for efficient debugging and variable inspection. The Sample Key Store certificate store type also needs to be added to Keyfactor. The exact settings are available in the install folder in this repository. This extension is configured to automatically log all incoming data it receives from the Universal Orchestrator. The log level needs to be set to at least Debug in the Universal Orchestrator settings for this information to appear in the logs. 

The overview of the Sample Orchestrator Store type is available here:
* [HP iLO Store Type](docs/hpilo.md)


## License

Apache License 2.0, see [LICENSE](LICENSE).

## Related Integrations

See all [Keyfactor Universal Orchestrator extensions](https://github.com/orgs/Keyfactor/repositories?q=orchestrator).