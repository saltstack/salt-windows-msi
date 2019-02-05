# Ideas

## Replace nssm

[On May 1, 2017, twangboy removed](https://github.com/saltstack/salt/commit/8c01aacd9b4d6be2e8cf991e3309e2a378737ea0)

- [saltminionservice](https://github.com/saltstack/salt/blob/3fb24929c6ebc3bfbe2a06554367f8b7ea980f5e/salt/utils/saltminionservice.py)

- [winservice](https://github.com/saltstack/salt/blob/3fb24929c6ebc3bfbe2a06554367f8b7ea980f5e/salt/utils/winservice.py)

## Directory structure

Files under INSTALLDIR are intended to be immutable.
Where to store mutable data, created and changed after installation, as log files or a private key?
