#!/usr/bin/env bash
set -euo pipefail

# Downloads the official Windscribe Windows x64 installer via windscribe.com redirect.
# Output: third_party/windscribe/Windscribe_<version>_amd64.exe

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${ROOT}/third_party/windscribe"
OFFICIAL_URL="https://windscribe.com/install/desktop/windows"

mkdir -p "${OUT_DIR}"
cd "${OUT_DIR}"

tmp="$(mktemp)"
trap 'rm -f "${tmp}"' EXIT

echo "Fetching redirect from ${OFFICIAL_URL}" >&2
curl -fsSL -D "${tmp}" -o Windscribe_installer.exe "${OFFICIAL_URL}"

# Final URL from curl -w would need -w '%{url_effective}' on a second request; rename from content-disposition is awkward.
# Keep a stable name pattern if possible by detecting version from redirect Location header.
location="$(awk -F': ' 'tolower($1)=="location"{print $2}' "${tmp}" | tr -d '\r' | tail -n1)"
if [[ -n "${location}" ]]; then
  base="$(basename "${location}")"
  if [[ "${base}" == *.exe ]]; then
    mv -f Windscribe_installer.exe "${base}"
    echo "Saved ${OUT_DIR}/${base}" >&2
    sha256sum "${base}" >&2
  else
    echo "Unexpected Location: ${location}" >&2
    mv -f Windscribe_installer.exe Windscribe_installer.exe
    echo "Saved ${OUT_DIR}/Windscribe_installer.exe" >&2
    sha256sum Windscribe_installer.exe >&2
  fi
else
  echo "Saved ${OUT_DIR}/Windscribe_installer.exe (no Location header)" >&2
  sha256sum Windscribe_installer.exe >&2
fi
