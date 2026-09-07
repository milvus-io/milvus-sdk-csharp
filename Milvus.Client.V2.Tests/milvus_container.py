#!/usr/bin/env python3
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

"""Start and stop the Linux Milvus container used by the C# SDK system tests.

The harness starts two containers: Milvus (embedded etcd) plus a MinIO sidecar.
The 3.0 server images cannot load segments under local storage (missing PK
bloom filter / json_stats meta.json), so the tests run against an
object-storage backend instead: COMMON_STORAGETYPE=remote with the default
`aws` cloud provider speaks the S3 API to the MinIO sidecar.

Usage:
    python3 milvus_container.py start    # Start containers, prints Milvus container ID
    python3 milvus_container.py stop <container_id>  # Stop Milvus and its MinIO sidecar
    python3 milvus_container.py logs <container_id>  # Print recent Milvus logs
"""

import argparse
import json
import signal
import subprocess
import sys
import time
import urllib.error
import urllib.request
import uuid


DEFAULT_IMAGE = "milvusdb/milvus:master-20260825-dd1ee671"
DEFAULT_GRPC_PORT = 29630
DEFAULT_HEALTH_PORT = 19191
DEFAULT_TIMEOUT = 300
REAP_LABEL = "milvus-sdk-csharp-test"
MINIO_IMAGE = "minio/minio:RELEASE.2024-12-18T13-15-44Z"
MINIO_PORT = 19100
MINIO_ACCESS_KEY = "minioadmin"
MINIO_SECRET_KEY = "minioadmin"
MINIO_BUCKET = "milvus-bucket"


def handle_termination(_signum: int, _frame: object) -> None:
    """Convert termination into an exception so startup cleanup still runs."""
    raise KeyboardInterrupt


def docker(*args: str, capture_output: bool = False) -> "subprocess.CompletedProcess[str]":
    """Run Docker and raise a useful error when the command fails."""
    try:
        return subprocess.run(
            ["docker", *args],
            check=True,
            stdout=subprocess.PIPE if capture_output else None,
            stderr=subprocess.PIPE if capture_output else None,
            universal_newlines=True,
        )
    except FileNotFoundError as error:
        raise RuntimeError("Docker is not installed or is not on PATH") from error
    except subprocess.CalledProcessError as error:
        detail = (error.stderr or error.stdout or "").strip()
        message = f"docker {' '.join(args)} failed"
        if detail:
            message = f"{message}: {detail}"
        raise RuntimeError(message) from error


def ensure_image(image: str) -> None:
    """Pull the image only when it is not already present."""
    result = subprocess.run(
        ["docker", "image", "inspect", image],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        universal_newlines=True,
    )
    if result.returncode != 0:
        print(f"Pulling {image}...", file=sys.stderr)
        docker("pull", image, capture_output=True)


def wait_for_milvus(grpc_port: int, health_port: int, timeout: int) -> None:
    """Wait for both the Milvus health endpoint and public API."""
    health_url = f"http://127.0.0.1:{health_port}/healthz"
    api_url = f"http://127.0.0.1:{grpc_port}/v2/vectordb/collections/list"
    payload = json.dumps({"dbName": "default"}).encode()
    deadline = time.monotonic() + timeout
    next_progress = time.monotonic() + 10

    print(f"Waiting for Milvus to become ready at {api_url}...", file=sys.stderr)
    while time.monotonic() < deadline:
        try:
            with urllib.request.urlopen(health_url, timeout=2) as response:
                healthy = 200 <= response.status < 300
            request = urllib.request.Request(
                api_url,
                data=payload,
                headers={
                    "Authorization": "Bearer root:Milvus",
                    "Content-Type": "application/json",
                },
                method="POST",
            )
            with urllib.request.urlopen(request, timeout=5) as response:
                ready = 200 <= response.status < 300
            if healthy and ready:
                print("Milvus is ready.", file=sys.stderr)
                return
        except (urllib.error.URLError, TimeoutError, OSError):
            pass

        now = time.monotonic()
        if now >= next_progress:
            remaining = max(0, int(deadline - now))
            print(f"Milvus is still starting ({remaining}s remaining)...", file=sys.stderr)
            next_progress = now + 10
        time.sleep(1)

    raise TimeoutError(f"timed out waiting for Milvus at {api_url}")


def print_container_logs(container_id: str) -> None:
    """Print recent Milvus logs before failed test cleanup."""
    print(f"Milvus container {container_id[:12]} logs:", file=sys.stderr)
    subprocess.run(
        ["docker", "logs", "--tail", "500", container_id],
        check=False,
        stdout=sys.stderr,
        stderr=sys.stderr,
    )


def remove_containers(*targets: str) -> bool:
    """Force-remove containers, ignoring targets that do not exist."""
    removed_any = False
    for target in targets:
        result = subprocess.run(
            ["docker", "rm", "--force", target],
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            universal_newlines=True,
        )
        if result.returncode == 0:
            removed_any = True
        elif "No such container" not in result.stderr:
            raise RuntimeError(result.stderr.strip() or f"failed to remove {target}")
    return removed_any


def stop_container(container_id: str) -> None:
    """Force-remove the Milvus test container and its MinIO sidecar."""
    names = [container_id]
    name_result = subprocess.run(
        ["docker", "inspect", container_id, "--format", "{{.Name}}"],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        universal_newlines=True,
    )
    if name_result.returncode == 0:
        base = name_result.stdout.strip().lstrip("/")
        if base:
            names = [base, f"{base}-minio"]

    if remove_containers(*names):
        print(f"Milvus test container {container_id[:12]} removed.", file=sys.stderr)


