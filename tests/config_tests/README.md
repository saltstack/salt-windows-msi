
### Edit a `*.test` file
- Contains the msi-properties, of which master and minion_id simulate user input via the GUI
- May contain the `dormant` keyword, which means the configuration remains (dormant) after uninstall
- Without the `dormant` keyword, no configuaration must exist after uninstall

### Optionally edit a `*.input` file
- Contains prior configuration
- Omit this file if there is no  prior configuration  

### Edit a `*.expected` file
- Contains configuration (after install)

### Run `test.cmd`
- Will generate and run *-install.bat and  *-uninstall.bat files for each *.test file
