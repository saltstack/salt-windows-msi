# Identify optional components/features

from __future__ import print_function
import os

def get_count_size_from_dir(start_path = '.'):
    total_size = 0
    count = 0
    for dirpath, dirnames, filenames in os.walk(start_path):
        for f in filenames:
            fp = os.path.join(dirpath, f)
            total_size += os.path.getsize(fp)
            count += 1
    return count, total_size


def go_dir(dir, comment='', url=''):
    count, size = get_count_size_from_dir(dir)
    #print("{:>5} {:>10} {}".format(count, size, dir))


def get_count_size_of_file_glob(dir):
    total_size = 0
    count = 0
    import glob
    for fp in glob.glob(dir):
        total_size += os.path.getsize(fp)
        count += 1
    return count, total_size
    

def go_files(dir, comment='', url=''):
    count, size = get_count_size_of_file_glob(dir)
    print("{:>5} {:>10} {}".format(count, size, dir))


go_dir(r'C:\salt\bin\Lib\site-packages\pip', 'Pip')
go_dir(r'C:\salt\bin\Lib\site-packages\lxml', 'processing XML and HTML', 'https://lxml.de/')
go_dir(r'C:\salt\bin\Lib\site-packages\salt\returners')
go_dir(r'C:\salt\bin\Lib\ensurepip')
go_dir(r'c:\salt\bin\Lib\distutils')
go_dir(r'c:\salt\bin\Lib\email')
go_dir(r'c:\salt\bin\Lib\hotshot', 'High-perfomance logging profiler')
go_dir(r'c:\salt\bin\Lib\pydoc_data')
go_dir(r'c:\salt\bin\Lib\xml')
go_dir(r'C:\salt\bin\Lib\site-packages\git')

go_files(r'C:\salt\bin\Lib\doctest*.*', 'framework for running examples in docstrings')
go_files(r'C:\salt\bin\Lib\pydoc*.*', 'Generate Python documentation in HTML or text')
go_files(r'C:\salt\bin\Lib\mailbox.py', 'Read/write support for Maildir, mbox, MH, Babyl, and MMDF mailboxes')

go_files(r'C:\salt\bin\Lib\site-packages\salt\states\git.*')
go_files(r'C:\salt\bin\Lib\site-packages\salt\modules\git*.*')

go_files(r'C:\salt\bin\Lib\site-packages\salt\states\docker_*.*')
go_files(r'C:\salt\bin\Lib\site-packages\salt\modules\docker*.*')

go_files(r'C:\salt\bin\Lib\site-packages\salt\states\boto_*.*')
go_files(r'C:\salt\bin\Lib\site-packages\salt\modules\boto*.*')

go_files(r'C:\salt\bin\Lib\site-packages\salt\modules\vsphere*')

go_files(r'c:\salt\bin\Lib\site-packages\salt\states\vagrant.py', 'Manange execution of Vagrant virtual machines on Salt minions')
go_files(r'c:\salt\bin\Lib\site-packages\salt\states\virtualenv_mod.py', 'Setup of Python virtualenv sandboxes')
go_files(r'c:\salt\bin\Lib\site-packages\salt\states\bigip.py', 'load-balancing configurations for F5 Big-IP')
go_files(r'c:\salt\bin\Lib\site-packages\salt\states\esxi.py', 'VMware ESXi Hosts')
go_files(r'c:\salt2\bin\Lib\site-packages\salt\states\grafana*.*', 'Grafana Dashboards')
go_files(r'c:\salt2\bin\Lib\site-packages\salt\states\jboss7.py', 'Manage JBoss 7 Application Server via CLI interface')
go_files(r'c:\salt2\bin\Lib\site-packages\salt\states\junos.py', 'Junos devices.  (Juniper Network Operating System)')
go_files(r'')
go_files(r'')
go_files(r'')


