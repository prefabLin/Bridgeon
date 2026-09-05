#!/usr/bin/env bash
# TDD gate: a `feat:` commit that touches a project must be preceded by a
# `test:` commit touching that project's suite.
set -euo pipefail

BASE="${1:-origin/main}"
if ! git rev-parse --verify --quiet "$BASE" >/dev/null; then
  BASE=$(git rev-list --max-parents=0 HEAD | tail -1)
fi

violations=0
while read -r sha subject; do
  [[ "$subject" =~ ^feat ]] || continue
  projects=$(git show --name-only --format= "$sha" | grep -oE '^src/[^/]+' | sort -u || true)
  [[ -n "$projects" ]] || continue
  for project in $projects; do
    suite="tests/$(basename "$project").Tests"
    if [[ -n "$(git log --format='%H' "$BASE..$sha^" -- "$suite" 2>/dev/null | head -1)" ]]; then
      echo "ok    ${sha:0:7} $subject"
    else
      echo "FAIL  ${sha:0:7} $subject"
      echo "      touches $project with no earlier test: commit on $suite"
      violations=$((violations + 1))
    fi
  done
done < <(git log --format='%H %s' "$BASE..HEAD")

(( violations == 0 )) || { echo; echo "$violations violation(s)."; exit 1; }
echo "commit order: clean"
