"""Makes a short voice sample: the same line spoken by each candidate voice, about 6 seconds each, on a card that
names the voice. A person listens to it and picks the narrator before any narration is generated.

Usage: python voices.py <output.mp4> ["<line>"] [<voice>...]

Without a line, it reads a line about Aspire; without voices, it uses VOICES, the shortlist of references/specs.md.
The rate is the rate of every video (see narrate.py). Browse and hear every voice in the Voice Gallery,
https://speech.microsoft.com/portal/voicegallery (only the voices without "HD" in their name work with the free
Edge voice service), or list them on https://learn.microsoft.com/azure/ai-services/speech-service/language-support?tabs=tts

Example:
    python voices.py voices.mp4 "The dashboard shows every resource of your app." en-US-AndrewMultilingualNeural en-US-AvaMultilingualNeural
"""
import subprocess
import sys
import tempfile
from pathlib import Path

import imageio_ffmpeg

import brand
from narrate import RATE, VOICE, speak

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()

# The shortlist to offer, in English: (voice, description). The first one is the default voice.
VOICES = [
    (VOICE, "Male, warm and confident (the default)"),
    ("en-US-BrianMultilingualNeural", "Male, approachable and casual"),
    ("en-US-ChristopherNeural", "Male, reliable, with authority"),
    ("en-US-AvaMultilingualNeural", "Female, expressive and friendly"),
    ("en-US-EmmaMultilingualNeural", "Female, cheerful and clear"),
    ("en-US-JennyNeural", "Female, friendly and considerate"),
]

LINE = "Aspire runs your whole app with one command, and the dashboard shows every resource, its logs, and its traces."


def name(voice):
    """en-US-AvaMultilingualNeural -> Ava (multilingual, en-US)."""
    parts = voice.split("-")
    language, short = "-".join(parts[:2]), parts[-1].removesuffix("Neural")
    multilingual = short.endswith("Multilingual")
    return f"{short.removesuffix('Multilingual')} ({'multilingual, ' if multilingual else ''}{language})"


def main():
    output = Path(sys.argv[1]).resolve()
    rest = sys.argv[2:]
    line = rest.pop(0) if rest and "Neural" not in rest[0] else LINE
    descriptions = dict(VOICES)
    voices = rest or [voice for voice, _ in VOICES]

    with tempfile.TemporaryDirectory() as temp:
        directory = Path(temp)
        parts = []
        for index, voice in enumerate(voices, 1):
            mp3 = directory / f"{index}.mp3"
            speak(line, mp3, voice=voice, rate=RATE)
            png = directory / f"{index}.png"
            brand.card(name(voice), descriptions.get(voice, voice), label=f"Voice {index}").save(png)
            mp4 = directory / f"{index}.mp4"
            subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-loop", "1", "-framerate", str(brand.FPS), "-i", str(png), "-i", str(mp3),
                            "-filter_complex", "[1:a]adelay=500|500,apad=pad_dur=0.8[a]", "-map", "0:v", "-map", "[a]", "-shortest",
                            "-c:v", "libx264", "-tune", "stillimage", "-crf", "28", "-pix_fmt", "yuv420p",
                            "-c:a", "aac", "-b:a", "96k", "-ar", "48000", "-ac", "1", str(mp4)], check=True)
            parts.append(mp4)
            print(f"Voice {index}: {voice}")
        concat = directory / "list.txt"
        concat.write_text("".join(f"file '{part.as_posix()}'\n" for part in parts), encoding="utf-8")
        subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(concat), "-c", "copy", str(output)], check=True)
    print(output)


if __name__ == "__main__":
    main()
