## How to use the msi builder toolkit

## Build client requirements

The build client is where the msi installer is built.

- 64bit Windows 10
- The Git repositories `salt` and `salt-windows-msi`
- .Net 3.5 SDK (for WiX)<sup>*</sup>
- [Wix 3](http://wixtoolset.org/releases/)<sup>**</sup>
- [Build tools 2015](https://www.microsoft.com/en-US/download/confirmation.aspx?id=48159)<sup>**</sup>
- Microsoft_VC140_CRT_x64.msm from Visual Studio 2015 in `c:\salt_msi_resources\`<sup>**</sup>
- Microsoft_VC140_CRT_x86.msm from Visual Studio 2015 in `c:\salt_msi_resources\`<sup>**</sup>
- Microsoft_VC120_CRT_x64.msm from Visual Studio 2013 in `c:\salt_msi_resources\`<sup>**</sup>
- Microsoft_VC120_CRT_x86.msm from Visual Studio 2013 in `c:\salt_msi_resources\`<sup>**</sup>

<sup>*</sup> `build_env.cmd` will open `optionalfeatures` if necessary.

<sup>**</sup> `build_env.cmd` will download and install if necessary.

How to include 64bit `rc.exe` (resource compiler) from the Windows SDK into path

Open "Build Tools for Visual Studio 2017"
- Select workload "Visual C++ build tools"
- Check options
  -  "C++/CLI support"
  -  "VC++ 2015.3 v14.00 (v140) toolset for desktop"
- Download 2 GB in 177 packages.

    set ttt=C:\Program Files (x86)\Windows Kits\8.1\bin\x64
    set path=%ttt%;%path%


### Step 1: build the Nullsoft (NSIS) exe installer

Read [Building and Developing on Windows](https://docs.saltstack.com/en/latest/topics/installation/windows.html#building-and-developing-on-windows)

Execute

    git checkout v3002.5

Repeat

    git clean -fxd
    git checkout .

until `git status` returns

    HEAD detached at v3002.5
    nothing to commit, working tree clean

First **clean the Python environment** then build

    clean_env.bat
    build.bat Python=3

### Step 2: build the msi installer

Execute

    cd c:\dev\salt-windows-msi
    build_env.cmd
    build.cmd

`build_env.cmd` uses a cache in `c:\salt_msi_resources`


[LGHT1076 ICE82 warnings are caused by the VC++ Runtime merge modules](https://sourceforge.net/p/wix/mailman/message/22945366/) and supressed.


### Remark on transaction safety

Wix is transaction safe: either the product is installed or the prior state is restored.

C# is not.

### Directory structure

- Product.wxs: main file.
  - (EXPERIMENTAL) salt-minion Windows Service
    - requires [saltminionservice](https://github.com/saltstack/salt/blob/167cdb344732a6b85e6421115dd21956b71ba25a/salt/utils/saltminionservice.py) or [winservice](https://github.com/saltstack/salt/blob/3fb24929c6ebc3bfbe2a06554367f8b7ea980f5e/salt/utils/winservice.py) [Removed](https://github.com/saltstack/salt/commit/8c01aacd9b4d6be2e8cf991e3309e2a378737ea0)
- CustomAction01/: custom actions in C#
- *-discovered-files.wxs: TEMPORARY FILE

### Naming conventions

Immediate custom actions serve initialization (before the install transaction starts) and must not change the system.
Deferred custom action change the system (within the installation transaction).

Postfix  | Example                            | Meaning
-------- | ---------------------------------- | -------
`_IMCAC` | `ReadConfig_IMCAC`                 | Immediate custom action written in C#
`_DECAX` | `uninst_NSIS_DECAX`                | Deferred custom action written in XML
`_DECAC` | `WriteConfig_DECAC`                | Deferred custom action written in C#
`_CADH`  | `WriteConfig_CADH`                 | Custom action data helper (only for deferred custom action)

### Extending

Additional configuration manipulations may be able to use the existing
MinionConfigurationExtension project. Current manipulations read the
value of a particular property (e.g. MASTER\_HOSTNAME) and apply to the
existing configuration file using a regular expression replace. Each new
manipulation will require changes to the following files:

- MinionConfiguration.cs: new method (i.e. new custom action).
- Product.wxs:
  - a &lt;CustomAction /&gt; entry to make the new method available.
  - a &lt;Custom /&gt; entry in the &lt;InstallSequence /&gt; to make the configuration change.
  - a &lt;Property /&gt; entry containing a default for the manipulated configuration setting.
- README.md: explain the new configuration option and log the property name.

If the new custom action requires its own dialog, these additional changes are required:

- GUI: &lt;Publish /&gt; entries hooking up the dialog buttons to other dialogs.
  Other dialogs will also have to be adjusted to maintain correct sequencing.


### Other Notes
msi conditions for Customm Actions in a table with install, uninstall, updgrade:
- https://stackoverflow.com/a/17608049


Install sequences documentation:

- [standard-actions-reference](https://docs.microsoft.com/en-us/windows/win32/msi/standard-actions-reference)
- [suggested-installuisequence](https://docs.microsoft.com/en-us/windows/win32/msi/suggested-installuisequence)
- [suggested-installexecutesequence](https://docs.microsoft.com/en-us/windows/win32/msi/suggested-installexecutesequence)
- [other docs](https://www.advancedinstaller.com/user-guide/standard-actions.html)

The Windows installer restricts the maximum values of the [ProductVersion property](https://docs.microsoft.com/en-us/windows/win32/msi/productversion):

- major.minor.build
- `255.255.65535`

Because of this restriction, the builder generates an internal version:
 - Salt 3012.1 becomes `30.12.1`
 - Salt 2018.3.4 becomes `18.3.4`


[Which Python version uses which MS VC CRT version](https://wiki.python.org/moin/WindowsCompilers)

- Python 2.7 = VC CRT 9.0 = VS 2008
- Python 3.6 = VC CRT 14.0 = VS 2017