def cleanup_failed_start(*container_names: str) -> None:
    """Remove every container that may exist after a failed startup."""
    try:
        remove_containers(*container_names)
    except (OSError, RuntimeError) as error:
        targets = ", ".join(container_names)
        print(f"Warning: could not clean up {targets}: {error}", file=sys.stderr)


def reap_label(grpc_port: int, health_port: int) -> str:
    """Return the label identifying containers that occupy this run's ports."""
    return f"{REAP_LABEL}={grpc_port}-{health_port}"


def reap_stale_containers(grpc_port: int, health_port: int) -> None:
    """Remove non-running leftovers without disrupting another active test run."""
    result = docker(
        "ps",
        "--all",
        "--filter",
        f"label={reap_label(grpc_port, health_port)}",
        "--format",
        "{{.ID}} {{.State}}",
        capture_output=True,
    )
    containers = [line.split(None, 1) for line in result.stdout.splitlines() if line.strip()]
    if not containers:
        return

    running = [container_id for container_id, state in containers if state == "running"]
    if running:
        ids = ", ".join(container_id[:12] for container_id in running)
        raise RuntimeError(
            f"Milvus test container already owns ports {grpc_port}/{health_port} ({ids}); "
            "another test run may still be active"
        )

    container_ids = [container_id for container_id, _state in containers]
    print(f"Removing {len(container_ids)} stale Milvus test container(s).", file=sys.stderr)
    docker("rm", "--force", *container_ids, capture_output=True)


def start_container(
    image: str,
    grpc_port: int,
    health_port: int,
    minio_port: int,
    timeout: int,
) -> str:
    """Start Milvus (embedded etcd, MinIO object storage) and wait for readiness."""
    docker("info", capture_output=True)
    reap_stale_containers(grpc_port, health_port)
    ensure_image(image)
    ensure_image(MINIO_IMAGE)

    name = f"milvus-sdk-csharp-{uuid.uuid4().hex[:12]}"
    minio_name = f"{name}-minio"
    try:
        minio_result = docker(
            "run",
            "--detach",
            "--name",
            minio_name,
            "--label",
            reap_label(grpc_port, health_port),
            "--env",
            f"MINIO_ROOT_USER={MINIO_ACCESS_KEY}",
            "--env",
            f"MINIO_ROOT_PASSWORD={MINIO_SECRET_KEY}",
            "--publish",
            f"{minio_port}:9000",
            MINIO_IMAGE,
            "server",
            "/data",
            capture_output=True,
        )
        if not minio_result.stdout.strip():
            raise RuntimeError("docker run did not return a MinIO container ID")

        result = docker(
            "run",
            "--detach",
            "--name",
            name,
            "--security-opt",
            "seccomp=unconfined",
            "--add-host",
            "host.docker.internal:host-gateway",
            "--label",
            reap_label(grpc_port, health_port),
            "--env",
            "ETCD_USE_EMBED=true",
            "--env",
            "ETCD_DATA_DIR=/var/lib/milvus/etcd",
            "--env",
            "DEPLOY_MODE=STANDALONE",
            "--env",
            "COMMON_STORAGETYPE=remote",
            "--env",
            "COMMON_STORAGE_USELOONFFI=true",
            "--env",
            "DATACOORD_COMPACTION_BUMPSCHEMAVERSION_ENABLED=true",
            "--env",
            f"MINIO_ADDRESS=host.docker.internal:{minio_port}",
            "--env",
            f"MINIO_BUCKET_NAME={MINIO_BUCKET}",
            "--env",
            f"MINIO_ROOT_USER={MINIO_ACCESS_KEY}",
            "--env",
            f"MINIO_ROOT_PASSWORD={MINIO_SECRET_KEY}",
            "--publish",
            f"{grpc_port}:19530",
            "--publish",
            f"{health_port}:9091",
            image,
            "milvus",
            "run",
            "standalone",
            capture_output=True,
        )
        container_id = result.stdout.strip()
        if not container_id:
            raise RuntimeError("docker run did not return a container ID")
    except BaseException:
        cleanup_failed_start(name, minio_name)
        raise

    try:
        wait_for_milvus(grpc_port, health_port, timeout)
    except BaseException:
        print_container_logs(container_id)
        stop_container(container_id)
        raise

    return container_id


def main() -> int:
    signal.signal(signal.SIGTERM, handle_termination)
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command")

    start_parser = subparsers.add_parser("start", help="start a Milvus test container")
    start_parser.add_argument("--image", default=DEFAULT_IMAGE)
    start_parser.add_argument("--grpc-port", "--port", dest="grpc_port", type=int, default=DEFAULT_GRPC_PORT)
    start_parser.add_argument("--health-port", type=int, default=DEFAULT_HEALTH_PORT)
    start_parser.add_argument("--minio-port", type=int, default=MINIO_PORT)
    start_parser.add_argument("--timeout", type=int, default=DEFAULT_TIMEOUT)

    stop_parser = subparsers.add_parser("stop", help="remove a Milvus test container")
    stop_parser.add_argument("container_id")

    logs_parser = subparsers.add_parser("logs", help="print Milvus test container logs")
    logs_parser.add_argument("container_id")

    args = parser.parse_args()
    if args.command is None:
        parser.error("a command is required")
    try:
        if args.command == "start":
            container_id = start_container(
                args.image,
                args.grpc_port,
                args.health_port,
                args.minio_port,
                args.timeout,
            )
            print(container_id)
        elif args.command == "stop":
            stop_container(args.container_id)
        else:
            print_container_logs(args.container_id)
    except (RuntimeError, TimeoutError) as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1
    except KeyboardInterrupt:
        print("Milvus container operation interrupted.", file=sys.stderr)
        return 130
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
