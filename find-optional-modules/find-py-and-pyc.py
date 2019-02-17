''' only py with pyc files are used '''

from __future__ import print_function
import os

count_py_without_pyc_dir = {}


SRCDIR = r'c:\salt'
TGTDIR = r'c:\salt-pyc'

fout_py_without_pyc = open('py_without_pyc.txt', 'w') 
fout_py_without_pyc_summary = open('py_without_pyc_summary.txt', 'w') 
fout_pyc_without_py = open('pyc_without_py.txt', 'w') 
fout_pyc_with_py = open('pyc_with_py.txt', 'w') 

def increase_key_in_hash(hash, key):
    if key in hash: hash[key] += 1
    else:           hash[key] = 1

def format_path(fp):
    fp = fp.replace(SRCDIR+'\\', '')
    fp = fp.replace('\\', '/')
    return fp +'\n'

def py_without_pyc(fp, dirpath):
    #os.remove(fp)
    fout_py_without_pyc.write(format_path(fp)) 
    increase_key_in_hash(count_py_without_pyc_dir, dirpath)

def pyc_with_py(fp):
    fout_pyc_with_py.write(format_path(fp)) 


def pyc_without_py(fp):
    fout_pyc_without_py.write(format_path(fp)) 


def action(start_path):
    found_py = 0
    found_pyc = 0
    for dirpath, dirnames, filenames in os.walk(start_path):
        for f in filenames:
            fp = os.path.join(dirpath, f)
            if fp.endswith('.py'):
                found_py += 1
                if not os.path.isfile(fp + 'c'):
                    py_without_pyc(fp, dirpath) 
            if fp.endswith('.pyc'):
                found_pyc += 1
                if os.path.isfile(fp[:-1]):
                    pyc_with_py(fp) 
                else:
                    pyc_without_py(fp)
    print('{:>5} py'.format(found_py))
    print('{:>5} pyc'.format(found_pyc))

action(SRCDIR)

import operator
for a,b in sorted(count_py_without_pyc_dir.items(), key=operator.itemgetter(1), reverse=True):
    fout_py_without_pyc_summary.write('{:>5}  {}'.format(b, a)+'\n')


fout_py_without_pyc.close()
fout_pyc_with_py.close()
