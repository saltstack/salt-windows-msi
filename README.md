Windows MSI installer build toolkit
================

This project creates a Salt Minion msi installer using [WiX][WiXId].

##Requirements##
- [Windows Installer][WindowsInstaller] 4.0
-  VC++ 2008 Redistributable, included since Windows Server 2008 SP2/Windows Vista.
  - (intentionally excluded due to its old age, see [Issue #18][issue18])
 
##Features##
- Allows installation to any directory. __TODO__
- Supports unattended ('silent') installation.
- Uninstall leaves configuration. Optionally removes configuration with `msiexec KEEP_CONFIG=0`.
- Upgrades an existing NSIS-installed Minion.
- Logging into %TEMP%\MSI?????.LOG
- A problem during the install causes the installation to be rolled back.
- Logging options (msiexec /l).


Available msiexec command line properties:
- `INSTALLFOLDER`: Where to install the files. Default `c:\salt\`. __DO NOT CHANGE__
- `MASTER_HOSTNAME`: The master hostname. Default `salt`.
- `MINION_HOSTNAME`: The minion id. Default `%COMPUTERNAME%`.
- `START_MINION_SERVICE`: Whether to start the salt-minion service after installation. Default `0` (false).
- `KEEP_CONFIG`: keep configuratioin on uninstall. Default `1` (true). Only from command line.

A kept configuration is reused on installation into its location.

###On unattended install ("silent install")###

An msi allows you to install unattended ("silently"), meaning without opening any window, while still providing
customized values for e.g. master hostname, minion id, installation path, using the following command line:

> msiexec /i *.msi /qb! PROPERTY=VALUE PROPERTY=VALUE 


##Build Requirement##

- Python 2.7 in `c:\python27`
- This project git clone in `c:\git\salt-windows-msi`
- Salt git clone in `c:\git\salt`
- The NSIS build in `c:\git\salt\pkg\windows`
- [WiX][WiXId] v3.10.
- [MSBuild 2015][MSBuild2015Id]
- .Net 4.5
- (Probably not required: Visual Studio 2013 or 2015)


###Building###

yclean.cmd and ybuild.cmd are shortcuts for msbuild.
You CANNOT build the msi in Visual Studio.

The build will produce:
 - $(StagingDir)/wix/Salt-Minion-$(DisplayVersion)-$(TargetPlatform).msi

###<a id="msbuild"></a>MSBuild###

General command line:

> msbuild msbuild.proj \[/t:target[,target2,..]] \[/p:property=value [ .. /p:... ] ]

A 'help' target is available which prints out all the targets, customizable
properties, and the current value of those properties:

> msbuild msbuild.proj /t:help


###Directory structure###

- msbuild.d/: MSBuild files:
  - BuildDistFragment.targets: find files, generate a WiX fragment from the extracted distribution.
  - DownloadVCRedist.targets: download the appropriate Visual C++ redistributable for the WiX Bundle build.
  - Minion.Common.targets: discover the correct distribution zip file, extract it and determine version.
- wix.d/: WiX files:
  - MinionConfigurationExtension/: A WiX Extension implementing custom actions for configuration manipulation.
  - MinionEXE/: (ORPHANED) This was the WiX bundle .exe project.
  - MinionMSI/: The WiX .msi project
    - dist-$(TargetPlatform).wxs: auto-generated list out of the distribution zip file.
    - MinionConfigurationExtensionCA.wxs: WiX code setting up the configuration manipulator custom actions.
    - MinionMSI.wixproj: the main project file.
    - Product.wxs: the main MSI description and event sequence.
    - service.wxs: Windows Service description/control settings (using nssm.exe).
    - SettingsCustomizationDlg.wxs: Dialog for the master/minion id properties.
    - WixUI_Minion.wxs: UI description.
- wix.sln: Visual Studio solution file. 
  - msbuild looks up the location of the wix.d directory, somehow using a GUID. 
  - ...Difficult to understand.



###Extending###

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

##On versioning##
The user sees a [3-tuple][version_html] version, e.g. `2016.11.3`.

msi rules demand that numbers must be smaller than 256, therefore only the "short year" is used.
e.g. `16.11.3.77`

The msi properties `DisplayVersion` and `InternalVersion` store these values.

[Internally][version_py], version is a 8-tuple.


##On directory structure##
Files under INSTALLDIR are intended to be immutable in Windows.
Mutable data, created and changed after installation, as log files or a private key, should not be stored under INSTALLDIR.
Doing so makes install/uninstall complex.


## Request for comment ##
Currently, the minion id is in the config file.
Proposal: name the private/public keys directly as the name of the file:
```
salt/conf/jim.pem
salt/conf/master/joe.pub
salt/conf/master/jane.pub
```

Allow master private key change:
```
salt/conf/master/joe.pub
salt/conf/master/joe(2018-04-12--14-30).pub
```

##Understanding imports##
msbuild.proj imports msbuild.d\Minion.Common.targets


[WiXId]: http://wixtoolset.org "WiX Homepage"
[MSBuildId]: http://msdn.microsoft.com/en-us/library/0k6kkbsd(v=vs.120).aspx "MSBuild Reference"
[MSBuild2015Id]: https://www.microsoft.com/en-in/download/details.aspx?id=48159
[version_html]:https://docs.saltstack.com/en/develop/topics/releases/version_numbers.html
[version_py]: https://github.com/saltstack/salt/blob/develop/salt/version.py
[WindowsInstaller]:https://en.wikipedia.org/wiki/Windows_Installer#Versions
[issue18]:https://github.com/markuskramerIgitt/salt-windows-msi/issues/18

