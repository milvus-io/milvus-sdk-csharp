#!/usr/bin/env bash
# Licensed to the LF AI & Data foundation under one
# or more contributor license agreements. See the NOTICE file
# distributed with this work for additional information
# regarding copyright ownership. The ASF licenses this file
# to you under the Apache License, Version 2.0 (the
# "License"); you may not use this file except in compliance
# with the License. You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Runs the Milvus.Client.V2 test suite (Unit, Integration, System) using the xunit v3
# in-process runner. The `dotnet test` VSTest adapter is unreliable on *some* local hosts (it
# fails with "Test process did not return valid JSON (non-object)"); GitHub Actions CI uses
# `dotnet test` because it works reliably there and is needed for coverage collection, so this
# script exists as the dependable local-driver alternative.
#
# Usage:
#   scripts/run_tests.sh [--ut | --it | --st | --all]
#
#   --ut       run only Unit tests
#   --it       run only Integration tests
#   --st       run only System tests (starts a real Milvus container)
#   --all      run all three layers (Unit, Integration, System); the default when no flag is given
#   --help     show this usage

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TESTS_PROJECT="$ROOT_DIR/Milvus.Client.V2.Tests"
DLL="$TESTS_PROJECT/bin/Debug/net8.0/Milvus.Client.V2.Tests.dll"

LAYERS=()
if [[ $# -eq 0 ]]; then
  LAYERS=(Unit Integration System)
fi

while [[ $# -gt 0 ]]; do
  case "$1" in
    --ut) LAYERS+=(Unit) ;;
    --it) LAYERS+=(Integration) ;;
    --st) LAYERS+=(System) ;;
    --all) LAYERS=(Unit Integration System) ;;
    --help|-h)
      sed -n '2,40p' "${BASH_SOURCE[0]}" | sed 's/^# \?//'
      exit 0
      ;;
    *) echo "Unknown argument: $1 (see scripts/run_tests.sh --help)" >&2; exit 1 ;;
  esac
  shift
done

echo "==> Building $ROOT_DIR/Milvus.Client.V2"
# Build the SDK project first so its reference assembly (obj/.../ref) is produced; building only the test
# project through a ProjectReference transitively builds the SDK but does not emit the ref assembly, which
# makes the test compilation fail with CS0006 when the SDK's obj was cleaned.
dotnet build "$ROOT_DIR/Milvus.Client.V2/Milvus.Client.V2.csproj"

echo "==> Building $TESTS_PROJECT"
dotnet build "$TESTS_PROJECT/Milvus.Client.V2.Tests.csproj"

for layer in "${LAYERS[@]}"; do
  echo "==> Running $layer tests"
  # The System layer starts its own Milvus container (via milvus_container.py) and tears it down.
  dotnet exec "$DLL" -trait "Category=$layer"
done
