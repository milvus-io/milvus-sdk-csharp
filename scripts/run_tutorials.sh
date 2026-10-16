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

# Builds and runs every tutorial in tutorial/ against a disposable Milvus container.
#
# Usage:
#   scripts/run_tutorials.sh [--list]
#
#   --list   print the available tutorial names and exit
#   (none)   start Milvus, run every tutorial 1_quickstart through 8_rbac, then stop Milvus
#
# Env:
#   MILVUS_URI        Milvus gRPC endpoint for the tutorial client (default localhost:<grpc-port>)
#   MILVUS_TOKEN      Bearer token / username:password for authenticated servers
#   MILVUS_GRPC_PORT  Milvus gRPC port mapped to the host (default 29530)
#   MILVUS_HEALTH_PORT  Milvus health-check port mapped to the host (default 29091)

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MILVUS_CONTAINER_SCRIPT="$ROOT_DIR/Milvus.Client.V2.Tests/milvus_container.py"
MILVUS_GRPC_PORT="${MILVUS_GRPC_PORT:-29530}"
MILVUS_HEALTH_PORT="${MILVUS_HEALTH_PORT:-29091}"
export MILVUS_URI="${MILVUS_URI:-localhost:$MILVUS_GRPC_PORT}"
MILVUS_CONTAINER_ID=""

if [[ "${1:-}" == "--list" ]]; then
  ls -1 "$ROOT_DIR"/tutorial/*/README.md 2>/dev/null | sed 's#.*/tutorial/##; s#/README.md##'
  exit 0
fi

if [[ "$#" -ne 0 ]]; then
  echo "Usage: $0 [--list]" >&2
  exit 1
fi

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "Tutorial server automation is supported on Linux only." >&2
  exit 1
fi

for tool in docker python3 dotnet; do
  if ! command -v "$tool" >/dev/null 2>&1; then
    echo "$tool is required to run the tutorials." >&2
    exit 1
  fi
done

cleanup_resources() {
  local status="$1"
  if [[ -n "$MILVUS_CONTAINER_ID" ]]; then
    if [[ "$status" -ne 0 ]]; then
      python3 -B "$MILVUS_CONTAINER_SCRIPT" logs "$MILVUS_CONTAINER_ID" || true
    fi
    python3 -B "$MILVUS_CONTAINER_SCRIPT" stop "$MILVUS_CONTAINER_ID" || true
    MILVUS_CONTAINER_ID=""
  fi
}

handle_exit() {
  local status=$?
  trap - EXIT INT TERM
  cleanup_resources "$status"
  exit "$status"
}

handle_signal() {
  local status="$1"
  trap - EXIT INT TERM
  cleanup_resources "$status"
  exit "$status"
}

trap handle_exit EXIT
trap 'handle_signal 130' INT
trap 'handle_signal 143' TERM

echo "==> Starting Milvus for tutorials"
MILVUS_CONTAINER_ID="$(
  python3 -B "$MILVUS_CONTAINER_SCRIPT" start \
    --grpc-port "$MILVUS_GRPC_PORT" \
    --health-port "$MILVUS_HEALTH_PORT"
)"

cd "$ROOT_DIR"
echo "==> Running beginner tutorials (1_quickstart through 6_dql)"
for tutorial_dir in tutorial/*/; do
  tutorial_name="${tutorial_dir#tutorial/}"
  tutorial_name="${tutorial_name%/}"
  project="$ROOT_DIR/$tutorial_dir$tutorial_name.csproj"
  if [[ ! -f "$project" ]]; then
    continue
  fi
  if [[ "$tutorial_name" == "7_database" ]]; then
    echo "==> Running advanced tutorials (database administration and RBAC)"
  fi
  if [[ "$tutorial_name" == "8_rbac" ]]; then
    echo "==> RBAC note: the default tutorial server does not enable authorization; RBAC calls may run, but permission-denial behavior is not validated."
  fi
  # Separate each tutorial's output from the previous one with a blank line.
  echo
  echo
  echo "==> Running $tutorial_dir"
  # Build from a clean output: a stale genruntimeconfig.cache can leave bin without the tutorial's
  # runtimeconfig.json, which makes `dotnet run` fail with "library 'libhostpolicy.so' was not found".
  # rm -rf bin/obj forces the SDK to re-emit runtimeconfig.json and deps.json.
  rm -rf "$ROOT_DIR/$tutorial_dir/bin" "$ROOT_DIR/$tutorial_dir/obj"
  dotnet run --project "$project"
done
