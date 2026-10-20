"""Assembles a video: title cards, slides, framed recordings, the narration, and captions.

Usage: python assemble.py <workspace>

Reads storyboard.json, durations.json, clips/<clip>/timeline.json and frames (from the recorder), and slides.py
(for "slide" clips). Writes <output>.mp4, <output>.vtt and <output>.srt in the workspace, where output is the
storyboard's "output". The intermediate files are in build/.
"""
import importlib.util
import json
import os
import re
import subprocess
import sys
from pathlib import Path

import imageio_ffmpeg
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import brand  # noqa: E402

FFMPEG = imageio_ffmpeg.get_ffmpeg_exe()
FPS = brand.FPS

# The renditions a video can be published in, by use (see references/specs.md). The storyboard lists the ones to
# make in "renditions" (["web"] by default). "web" is <output>.mp4; the others are <output>-<name>.mp4. Every
# rendition is encoded from build/master.mp4, at 1920x1080: the layout is drawn for 16:9 Full HD, so there's no
# vertical or 4K rendition. CRF_<NAME> (or CRF, for "web") overrides a rendition's CRF.
RENDITIONS = {
    "web": {"use": "GitHub pull requests and issues, aspire.dev pages", "size": (1920, 1080), "crf": 33,
            "audio": ("64k", 1), "limit": 10_000_000},
    "hd": {"use": "YouTube, conferences, editing: the best quality, no size limit", "size": (1920, 1080), "crf": 20,
           "audio": ("160k", 2), "limit": None},
    "light": {"use": "Teams, Slack, email, phones: small and quick to load", "size": (1280, 720), "crf": 30,
              "audio": ("64k", 1), "limit": 5_000_000},
}

# Timing.
CHAPTER_CARD_SECONDS = 2.6
CARD_LEAD, CARD_TAIL = 1.0, 1.4
SLIDE_LEAD, SLIDE_GAP, SLIDE_TAIL = 0.6, 0.5, 1.0
CLIP_LEAD, CLIP_TAIL = 0.7, 1.0

# Zooms in recordings (see references/specs.md): slight, eased, and never so large the element loses its context.
ZOOM_SECONDS = 0.8        # the length of a zoom in, out, or from one element to another
ZOOM_MAX = 1.4            # the largest zoom, whatever the recorder asks
ZOOM_FILL = 0.8           # the zoomed element fills at most this share of the width or the height
ZOOM_MIN = 1.06           # a smaller zoom isn't worth the movement: the element stays wide
ZOOM_MERGE = 1.5          # a zoom out followed this soon by a zoom in pans from one element to the next instead


def run(args):
    result = subprocess.run([FFMPEG, "-hide_banner", "-loglevel", "error", "-y", *args], capture_output=True, text=True)
    if result.returncode != 0:
        raise SystemExit(result.stderr)


def master_video():
    return ["-c:v", "libx264", "-preset", "medium", "-crf", "16", "-r", str(FPS), "-pix_fmt", "yuv420p"]


def master_audio():
    return ["-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2"]


