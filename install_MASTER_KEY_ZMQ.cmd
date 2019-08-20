
@echo off
:: Get the (last) file name
for /f "delims=" %%a in ('dir /b wix.d\MinionMSI\bin\Release\*.msi')   do @set "msi=%%a"

@echo on
msiexec /i wix.d\MinionMSI\bin\Release\%msi% MASTER_KEY=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqIW0Fypy2IqrtaLVhTW0DKC/L6VLoGJl1x4Wn18xP0ku3pFe4Q0EZheVnZWO4s+yVrMUWsBabuZYfX/P1088c9p4bFNGfao9WUe0jhbZ23Mt1SDYa22lEDmw7/uHekntQoSu943aCs3p5Mx3XcoeBo3B8z6B+QHtJfxbAhNgPkUw2JOLHwLkhQWVc8HTwNvoY72/ZaqdbjiQUpl0Ohy6zKkdhBvX40PLbRKcSEkwt9/0D0hqiBAPvtSgTw0sdJTxmBOCCrsf6YAq6YcioZwAwQZX9ycvfYihcxkauBnbX2AMp/85LL8BKUBp4cde1f5YJ/G9YZLMSqr4iQN/cvQN2QIDAQAB ZMQ_FILTERING=True MASTER=server3000
