# Windows MSI installer build toolkit

This project creates a Salt Minion msi installer using [WiX][WiX_link].

The focus is on 64bit, unattended install.

## Features

- Change installation directory __BLOCKED BY__ [issue#38430](https://github.com/saltstack/salt/issues/38430)
- Uninstall leaves configuration by default, optionally removes configuration with `msiexec /x KEEP_CONFIG=0`
- Creates a very verbose log file, by default named %TEMP%\MSIxxxxx.LOG, where xxxxx are 5 random lowercase letters and numbers. The name of the log can be specified with `msiexec /log example.log`
- Upgrades NSIS installations

Minion-specific msi-properties:

  Property              |  Default                | Comment
 ---------------------- | ----------------------- | ------
 `MASTER_HOSTNAME`      | `salt`                  | The master hostname
 `MASTER_KEY`           |                         | The master public key. See below.
 `MINION_HOSTNAME`      | `%COMPUTERNAME%`        | The minion id
 `START_MINION_SERVICE` | `0` (_false_)           | Whether to start the salt-minion service after installation
 `KEEP_CONFIG`          | `1` (_true_)            | keep configuration on uninstall. Only from command line
 `MINION_CONFIGFILE`    | `C:\salt\conf\minion`   | The minion config file       __DO NOT CHANGE (yet)__
 `INSTALLFOLDER`        | `c:\salt\`              | Where to install the Minion  __DO NOT CHANGE (yet)__

Kept configuration is reused on installation into its location.

Kept configuration is `C:\salt\conf\minion` and all `C:\salt\conf\minion.d\*.conf` (except `_schedule.conf`), in that order.

You can set a new master with `MASTER_HOSTNAME`. This will overrule the master in a kept configuration.

You can set a new master public key with `MASTER_KEY`, but you must convert it into one line:

- Remove the first and the last line (`-----BEGIN PUBLIC KEY-----` and `-----END PUBLIC KEY-----`).
- Remove linebreaks.
- From the default public key file (458 bytes), the one-line key has 394 characters.
- Example: `msiexec /i YOUR.msi MASTER_HOSTNAME=YOU MASTER_KEY=MIIBIjA...2QIDAQAB`

### On unattended install ("silent install")

An msi allows you to install unattended ("silently"), meaning without opening any window, while still providing
customized values for e.g. master hostname, minion id, installation path, using the following generic command:

> msiexec /i *.msi /qb! PROPERTY=VALUE PROPERTY=VALUE

Concreate example:

> msiexec /i Salt-Minion-2018.3.4-64bit.msi /qb! MASTER_HOSTNAME=salt2


## Target client requirements

The target client is where the installer is deployed.

- 64bit
- Windows 7 (workstation), Server 2012 (domain controller), or higher.

## Build client requirements

The build client is where the installer is created.

- 64bit Windows 10
- Salt clone in `c:\git\salt\`
- This clone in `c:\git\salt-windows-msi\`
- .Net 3.5 SDK (for WiX)<sup>*</sup>
- Microsoft_VC90_CRT_x86_x64.msm from Visual Studio 2008 SP2 in `c:\salt_msi_resources\`
- [Wix 3.11](http://wixtoolset.org/releases/)<sup>**</sup>
- [Build tools 2015](https://www.microsoft.com/en-US/download/confirmation.aspx?id=48159)<sup>**</sup>

<sup>*</sup> `build_env.cmd` will open `optionalfeatures` if necessary.

<sup>**</sup> downloaded and installed by `build_env.cmd` if necessary.

### Build the exe installer

[Building and Developing on Windows](https://docs.saltstack.com/en/latest/topics/installation/windows.html#building-and-developing-on-windows)

Prepare

    cd c:\git\salt\pkg\windows
    git checkout v2018.3.4
    git checkout .
    git clean -fd

until `git status` returns

    HEAD detached at v2018.3.4
    nothing to commit, working tree clean

then

    clean_env.bat
    build.bat

### Build the msi installer

Build the exe installer first.

    cd c:\git\salt-windows-msi
    build_env.cmd
    build.cmd

### MSBuild

General command line:

> msbuild msbuild.proj \[/t:target[,target2,..]] \[/p:property=value [ .. /p:... ] ]

A 'help' target is available which prints out all the targets, customizable
properties, and the current value of those properties:

> msbuild msbuild.proj /t:help

### Directory structure

- msbuild.d/: build the installer:
  - BuildDistFragment.targets: find files (from the extracted distribution?).
  - DownloadVCRedist.targets: (ORPHANED) download Visual C++ redistributable for bundle.
  - Minion.Common.targets: set version and platform parameters.
- wix.d/: installer sources:
  - MinionConfigurationExtension/: C# for custom actions:
    - MinionConfiguration.cs
  - MinionEXE/: (ORPHANED) create a bundle.
  - MinionMSI/: create a msi:
    - dist-$(TargetPlatform).wxs: found files (from the distribution zip file?).
    - MinionConfigurationExtensionCA.wxs: custom actions boilerplate.
    - MinionMSI.wixproj: msbuild boilerplate.
    - Product.wxs: main file, that e.g. includes the UI description.
    - service.wxs: salt-minion Windows Service using ssm.exe, the Salt Service Manager.
    - servicePython.wxs: (EXPERIMENTAL) salt-minion Windows Service
      - requires [saltminionservice](https://github.com/saltstack/salt/blob/167cdb344732a6b85e6421115dd21956b71ba25a/salt/utils/saltminionservice.py) or [winservice](https://github.com/saltstack/salt/blob/3fb24929c6ebc3bfbe2a06554367f8b7ea980f5e/salt/utils/winservice.py) [Removed](https://github.com/saltstack/salt/commit/8c01aacd9b4d6be2e8cf991e3309e2a378737ea0)
    - SettingsCustomizationDlg.wxs: Dialog for the master/minion properties.
    - WixUI_Minion.wxs: UI description, that includes the dialog.
- msbuild.proj: main msbuild file.
- wix.sln: Visual Studio solution file, needed to build the installer.

### Naming conventions

### For WiX

Prefix  | Example                 | Meaning
------- | ----------------------- | -------
`IMCA_` | `IMCA_NukeConf`         | Immediate custom action
`DECA_` | `DECA_SetMaster`        | Deferred custom action
`CADH_` | `CADH_SetMaster`        | Custom action data helper for DECA_
`COMP_` | `COMP_NukeBin`          | Component
`DIR_`  | `DIR_conf`              | Directory

### Extending

Additional configuration manipulations may be able to use the existing
MinionConfigurationExtension project. Current manipulations read the
value of a particular property (e.g. MASTER\_HOSTNAME) and apply to the
existing configuration file using a regular expression replace. Each new
manipulation will require changes to the following files:

- MinionConfiguration.cs: a new method (i.e. new custom action).
- MinionConfigurationExtensionCA.wxs: a &lt;CustomAction /&gt; entry to
  make the new method available.
- Product.wxs: a &lt;Custom /&gt; entry in the &lt;InstallSequence /&gt;
  to make the configuration change.
- Product.wxs: a &lt;Property /&gt; entry containing a default for the
  manipulated configuration setting.
- README.md: alter this file to explain the new configuration option and
  log the property name.

If the new custom action should be exposed to the UI, additional changes
are required:

- SettingsCustomizatonDlg.wxs: There is room to add 1-2 more properties to this dialog.
- WixUI_Minion.wxs: A &lt;ProgressText /&gt; entry providing a brief description of what the new action is doing.

If the new custom action requires its own dialog, these additional changes are required:

- The new dialog file.
- WixUI_Minion.wxs: &lt;Publish /&gt; entries hooking up the dialog buttons to other dialogs.
  Other dialogs will also have to be adjusted to maintain correct sequencing.
- MinionMSI.wixproj: The new dialog must be added as a &lt;Compile /&gt; item to be included in the build.

### Other Notes

[Wix-Setup-Samples](https://github.com/deepak-rathi/Wix-Setup-Samples)

[Which Python version uses which MS VC CRT version](https://wiki.python.org/moin/WindowsCompilers)

- Python 2.7 = VC CRT 9.0 = VS 2008  
- Python 3.6 = VC CRT 14.0 = VS 2017

Distutils contains bin/Lib/distutils/command/bdist_msi.py, which probably does not work.

[WiX_link]: http://wixtoolset.org
[MSBuild_link]: http://msdn.microsoft.com/en-us/library/0k6kkbsd(v=vs.120).aspx
[MSBuild2015_link]: https://www.microsoft.com/en-US/download/details.aspx?id=48159
[SALT_versions_link]:https://docs.saltstack.com/en/develop/topics/releases/version_numbers.html
[salt_versions_py_link]: https://github.com/saltstack/salt/blob/develop/salt/version.py
[WindowsInstaller4.5_link]:https://www.microsoft.com/en-us/download/details.aspx?id=8483
[MSDN_ProductVersion_link]:https://msdn.microsoft.com/en-us/library/windows/desktop/aa370859
