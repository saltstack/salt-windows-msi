Windows MSI installer build toolkit
================

The 'wix/' directory (together with wix.sln ?) produce an msi installer using [WiX][WiXId].

The build is semi-automated using an [MSBuild][MSBuildId].

 
##Differences vs. NSIS installer##

The msi differs from the NSIS installer in:

- It allows installation to any directory TODO.
- It supports unattended installation.
- It leaves configuaration. (remove configuration with KEEP_CONFIG=0)
- ?? It does not download or install the VC++ redistributable. ??
- ?? Since the msi does not install the Visual C++ redistributable, it must be installed separately ??

There are additional benefits:

- A problem during the .msi install will be rolled back automatically.
- An msi offers built-in logging (/l option to msiexec).
- *_amd64.msi only installs on 64bit Windows

###On unattended install ("silent install")###

An msi allows you to install unattended ("silently"), meaning without opening any Windows, while still providing
customized values for e.g. master hostname, minion id, installation path, using the following command line:

> msiexec /i Salt-Minion-$version-$platform.msi /qn [ PROPERTY=VALUE [ .. PROPERTY=VALUE ] ]


Available properties:

- MASTER\_HOSTNAME: The master hostname. The default is 'salt'.
- MINION\_HOSTNAME: The minion id. The default is '%COMPUTERNAME%'.
- START\_MINION\_SERVICE: Whether to start the salt-minion service after installation. The default is false.
- KEEP_CONFIG: keep c:\salt\conf. Default is 1 (true). Only from commandline
- INSTALLFOLDER: Where to install the files. Default is 'c:\salt'. DO NOT CHANGE

Note:
Because the user can set installdir, therefore the location of the configuration, 
The location of the configuration must be stored in the Registry on uninstall with KEEP_CONFIG=1.

##General Requirements##

- A NSIS build in `git\salt`, with this project in `git\salt-windows-msi`
- [WiX][WiXId] v3.9.
- [MSBuild 2013][MSBuild2913Id] and .Net 4.5

###Building###

You can build the msi from the command line using the included msbuild project.

> msbuild msbuild.proj [ /p:property=value [ /p:... ] ]

See the [msbuild](#msbuild) section for details on available
targets and properties.

You can build the msi also in Visual Studio, but the embedded defaults for
version, paths, etc. may be incorrect on your machie.

The build will produce:
 - $(StagingDir)/wix/Salt-Minion-$(DisplayVersion)-$(TargetPlatform).msi

###<a id="msbuild"></a>MSBuild###

General command line:

> msbuild msbuild.proj \[/t:target[,target2,..]] \[/p:property=value [ .. /p:... ] ]

A 'help' target is available which prints out all the targets, customizable
properties, and the current value of those properties:

> msbuild msbuild.proj /t:help


###Components###

- common/targets/: MSBuild targets files used by the WiX projects.
  - BuildDistFragment.targets: contains msbuild targets to generate a WiX
    fragment from the extracted distribution.
  - DownloadVCRedist.targets: contains msbuild targets to download the
    appropriate Visual C++ redistributable for the WiX Bundle build.
  - Minion.Common.targets: contains targets to discover the correct
    distribution zip file, extract it and calculate versions based on its name.
- wix.sln: Visual Studio solution file. Requires VS2010 or above. See
  note above about dependencies.
- wix/MinionConfigurationExtension/: A WiX Extension implementing custom
  actions for configuration manipulation.
- wix/MinionMSI/: This is the WiX .msi project
  - dist-$(TargetPlatform).wxs: WiX fragment describing the contents of the
    distribution zip file. This is autogenerated and added to the compile at
    build time; it does not show up in the Visual Studio solution.
  - SettingsCustomizationDlg.wxs: A custom MSI dialog for the master/minion id
    properties.
  - MinionMSI.wixproj: the main project file.
  - MinionConfigurationExtensionCA.wxs: A WiX fragment setting up the
    configuration manipulator custom actions.
  - Product.wxs: contains the main MSI description and event sequence
  - service.wxs: contains a WiX component for nssm.exe and the
    associated Windows Service description/control settings.
    - wix\MinionMSI\dist-amd64.wxs lists all the discovered sources.
    - Because nssm.exe must be a (handwritten) WiX component in service.wxs, it also must be excluded from dist-amd64.wxs. 
    - nssm.xsl excludes nssm.exe from dist-amd64.wxs.
  - WixUI\_Minion.wxs: WiX fragment describing the UI for the setup.
  - Banner.jpg: Used as the top bar banner in most of the UI dialogs.
  - Dialog.jpg: Used as the dialog background for Welcome and Exit dialogs.


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

- SettingsCustomizatonDlg.wxs: There is room to add 1-2 more properties
  to this dialog.
- WixUI\_Minion.wxs: A &lt;ProgressText /&gt; entry providing a brief
  description of what the new action is doing.

If the new custom action requires its own dialog, these additional
changes are required:

- The new dialog file.
- WixUI\_Minion.wxs: &lt;Publish /&gt; entries hooking up the dialog
  buttons to other dialogs. Other dialogs will also have to be adjusted
  to maintain correct sequencing.
- MinionMSI.wixproj: The new dialog must be added as a &lt;Compile /&gt;
  item to be included in the build.

##On versioning##
The user sees a [3-tuple][version_html] version, e.g. `2016.11.0`.

[Internally][version_py], version is a 8-tuple:
- major,
- minor,
- bugfix,
- mbugfix,
- pre_type,
- pre_num,
- noc,
- sha

E.g. (2016, 11, 0, 0, '', 0, 461, u'g723699f')

The msi properties `DisplayVersion` and `InternalVersion` store these values.

msi rules demand that the major version of the InternalVersion must be smaller than 265, therefore only the "short year" is used for the major InternalVersion.


##Suggested Improvements##

- Have the WiX setup detect and uninstall existing NSIS installations (and
  vice-versa).
- Add other configuration manipulations.
- Write new configuration to minion.d instead of editing the distributed
  minion config.
- Develop a custom bootstrapper application to replace the default WiX
  bootstrapper, and move the UI from the .msi to the bundle .exe.
- Nice install dialog art.

[WiXId]: http://wixtoolset.org "WiX Homepage"
[MSBuildId]: http://msdn.microsoft.com/en-us/library/0k6kkbsd(v=vs.120).aspx "MSBuild Reference"
[MSBuild2913Id]: https://www.microsoft.com/en-in/download/details.aspx?id=40760
[version_html]: https://docs.saltstack.com/en/latest/topics/releases/version_numbers.html
[version_py]: https://github.com/saltstack/salt/blob/develop/salt/version.py
