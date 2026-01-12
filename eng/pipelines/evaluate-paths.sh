#!/usr/bin/env bash
# evaluate-paths.sh - Determines which test categories should run based on changed files
#
# Usage:
#   evaluate-paths.sh --config <config.yml> --base <sha> --head <sha>
#   evaluate-paths.sh --config <config.yml> --test-files "file1 file2 ..."
#   evaluate-paths.sh --dry-run [...]
#
# Outputs (to $GITHUB_OUTPUT or stdout in dry-run mode):
#   run_templates=true/false
#   run_cli_e2e=true/false
#   run_endtoend=true/false
#   run_integrations=true/false
#   run_extension=true/false

set -e

# Defaults
CONFIG_FILE=""
BASE_SHA=""
HEAD_SHA=""
DRY_RUN=false
TEST_FILES=""
TEST_MODE=false
VERBOSE=false

# Category tracking (portable, no associative arrays)
RUN_TEMPLATES=false
RUN_CLI_E2E=false
RUN_ENDTOEND=false
RUN_INTEGRATIONS=false
RUN_EXTENSION=false
MATCHED_FILES=""

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --config)
            CONFIG_FILE="$2"
            shift 2
            ;;
        --base)
            BASE_SHA="$2"
            shift 2
            ;;
        --head)
            HEAD_SHA="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --test-files)
            TEST_FILES="$2"
            TEST_MODE=true
            shift 2
            ;;
        --verbose|-v)
            VERBOSE=true
            shift
            ;;
        *)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
    esac
done

# Logging functions
log_info() {
    echo "=== $1 ==="
}

log_detail() {
    echo "  $1"
}

log_verbose() {
    if [ "$VERBOSE" = true ]; then
        echo "    $1"
    fi
}

# Output function - writes to GITHUB_OUTPUT or stdout
output() {
    local key="$1"
    local value="$2"

    if [ "$DRY_RUN" = true ]; then
        echo "$key=$value"
    elif [ -n "$GITHUB_OUTPUT" ]; then
        echo "$key=$value" >> "$GITHUB_OUTPUT"
    else
        echo "$key=$value"
    fi
}

# Convert glob pattern to extended regex for grep
# Handles: ** (any path), * (any segment), ? (single char)
glob_to_regex() {
    local pattern="$1"
    local regex=""
    local i=0
    local len=${#pattern}

    while [ $i -lt $len ]; do
        local char="${pattern:$i:1}"
        local next_char="${pattern:$((i+1)):1}"

        case "$char" in
            '*')
                if [ "$next_char" = "*" ]; then
                    # ** matches any path (including /)
                    regex+=".*"
                    i=$((i + 1))
                else
                    # * matches any segment (not including /)
                    regex+="[^/]*"
                fi
                ;;
            '?')
                # ? matches single char
                regex+="."
                ;;
            '.'|'['|']'|'^'|'$'|'('|')'|'{'|'}'|'|'|'+')
                # Escape special regex chars
                regex+="\\$char"
                ;;
            *)
                regex+="$char"
                ;;
        esac
        i=$((i + 1))
    done

    # Anchor the pattern
    echo "^${regex}\$"
}

# Check if a file matches a glob pattern
matches_glob() {
    local file="$1"
    local pattern="$2"
    local regex

    regex=$(glob_to_regex "$pattern")

    if echo "$file" | grep -qE "$regex"; then
        return 0
    fi
    return 1
}

