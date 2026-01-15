@echo off

set "AgilitySDKVer=618"

@echo Patching executable.. (ver:%AgilitySDKVer%)
@call "..\..\..\..\Patcher\D3D12SDKVersionPatcher\D3D12SDKVersionPatcher.exe" "%1.exe" "%AgilitySDKVer%" ".\\D3D12\\"

@if %ERRORLEVEL% GEQ 1 goto alreadyPatched

@del "%1.exe"
@move "%1.D3D12_%AgilitySDKVer%.exe" "%1.exe"

goto eof

:alreadyPatched
set "%ERRORLEVEL%=0"
@echo Executable already patched!

:eof