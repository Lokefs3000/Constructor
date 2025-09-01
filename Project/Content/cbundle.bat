@echo off

setlocal EnableDelayedExpansion

set "IN_ARGS="
for %%i in (%*) do (
	if not %%i==%1 if not %%i==%2 (
		set "IN_ARGS=!IN_ARGS!%%i "
	)
)

@call "D:\source\repos\Constructor\Editor\bin\Debug\net9.0\Editor.exe" bundle -o %1 -i %IN_ARGS%