# Parse YAML config using shell tools (yq-independent)
# Returns patterns line by line
parse_yaml_list() {
    local file="$1"
    local path="$2"  # e.g., "fallback" or "categories.templates.include"

    local in_section=false
    local section_depth=0
    local current_path=""
    local target_depth=0

    # Calculate target depth from path
    case "$path" in
        fallback)
            target_depth=0
            ;;
        categories.*.include|categories.*.exclude)
            target_depth=4
            ;;
    esac

    while IFS= read -r line || [ -n "$line" ]; do
        # Skip comments and empty lines
        [[ "$line" =~ ^[[:space:]]*# ]] && continue
        [[ -z "${line// }" ]] && continue

        # Count leading spaces
        local stripped="${line#"${line%%[![:space:]]*}"}"
        local spaces=$((${#line} - ${#stripped}))

        # Check for section headers (key:)
        if [[ "$stripped" =~ ^([a-zA-Z_][a-zA-Z0-9_]*):(.*)$ ]]; then
            local key="${BASH_REMATCH[1]}"

            # Calculate depth (2 spaces per level)
            local depth=$((spaces / 2))

            # Build current path based on depth
            if [ $depth -eq 0 ]; then
                current_path="$key"
            elif [ $depth -eq 1 ]; then
                current_path="${current_path%%.*}.$key"
            elif [ $depth -eq 2 ]; then
                local first_part="${current_path%%.*}"
                local second_part="${current_path#*.}"
                second_part="${second_part%%.*}"
                current_path="${first_part}.${second_part}.$key"
            fi

            # Check if this is our target section
            if [ "$current_path" = "$path" ]; then
                in_section=true
                section_depth=$spaces
                continue
            else
                # If we were in section and now at same or lower depth, we're done
                if [ "$in_section" = true ] && [ $spaces -le $section_depth ]; then
                    in_section=false
                fi
            fi
        fi

        # If in our target section, extract list items
        if [ "$in_section" = true ]; then
            if [[ "$stripped" =~ ^-[[:space:]]*(.+)$ ]]; then
                local item="${BASH_REMATCH[1]}"
                # Remove quotes if present
                item="${item#\"}"
                item="${item%\"}"
                item="${item#\'}"
                item="${item%\'}"
                echo "$item"
            elif [[ "$stripped" =~ ^[a-zA-Z_] ]] && [[ ! "$stripped" =~ ^- ]]; then
                # Found a new key at same or lower depth, we're done
                local item_spaces=$((${#line} - ${#stripped}))
                if [ $item_spaces -le $section_depth ]; then
                    in_section=false
                fi
            fi
        fi
    done < "$file"
}

# Get all category names from config
get_categories() {
    local file="$1"

    local in_categories=false

    while IFS= read -r line || [ -n "$line" ]; do
        [[ "$line" =~ ^[[:space:]]*# ]] && continue
        [[ -z "${line// }" ]] && continue

        local stripped="${line#"${line%%[![:space:]]*}"}"
        local spaces=$((${#line} - ${#stripped}))

        if [[ "$stripped" = "categories:" ]]; then
            in_categories=true
            continue
        fi

        if [ "$in_categories" = true ]; then
            if [ $spaces -eq 0 ]; then
                # End of categories section
                break
            elif [ $spaces -eq 2 ]; then
                # Category name
                if [[ "$stripped" =~ ^([a-zA-Z_][a-zA-Z0-9_]*):$ ]]; then
                    echo "${BASH_REMATCH[1]}"
                fi
            fi
        fi
    done < "$file"
}

# Mark a category as needing to run
mark_category() {
    local category="$1"
    case "$category" in
        templates) RUN_TEMPLATES=true ;;
        cli_e2e) RUN_CLI_E2E=true ;;
        endtoend) RUN_ENDTOEND=true ;;
        integrations) RUN_INTEGRATIONS=true ;;
        extension) RUN_EXTENSION=true ;;
    esac
}

# Check if a file has been matched
is_file_matched() {
    local file="$1"
    echo "$MATCHED_FILES" | grep -qxF "$file"
}

# Mark a file as matched
mark_file_matched() {
    local file="$1"
    MATCHED_FILES="$MATCHED_FILES"$'\n'"$file"
}

# Main logic

# Validate config file
if [ -z "$CONFIG_FILE" ]; then
    echo "Error: --config is required" >&2
    exit 1
fi

if [ ! -f "$CONFIG_FILE" ]; then
    echo "Error: Config file not found: $CONFIG_FILE" >&2
    exit 1
fi

log_info "Conditional Test Execution"
log_detail "Config: $CONFIG_FILE"

# Get changed files
if [ "$TEST_MODE" = true ]; then
    # Test mode - use provided files (may be empty)
    CHANGED_FILES=$(echo "$TEST_FILES" | tr ' ' '\n' | grep -v '^$' || true)
    log_detail "Mode: test files"
elif [ -n "$BASE_SHA" ] && [ -n "$HEAD_SHA" ]; then
    # Git diff mode
    log_detail "Base: $BASE_SHA"
    log_detail "Head: $HEAD_SHA"
    CHANGED_FILES=$(git diff --name-only "$BASE_SHA".."$HEAD_SHA" 2>/dev/null || echo "")
else
    # Default - uncommitted changes
    log_detail "Mode: uncommitted changes"
    CHANGED_FILES=$(git diff --name-only 2>/dev/null || echo "")
fi

# Count and display changed files
if [ -z "$CHANGED_FILES" ]; then
    log_info "Changed Files (0)"
    log_detail "No files changed"

    # No changes = nothing to run
    output "run_templates" "false"
    output "run_cli_e2e" "false"
    output "run_endtoend" "false"
    output "run_integrations" "false"
    output "run_extension" "false"
    exit 0
fi

FILE_COUNT=$(echo "$CHANGED_FILES" | wc -l | tr -d ' ')
log_info "Changed Files ($FILE_COUNT)"
echo "$CHANGED_FILES" | head -20 | while read -r f; do
    log_detail "$f"
done
if [ "$FILE_COUNT" -gt 20 ]; then
    log_detail "... and $((FILE_COUNT - 20)) more"
fi
echo ""

# Check fallback patterns
log_info "Checking Fallback Patterns"
FALLBACK_TRIGGERED=false

FALLBACK_PATTERNS=$(parse_yaml_list "$CONFIG_FILE" "fallback")

while IFS= read -r pattern || [ -n "$pattern" ]; do
    [ -z "$pattern" ] && continue

    log_verbose "Checking: $pattern"

    while IFS= read -r file || [ -n "$file" ]; do
        [ -z "$file" ] && continue

        if matches_glob "$file" "$pattern"; then
            log_detail "MATCH: $file -> $pattern"
            FALLBACK_TRIGGERED=true
            break 2
        fi
    done <<< "$CHANGED_FILES"
done <<< "$FALLBACK_PATTERNS"

if [ "$FALLBACK_TRIGGERED" = true ]; then
    log_detail "Result: Fallback triggered - running ALL tests"
    echo ""

    log_info "Summary"
    output "run_templates" "true"
    output "run_cli_e2e" "true"
    output "run_endtoend" "true"
    output "run_integrations" "true"
    output "run_extension" "true"

    log_detail "run_templates=true"
    log_detail "run_cli_e2e=true"
    log_detail "run_endtoend=true"
    log_detail "run_integrations=true"
    log_detail "run_extension=true"
    exit 0
fi

log_detail "Result: No fallback triggered"
echo ""

# Evaluate each category
log_info "Evaluating Categories"

CATEGORIES=$(get_categories "$CONFIG_FILE")

# For each file, find which categories match
while IFS= read -r file || [ -n "$file" ]; do
    [ -z "$file" ] && continue

    while IFS= read -r category || [ -n "$category" ]; do
        [ -z "$category" ] && continue

        matches_include=false
        matches_exclude=false

        # Check include patterns
        INCLUDE_PATTERNS=$(parse_yaml_list "$CONFIG_FILE" "categories.$category.include")
        while IFS= read -r pattern || [ -n "$pattern" ]; do
            [ -z "$pattern" ] && continue

            if matches_glob "$file" "$pattern"; then
                matches_include=true
                break
            fi
        done <<< "$INCLUDE_PATTERNS"

        if [ "$matches_include" = true ]; then
            # Check exclude patterns
            EXCLUDE_PATTERNS=$(parse_yaml_list "$CONFIG_FILE" "categories.$category.exclude")
            while IFS= read -r pattern || [ -n "$pattern" ]; do
                [ -z "$pattern" ] && continue

                if matches_glob "$file" "$pattern"; then
                    matches_exclude=true
                    break
                fi
            done <<< "$EXCLUDE_PATTERNS"

            if [ "$matches_exclude" = false ]; then
                log_detail "[$category] Matched: $file"
                mark_category "$category"
                mark_file_matched "$file"
            else
                log_verbose "[$category] Excluded: $file"
            fi
        fi
    done <<< "$CATEGORIES"
done <<< "$CHANGED_FILES"

echo ""

# Check for unmatched files (conservative fallback)
log_info "Unmatched Files Check"
HAS_UNMATCHED=false

while IFS= read -r file || [ -n "$file" ]; do
    [ -z "$file" ] && continue

    if ! is_file_matched "$file"; then
        log_detail "Unmatched: $file"
        HAS_UNMATCHED=true
    fi
done <<< "$CHANGED_FILES"

if [ "$HAS_UNMATCHED" = true ]; then
    log_detail "Result: Unmatched files found - running ALL tests (conservative)"
    echo ""

    log_info "Summary"
    output "run_templates" "true"
    output "run_cli_e2e" "true"
    output "run_endtoend" "true"
    output "run_integrations" "true"
    output "run_extension" "true"

    log_detail "run_templates=true"
    log_detail "run_cli_e2e=true"
    log_detail "run_endtoend=true"
    log_detail "run_integrations=true"
    log_detail "run_extension=true"
    exit 0
fi

log_detail "Result: All files matched at least one category"
echo ""

# Output results
log_info "Summary"

output "run_templates" "$RUN_TEMPLATES"
output "run_cli_e2e" "$RUN_CLI_E2E"
output "run_endtoend" "$RUN_ENDTOEND"
output "run_integrations" "$RUN_INTEGRATIONS"
output "run_extension" "$RUN_EXTENSION"

log_detail "run_templates=$RUN_TEMPLATES"
log_detail "run_cli_e2e=$RUN_CLI_E2E"
log_detail "run_endtoend=$RUN_ENDTOEND"
log_detail "run_integrations=$RUN_INTEGRATIONS"
log_detail "run_extension=$RUN_EXTENSION"
