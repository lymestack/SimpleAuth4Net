@echo off

:: Set the paths for the .NET and Angular applications
set NET_API_PATH=.\WebApi\WebApi
set ANGULAR_APP_PATH=.\ng-app

:: Start the .NET WebAPI
start cmd /k "cd /d %NET_API_PATH% && echo Running .NET API && dotnet watch run"

:: Start the Angular application
:: This will first run 'npm install' to install dependencies, then 'npm start' to run the Angular app
start cmd /k "cd /d %ANGULAR_APP_PATH% && echo Preparing to start Angular App - Installing node dependencies. && echo If this is your first time running this, you might want to go get a drink.... && npm install && echo Running Angular App... Browser will automatically launch when ready... && npm start"
