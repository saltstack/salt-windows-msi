rem If you want to test config-tests, do this before building
xcopy /s /y _mock_salt_pkg_windows\buildenv ..\salt-windows-nsis\scripts\buildenv\
xcopy /s /y _mock_salt_pkg_windows\build ..\salt-windows-nsis\build\
