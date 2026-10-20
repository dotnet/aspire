"""Makes a short pronunciation sample: one labeled option of about 5 seconds per spelling of a word, each saying the
same line with the video's voice. A person listens to it and picks the option that sounds right, before any narration
is generated with it. Also prints the words the voice speaks for each spelling.

Usage: python pronounce.py [--storyboard <storyboard.json>] [--voice <voice>] [--rate <rate>]
                            <output.mp4> "<line with {}>" "<written word>" "<spelling>" ["<spelling>"...]

The sample must use the voice and the rate of the video's narration: pass the storyboard (its "voice" and "rate"), or
--voice and --rate. Without them, it uses the default voice and rate (see narrate.py).

Example:
    python pronounce.py --storyboard <workspace>/storyboard.json asp-net.mp4 "The dashboard is an {} Core app." "ASP.NET" "A.S.P. .NET" "A-S-P dot net" "A S P dot net"

Each option shows the line with the written word, as in the captions.
"""
import asyncio
import json
import subprocess
import sys
import tempfile
from pathlib import Path

import imageio_ffmpeg

import brand
from narrate import RATE, VOICE, speak

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()


async def option(directory, index, line, spelling, caption, voice, rate):
    mp3 = directory / f"{index}.mp3"
    words = speak(line.format(spelling), mp3, words=True, voice=voice, rate=rate)
    print(f"Option {index}: {spelling!r} is spoken as {words}")

    png = directory / f"{index}.png"
    brand.card(f"Option {index}", caption, label="Pronunciation check").save(png)
    mp4 = directory / f"{index}.mp4"
    subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-loop", "1", "-framerate", str(brand.FPS), "-i", str(png), "-i", str(mp3),
                    "-filter_complex", "[1:a]adelay=500|500,apad=pad_dur=0.8[a]", "-map", "0:v", "-map", "[a]", "-shortest",
                    "-c:v", "libx264", "-tune", "stillimage", "-crf", "28", "-pix_fmt", "yuv420p",
                    "-c:a", "aac", "-b:a", "96k", "-ar", "48000", "-ac", "1", str(mp4)], check=True)
    return mp4


def options(args):
    """Takes --storyboard, --voice and --rate out of the arguments: the voice and rate of the video."""
    voice, rate, rest = VOICE, RATE, []
    while args:
        arg = args.pop(0)
        if arg == "--storyboard":
            storyboard = json.loads(Path(args.pop(0)).read_text(encoding="utf-8"))
            voice, rate = storyboard.get("voice", voice), storyboard.get("rate", rate)
        elif arg == "--voice":
            voice = args.pop(0)
        elif arg == "--rate":
            rate = args.pop(0)
        else:
            rest.append(arg)
    return voice, rate, rest


async def main():
    voice, rate, args = options(sys.argv[1:])
    output, line, written, spellings = Path(args[0]).resolve(), args[1], args[2], args[3:]
    caption = line.format(written)
    print(f"Voice: {voice} at {rate}")
    with tempfile.TemporaryDirectory() as temp:
        directory = Path(temp)
        parts = [await option(directory, index, line, spelling, caption, voice, rate) for index, spelling in enumerate(spellings, 1)]
        concat = directory / "list.txt"
        concat.write_text("".join(f"{brand.concat_entry(part)}\n" for part in parts), encoding="utf-8")
        subprocess.run([FFMPEG, "-y", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(concat), "-c", "copy", str(output)], check=True)
    print(output)


asyncio.run(main())
