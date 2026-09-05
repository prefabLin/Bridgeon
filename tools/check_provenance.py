#!/usr/bin/env python3
"""Everything in this repository is written for it.

Three rules, checked as an ALLOW-LIST rather than a filter:

  * no spreadsheet or database file — reference values belong in code,
    derived from published formulas, where they can be read and tested;
  * no imported datasets under a reserved directory;
  * every CJK string must be registered in tools/zh_vocabulary.txt, the
    vocabulary we author for Bridgeon's own interface.

An allow-list is the point. A filter only removes what somebody thought to look
for; requiring registration makes admitting content a deliberate act, and the
default answer no.
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
VOCAB_FILE = ROOT / "tools" / "zh_vocabulary.txt"

FORBIDDEN_SUFFIXES = {".xls", ".xlsx", ".xlsm", ".xlt", ".xltx", ".bws",
                      ".mdb", ".accdb", ".frm", ".bas", ".cls"}
IMPORTED_DATA_DIRS = {"data", "datasets", "fixtures", "imported", "vendor"}
SKIP_DIRS = {".git", "bin", "obj", "node_modules", "StrykerOutput", "__pycache__"}

# Built from codepoints rather than written literally, so this file does not
# trip its own check: CJK Unified Ideographs, Extension A, and Compatibility.
CJK = re.compile("[" + "".join(
    f"{chr(lo)}-{chr(hi)}" for lo, hi in
    ((0x3400, 0x4DBF), (0x4E00, 0x9FFF), (0xF900, 0xFAFF))) + "]+")


def load_vocabulary() -> set[str]:
    if not VOCAB_FILE.exists():
        return set()
    return {
        line.split("#", 1)[0].strip()
        for line in VOCAB_FILE.read_text(encoding="utf-8").splitlines()
        if line.split("#", 1)[0].strip()
    }


def walk():
    for path in sorted(ROOT.rglob("*")):
        if any(part in SKIP_DIRS for part in path.parts):
            continue
        if path.is_file():
            yield path


def main() -> int:
    vocabulary = load_vocabulary()
    problems: list[str] = []

    for path in walk():
        rel = path.relative_to(ROOT)

        if path.suffix.lower() in FORBIDDEN_SUFFIXES:
            problems.append(f"  {rel}  — a spreadsheet or database file; reference values belong in code")
            continue
        if any(part in IMPORTED_DATA_DIRS for part in rel.parts):
            problems.append(f"  {rel}  — under a directory reserved for imported data")
            continue
        if rel == pathlib.Path("tools/zh_vocabulary.txt"):
            continue

        try:
            text = path.read_text(encoding="utf-8")
        except (UnicodeDecodeError, OSError):
            continue

        for match in CJK.finditer(text):
            phrase = match.group(0)
            if phrase in vocabulary:
                continue
            line = text.count("\n", 0, match.start()) + 1
            problems.append(
                f"  {rel}:{line}  — unregistered CJK text {phrase!r}")

    if problems:
        seen: list[str] = []
        for p in problems:
            if p not in seen:
                seen.append(p)
        print(f"provenance check failed in {len(seen)} place(s):\n")
        print("\n".join(seen[:50]))
        if len(seen) > 50:
            print(f"  ... and {len(seen) - 50} more")
        print(f"""
Every CJK string must be one written for Bridgeon and registered in
{VOCAB_FILE.relative_to(ROOT)}. Reference values belong in code, derived from a
published formula. Remove anything else rather than registering it.""")
        return 1

    print(f"provenance ok: all content original, "
          f"{len(vocabulary)} registered CJK phrase(s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
