#!/bin/bash
# Builds SImulator.app (Apple Silicon or Intel) and packs it into a .dmg.
# Usage: ./build-dmg.sh [osx-arm64|osx-x64]
set -euo pipefail

RID="${1:-osx-arm64}"
HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../../.." && pwd)"
OUT="$REPO/bin/macos/$RID"
APP="$OUT/SImulator.app"
DOTNET="${DOTNET:-dotnet}"

VERSION="$(sed -n 's:.*<SImulatorVersion>\(.*\)</SImulatorVersion>.*:\1:p' "$REPO/Directory.Build.props")"

rm -rf "$OUT"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

"$DOTNET" publish "$HERE/SImulator.Mac.csproj" -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=false -p:UseAppHost=true -o "$APP/Contents/MacOS"

# Icon
ICONSET="$OUT/SImulator.iconset"
mkdir -p "$ICONSET"
for size in 16 32 64 128 256 512; do
    sips -z $size $size "$HERE/../SImulator/Images/logo.png" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
    sips -z $((size * 2)) $((size * 2)) "$HERE/../SImulator/Images/logo.png" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/SImulator.icns"
rm -rf "$ICONSET"

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>SImulator</string>
    <key>CFBundleDisplayName</key><string>SImulator</string>
    <key>CFBundleIdentifier</key><string>com.vladimirkhil.simulator</string>
    <key>CFBundleVersion</key><string>$VERSION</string>
    <key>CFBundleShortVersionString</key><string>$VERSION</string>
    <key>CFBundleExecutable</key><string>SImulator</string>
    <key>CFBundleIconFile</key><string>SImulator.icns</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>LSMinimumSystemVersion</key><string>12.0</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>NSLocalNetworkUsageDescription</key><string>SImulator uses the local network for players' web buttons.</string>
    <key>CFBundleDocumentTypes</key>
    <array>
        <dict>
            <key>CFBundleTypeName</key><string>SIGame package</string>
            <key>CFBundleTypeExtensions</key><array><string>siq</string></array>
            <key>CFBundleTypeRole</key><string>Viewer</string>
        </dict>
    </array>
</dict>
</plist>
PLIST

# Ad-hoc signature (no Apple Developer certificate)
codesign --force --deep --sign - "$APP"

DMG="$REPO/bin/macos/SImulator-$VERSION-$RID.dmg"
rm -f "$DMG"
STAGE="$OUT/dmg"
mkdir -p "$STAGE"
cp -R "$APP" "$STAGE/"
ln -s /Applications "$STAGE/Applications"
hdiutil create -volname "SImulator $VERSION" -srcfolder "$STAGE" -ov -format UDZO "$DMG" >/dev/null
rm -rf "$STAGE"

echo "Done: $DMG"
