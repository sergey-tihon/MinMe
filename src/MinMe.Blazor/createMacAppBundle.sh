dotnet publish -c Release --self-contained -r osx.10.15-x64

rm -rf bin/MinMe.Blazor.app
mkdir bin/MinMe.Blazor.app
mkdir bin/MinMe.Blazor.app/Contents

cat > bin/MinMe.Blazor.app/Contents/Info.plist <<- EOM
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple Computer//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleGetInfoString</key>
  <string>MinMe.Blazor</string>
  <key>CFBundleExecutable</key>
  <string>MinMe.Blazor</string>
  <key>CFBundleIdentifier</key>
  <string>com.your-company-name.www</string>
  <key>CFBundleName</key>
  <string>MinMe.Blazor</string>
  <key>CFBundleIconFile</key>
  <string>mac-app-icon.icns</string>
  <key>CFBundleShortVersionString</key>
  <string>0.01</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>IFMajorVersion</key>
  <integer>0</integer>
  <key>IFMinorVersion</key>
  <integer>1</integer>
  <key>NSHighResolutionCapable</key>
  <string>true</string>
</dict>
</plist>
EOM


#mkdir bin/MinMe.Blazor.app/Contents/Resources
#cp mac-app-icon.icns bin/MinMe.Blazor.app/Contents/Resources

mkdir bin/MinMe.Blazor.app/Contents/macOS
cp -R bin/Release/netcoreapp3.1/osx.10.15-x64/publish/* bin/MinMe.Blazor.app/Contents/macOS
