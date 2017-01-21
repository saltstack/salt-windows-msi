

copy C:\saltrepo_local_cache\64\vcredist_x64_2008_mfc.exe C:\git\salt\pkg\windows\prereqs\vcredist.exe


goto eof

from build_pkg.bat

The regular user can bitsadmin /transfer "VCRedist 2008 MFC AMD64" "%Url64%" "%PreDir%\vcredist.exe"
 "http://repo.saltstack.com/windows/dependencies/64/vcredist_x64_2008_mfc.exe" 
but it takes 2 minutes.



The admin account cannot transfer, Error is

BITSADMIN version 3.0 [ 7.5.7601 ]
BITS administration utility.
(C) Copyright 2000-2006 Microsoft Corp.

BITSAdmin is deprecated and is not guaranteed to be available in future versions of Windows.
Administrative tools for the BITS service are now provided by BITS PowerShell cmdlets.

Unable to add file - 0x800704dd

:eof

