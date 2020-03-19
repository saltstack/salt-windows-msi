:::: Remove salt libraries
::: DONT
::: c:\git\salt\pkg\windows\buildenv\bin\Lib\email
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\distutils
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\ensurepip
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\http
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\lib2to3
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\pydoc_data

:::: Remove salt modules/states (TODO other locations)
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\aptpkg.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\azurearm_network.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\bigip.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\boto_ec2.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\boto_vpc.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\debian_ip.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\dockermod.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\git.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\lxc.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\lxd.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\postgres.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\vsphere.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\win_lgpo.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\zabbix.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\modules\zypperpkg.py
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\salt\states\git.py


:::: Remove python packages
::::         Pythonwin - Python IDE and GUI Framework for Windows.
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\_mssql.cp35-win_amd64.pyd
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\pycurl.cp35-win_amd64.pyd
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\pymssql.cp35-win_amd64.pyd
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\win32\win32gui.pyd
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\win32\winxpgui.pyd
del          c:\git\salt\pkg\windows\buildenv\bin\Lib\turtle.py
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\Cryptodome\SelfTest
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\git
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\pip
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\salt-3000-py3.5.egg\share
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\site-packages\setuptools
rmdir /s /q  c:\git\salt\pkg\windows\buildenv\bin\Lib\unittest



