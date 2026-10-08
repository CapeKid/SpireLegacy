#!/usr/bin/env bash
set -euo pipefail
package_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
reader_python="$package_dir/ReaderLinux/bin/python3.12"
if [[ ! -f "$reader_python" ]]; then
  echo 'Linux reader runtime is missing. Extract the Steam Deck/Linux package first.' >&2
  exit 1
fi
if [[ ! -x "$reader_python" ]]; then chmod u+x -- "$reader_python"; fi
export PYTHONPATH="$package_dir/ReaderLinux/site"
exec "$reader_python" "$package_dir/launch_linux.py" "$@"
