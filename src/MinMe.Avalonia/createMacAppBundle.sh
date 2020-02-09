dotnet publish -c Release --self-contained -r osx.10.15-x64

rm -rf bin/MinMe.Avalonia.app
mkdir bin/MinMe.Avalonia.app

mkdir bin/MinMe.Avalonia.app/Contents
cp Assets/Info.plist bin/MinMe.Avalonia.app/Contents/

mkdir bin/MinMe.Avalonia.app/Contents/Resources
cp Assets/AppIcon.icns bin/MinMe.Avalonia.app/Contents/Resources

mkdir bin/MinMe.Avalonia.app/Contents/macOS
cp -R bin/Release/netcoreapp3.1/osx.10.15-x64/publish/* bin/MinMe.Avalonia.app/Contents/macOS
