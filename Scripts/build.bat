cd ..
dotnet publish -c Release --runtime win-x64 --self-contained false /p:PublishSingleFile=true
pause