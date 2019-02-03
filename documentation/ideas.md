
## On directory structure ##
Files under INSTALLDIR are intended to be immutable in Windows.
Mutable data, created and changed after installation, as log files or a private key, should not be stored under INSTALLDIR.
Doing so makes install/uninstall complex.


## Request for comment ##
Currently, the minion id is in the config file.
Proposal: name the private/public keys directly as the name of the file:
```
salt/conf/jim.pem
salt/conf/master/joe.pub
salt/conf/master/jane.pub
```

Allow master private key change:
```
salt/conf/master/joe.pub
salt/conf/master/joe(2018-04-12--14-30).pub
```
