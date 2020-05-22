@echo off
mkdir     C:\salt\bin
mkdir     C:\salt\var
mkdir     C:\salt\conf
echo 123> C:\salt\bin\test_deleteme_on_uninstall.txt
echo 123> C:\salt\var\test_deleteme_on_uninstall.txt
echo 123> C:\salt\conf\test_deleteme_on_uninstall.txt
call test_1read
