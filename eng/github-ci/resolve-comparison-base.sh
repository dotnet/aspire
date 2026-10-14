#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Print a comparison base only when complete history proves a reachable merge-base.
# Exit 1 means an unsafe/unresolved comparison (selector must run ALL); exit 2 is
# a fetch failure (infrastructure error). Diagnostics go to stderr, never stdout.
set -euo pipefail

base="${1:?Expected event base SHA}"
head="${2:?Expected PR head SHA}"

if [ "${GITHUB_EVENT_NAME:-}" = "pull_request" ]; then
    if [[ ! "${GITHUB_REF:-}" =~ ^refs/pull/[0-9]+/merge$ ]] ||
        [ "$(git --no-pager rev-parse HEAD)" != "${GITHUB_SHA:-}" ]; then
        echo "::warning::Cannot verify the checked-out PR merge identity; refusing a selective comparison." >&2
        exit 1
    fi

    # Read raw headers, not rev-list: a depth-1 checkout hides parents from the
    # revision walker but retains the commit's "parent <sha>" headers. GitHub's
    # merge commit must have exactly base first, then the event's real PR head.
    # The event base.sha can be stale after the PR merges newer base commits.
    parents=()
    while IFS= read -r parent; do
        parents+=("$parent")
    done < <(git --no-pager cat-file -p HEAD | sed -n '/^$/q;s/^parent //p')
    if [ "${#parents[@]}" -ne 2 ] || [ "${parents[1]}" != "$head" ]; then
        echo "::warning::Checked-out merge must have exactly two parents with PR head $head second; refusing a selective comparison." >&2
        exit 1
    fi
    base="${parents[0]}"
    echo "Using pinned PR comparison base $base from checked-out merge ${GITHUB_SHA} (event base ${1})." >&2
fi

# Make the pinned endpoints available without fetching a moving branch tip.
if ! git --no-pager cat-file -e "${base}^{commit}" 2>/dev/null ||
    ! git --no-pager cat-file -e "${head}^{commit}" 2>/dev/null; then
    if ! git --no-pager fetch --no-tags --depth=1 origin "$base" "$head" >&2; then
        echo "::error::Failed to fetch comparison base $base and head $head." >&2
        exit 2
    fi
fi
if ! git --no-pager cat-file -e "${base}^{commit}" 2>/dev/null ||
    ! git --no-pager cat-file -e "${head}^{commit}" 2>/dev/null; then
    echo "::error::Comparison base $base or head $head is unavailable after fetch." >&2
    exit 2
fi

# Shallow boundaries act as roots: short side branches can expose an older common
# ancestor while the true merge-base is still hidden. Finding any merge-base is
# therefore insufficient, even when git diff succeeds. Complete the history first.
# https://git-scm.com/docs/shallow
if [ "$(git --no-pager rev-parse --is-shallow-repository)" = "true" ]; then
    if ! git --no-pager fetch --no-tags --unshallow origin "$base" "$head" >&2; then
        echo "::error::Failed to complete comparison history for base $base and head $head." >&2
        exit 2
    fi
    if [ "$(git --no-pager rev-parse --is-shallow-repository)" = "true" ]; then
        echo "::warning::Comparison history remains shallow; refusing a selective comparison." >&2
        exit 1
    fi
fi
if ! merge_bases=$(git --no-pager merge-base --all "$base" "$head"); then
    echo "::warning::No merge-base of base $base and head $head exists in complete history; refusing a selective comparison." >&2
    exit 1
fi
# Criss-cross merges can have several equally good ancestors. Three-dot diff
# chooses only one, so it can omit changes relative to the other ancestors.
# https://git-scm.com/docs/git-merge-base#_discussion
if [[ "$merge_bases" = *$'\n'* ]]; then
    echo "::warning::Multiple merge-bases of base $base and head $head exist; refusing an ambiguous selective comparison." >&2
    exit 1
fi

printf '%s\n' "$base"
