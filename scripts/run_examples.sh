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

# Builds and runs every example in examples/v2 against a Milvus server.
#
# Usage:
#   scripts/run_examples.sh [--list] [ExampleName ...]
#
#   no arguments     run every example (discovered from Program.cs), one by one
#   --list           print the available example names and exit
#   ExampleName ...  run only the named example(s)
#
# Env:
#   MILVUS_URI     Milvus gRPC endpoint (default localhost:19530)
#   MILVUS_TOKEN   Bearer token / username:password for authenticated servers

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT_DIR/examples/v2"

# Discover example names from the dispatch dictionary in Program.cs.
discover_examples() {
  sed -n 's/.*\[nameof(\([A-Za-z0-9]*\))\] = .*/\1/p' "$PROJECT/Program.cs"
}

if [[ "${1:-}" == "--list" ]]; then
  discover_examples
  exit 0
fi

echo "==> Building $ROOT_DIR/Milvus.Client.V2"
# Build the SDK project first so its reference assembly (obj/.../ref) is produced; building only the examples
# project through a ProjectReference transitively builds the SDK but does not emit the ref assembly, which
# makes the examples compilation fail with CS0006 when the SDK's obj was cleaned.
dotnet build "$ROOT_DIR/Milvus.Client.V2/Milvus.Client.V2.csproj"

echo "==> Building $PROJECT"
dotnet build "$PROJECT/Milvus.Examples.csproj"

examples=()
if [[ $# -gt 0 ]]; then
  examples=("$@")
else
  mapfile -t examples < <(discover_examples)
fi

if [[ ${#examples[@]} -eq 0 ]]; then
  echo "No examples to run." >&2
  exit 1
fi

status=0
for example in "${examples[@]}"; do
  echo "==> Running $example"
  if ! dotnet run --project "$PROJECT" --no-build -- "$example"; then
    echo "Example '$example' FAILED" >&2
    status=1
  fi
done

exit "$status"
