echo "location: %CD%"
if exist Resources (
	xcopy Resources\* ..\RandomPlus\ /YE
	rd /s /q bin
	rd /s /q obj
	exit /b 0
) else (
	echo Cannot build mod distribution directory. Release build not found in bin\Release directory.
	pause
)