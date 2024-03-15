echo "location: %CD%"
if exist bin\Release\RandomPlus.dll (
	xcopy Resources\* ..\RandomPlus\ /YE
	xcopy bin\Release\RandomPlus.dll ..\RandomPlus\1.5\Assemblies\ /Y
	rd /s /q bin
	rd /s /q obj
	exit /b 0
) else (
	echo Cannot build mod distribution directory. Release build not found in bin\Release directory.
	pause
)