## Overview

This is an HPiLO orchestrator extension.

## Requirements

The account the Universal Orchestrator is running under needs to have read/write access to the .json file.

This orchestrator extension was written to work with HP iLO 6. 

## Installation

The compiled binaries can be deployed to the extensions folder at the location of the Universal Orchestrator installation. However, to get the most use out of this extension, it is recommended to use the Visual Studio project. You should either install Visual Studio on the machine you run the orchestrator or link the debugger remotely. In case you setup Visual Studio locally, you could use a symlink to link the Visual studio output directory to the extensions folder, specifically making a subfolder named "SOS". Once this is done and the code is compiled, you can attach the Visual Studio debugger to the Universal Orchestrator process for efficient debugging and variable inspection. The Sample Key Store certificate store type also needs to be added to Keyfactor. The exact settings are available in the install folder in this repository. This extension is configured to automatically log all incoming data it receives from the Universal Orchestrator. The log level needs to be set to at least Debug in the Universal Orchestrator settings for this information to appear in the logs. 

The overview of the Sample Orchestrator Store type is available here:
* [HP iLO Store Type](docs/hpilo.md)