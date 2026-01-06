@echo off

set "AgilitySDKVer=618"

@echo "Patching executable.. (ver:%AgilitySDKVer%)"
"..\..\..\..\Patcher\D3D12SDKVersionPatcher\D3D12SDKVersionPatcher.exe" "%1.exe" "%AgilitySDKVer%" ".\\D3D12\\"
@del "%1.exe"
@move "%1.D3D12_%AgilitySDKVer%.exe" "%1.exe"