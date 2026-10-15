import json
import sys
from pathlib import Path


def compare(original: Path, compact: Path) -> dict:
    expected = {p.relative_to(original): p for p in original.rglob("*.trx")}
    actual = {p.relative_to(compact): p for p in compact.rglob("*.trx")}
    if not expected:
        raise ValueError("No original TRX files found")
    if expected.keys() != actual.keys():
        raise ValueError(
            f"TRX inventory differs: missing={sorted(expected.keys() - actual.keys())}, "
            f"extra={sorted(actual.keys() - expected.keys())}"
        )
    for relative in expected:
        if expected[relative].read_bytes() != actual[relative].read_bytes():
            raise ValueError(f"TRX bytes differ: {relative}")
    counts = {}
    for relative in expected:
        platform = relative.parts[0]
        counts[platform] = counts.get(platform, 0) + 1
    if set(counts) != {"ubuntu-latest", "windows-latest", "macos-latest"}:
        raise ValueError(f"Missing platform coverage: {counts}")
    return {
        "identical_paths_and_bytes": True,
        "trx_files": len(expected),
        "platforms": counts,
        "total_trx_bytes": sum(p.stat().st_size for p in expected.values()),
    }


if __name__ == "__main__":
    result = compare(Path(sys.argv[1]), Path(sys.argv[2]))
    Path(sys.argv[3]).write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result))
