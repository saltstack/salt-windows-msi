## How to program the msi builder toolkit

This is documentation how to program the msi build toolkit.


## Development requirements

To contribute to the development of the MSI itself, although not required, Visual Studio can be quite helpful.
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
is free for developers and does not expire. What this enables you to do is build the MSI using the built-in Wix
commands, all from within the IDE. Note that this requires:
 - [This](https://marketplace.visualstudio.com/items?itemName=WixToolset.WixToolsetVisualStudio2015Extension)
 extention 


## Build client requirements

The build client is where the msi installer is built.

- 64bit Windows 10
- The Git repositories `salt` and `salt-windows-msi`
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

    cd c:\dev\salt\pkg\windows
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

    cd c:\dev\salt-windows-msi
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

### Directory structure

To achieve an atomic installation (either installed or the prior state is restored), all changes (filesystem and registry) must be manipulated by WiX code.
The C# code is needed to manipulate the configuration files. 

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
    - dist-$(TargetPlatform).wxs: files from NSIS.
    - MinionConfigurationExtensionCA.wxs: custom actions boilerplate.
    - MinionMSI.wixproj: msbuild boilerplate.
    - Product.wxs: main file.
    - ProductUI.wxs: UI flow description.
    - ProductUIsettings.wxs: Dialog for the master/minion properties.
    - service.wxs: salt-minion Windows Service using ssm.exe, the Salt Service Manager.
    - servicePython.wxs: (EXPERIMENTAL) salt-minion Windows Service
      - requires [saltminionservice](https://github.com/saltstack/salt/blob/167cdb344732a6b85e6421115dd21956b71ba25a/salt/utils/saltminionservice.py) or [winservice](https://github.com/saltstack/salt/blob/3fb24929c6ebc3bfbe2a06554367f8b7ea980f5e/salt/utils/winservice.py) [Removed](https://github.com/saltstack/salt/commit/8c01aacd9b4d6be2e8cf991e3309e2a378737ea0)

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
- ProductUI.wxs: A &lt;ProgressText /&gt; entry providing a brief description of what the new action is doing.

If the new custom action requires its own dialog, these additional changes are required:

- The new dialog file.
- ProductUI.wxs: &lt;Publish /&gt; entries hooking up the dialog buttons to other dialogs.
  Other dialogs will also have to be adjusted to maintain correct sequencing.
- MinionMSI.wixproj: The new dialog must be added as a &lt;Compile /&gt; item to be included in the build.

### MSBuild

General command line:

> msbuild msbuild.proj \[/t:target[,target2,..]] \[/p:property=value [ .. /p:... ] ]

A 'help' target is available which prints out all the targets, customizable
properties, and the current value of those properties:

> msbuild msbuild.proj /t:help

### Other Notes

The Windows installer restricts the maximum values of the [ProductVersion property](https://docs.microsoft.com/en-us/windows/win32/msi/productversion): 

- major.minor.build 
- `255.255.65535`

Because of this restriction "Salt 2018.3.4" has ProductVersion `18.3.4`.

[Wix-Setup-Samples](https://github.com/deepak-rathi/Wix-Setup-Samples)

[Which Python version uses which MS VC CRT version](https://wiki.python.org/moin/WindowsCompilers)

- Python 2.7 = VC CRT 9.0 = VS 2008  
- Python 3.6 = VC CRT 14.0 = VS 2017

Distutils contains bin/Lib/distutils/command/bdist_msi.py, which probably does not work.
