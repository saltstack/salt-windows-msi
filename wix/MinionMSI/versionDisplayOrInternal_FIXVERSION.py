#### 2016-01-27  mkir  Problem 
#Traceback (most recent call last):
#  File "versionDisplayOrInternal.py", line 13, in <module>
#    import version
#  File "../../../salt/salt\version.py", line 14, in <module>
#    from salt.ext import six
#ImportError: No module named salt.ext
#### 2016-01-27  mkr   Quick Fix. For Solution I need help from Saltstack


InternalVersion = "16.11.1.400"
DisplayVersion = "2016.11.1"
import sys


show_internal = len(sys.argv) > 1 and sys.argv[1] == 'i'
if show_internal:
	print(InternalVersion)
else:
	print(DisplayVersion)

