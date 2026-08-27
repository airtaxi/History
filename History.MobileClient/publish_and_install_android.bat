dotnet publish -f net10.0-android -c Release -o ./publish
if errorlevel 1 (
    echo Build failed. Skipping install.
    pause
    exit /b 1
)
adb install -r publish\com.airtaxi.history-Signed.apk
pause