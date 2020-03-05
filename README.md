# Windows MSI installer build toolkit

This creates a Salt Minion msi installer using [WiX](http://wixtoolset.org).

The focus is on 64bit, unattended install.

## Introduction

[On Windows installers](http://unattended.sourceforge.net/installers.php)

An msi installer allows unattended/silent installations, meaning without opening any window, while still providing
customized values for e.g. master hostname, minion id, installation path, using the following generic command:

> msiexec /i *.msi PROPERTY1=VALUE1 PROPERTY2=VALUE2 PROPERTY3="VALUE3a and 3b" PROPERTY4=""

Values must be quoted when they contains whitespace, or to unset a property, as in `PROPERTY4=""`

Example: Set the master:

> msiexec /i *.msi MASTER=salt2

Example: set the master and its key:

> msiexec /i *.msi MASTER=salt2 MASTER_KEY=MIIBIjA...2QIDAQAB

Example: uninstall and remove configuration

> MsiExec.exe /X *.msi KEEP_CONFIG=""

## Features

- Creates a very verbose log file, by default %TEMP%\MSIxxxxx.LOG, where xxxxx are 5 random lowercase letters or numbers. The name of the log can be specified with `msiexec /log example.log`
- Upgrades NSIS installations __UNDER REVIEW__

Salt Minion-specific msi-properties:

  Property              |  Default                | Comment
 ---------------------- | ----------------------- | ------
 `MASTER`               | `salt`                  | The master (name or IP). Only a single master.
 `MASTER_KEY`           |                         | The master public key. See below.
 `ZMQ_filtering`        |                         | Set to `1` if the master requires zmq_filtering.
 `MINION_ID`            | Hostname                | The minion id.
 `MINION_ID_CACHING`    | `1`                     | Set to `""` if the minion id shall be determined at each salt-minion service start.
 `MINION_ID_FUNCTION`   |                         | Set minion id by module function. See below
 `MINION_CONFIG`        |                         | Content to be written into the `minion` config file. See below.
 `START_MINION`         | `1`                     | Set to `""` to prevent the start of the `salt-minion` service.
 `KEEP_CONFIG`          | `1`                     | Set to `""` to remove configuration on uninstall.
 `CONFIG_TYPE`          | `Existing`              | Or `Custom` or `Default` or `New`. See below.
 `CUSTOM_CONFIG`        |                         | Name of a custom config file in the same path as the installer or full path. Requires `CONFIG_TYPE=Custom`. __ONLY FROM COMMANDLINE__
 `INSTALLFOLDER`        | `C:\salt\`              | Where to install the Minion  __DO NOT CHANGE (yet)__  --- __BLOCKED BY__ [issue#38430](https://github.com/saltstack/salt/issues/38430)


These files and directories are regarded as config and kept:

- C:\salt\conf\minion
- C:\salt\conf\minion.d\
- c:\salt\var\cache\salt\minion\extmods\
- c:\salt\var\cache\salt\minion\files\

Master and id are read from
 - file `C:\salt\conf\minion`
 - If `CONFIG_TYPE=New` then addionally from files `C:\salt\conf\minion.d\*.conf`

You can set a new master with `MASTER`. This will overrule the master in a kept configuration.

You can set a new master public key with `MASTER_KEY`, after you converted it into one line like so:

- Remove the first and the last line (`-----BEGIN PUBLIC KEY-----` and `-----END PUBLIC KEY-----`).
- Remove linebreaks.

### `MINION_CONFIG`

If `MINION_CONFIG` is set, the installer creates the file `c:\salt\conf\minion` with it. For line breaks, use "^".

Example `MINION_CONFIG="a: A^b: B"` results in:

    a: A
    b: B

### `MINION_ID_FUNCTION`

The minion ID can be set by a user defined module function ([Further reading](https://github.com/saltstack/salt/pull/41619)).

If `MINION_ID_FUNCTION` is set, the installer creates module file `c:\salt\var\cache\salt\minion\extmods\modules\id_function.py` with the content

    import socket
    def id_function():
      return MINION_ID_FUNCTION

Example `MINION_ID_FUNCTION=socket.gethostname()` results in:

    import socket
    def id_function():
      return socket.gethostname()

Remember to create the same file as `/sr/salt/_modules/id_function.py` on your server, so that `saltutil.sync_all` will keep the file on the minion.


### `CONFIG_TYPE`

There are 4 scenarios the installer tries to account for:

1. existing-config (default)
2. custom-config
3. default-config
4. new-config

Existing

This setting makes no changes to the existing config and just upgrades/downgrades salt.
Makes for easy upgrades. Just run the installer with a silent option.
If there is no existing config, then the default is used and `master` and `minion id` are applied if passed.

Custom

This setting will lay down a custom config passed via the command line.
Since we want to make sure the custom config is applied correctly, we'll need to back up any existing config.
1. `minion` config renamed to `minion-<timestamp>.bak`
2. `minion_id` file renamed to `minion_id-<timestamp>.bak`
3. `minion.d` directory renamed to `minion.d-<timestamp>.bak`
Then the custom config is laid down by the installer... and `master` and `minion id` should be applied to the custom config if passed.

Default

This setting will reset config to be the default config contained in the pkg.
Therefore, all existing config files should be backed up
1. `minion` config renamed to `minion-<timestamp>.bak`
2. `minion_id` file renamed to `minion_id-<timestamp>.bak`
3. `minion.d` directory renamed to `minion.d-<timestamp>.bak`
Then the default config file is laid down by the installer... settings for `master` and `minion id` should be applied to the default config if passed

New

Each Salt property (MASTER, ZMQ_FILTERING or MINON_ID) given is changed in all present config files or, if missing, added as a new file to the minion.d directory.

## Target client requirements

The target client is where the installer is deployed.

- Windows 7 (for workstations), Server 2012 (for domain controllers), or higher.
