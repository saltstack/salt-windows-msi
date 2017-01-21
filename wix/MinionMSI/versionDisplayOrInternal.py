# 2017-01-18 Markus   git describe
# 2016-12-02 Markus   
# show DisplayVersion 2016.11.1 or InternalVersion 16.11.1.444
#

from __future__ import absolute_import, print_function

import sys
import subprocess
version_string = ""
git_exe = r'"C:\Program Files\Git\cmd\git.exe" '
git_opt = r'--git-dir=/git/salt/.git --work-tree=/git/salt '
git_cmd = r'describe --tags --first-parent --match v[0-9]* --always'
git_command_string = git_exe + git_opt + git_cmd
try:
	version_string = str(subprocess.check_output (git_command_string))
	#print (version_string)
except Exception as  e:
	print (str(e))
	sys.exit(1)
	
#print("version_string = " + version_string)
	
version_list   = version_string.replace('.',' ').replace('-',' ').split() # all elements are strings
year    = version_list[0]
month   = version_list[1]
bugfix  = version_list[2]
if year.startswith('v'):           year           = year[1:]
if version_string.startswith('v'): version_string = version_string[1:]
DisplayVersion  =  version_string    # Probably `git describe` becomes v2016.11.1 after a long series of 2016.11.1-2151-g559bee3 
InternalVersion =  year[2:] + '.' + month + '.' + bugfix 
if len(version_list) > 3: # mbugfix
	InternalVersion += '.' + version_list[3]

show_internal = len(sys.argv) > 1 and sys.argv[1] == 'i'
if show_internal:
	print(InternalVersion)
else:
	print(DisplayVersion)


