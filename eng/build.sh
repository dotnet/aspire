#!/usr/bin/env bash

set -ue

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

usage()
{
  echo "Common settings:"
  echo "  --arch (-a)                     Target platform: x86, x64, arm or arm64."
  echo "                                  [Default: Your machine's architecture.]"
  echo "  --binaryLog (-bl)               Output binary log."
  echo "  --configuration (-c)            Build configuration: Debug or Release."
  echo "                                  [Default: Debug]"
  echo "  --help (-h)                     Print help and exit."
  echo "  --os                            Target operating system: windows, linux, or osx."
  echo "                                  [Default: Your machine's OS.]"
  echo "  --verbosity (-v)                MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]."
  echo "                                  [Default: Minimal]"
  echo ""

  echo "Actions (defaults to --restore --build):"
  echo "  --build (-b)               Build all source projects."
  echo "                             This assumes --restore has been run already."
  echo "  --clean                    Clean the solution."
  echo "  --pack                     Package build outputs into NuGet packages."
  echo "  --publish                  Publish artifacts (e.g. symbols)."
  echo "                             This assumes --build has been run already."
  echo "  --rebuild                  Rebuild all source projects."
  echo "  --restore (-r)             Restore dependencies."
  echo "  --sign                     Sign build outputs."
  echo "  --test (-t)                Incrementally builds and runs tests."
  echo "                             Use in conjunction with --testnobuild to only run tests."
  echo ""

  echo "Libraries settings:"
  echo "  --testnobuild              Skip building tests when invoking -test."
  echo "  --build-extension          Build the VS Code extension."
  echo "  --restore-maui             Restore the MAUI workload after restore (only on macOS)."
  echo ""

  echo "Command line arguments starting with '/p:' are passed through to MSBuild."
  echo "Arguments can also be passed in with a single hyphen."
  echo ""
}

arguments=''
extraargs=''
restore_maui=false

# Check if an action is passed in
declare -a actions=("b" "build" "r" "restore" "rebuild" "testnobuild" "sign" "publish" "clean" "t" "test" "build-extension")
actInt=($(comm -12 <(printf '%s\n' "${actions[@]/#/-}" | sort) <(printf '%s\n' "${@/#--/-}" | sort)))

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"

  case "$opt" in
     -help|-h|-\?|/?)
      usage
      exit 0
      ;;

     -arch|-a)
      if [ -z ${2+x} ]; then
        echo "No architecture supplied. See help (--help) for supported architectures." 1>&2
        exit 1
      fi
      passedArch="$(echo "$2" | tr "[:upper:]" "[:lower:]")"
      case "$passedArch" in
        x64|x86|arm|arm64)
          arch=$passedArch
          ;;
        *)
          echo "Unsupported target architecture '$2'."
          echo "The allowed values are x86, x64, arm, arm64."
          exit 1
          ;;
      esac
      arguments="$arguments /p:TargetArchitecture=$arch"
      shift 2
      ;;

     -configuration|-c)
      if [ -z ${2+x} ]; then
        echo "No configuration supplied. See help (--help) for supported configurations." 1>&2
        exit 1
      fi
      passedConfig="$(echo "$2" | tr "[:upper:]" "[:lower:]")"
      case "$passedConfig" in
        debug|release)
          val="$(tr '[:lower:]' '[:upper:]' <<< ${passedConfig:0:1})${passedConfig:1}"
          ;;
        *)
          echo "Unsupported target configuration '$2'."
          echo "The allowed values are Debug and Release."
          exit 1
          ;;
      esac
      arguments="$arguments -configuration $val"
      shift 2
      ;;

     -os)
      if [ -z ${2+x} ]; then
        echo "No target operating system supplied. See help (--help) for supported target operating systems." 1>&2
        exit 1
      fi
      passedOS="$(echo "$2" | tr "[:upper:]" "[:lower:]")"
      case "$passedOS" in
        windows)
          os="windows" ;;
        linux)
          os="linux" ;;
        osx)
          os="osx" ;;
        *)
          echo "Unsupported target OS '$2'."
          echo "Try 'build.sh --help' for values supported by '--os'."
          exit 1
          ;;
      esac
      arguments="$arguments /p:TargetOS=$os"
      shift 2
      ;;

     -testnobuild)
      arguments="$arguments /p:VSTestNoBuild=true"
      shift 1
      ;;

     -build-extension)
      extraargs="$extraargs /p:BuildExtension=true"
      shift 1
      ;;

     -restore-maui)
      restore_maui=true
      shift 1
      ;;

     *)
      extraargs="$extraargs $1"
      shift 1
      ;;
  esac
done

if [ ${#actInt[@]} -eq 0 ]; then
    arguments="-restore -build $arguments"
fi

if [[ "${TreatWarningsAsErrors:-}" == "false" ]]; then
    arguments="$arguments -warnAsError 0"
fi

arguments="$arguments $extraargs"
"$scriptroot/common/build.sh" $arguments
buildExitCode=$?

# Install MAUI workload after restore if --restore-maui was passed
# Only on macOS (MAUI doesn't support Linux, Windows uses .cmd)
if [ "$restore_maui" = true ] && [ $buildExitCode -eq 0 ]; then
  # Check if we're on macOS
  if [[ "$(uname -s)" == "Darwin" ]]; then
    echo ""
    echo "Installing MAUI workload..."
    
    repo_root="$(cd "$scriptroot/.." && pwd)"
    dotnet_sh="$repo_root/dotnet.sh"
    
    if "$dotnet_sh" workload install maui; then
      echo "MAUI workload installed successfully."
    else
      echo ""
      echo "WARNING: Failed to install MAUI workload. You may need to run this command manually:"
      echo "  $dotnet_sh workload install maui"
      echo ""
      echo "The MAUI playground may not work without the MAUI workload installed."
    fi
  fi
fi

exit $buildExitCode
