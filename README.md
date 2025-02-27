## The Software Prototype of Digital Scapegoat: An Incentive Deception Model for Defending Unknown Stealing Attacks on Critical Data Resources

Welcome to the Digital Scapegoat Project repository. This repository contains the software prototype for the DS-IDep model, developed to defend critical data resources against unknown stealing attacks. This document will help you utilize the system effectively.

---

## Table of Contents
1. [Overview](#overview)
2. [Installation Guide](#installation-guide)
3. [Source Code Components](#source-code-components)

---

### Overview
The Digital Scapegoat project implements the DS-IDep (Digital Scapegoat Incentive Deception) model as a prototype system to protect critical data resources from advanced unknown attacks. The prototype is built and tested on the Windows platform, leveraging multiple tools and frameworks.

### Installation Guide

#### Prerequisites
The installation package includes all necessary dependencies:
- [WinFSP 2.0.23075](https://winfsp.dev/)
- [Cygwin 3.5.3](https://cygwin.com/)

#### Supported Operating Systems
The software is compatible with the following Windows operating systems:
- Windows 8.1
- Windows 10
- Windows 11

#### Installation Steps
1. Download the `DSIDepProgramChineseInstaller.exe` file or the `DSIDepProgramEnglishInstaller.exe` in the `Releases`.
2. Run the installer (default installation path is `C:\SZTSProgramInstaller\SZTSProgram`; custom installation paths are not supported).
3. During installation, allow all prompted permissions.
4. Once the installation completes, a new shortcut will appear on your desktop.
5. All program and data files will be stored in `C:\SZTSProgramInstaller`.

For detailed instructions, refer to the user manual `DigitalScapegoatSoftwareUserManual-ChineseVersion.pdf` or `DigitalScapegoatSoftwareUserManual-EnglishVersion.pdf`.

---

### Source Code Components
#### Source Code
The key source code for the Digital Scapegoat software prototype is located in the `DS-IDep-SourceCode` directory. 
- **DSIDep**: Use **Visual Studio 2022 Preview** to open the `.sln` file and compile the project.
- **Scripts**: Contains components for Windows terminal log collection.

#### Release Programs
The above source code depends on the release programs and configuration files located in the `DS-IDep-ReleaseProgram` directory. Specifically:
- **SZTSProgram Directory**: Provides essential executable programs, such as:
  - `unionFileSubstituteSystem.exe`: Controls DS-IDep FileSystem mounting and unmounting.
  - `SZTSLogCollectionProgram.exe`: Manages DS-IDep log collection and analysis.
  - `PopPwd.exe`: Displays password prompts for identity authentication.

- **SZTSConfig Directory**: Contains configuration files, such as:
  - `forms.json`: Configures software parameters.
  - `f_secure.json`: Configures identity authentication passwords.
