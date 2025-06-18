#!/bin/bash

# Script to find all packages that need to be mirrored to internal feeds
# This works by iteratively adding nuget.org as a source for missing packages
# until restore succeeds completely

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting iterative missing package detection...${NC}"

# Create backup of original NuGet.config
cp ../NuGet.config ../NuGet.config.backup
echo -e "${GREEN}Created backup of NuGet.config${NC}"

# Function to restore original config
cleanup() {
    echo -e "${YELLOW}Restoring original NuGet.config...${NC}"
    mv ../NuGet.config.backup ../NuGet.config 2>/dev/null || true
    echo -e "${GREEN}Cleanup completed${NC}"
}

# Set trap to cleanup on exit
trap cleanup EXIT

# Initialize arrays to track missing packages
declare -a missing_packages=()
declare -a missing_versions=()

# Function to extract package name and version from error message
extract_package_from_error() {
    local error_line="$1"
    # Try different patterns to extract package names and versions from various error formats
    local package_name=""
    local package_version=""
    
    # Pattern 1: "Unable to find package 'PackageName'"
    if [[ $error_line =~ Unable\ to\ find\ package\ \'([^\']+)\' ]]; then
        package_name="${BASH_REMATCH[1]}"
    # Pattern 2: "Package 'PackageName' is not found"
    elif [[ $error_line =~ Package\ \'([^\']+)\'\ is\ not\ found ]]; then
        package_name="${BASH_REMATCH[1]}"
    # Pattern 3: "error NU1101: Unable to find package PackageName"
    elif [[ $error_line =~ NU1101:.*Unable\ to\ find\ package\ ([^\ ]+) ]]; then
        package_name="${BASH_REMATCH[1]}"
    # Pattern 4: "error NU1102: Unable to find package PackageName with version"
    elif [[ $error_line =~ NU1102:\ Unable\ to\ find\ package\ ([^\ ]+)\ with\ version ]]; then
        package_name="${BASH_REMATCH[1]}"
        # Extract version from the same line using grep
        package_version=$(echo "$error_line" | grep -oE '\([>=]+\s+[^)]+\)' | sed 's/[()>=]//g' | tr -d ' ')
        if [ -z "$package_version" ]; then
            package_version=$(echo "$error_line" | grep -oE '[0-9]+\.[0-9]+[0-9a-zA-Z.-]*' | head -1)
        fi
    # Pattern 5: "The given key 'PackageName' was not present in the dictionary"
    elif [[ $error_line =~ The\ given\ key\ \'([^\']+)\'\ was\ not\ present\ in\ the\ dictionary ]]; then
        package_name="${BASH_REMATCH[1]}"
    # Pattern 6: More generic pattern for package names in error messages
    elif [[ $error_line =~ ([A-Za-z0-9][A-Za-z0-9._-]*[A-Za-z0-9])\ [0-9]+\.[0-9]+ ]]; then
        package_name=$(echo "$error_line" | grep -oE '[A-Za-z0-9][A-Za-z0-9._-]*[A-Za-z0-9]\ [0-9]+\.[0-9]+[0-9a-zA-Z.-]*' | head -1 | cut -d' ' -f1)
    fi
    
    # Return both package name and version (separated by a tab)
    if [ -n "$package_name" ]; then
        if [ -n "$package_version" ]; then
            echo -e "$package_name\t$package_version"
        else
            echo -e "$package_name\t"
        fi
    fi
}

# Function to add package to nuget.org source mapping
add_package_to_nuget_mapping() {
    local package_name="$1"
    echo -e "${YELLOW}Adding package '$package_name' to nuget.org source mapping...${NC}"
    
    # Use xmlstarlet or sed to modify the NuGet.config
    # First, check if nuget.org source exists, if not add it
    if ! grep -q 'key="nuget.org"' ../NuGet.config; then
        echo -e "${YELLOW}Adding nuget.org as a package source...${NC}"
        ../dotnet.sh nuget add source https://api.nuget.org/v3/index.json --name nuget.org
    fi
    
    # Add the package pattern to nuget.org source mapping
    # We'll use a simple sed approach to add the package pattern
    if grep -q '<packageSource key="nuget.org">' ../NuGet.config; then
        # If nuget.org mapping exists, add the package to it
        sed -i "/<packageSource key=\"nuget.org\">/a\\      <package pattern=\"$package_name\" />" ../NuGet.config
    else
        # If nuget.org mapping doesn't exist, create it
        sed -i "/<\/packageSourceMapping>/i\\    <packageSource key=\"nuget.org\">\\      <package pattern=\"$package_name\" />\\    </packageSource>" ../NuGet.config
    fi
    
    echo -e "${GREEN}Added '$package_name' to nuget.org source mapping${NC}"
}

# Clear package cache to start fresh
echo -e "${YELLOW}Clearing NuGet cache...${NC}"
../dotnet.sh nuget locals all --clear

# Main loop: keep trying restore until it succeeds
iteration=1
max_iterations=50  # Safety limit

echo -e "${GREEN}Starting iterative restore process...${NC}"
echo "================================================================="

while [ $iteration -le $max_iterations ]; do
    echo -e "${YELLOW}Iteration $iteration: Attempting restore...${NC}"
    
    # Try restore and capture output
    if ../dotnet.sh restore --verbosity minimal > "restore_attempt_$iteration.log" 2>&1; then
        echo -e "${GREEN}SUCCESS! Restore completed successfully on iteration $iteration${NC}"
        break
    else
        echo -e "${RED}Restore failed on iteration $iteration${NC}"
        
        # Analyze the error to find missing packages
        echo -e "${YELLOW}Analyzing errors...${NC}"
        
        # Extract error lines
        grep -i "error.*NU1102\|error.*NU1101\|given key.*not present\|unable to find" "restore_attempt_$iteration.log" > "errors_$iteration.log" 2>/dev/null || true
        
        if [ ! -s "errors_$iteration.log" ]; then
            echo -e "${RED}Could not find specific package errors. Full log:${NC}"
            tail -20 "restore_attempt_$iteration.log"
            break
        fi
        
        # Extract package names and versions from errors
        found_new_package=false
        while IFS= read -r error_line; do
            package_info=$(extract_package_from_error "$error_line")
            
            if [ -n "$package_info" ]; then
                package_name=$(echo -e "$package_info" | cut -f1)
                package_version=$(echo -e "$package_info" | cut -f2)
                
                # Check if we've already processed this package
                if [[ ! " ${missing_packages[@]} " =~ " ${package_name} " ]]; then
                    if [ -n "$package_version" ]; then
                        echo -e "${RED}Found missing package: $package_name (version >= $package_version)${NC}"
                        missing_versions+=("$package_name >= $package_version")
                    else
                        echo -e "${RED}Found missing package: $package_name${NC}"
                        missing_versions+=("$package_name")
                    fi
                    missing_packages+=("$package_name")
                    add_package_to_nuget_mapping "$package_name"
                    found_new_package=true
                    break  # Process one package at a time
                fi
            fi
        done < "errors_$iteration.log"
        
        # Check if we still have NU1102 errors even if no new packages were found
        # This means we need to continue even if we didn't find a new package to add
        if [ "$found_new_package" = false ]; then
            nu1102_count=$(grep -c "NU1102" "restore_attempt_$iteration.log" 2>/dev/null || echo "0")
            if [ "$nu1102_count" -gt 0 ]; then
                echo -e "${YELLOW}Still have $nu1102_count NU1102 errors, but no new packages identified.${NC}"
                echo -e "${YELLOW}This might indicate version conflicts or other issues.${NC}"
                echo -e "${YELLOW}Last few lines of restore log:${NC}"
                tail -15 "restore_attempt_$iteration.log"
                
                # Try to extract any other package names from NU1102 errors we might have missed
                grep "NU1102.*Unable to find package" "restore_attempt_$iteration.log" | head -3 | while IFS= read -r nu_error; do
                    echo -e "${YELLOW}Examining: $nu_error${NC}"
                done
                
                # Continue for a few more iterations in case there are dependency resolution issues
                if [ $iteration -lt 15 ]; then
                    echo -e "${YELLOW}Continuing to next iteration...${NC}"
                    continue
                else
                    echo -e "${RED}Too many iterations with NU1102 errors. Stopping.${NC}"
                    break
                fi
            else
                echo -e "${RED}No NU1102 errors found, but restore still failed. This might be a different type of error.${NC}"
                echo -e "${YELLOW}Last few lines of restore log:${NC}"
                tail -10 "restore_attempt_$iteration.log"
                break
            fi
        fi
    fi
    
    ((iteration++))
done

if [ $iteration -gt $max_iterations ]; then
    echo -e "${RED}Reached maximum iterations ($max_iterations). There might be an issue.${NC}"
fi

echo ""
echo "================================================================="
echo -e "${GREEN}SUMMARY${NC}"
echo "================================================================="

if [ ${#missing_packages[@]} -eq 0 ]; then
    echo -e "${GREEN}No missing packages found! All packages are available in internal feeds.${NC}"
else
    echo -e "${YELLOW}Packages that need to be mirrored to internal feeds:${NC}"
    echo ""
    for i in "${!missing_packages[@]}"; do
        echo "- ${missing_versions[$i]}"
    done
    echo ""
    echo -e "${YELLOW}Total packages to mirror: ${#missing_packages[@]}${NC}"
    
    # Save the list to files
    printf '%s\n' "${missing_packages[@]}" > missing_packages_list.txt
    printf '%s\n' "${missing_versions[@]}" > missing_packages_with_versions.txt
    echo -e "${GREEN}Missing packages list saved to: missing_packages_list.txt${NC}"
    echo -e "${GREEN}Missing packages with versions saved to: missing_packages_with_versions.txt${NC}"
fi

echo ""
echo -e "${GREEN}Log files generated:${NC}"
for i in $(seq 1 $((iteration-1))); do
    if [ -f "restore_attempt_$i.log" ]; then
        echo "- restore_attempt_$i.log: Restore attempt $i log"
    fi
    if [ -f "errors_$i.log" ]; then
        echo "- errors_$i.log: Errors from attempt $i"
    fi
done

echo ""
echo -e "${YELLOW}The NuGet.config has been modified to include nuget.org mappings for missing packages.${NC}"
echo -e "${YELLOW}It will be restored to original state when the script exits.${NC}"
