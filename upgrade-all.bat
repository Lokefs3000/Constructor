@echo off

set FRAMEWORK=net10.0
set ROOT_DIR=%~dp0

echo Upgrading engine..

upgrade-assistant upgrade %ROOT_DIR%Primary\Primary.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Primary.Common\Primary.Common.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Primary.Interop\Primary.Interop.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Primary.R2.ForwardPlus\Primary.R2.ForwardPlus.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Primary.Rendering2\Primary.Rendering2.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Runtime\Runtime.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Interop.D3D12MemAlloc\Interop.D3D12MemAlloc.csproj --operation Inplace --targetFramework %FRAMEWORK%

echo Upgrading RHI..

upgrade-assistant upgrade %ROOT_DIR%Primary.RHI\Primary.RHI.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Primary.RHI.Direct3D12\Primary.RHI.Direct3D12.csproj --operation Inplace --targetFramework %FRAMEWORK%

echo Upgrading editor..

upgrade-assistant upgrade %ROOT_DIR%Editor\Editor.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Editor.Gui\Editor.Gui.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Editor.Interop\Editor.Interop.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Editor.Processors\Editor.Processors.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Editor.Shaders\Editor.Shaders.csproj --operation Inplace --targetFramework %FRAMEWORK%
upgrade-assistant upgrade %ROOT_DIR%Editor.Geometry\Editor.Geometry.csproj --operation Inplace --targetFramework %FRAMEWORK%

echo Upgrading tools..

upgrade-assistant upgrade %ROOT_DIR%ShaderGen\ShaderGen.csproj --operation Inplace --targetFramework %FRAMEWORK%