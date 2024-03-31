cd ..
dotnet publish -c Release --runtime win-x64 /p:PublishAot=true;_SuppressWinFormsTrimError=true
pause