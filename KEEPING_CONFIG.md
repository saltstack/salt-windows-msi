
The user can  
 - chose an installation root (INSTALLDIR).
 - keep configuration on uninstall.

Only keeping INSTALLDIR/salt/conf on uninstall makes no sense, because its location becomes unknown for the next install.
We therefore need to keep a pointer to INSTALLDIR/salt/conf in a well-known location.

The registry seems to be write protected for msi custom actions at uninstall time.

From the Windows [WELL KNOWN FOLDERS][MSDN_WELL_KNONW_FOLDERS]:
 - `%SystemDrive%\ProgramData` is used for application data that is not user specific.

On uninstall, we store a plain text file
`%SystemDrive%\ProgramData\SaltStack\SaltMinion\KEPT_CONFIG`
which contains the path to the (former) INSTALLDIR/salt/conf.

This file is only created on uninstall with `KEEP_CONFIG=1` (the default).
On uninstall with KEEP_CONFIG=0, no configuration remains. 
Maybe the logs should remain.

A (renewed) installation looks for configuration path 
 - in `%SystemDrive%\ProgramData\SaltStack\SaltMinion\KEPT_CONFIG`
 
Installation could also search
 - `%ProgramFiles%\SaltStack\SaltMinion`
 - `c:\salt`


## Install procedure ##
If not installed, search for old root.
if configuration found and tested:
  show old root, master, minion
Does the user want to keep minion and master? If yes:
  Does the user want to change root? If yes:
    ask for new root
    change root
else:
  remove old configuration
  remove KEPT_CONFIG
  ask user for root.

 
 
## Further thinking ##

Files under INSTALLDIR are intended to be immutable in Windows.
Mutable data, created and changed after installation, as log files or a private key, should not be stored under INSTALLDIR.
Doing so makes install/uninstall complex.


[MSDN_WELL_KNONW_FOLDERS]: https://msdn.microsoft.com/en-us/library/windows/desktop/dd378457.aspx