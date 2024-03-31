cd ..
dotnet publish -c Release --runtime win-x64 /p:PublishAot=true /p:UseSystemResourceKeys=true /p:InvariantGlobalization=true /p:OptimizationPreference=Size 
pause