class Assembler:
    def __init__(self, workspace):
        self.workspace = workspace
        self.build = workspace / "build"
        self.build.mkdir(exist_ok=True)
        self.storyboard = json.loads((workspace / "storyboard.json").read_text(encoding="utf-8"))
        self.durations = json.loads((workspace / "durations.json").read_text(encoding="utf-8"))
        # "narration": false makes a silent video, without captions; "branding": false, one without the Aspire look.
        self.narrated = self.storyboard.get("narration", True)
        self.branded = self.storyboard.get("branding", True)
        self.slides = None
        if (workspace / "slides.py").exists():
            spec = importlib.util.spec_from_file_location("slides", workspace / "slides.py")
            self.slides = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(self.slides)

    def audio(self, step_id):
        return str(self.workspace / "audio" / f"{step_id}.mp3")

    def sounds(self, step, offset):
        """The narration of a step at an offset, for still(); nothing in a video without narration."""
        return [(self.audio(step["id"]), offset)] if self.narrated else []

    def card(self, title, subtitle, label=None, big=False):
        if self.branded:
            return brand.card(title, subtitle, label=label, big=big)
        return brand.plain_card(title, subtitle, label=label)

    @staticmethod
    def mix(sounds, first_input, seconds):
        """The inputs and filters that mix narration, a list of (mp3, offset), into [aout]; or silence."""
        if not sounds:
            return (["-f", "lavfi", "-t", f"{seconds:.3f}", "-i", "anullsrc=r=48000:cl=stereo"],
                    [f"[{first_input}:a]anull[aout]"])
        args, filters = [], []
        for index, (mp3, offset) in enumerate(sounds):
            args += ["-i", mp3]
            filters.append(f"[{first_input + index}:a]adelay={int(offset * 1000)}|{int(offset * 1000)}[a{index}]")
        inputs = "".join(f"[a{i}]" for i in range(len(sounds)))
        filters.append(f"{inputs}amix=inputs={len(sounds)}:normalize=0,apad,atrim=0:{seconds:.3f}[aout]")
        return args, filters

    @staticmethod
    def frame_entries(directory, frames, start, end):
        """The recorded frames between start and end, each shown until the next one: (path, seconds)."""
        entries = []
        for index, item in enumerate(frames):
            next_t = frames[index + 1]["t"] if index + 1 < len(frames) else end
            a, b = max(item["t"], start), min(next_t, end)
            if b > a:
                entries.append((directory / "frames" / item["file"], b - a))
            elif item["t"] <= start < next_t:
                entries.append((directory / "frames" / item["file"], 0.001))
        return entries

    def write_concat(self, name, entries):
        concat = self.build / f"{name}.frames.txt"
        with open(concat, "w", encoding="utf-8") as list_file:
            for path, length in entries:
                list_file.write(f"{brand.concat_entry(path)}\nduration {length:.4f}\n")
            list_file.write(f"{brand.concat_entry(entries[-1][0])}\n")
        return concat

    def still(self, image_path, seconds, sounds, output, fade_in=True, fade_out=True):
        """A still image with narration: a list of (mp3, offset in seconds)."""
        args = ["-loop", "1", "-framerate", str(FPS), "-t", f"{seconds:.3f}", "-i", str(image_path)]
        fades = (["fade=t=in:st=0:d=0.4"] if fade_in else []) + ([f"fade=t=out:st={seconds - 0.4:.3f}:d=0.4"] if fade_out else [])
        video = ",".join(fades + ["format=yuv420p"])
        if sounds:
            filters = []
            for index, (mp3, offset) in enumerate(sounds):
                args += ["-i", mp3]
                filters.append(f"[{index + 1}:a]adelay={int(offset * 1000)}|{int(offset * 1000)}[a{index}]")
            mix = "".join(f"[a{i}]" for i in range(len(sounds)))
            filters.append(f"{mix}amix=inputs={len(sounds)}:normalize=0,apad,atrim=0:{seconds:.3f}[aout]")
            filters.append(f"[0:v]{video}[vout]")
            args += ["-filter_complex", ";".join(filters), "-map", "[vout]", "-map", "[aout]"]
        else:
            args += ["-f", "lavfi", "-t", f"{seconds:.3f}", "-i", "anullsrc=r=48000:cl=stereo", "-vf", video, "-map", "0:v", "-map", "1:a"]
        run(args + master_video() + master_audio() + ["-t", f"{seconds:.3f}", str(output)])

    def zoomed_frames(self, clip_id, directory, timeline, start, end):
        """The frames of a recording with zooms: one entry per output frame, zoomed in Python with sub-pixel
        precision (ffmpeg's zoompan shakes), or the recorded frame itself while there is no zoom."""
        frames = timeline["frames"]
        first = Image.open(directory / "frames" / frames[0]["file"])
        width, height = first.size
        css_w, css_h = timeline.get("viewport", [1536, 864])
        factor = width / css_w

        # The target of each zoom: a scale, and a center that keeps the view inside the page.
        events = []
        for zoom in sorted(timeline["zooms"], key=lambda item: item["t"]):
            target = (1.0, width / 2, height / 2)
            if zoom["box"]:
                x, y, w, h = (value * factor for value in zoom["box"])
                scale = min(zoom.get("scale", 1.25), ZOOM_MAX, ZOOM_FILL * width / max(w, 1), ZOOM_FILL * height / max(h, 1))
                if scale >= ZOOM_MIN:
                    half_w, half_h = width / scale / 2, height / scale / 2
                    target = (scale, min(max(x + w / 2, half_w), width - half_w), min(max(y + h / 2, half_h), height - half_h))
            events.append((zoom["t"], target))
        merged = []
        for index, (t, target) in enumerate(events):
            following = events[index + 1] if index + 1 < len(events) else None
            if target[0] == 1.0 and following and following[1][0] > 1.0 and following[0] - t < ZOOM_MERGE:
                continue
            merged.append((t, target))

        def state(t):
            current, origin, since = (1.0, width / 2, height / 2), None, None
            for event_t, target in merged:
                if event_t > t:
                    break
                if origin is not None:
                    current = blend(origin, target_before, min(1.0, (event_t - since) / ZOOM_SECONDS))
                origin, target_before, since = current, target, event_t
            if origin is None:
                return current
            return blend(origin, target_before, min(1.0, (t - since) / ZOOM_SECONDS))

        def blend(a, b, progress):
            eased = progress * progress * (3 - 2 * progress)
            return tuple(x + (y - x) * eased for x, y in zip(a, b))

        zoom_directory = self.build / f"{clip_id}.zoom"
        zoom_directory.mkdir(exist_ok=True)
        entries, source_index, cache = [], 0, {}
        count = int(round((end - start) * FPS))
        for index in range(count):
            t = start + index / FPS
            while source_index + 1 < len(frames) and frames[source_index + 1]["t"] <= t:
                source_index += 1
            source = frames[source_index]["file"]
            scale, cx, cy = state(t)
            if scale < 1.001:
                entries.append((directory / "frames" / source, 1 / FPS))
                continue
            key = (source, round(scale, 4), round(cx, 1), round(cy, 1))
            if key not in cache:
                image = Image.open(directory / "frames" / source).convert("RGB")
                x0, y0 = cx - width / scale / 2, cy - height / scale / 2
                zoomed = image.transform((width, height), Image.Transform.AFFINE, (1 / scale, 0, x0, 0, 1 / scale, y0),
                                         resample=Image.Resampling.BICUBIC)
                path = zoom_directory / f"{index:06}.jpg"
                zoomed.save(path, quality=92)
                cache[key] = path
            entries.append((cache[key], 1 / FPS))
        return entries

    def recording(self, clip_id, chapter_title, label, steps, output):
        """A recording in the frame, with the narration of each step at the time the recorder started it."""
        directory = self.workspace / "clips" / clip_id
        timeline = json.loads((directory / "timeline.json").read_text())
        recorded = {step["id"]: step["t"] for step in timeline["steps"]}
        missing = [step["id"] for step in steps if step["id"] not in recorded]
        if missing:
            raise SystemExit(f"{clip_id} has no recording of {missing}")

        last = steps[-1]["id"]
        start = max(0.0, recorded[steps[0]["id"]] - CLIP_LEAD)
        narration_end = recorded[last] + self.durations[last]
        end = max(min(timeline["end"], narration_end + CLIP_TAIL), narration_end + 0.6)
        seconds = end - start

        # The frames as a concat list, each shown until the next one.
        frames = timeline["frames"]
        entries = []
        if timeline.get("zooms") and self.storyboard.get("zoom", True):
            entries = self.zoomed_frames(clip_id, directory, timeline, start, end)
        else:
            entries = self.frame_entries(directory, frames, start, end)
        if not entries:
            raise SystemExit(f"{clip_id}: no frames")
        concat = self.write_concat(clip_id, entries)

        # The recording in the purple frame, under the header; or, without branding, filling the whole frame.
        frame_png = self.build / f"{clip_id}.frame.png"
        if self.branded:
            brand.frame(chapter_title, label).save(frame_png)
            video_w, video_h, video_x, video_y = brand.VIDEO_W, brand.VIDEO_H, brand.VIDEO_X, brand.VIDEO_Y
        else:
            Image.new("RGB", (brand.W, brand.H), (0, 0, 0)).save(frame_png)
            video_w, video_h, video_x, video_y = brand.W, brand.H, 0, 0

        args = ["-loop", "1", "-framerate", str(FPS), "-t", f"{seconds:.3f}", "-i", str(frame_png),
                "-f", "concat", "-safe", "0", "-i", str(concat)]
        filters = [
            f"[1:v]scale={video_w}:{video_h}:flags=lanczos,fps={FPS},setpts=PTS-STARTPTS[rec]",
            f"[0:v][rec]overlay={video_x}:{video_y}:shortest=0,trim=0:{seconds:.3f},"
            f"fade=t=in:st=0:d=0.35,fade=t=out:st={seconds - 0.35:.3f}:d=0.35,format=yuv420p[vout]",
        ]
        offsets = [(step, recorded[step["id"]] - start) for step in steps]
        sounds = [(self.audio(step["id"]), offset) for step, offset in offsets] if self.narrated else []
        audio_args, audio_filters = self.mix(sounds, 2, seconds)
        args += audio_args + ["-filter_complex", ";".join(filters + audio_filters), "-map", "[vout]", "-map", "[aout]"]
        run(args + master_video() + master_audio() + ["-t", f"{seconds:.3f}", str(output)])
        return seconds, offsets

    def run(self):
        segments, captions, clock = [], [], 0.0

        def caption(text, start, length):
            if not self.narrated or not text:
                return
            captions.extend((clock + a, clock + b, c) for a, b, c in chunks(text, start, length))

        for chapter in self.storyboard["chapters"]:
            title, subtitle = chapter["title"], chapter["subtitle"]
            # Chapters made only of cards, the opening and the closing, have no chapter card.
            numbered = any(clip["kind"] != "card" for clip in chapter["clips"])
            # "chapter": the word for "Chapter" in the language of the video.
            label = f"{self.storyboard.get('chapter', 'Chapter')} {int(chapter['id'])}" if numbered else None
            # "chapterCards": false leaves out the chapter cards; the header's pill still numbers the chapter.
            if numbered and self.storyboard.get("chapterCards", True):
                png, path = self.build / f"card-{chapter['id']}.png", self.build / f"card-{chapter['id']}.mp4"
                self.card(title, subtitle, label=label).save(png)
                # "chapterCardSeconds": a shorter (or longer) chapter card for this video only.
                card_seconds = float(self.storyboard.get("chapterCardSeconds", CHAPTER_CARD_SECONDS))
                self.still(png, card_seconds, [], path)
                segments.append(path)
                clock += card_seconds

            for clip in chapter["clips"]:
                print("encoding", clip["id"], flush=True)
                steps = clip.get("steps", [])
                if clip["kind"] == "card":
                    png, path = self.build / f"{clip['id']}.png", self.build / f"{clip['id']}.mp4"
                    self.card(title, subtitle, big=True).save(png)
                    step = steps[0]
                    seconds = CARD_LEAD + self.durations[step["id"]] + CARD_TAIL
                    self.still(png, seconds, self.sounds(step, CARD_LEAD), path)
                    caption(step.get("text"), CARD_LEAD, self.durations[step["id"]])
                    segments.append(path)
                    clock += seconds
                elif clip["kind"] == "slide":
                    if self.slides is None:
                        raise SystemExit(f"{clip['id']} is a slide clip, but the workspace has no slides.py")
                    for index, step in enumerate(steps):
                        first, last = index == 0, index == len(steps) - 1
                        png, path = self.build / f"slide-{step['id']}.png", self.build / f"slide-{step['id']}.mp4"
                        self.slides.slide(step["id"], title, label).save(png)
                        lead = SLIDE_LEAD if first else SLIDE_GAP / 2
                        seconds = lead + self.durations[step["id"]] + (SLIDE_TAIL if last else SLIDE_GAP)
                        self.still(png, seconds, self.sounds(step, lead), path, fade_in=first, fade_out=last)
                        caption(step.get("text"), lead, self.durations[step["id"]])
                        segments.append(path)
                        clock += seconds
                else:
                    path = self.build / f"{clip['id']}.mp4"
                    seconds, offsets = self.recording(clip["id"], title, label, steps, path)
                    for step, offset in offsets:
                        caption(step.get("text"), offset, self.durations[step["id"]])
                    segments.append(path)
                    clock += seconds

        concat = self.build / "segments.txt"
        concat.write_text("".join(f"{brand.concat_entry(path)}\n" for path in segments), encoding="utf-8")
        master = self.build / "master.mp4"
        run(["-f", "concat", "-safe", "0", "-i", str(concat), "-c", "copy", str(master)])

        names = self.storyboard.get("renditions", ["web"])
        unknown = [name for name in names if name not in RENDITIONS]
        if unknown:
            raise SystemExit(f"Unknown renditions {unknown}: use {list(RENDITIONS)}")
        outputs = []
        for name in names:
            output = self.workspace / (f"{self.storyboard['output']}.mp4" if name == "web" else f"{self.storyboard['output']}-{name}.mp4")
            print("compressing", name, flush=True)
            publish(master, output, name)
            outputs.append((name, output))

        # A video without narration has no captions: remove those of an earlier, narrated build, so they don't
        # get delivered with the silent video.
        base = self.workspace / self.storyboard["output"]
        if not self.narrated:
            for suffix in (".srt", ".vtt"):
                Path(f"{base}{suffix}").unlink(missing_ok=True)
        else:
            with open(f"{base}.srt", "w", encoding="utf-8") as srt:
                for index, (a, b, text) in enumerate(captions, 1):
                    srt.write(f"{index}\n{timestamp(a)} --> {timestamp(b)}\n{text}\n\n")
            with open(f"{base}.vtt", "w", encoding="utf-8") as vtt:
                vtt.write("WEBVTT\n\n")
                for a, b, text in captions:
                    # WebVTT cues are markup: escape the text, so "List<T>" shows as written.
                    cue = text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
                    vtt.write(f"{timestamp(a, '.')} --> {timestamp(b, '.')}\n{cue}\n\n")

        print(f"{clock / 60:.1f} minutes, {len(segments)} segments")
        for name, output in outputs:
            size = output.stat().st_size
            limit = RENDITIONS[name]["limit"]
            print(f"{output}: {name}, {size / 1_000_000:.1f} MB")
            if limit and size >= limit:
                variable = "CRF" if name == "web" else f"CRF_{name.upper()}"
                print(f"  Over {limit / 1_000_000:.0f} MB: raise {variable} one step ({variable}={crf(name) + 1}), "
                      "or shorten the video.")


