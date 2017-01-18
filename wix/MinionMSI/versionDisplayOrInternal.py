# 2017-01-18 Markus   git describe
# 2016-12-02 Markus   
# show DisplayVersion 2016.11.1 or InternalVersion 16.11.1.444
#

from __future__ import absolute_import, print_function

import sys
import subprocess
version_git_describe = ""

try:
	version_git_describe = str(subprocess.check_output (r'"C:\Program Files\Git\cmd\git.exe" --git-dir=/git/salt/.git --work-tree=/git/salt describe'))
	#print (version_git_describe)
except Exception as  e:
	print (str(e))
	sys.exit(1)
	
#print("version_git_describe = " + version_git_describe)
	
version_list   = version_git_describe.replace('.',' ').replace('-',' ').split() # all elements are strings
year    = version_list[0]
minor   = version_list[1]
bugfix  = version_list[2]
if year.startswith('v'):                 year                 = year[1:]
if version_git_describe.startswith('v'): version_git_describe = version_git_describe[1:]
DisplayVersion  =  version_git_describe    # Probably `git describe` becomes v2016.11.1 after a long series of 2016.11.1-2151-g559bee3 
InternalVersion =  year[2:] + '.' + minor + '.' + bugfix 
if len(version_list) > 3: # mbugfix
	InternalVersion += '.' + version_list[3]

show_internal = len(sys.argv) > 1 and sys.argv[1] == 'i'
if show_internal:
	print(InternalVersion)
else:
	print(DisplayVersion)


