"""Delete a Windows 'nul' reserved-name file from the repo root.

Windows treats 'nul' as a device name, so normal deletion fails.
This script uses the Win32 MoveFileW API with the \\?\ prefix to
rename the file first, then deletes the renamed file normally.

Usage: python delete-windows-nul.py
"""

import ctypes
import os
import sys
from ctypes import wintypes

REPO_ROOT = os.path.dirname(os.path.abspath(__file__))
NUL_PATH = os.path.join(REPO_ROOT, "nul")
TEMP_NAME = "nul_to_delete"
TEMP_PATH = os.path.join(REPO_ROOT, TEMP_NAME)


def make_unc(path: str) -> str:
    """Convert a path to \\\\?\\ extended-length format."""
    return "\\\\?\\" + os.path.abspath(path)


def main() -> None:
    if not os.path.exists(REPO_ROOT):
        print(f"Repo root not found: {REPO_ROOT}")
        sys.exit(1)

    # Check if git sees the nul file as untracked
    import subprocess

    result = subprocess.run(
        ["git", "status", "--short"],
        capture_output=True,
        text=True,
        cwd=REPO_ROOT,
    )
    if "nul" not in result.stdout:
        print("No 'nul' file found in git status. Nothing to do.")
        return

    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    kernel32.MoveFileW.argtypes = [wintypes.LPCWSTR, wintypes.LPCWSTR]
    kernel32.MoveFileW.restype = wintypes.BOOL

    src = make_unc(NUL_PATH)
    dst = make_unc(TEMP_PATH)

    ok = kernel32.MoveFileW(src, dst)
    if not ok:
        err = ctypes.get_last_error()
        print(f"MoveFileW failed with error {err}")
        sys.exit(1)

    os.remove(TEMP_PATH)
    print("Deleted 'nul' file successfully.")


if __name__ == "__main__":
    main()
