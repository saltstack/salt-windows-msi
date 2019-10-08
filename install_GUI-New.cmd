@echo off
:: Get the (last) file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix.d\MinionMSI\bin\Release\%msi% CONFIG_TYPE=New MINION_ID_FUNCTION=socket.gethostname() MINION_CONFIG="a: A,bb: BB,ccc444: CCC" MASTER=ma111 MINION_ID=mi111
