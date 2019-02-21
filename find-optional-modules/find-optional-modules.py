'''
Find the  directories and files of optional modules.
'''

from __future__ import print_function
import os
import glob
from humanreadable_bytes import humanreadable_bytes

pyc_with_py = {}
with open ('pyc_with_py.txt', 'r') as inputFile:
    for pyc in inputFile.read().splitlines():
        pyc_with_py[pyc] = 1


def files_from_dir(dir):
    ret = []
    for dirpath, dirnames, filenames in os.walk(dir):
        for filename in filenames:
            full_file_name = os.path.join(dirpath, filename)
            ret.append(full_file_name)
    return ret


def files_from_glob(file_pattern):
    ret = []
    for full_file_name in glob.glob(file_pattern):
        ret.append(full_file_name)
    assert len(ret) != 0, 'No files in {}'.format(file_pattern)
    return ret


def count_pyc_py_size(file):
    total_size, pyccount, pycount = 0, 0, 0
    if os.path.isdir(file): files = files_from_dir(file)
    else:                   files = files_from_glob(file)
    for full_file_name in files:
        pyc1 = full_file_name.replace('C:/salt/', '').replace('c:/salt/', '').replace('\\', '/') + 'c'
        if pyc1 in pyc_with_py:
            pyccount += 1
        total_size += os.path.getsize(full_file_name)
        pycount += 1
    return pyccount, pycount, total_size


def desc_module(comment, files):
    # Count *.pyc, *.py and size of a module
    pyccount, pycount, size = 0, 0, 0
    for file in files:
        pyccount1, pycount1, size1 = count_pyc_py_size('C:/salt/'+file)
        pyccount += pyccount1
        pycount += pycount1
        size += size1
    print("{:>5} {:>10}  {:>10}   {}".format(pyccount, pycount, humanreadable_bytes(size), comment))



desc_module('Pip',       ['bin/Lib/site-packages/pip', 'bin/Lib/ensurepip'])
desc_module('Distutils', ['bin/Lib/distutils'])
desc_module('Setuptools', ['bin/Lib/site-packages/setuptools'])
desc_module('Wheel', ['bin/Lib/site-packages/wheel'])
desc_module('Processing XML and HTML', ['bin/Lib/site-packages/lxml'])
desc_module('Returners', ['bin/Lib/site-packages/salt/returners'])
desc_module('Email', ['bin/Lib/email'])
desc_module('High-perfomance logging profiler', ['bin/Lib/hotshot'])
desc_module('Pydoc_data', ['bin/Lib/pydoc_data'])
desc_module('Framework for running examples in docstrings', ['bin/Lib/doctest*.*'], )
desc_module('Generate Python documentation in HTML or text', ['bin/Lib/pydoc*.*'])
desc_module('Read/write support for Maildir, mbox, MH, Babyl, and MMDF mailboxes', ['bin/Lib/mailbox.py'])
desc_module('Git', ['bin/Lib/site-packages/git', 'bin/Lib/site-packages/salt/states/git.*', 'bin/Lib/site-packages/salt/modules/git*.*'])
desc_module('Docker', ['bin/Lib/site-packages/salt/states/docker_*.*', 'bin/Lib/site-packages/salt/modules/docker*.*'])
desc_module('Boto', ['bin/Lib/site-packages/salt/states/boto_*.*', 'bin/Lib/site-packages/salt/modules/boto*.*'])
desc_module('Vsphere', ['bin/Lib/site-packages/salt/modules/vsphere*'])
desc_module('Manange execution of Vagrant virtual machines on Salt minions', ['bin/Lib/site-packages/salt/states/vagrant.py'])
desc_module('Setup of Python virtualenv sandboxes', ['bin/Lib/site-packages/salt/states/virtualenv_mod.py'])
desc_module('Load-balancing configurations for F5 Big-IP', ['bin/Lib/site-packages/salt/states/bigip.py'])
desc_module('VMware ESXi Hosts', ['bin/Lib/site-packages/salt/states/esxi.py'])
desc_module('Grafana Dashboards', ['bin/Lib/site-packages/salt/states/grafana*.*'])
desc_module('Manage JBoss 7 Application Server via CLI interface', ['bin/Lib/site-packages/salt/states/jboss7.py'])
desc_module('Junos devices', ['bin/Lib/site-packages/salt/states/junos.py'])


