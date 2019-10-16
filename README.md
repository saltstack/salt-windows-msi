# Windows MSI installer build toolkit

This project creates a Salt Minion msi installer using [WiX][WiX_link].

The focus is on 64bit, unattended install.

## Introduction

[Introduction on Windows installers](http://unattended.sourceforge.net/installers.php)

An msi installer allows unattended/silent installations, meaning without opening any window, while still providing
customized values for e.g. master hostname, minion id, installation path, using the following generic command:

> msiexec /i *.msi PROPERTY1=VALUE1 PROPERTY2=VALUE2 PROPERTY3="VALUE3a and 3b"

Values must be quoted when they contains whitespace, or to unset a property, as in `PROPERTY=""`

Example: Set the master:

> msiexec /i *.msi MASTER=salt2

Example: set the master and its key:

> msiexec /i *.msi MASTER=salt2 MASTER_KEY=MIIBIjA...2QIDAQAB

Example: uninstall and remove configuration

> MsiExec.exe /X *.msi KEEP_CONFIG=""

## Features

- Creates a very verbose log file, by default named %TEMP%\MSIxxxxx.LOG, where xxxxx are 5 random lowercase letters and numbers. The name of the log can be specified with `msiexec /log example.log`
- Upgrades NSIS installations
- Change installation directory __BLOCKED BY__ [issue#38430](https://github.com/saltstack/salt/issues/38430)

Minion-specific msi-properties:

  Property              |  Default                | Comment
 ---------------------- | ----------------------- | ------
 `MASTER`               | `salt`                  | The master (name or IP). Only a single master. 
 `MASTER_KEY`           |                         | The master public key. See below.
 `ZMQ_filtering`        |                         | Set to `1` if the master requires zmq_filtering.
 `MINION_ID`            | Hostname                | The minion id.
 `MINION_ID_CACHING`    | `1`                     | Set to `""` if the minion id shall be determined at each salt-minion service start.
 `MINION_ID_FUNCTION`   |                         | Set minion id by module function. See below
 `MINION_CONFIGFILE`    | `C:\salt\conf\minion`   | Name of a custom config file in the same path as the installer or the full path.
 `MINION_CONFIG`        |                         | Written to the `minion` config file, lines are separated by comma. See below.
 `START_MINION`         | `1`                     | Set to `""` to prevent the start of the salt-minion service.
 `KEEP_CONFIG`          | `1`                     | Set to `""` to remove configuration on uninstall.
 `CONFIG_TYPE`          | `Existing`              | Or `Custom` or `Default` or `New`. See below.
 `INSTALLFOLDER`        | `C:\salt\`              | Where to install the Minion  __DO NOT CHANGE (yet)__

These files and directories are regarded as config and kept:

- C:\salt\conf\minion
- C:\salt\conf\minion.d\
- c:\salt\var\cache\salt\minion\extmods\
- c:\salt\var\cache\salt\minion\files\

Master and id are read from 
 - file `C:\salt\conf\minion`
 - files `C:\salt\conf\minion.d\*.conf`

You can set a new master with `MASTER`. This will overrule the master in a kept configuration.

You can set a new master public key with `MASTER_KEY`, but you must convert it into one line:

- Remove the first and the last line (`-----BEGIN PUBLIC KEY-----` and `-----END PUBLIC KEY-----`).
- Remove linebreaks.
- From the default public key file (458 bytes), the one-line key has 394 characters.

### `MINION_CONFIG`

If `MINION_CONFIG` is set, the installer creates the file `c:\salt\conf\minion` with the content. To include whitespace, use double quotes around. For line breaks, use "^".

Example `MINION_CONFIG="a: A^b: B"` results in:

    a: A
    b: B

### `MINION_ID_FUNCTION`

The minion ID can be set by a user defined module function ([Further reading](https://github.com/saltstack/salt/pull/41619)).

If `MINION_ID_FUNCTION` is set, the installer creates module file `c:\salt\var\cache\salt\minion\extmods\modules\id_function.py` with the content

    import socket
    def id_function():
      return MINION_ID_FUNCTION

Example `MINION_ID_FUNCTION=socket.gethostname()` results in:

    import socket
    def id_function():
      return socket.gethostname()

Remember to create the same file as `/sr/salt/_modules/id_function.py` on your server, so that `saltutil.sync_all` will keep the file on the minion.


### `CONFIG_TYPE` 

There are 4 scenarios the installer tries to account for:

1. existing-config (default)
2. custom-config
3. default-config
4. new-config

Existing

This setting makes no changes to the existing config and just upgrades/downgrades salt. 
Makes for easy upgrades. Just run the installer with a silent option. 
If there is no existing config, then the default is used and `master` and `minion id` are applied if passed.

Custom

This setting will lay down a custom config passed via the command line. 
Since we want to make sure the custom config is applied correctly, we'll need to back up any existing config.
1. `minion` config renamed to `minion-<timestamp>.bak`
2. `minion_id` file renamed to `minion_id-<timestamp>.bak`
3. `minion.d` directory renamed to `minion.d-<timestamp>.bak`
Then the custom config is laid down by the installer... and `master` and `minion id` should be applied to the custom config if passed.

Default

This setting will reset config to be the default config contained in the pkg. 
Therefore, all existing config files should be backed up
1. `minion` config renamed to `minion-<timestamp>.bak`
2. `minion_id` file renamed to `minion_id-<timestamp>.bak`
3. `minion.d` directory renamed to `minion.d-<timestamp>.bak`
Then the default config file is laid down by the installer... settings for `master` and `minion id` should be applied to the default config if passed

New

Each Salt property (MASTER, ZMQ_FILTERING or MINON_ID) given is changed in all present config files or, if missing, added as a new file to the minion.d directory.

## Target client requirements

The target client is where the installer is deployed.

- 64bit
- Windows 7 (workstation), Server 2012 (domain controller), or higher.

## Development requirements

To contibrute to the development of the MSI itself, although not required, Visual Studio can be quite helpful.
If you wish to develop within an IDE, Visual Studio 2015 is recommended. Otherwise, you will need to edit the
main .sln file. This file describes and *moderately* forces you to use VS2015. This is described below within
the salt-windows-msi.sln file:
```
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 14
VisualStudioVersion = 14.0.25420.1
MinimumVisualStudioVersion = 10.0.40219.1
```  
The VisualStudioVersion: 14.0.25420.1 translates to VS2015 with Service Pack 3. This is available for download
for free from https://visualstudio.microsoft.com/vs/older-downloads/ so long as you have a develop account. This
is free for developers and does not expire. What this enables you to do is build the MSI using the build in Wix
commands, all from within the IDE. Note that this requires:
 - [This](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2015Extension)
 extention 


## Build client requirements

The build client is where the msi installer is built.

- 64bit Windows 10
- Salt clone in `c:\git\salt\`
- This clone in `c:\git\salt-windows-msi\`
- .Net 3.5 SDK (for WiX)<sup>*</sup>
- Microsoft_VC90_CRT_x86_x64.msm from Visual Studio 2008 SP2 in `c:\salt_msi_resources\`<sup>**</sup>
- Microsoft_VC140_CRT_x64.msm from Visual Studio 2015 in `c:\salt_msi_resources\`<sup>**</sup>
- Microsoft_VC140_CRT_x86.msm from Visual Studio 2015 in `c:\salt_msi_resources\`<sup>**</sup>
- [Wix 3.11](http://wixtoolset.org/releases/)<sup>**</sup>
- [Build tools 2015](https://www.microsoft.com/en-US/download/confirmation.aspx?id=48159)<sup>**</sup>

<sup>*</sup> `build_env.cmd` will open `optionalfeatures` if necessary.

<sup>**</sup> `build_env.cmd` will download and install if necessary.

Optionally: [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=WixToolset.WiXToolset)

### Step 1: build the exe installer

[Building and Developing on Windows](https://docs.saltstack.com/en/latest/topics/installation/windows.html#building-and-developing-on-windows)

Execute

    cd c:\git\salt\pkg\windows
    git checkout v2018.3.4
    git checkout .
    git clean -fd

until `git status` returns

    HEAD detached at v2018.3.4
    nothing to commit, working tree clean

then execute 

    clean_env.bat
    build.bat

### Step 2: build the msi installer

Execute

    cd c:\git\salt-windows-msi
    build_env.cmd
    build.cmd

You should see screen output containing:

    Build succeeded
      warning CNDL1150
      warning CNDL1150
        2 Warning(s)
        0 Error(s)

To test, you may use one of `test*.cmd`.

To read the most recent msi logfile, you may use `open_last_log_in_code.cmd`

## How to program the msi builder toolkit

The remainder is documentation how to program the msi build toolkit.

The C# code is needed to manipulate the configuration files. 

To achieve an atomic installation (either installed or the prior state is restored), all changes (filesystem and registry) must be manipulated by WiX code.

### Directory structure

- msbuild.proj: main MSbuild file.
- msbuild.d/: contains MSbuild resource files:
  - BuildDistFragment.targets: find files (from the extracted distribution?).
  - DownloadVCRedist.targets: (ORPHANED) download Visual C++ redistributable for bundle.
  - Minion.Common.targets: set version and platform parameters, set the file base-name of the msi.
- salt-windows-msi.sln: Visual Studio solution file, included in msbuild.proj.
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

### Naming conventions

Postfix  | Example                            | Meaning
-------- | ---------------------------------- | -------
`_IMCAC` | `ReadConfig_IMCAC`                 | Immediate custom action written in C#
`_IMCAX` |                                    | Immediate custom action written in XML
`_DECAC` | `Uninstall_excl_Config_DECAC`      | Deferred custom action written in C#
`_CADH`  | `Uninstall_excl_Config_CADH`       | Custom action data helper (only for deferred custom action)

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

### MSBuild

General command line:

> msbuild msbuild.proj \[/t:target[,target2,..]] \[/p:property=value [ .. /p:... ] ]

A 'help' target is available which prints out all the targets, customizable
properties, and the current value of those properties:

> msbuild msbuild.proj /t:help

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
