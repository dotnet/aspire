"""Generates the narration of a video: one mp3 per step of storyboard.json, with the Aspire voice, and
durations.json, which the recorder and the assembly read. The voice is spoken by tts.mjs (Node and msedge-tts).

Usage: python narrate.py <workspace> [--force]

Only the steps whose text changed are generated again. A storyboard with "narration": false has no voice: it only
writes durations.json, from each step's "seconds" (3 by default).
"""
import asyncio
import json
import re
import subprocess
import sys
import time
from pathlib import Path

import imageio_ffmpeg

# Speaks a text with the voice, through the msedge-tts npm package: see tts.mjs.
TTS = Path(__file__).resolve().parent / "tts.mjs"

# The voice of every Aspire video. See references/specs.md.
VOICE = "en-US-AndrewMultilingualNeural"
RATE = "+2%"

# How long a step lasts in a video without narration, unless it says ("seconds").
SILENT_STEP_SECONDS = 3.0

# How the voice must say words it misreads. The subtitles keep the written form. A storyboard can add its own in
# "spoken"; add the ones that apply to every video here.
SPOKEN = {
    "ASP.NET": "A.S.P. .NET",
}


def spoken(text, overrides):
    for written, said in {**SPOKEN, **overrides}.items():
        text = text.replace(written, said)
    return text


def duration(path):
    result = subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(), "-hide_banner", "-i", str(path)], capture_output=True, text=True)
    h, m, s = re.search(r"Duration: (\d+):(\d+):([\d.]+)", result.stderr).groups()
    return int(h) * 3600 + int(m) * 60 + float(s)


def speak(text, path, words=False, voice=VOICE, rate=RATE):
    """Writes text, spoken with the voice, to path. words: returns the words the voice speaks."""
    for attempt in range(4):
        result = subprocess.run(["node", str(TTS), voice, rate, str(path), *(["--words"] if words else [])],
                                input=text, capture_output=True, text=True, encoding="utf-8")
        if result.returncode == 0:
            return json.loads(result.stdout) if words else None
        if attempt == 3:
            raise SystemExit(f"The voice failed: {result.stderr.strip()[-600:]}")
        time.sleep(2)


async def synthesize(text, path, voice=VOICE, rate=RATE):
    speak(text, path, voice=voice, rate=rate)


async def main():
    workspace = Path(sys.argv[1] if len(sys.argv) > 1 and not sys.argv[1].startswith("--") else ".").resolve()
    force = "--force" in sys.argv
    storyboard = json.loads((workspace / "storyboard.json").read_text(encoding="utf-8"))
    if not storyboard.get("narration", True):
        durations = {step["id"]: float(step.get("seconds", SILENT_STEP_SECONDS))
                     for chapter in storyboard["chapters"] for clip in chapter["clips"] for step in clip.get("steps", [])}
        (workspace / "durations.json").write_text(json.dumps(durations, indent=1), encoding="utf-8")
        print(f"{len(durations)} steps, no narration, {sum(durations.values()) / 60:.1f} minutes")
        return
    overrides = storyboard.get("spoken", {})
    # A video in another language, or with another voice, names it in the storyboard: "voice" and "rate".
    voice, rate = storyboard.get("voice", VOICE), storyboard.get("rate", RATE)
    audio = workspace / "audio"
    audio.mkdir(exist_ok=True)
    durations = {}

    for chapter in storyboard["chapters"]:
        for clip in chapter["clips"]:
            for step in clip.get("steps", []):
                text = spoken(step["text"], overrides)
                said = f"{voice} {rate}\n{text}"
                path = audio / f"{step['id']}.mp3"
                stamp = audio / f"{step['id']}.txt"
                if force or not path.exists() or not stamp.exists() or stamp.read_text(encoding="utf-8") not in (said, text if voice == VOICE and rate == RATE else None):
                    print("narrating", step["id"], flush=True)
                    await synthesize(text, path, voice, rate)
                    stamp.write_text(said, encoding="utf-8")
                durations[step["id"]] = round(duration(path), 2)

    (workspace / "durations.json").write_text(json.dumps(durations, indent=1), encoding="utf-8")
    print(f"{len(durations)} steps, {sum(durations.values()) / 60:.1f} minutes of narration")


if __name__ == "__main__":
    asyncio.run(main())
