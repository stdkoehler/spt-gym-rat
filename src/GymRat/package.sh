#!/usr/bin/env bash
# Runs the tests, builds the mod in Release and zips it as a ready-to-extract release archive
# with BepInEx/ and user/ at the root, so it can be extracted straight into an SPT root install.
set -euo pipefail

# Ensure dotnet is on PATH in Windows Git Bash and WSL environments.
export PATH="/c/Program Files/dotnet:/mnt/c/Program Files/dotnet:$PATH"

cd "$(dirname "${BASH_SOURCE[0]}")"

if [ -t 1 ] || [ -t 2 ]; then
    RED='\033[1;31m'; GREEN='\033[1;32m'; CYAN='\033[1;36m'; NC='\033[0m'
else
    RED=''; GREEN=''; CYAN=''; NC=''
fi

fail() { echo -e "${RED}ERROR: $*${NC}" >&2; exit 1; }

MOD_NAME=GymRat
TEST_DIR="../../tests/GymRat.Tests"
VERSION=$(sed -n 's/.*SemanticVersioning\.Version Version { get; init; } = new("\(.*\)").*/\1/p' "${MOD_NAME}Metadata.cs")
[ -n "$VERSION" ] || fail "Could not read version from ${MOD_NAME}Metadata.cs"

echo -e "${CYAN}==> Packaging ${MOD_NAME} v${VERSION}${NC}"

echo -e "${CYAN}==> Running tests (${TEST_DIR})...${NC}"
[ -d "$TEST_DIR" ] || fail "Test directory not found: ${TEST_DIR}"
(cd "$TEST_DIR" && dotnet run) || fail "Test suite failed. Packaging aborted."
echo -e "${GREEN}All tests passed.${NC}"

echo -e "${CYAN}==> Building ${MOD_NAME} (Release)...${NC}"
dotnet build -c Release || fail "Release build failed. Packaging aborted."

echo -e "${CYAN}==> Building ${MOD_NAME}.Client (Release)...${NC}"
dotnet build ../GymRat.Client/GymRat.Client.csproj -c Release || fail "Client build failed. Packaging aborted."

PACKAGE_DIR=Package
STAGE_ROOT="${PACKAGE_DIR}/staging"
STAGE_DIR="${STAGE_ROOT}/SPT_Runtime/user/mods/${MOD_NAME}"
PLUGIN_STAGE_DIR="${STAGE_ROOT}/BepInEx/plugins"
ARCHIVE="${PACKAGE_DIR}/${MOD_NAME}-${VERSION}.zip"

echo -e "${CYAN}==> Creating release archive (${ARCHIVE})...${NC}"
rm -rf "${STAGE_ROOT}" "${PACKAGE_DIR}/SPT_Runtime"
mkdir -p "${STAGE_DIR}"
mkdir -p "${PLUGIN_STAGE_DIR}"
cp -r "bin/Release/${MOD_NAME}/." "${STAGE_DIR}/"
cp "../GymRat.Client/bin/Release/GymRatClient.dll" "${PLUGIN_STAGE_DIR}/"

rm -f "${ARCHIVE}"
if command -v zip >/dev/null 2>&1; then
    (cd "${STAGE_ROOT}" && zip -rq "../$(basename "${ARCHIVE}")" BepInEx SPT_Runtime)
else
    # Git Bash does not always ship zip; fall back to PowerShell.
    powershell.exe -NoProfile -Command \
        "Compress-Archive -Path '${STAGE_ROOT}/BepInEx', '${STAGE_ROOT}/SPT_Runtime' -DestinationPath '${ARCHIVE}' -Force"
fi
rm -rf "${STAGE_ROOT}"

echo -e "${GREEN}==> Packaged ${MOD_NAME} v${VERSION}: ${ARCHIVE}${NC}"
