#!/bin/bash
set -euo pipefail
# Usage: bash export_iphone.sh /path/to/staged-project registered.bundle.id TEAMID1234 [0.1.0] [1]
if [[ $# -lt 3 || $# -gt 5 ]]; then
  echo 'Usage: bash export_iphone.sh PROJECT BUNDLE_ID TEAM_ID [VERSION] [BUILD]' >&2
  exit 2
fi
project="$(cd "$1" && pwd)"
[[ -f "$project/.testflight-staging.json" ]] || { echo 'Not a staged TestFlight copy' >&2; exit 2; }
unity="${MECH_UNITY_EDITOR:-/Applications/Unity/Hub/Editor/6000.3.18f1/Unity.app/Contents/MacOS/Unity}"
[[ -x "$unity" ]] || { echo "Install Unity 6000.3.18f1 with iOS Build Support: $unity" >&2; exit 2; }
[[ "$(uname -s)" == Darwin ]] || { echo 'Run this script on macOS' >&2; exit 2; }
xcode_version="$(xcodebuild -version | awk '/^Xcode / {print $2}')"
sdk_version="$(xcrun --sdk iphoneos --show-sdk-version)"
[[ "${xcode_version%%.*}" -ge 26 && "${sdk_version%%.*}" -ge 26 ]] || {
  echo 'Use a supported release Xcode 26+ with iOS SDK 26+ for App Store Connect' >&2; exit 2;
}
export MECH_APPLE_BUNDLE_ID="$2" MECH_APPLE_TEAM_ID="$3"
export MECH_APPLE_VERSION="${4:-0.1.0}" MECH_APPLE_BUILD="${5:-1}"
mkdir -p "$project/Logs"
log="$project/Logs/testflight-${MECH_APPLE_VERSION}-${MECH_APPLE_BUILD}.log"
"$unity" -batchmode -quit -projectPath "$project" -buildTarget iOS \
  -executeMethod TestFlightBuild.Export -logFile "$log"
output="$project/Builds/iPhone/${MECH_APPLE_VERSION}-${MECH_APPLE_BUILD}"
[[ -d "$output/Unity-iPhone.xcodeproj" ]] || { echo "Export missing; inspect $log" >&2; exit 1; }
echo "Xcode project ready: $output/Unity-iPhone.xcodeproj"
echo 'Next: real iPhone test, icon/privacy validation, Xcode Archive and Validate App.'