def crf(name):
    variable = os.environ.get(f"CRF_{name.upper()}") or (os.environ.get("CRF") if name == "web" else None)
    return int(variable or RENDITIONS[name]["crf"])


def publish(master, output, name="web"):
    """A published encode: H.264 tuned for screen content, AAC speech, fast start, at the rendition's size."""
    rendition = RENDITIONS[name]
    width, height = rendition["size"]
    bitrate, channels = rendition["audio"]
    scale = [] if (width, height) == (brand.W, brand.H) else ["-vf", f"scale={width}:{height}:flags=lanczos"]
    run(["-i", str(master), *scale, "-c:v", "libx264", "-preset", "slow", "-crf", str(crf(name)), "-tune", "stillimage",
         "-pix_fmt", "yuv420p", "-r", str(FPS), "-g", str(FPS * 10),
         "-c:a", "aac", "-b:a", bitrate, "-ar", "48000", "-ac", str(channels), "-movflags", "+faststart", str(output)])


def timestamp(seconds, separator=","):
    ms = int(round(seconds * 1000))
    return f"{ms // 3600000:02}:{ms // 60000 % 60:02}:{ms // 1000 % 60:02}{separator}{ms % 1000:03}"


def chunks(text, start, length):
    """Splits a narration into captions of at most about 13 words, timed by their share of the characters."""
    parts = []
    for sentence in re.split(r"(?<=[.!?:])\s+", text):
        words = sentence.split()
        while words:
            take, words = words[:13], words[13:]
            if len(words) < 4:
                take, words = take + words, []
            parts.append(" ".join(take))
    total = sum(len(part) + 6 for part in parts)
    t = start
    for part in parts:
        share = length * (len(part) + 6) / total
        yield t, t + share, part
        t += share


if __name__ == "__main__":
    Assembler(Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()).run()
