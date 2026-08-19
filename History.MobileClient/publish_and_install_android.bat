dotnet publish -f net10.0-android -c Release -o ./publish
adb install -r publish\com.airtaxi.history-Signed.apk
pause