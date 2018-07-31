Windows MSI installer build toolkit
================

This project creates a Salt Minion msi installer using [WiX][WiXId].

## Features ##

- Change installation directory __BLOCKED BY__ <a href=https://github.com/saltstack/salt/issues/38430>issue #38430</a>
- Uninstall leaves configuration, remove with `msiexec /x KEEP_CONFIG=0`
- Logging into %TEMP%\MSIxxxxx.LOG, options with `msiexec /l`
- Upgrades NSIS installations


Minion-specific msi-properties:

  Property              |  Default        | Comment                                                    
 ---------------------- | --------------- | -----------------------------------------------------------
 `INSTALLFOLDER`        | `c:\salt\`      | Where to install the Minion  __DO NOT CHANGE__             
 `MASTER_HOSTNAME`      | `salt`          | The master hostname                                         
 `MINION_HOSTNAME`      | `%COMPUTERNAME%`| The minion id                                                
 `START_MINION_SERVICE` | `0` (_false_)   | Whether to start the salt-minion service after installation
 `KEEP_CONFIG`          | `1` (_true_)    | keep configuratioin on uninstall. Only from command line


A kept configuration is reused on installation into its location.

### On unattended install ("silent install") ###

An msi allows you to install unattended ("silently"), meaning without opening any window, while still providing
customized values for e.g. master hostname, minion id, installation path, using the following command line:

> msiexec /i *.msi /qb! PROPERTY=VALUE PROPERTY=VALUE 

## Requirements ##
- .Net 2.0, or higher
 

## Build Requirement ##

- Windows 64bit
- Salt clone in `c:/git/salt/`
- This clone in `c:/git/salt-windows-msi/`
- Python 2.7 in `c:/python27/`
- [WiX][WiXId] v3.10
- [MSBuild 2015][MSBuild2015Id]
- .Net 4.5 SDK
- http://repo.saltstack.com//windows/dependencies/64/Microsoft_VC90_CRT_x86_x64.msm in `c:/saltrepo_local_cache/64/`

### Build procedure ###


```
cd c:\git\salt
git checkout v2016.11.3
    # Apply https://github.com/saltstack/salt/pull/39274  (msi numbering scheme in version.py)
    # git status must only show `modified: salt/version.py`
cd c:\git\salt\pkg\windows
build.bat
cd c:\git\salt-windows-msi
yclean.cmd
ybuild.cmd
    # Expect 84 ICE03 warnings

```

### <a id="msbuild"></a>MSBuild ###

General command line:

> msbuild msbuild.proj \[/t:target[,target2,..]] \[/p:property=value [ .. /p:... ] ]

A 'help' target is available which prints out all the targets, customizable
properties, and the current value of those properties:

> msbuild msbuild.proj /t:help


### Directory structure ###

- msbuild.d/: build the installer:
  - BuildDistFragment.targets: find files (from the extracted distribution?).
  - DownloadVCRedist.targets: (ORPHANED) download Visual C++ redistributable.
  - Minion.Common.targets: set version and platform parameters.
- wix.d/: installer sources:
  - MinionConfigurationExtension/: C# for custom actions:
    - MinionConfiguration.cs
  - MinionEXE/: (ORPHANED) create a bundle.
  - MinionMSI/: create a msi:
    - dist-$(TargetPlatform).wxs: found files (from the distribution zip file?).
    - MinionConfigurationExtensionCA.wxs: custom actions boilerplate.
    - MinionMSI.wixproj: msbuild boilerplate.
    - Product.wxs: main file.
    - service.wxs: Windows Service (using ssm.exe, the Salt Service Manager).
    - SettingsCustomizationDlg.wxs: Dialog for the master/minion properties.
    - WixUI_Minion.wxs: UI description.
- msbuild.proj: main msbuild file.
- wix.sln: Visual Studio solution file, needed to build the installer.




### Extending ###

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

## On versioning ##
[Microsoft defines software versions][MSDN_ProductVersion] as `major.minor.build` with maximum values of 255.255.65535.

This definition lacks the `micro` field used in Salt 2016.11.3 (or Python 3.6.2) and one has to make a decision for the micro field:

 1) Deviate from the Microsoft definition and use the third field as micro
 2) Stick with the Microsoft definition and discard the micro field.
 
I see no value in sticking with the Microsoft definition and chose 1.

Microsoft major version must be smaller than 256, therefore I use the short year (16 instead of 2016).

In WiX terms: when `DisplayVersion` is [2016.11.3][SALT_versions], `InternalVersion` becomes 16.11.3.

`InternalVersion` is displayed in the Version column of "Programs and Features" (formally "Add remove program") (appwiz.cpl)



[WiXId]: http://wixtoolset.org "WiX Homepage"
[MSBuildId]: http://msdn.microsoft.com/en-us/library/0k6kkbsd(v=vs.120).aspx "MSBuild Reference"
[MSBuild2015Id]: https://www.microsoft.com/en-US/download/details.aspx?id=48159
[SALT_versions]:https://docs.saltstack.com/en/develop/topics/releases/version_numbers.html
[version_py]: https://github.com/saltstack/salt/blob/develop/salt/version.py
[WindowsInstaller4.5_link]:https://www.microsoft.com/en-us/download/details.aspx?id=8483
[issue18]:https://github.com/markuskramerIgitt/salt-windows-msi/issues/18
[MSDN_ProductVersion]:https://msdn.microsoft.com/en-us/library/windows/desktop/aa370859

