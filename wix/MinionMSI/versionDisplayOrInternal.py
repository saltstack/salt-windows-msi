# 2016-12-02 Markus   
# show DisplayVersion or InternalVersion
#
# Could be much nicer but I don't know how to access the class in version
# So __version__ is all I have
# e.g.       2016.3.2-72-gc1deb94
# Could also be directly in version.py

from __future__ import absolute_import, print_function

import sys
sys.path.insert(0, '../../../salt/salt')   # path to version.py
import version

version_list   = version.__version__.replace('.',' ').replace('-',' ').split() # all elements are strings
year    = version_list[0]
minor   = version_list[1]
bugfix  = version_list[2]
DisplayVersion  =   year    + '.' + minor + '.' + bugfix                                   # 
InternalVersion =  year[2:] + '.' + minor + '.' + bugfix 
if len(version_list) > 3: # mbugfix
	InternalVersion += '.' + version_list[3]

show_internal = len(sys.argv) > 1 and sys.argv[1] == 'i'
if show_internal:
	print(InternalVersion)
else:
	print(DisplayVersion)


