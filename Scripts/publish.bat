cd ../CoreUtils/
dotnet publish -c Release -f net8.0
dotnet publish -c Release -f netstandard2.0
dotnet publish -c Release -f netstandard2.1
dotnet pack -c Release
pause