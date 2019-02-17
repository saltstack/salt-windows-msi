''' delete pyc except salt-minion.pyc '''

from __future__ import print_function
import os

SRCDIR = r'c:\salt'

def action(start_path):
    skipped = 0
    deleted = 0
    for dirpath, dirnames, filenames in os.walk(start_path):
        for f in filenames:
            fp = os.path.join(dirpath, f)
            if fp.endswith('.pyc'):
                if f == 'salt-minion.pyc':
                    skipped += 1
                else:
                    os.remove(fp)
                    deleted += 1
    print('{} skipped'.format(skipped))
    print('{} deleted'.format(deleted))


action(SRCDIR)

