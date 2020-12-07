
from __future__ import print_function
import os

SRCDIR = r'c:\git\salt\salt\grains'
SRCDIR = r'c:\git\salt\salt\modules'

def format_path(filehandle):
    filehandle = filehandle.replace(SRCDIR+'\\', '')
    filehandle = filehandle.replace('\\', '/')
    return filehandle +'\n'


def action(start_path):
    withplatform = 0
    withoutplatform = 0
    for dirpath, _, filenames in os.walk(start_path):
        for filename in filenames:
            ffp = os.path.join(dirpath, filename)
            if ffp.endswith('.py'):
                goo = ""
                with open(ffp, encoding="ascii", errors="ignore") as filehandle:
                    for line in filehandle.readlines():
                        if ":platform:" in line:
                            goo = line.rstrip("\n")
                            goo = goo.replace(":platform:","")
                            continue
            if len(goo) > 0:
                print('{:25}   {}'.format(filename, goo))
                withplatform += 1
            else:
                withoutplatform += 1
    print('withplatform     {}'.format(withplatform))
    print('withoutplatform  {}'.format(withoutplatform))

action(SRCDIR)
print("sss")

