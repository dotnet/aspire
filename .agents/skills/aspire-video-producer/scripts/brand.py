"""The Aspire look of every video: colors, fonts, the logo, backgrounds, title cards, the frame around recordings,
and the building blocks of slides (headings, bullet cards, code panels).

Don't change these values for a single video: every Aspire video shares them. See references/specs.md.
"""
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont
from pygments import lex
from pygments.lexers import (BashLexer, CSharpLexer, HtmlLexer, JsonLexer, JavascriptLexer, PowerShellLexer,
                             TypeScriptLexer, XmlLexer, YamlLexer)
from pygments.token import Comment, Keyword, Name, Number, Operator, Punctuation, String

# The branding assets that ship with the skill: <skill>/assets (from aspire.dev, the fonts under the SIL OFL).
ASSETS = Path(__file__).resolve().parents[1] / "assets"
FONTS = ASSETS / "fonts"
SYMBOL = ASSETS / "logo" / "aspire-logo-1024.png"

# The output frame.
W, H = 1920, 1080
FPS = 30

# The Aspire brand colors (aspire.dev, src/frontend/src/styles/aspire-brand.css).
PURPLE = (81, 43, 212)        # #512BD4, the brand purple: pills, the logo
PRIMARY = (116, 85, 221)      # #7455DD, the primary color: accents, frame, highlights, cursor clicks
SECONDARY = (185, 170, 238)   # #B9AAEE, light accents on the dark background: chapter labels
LAVENDER = (220, 213, 246)    # #DCD5F6, muted
MAGENTA = (214, 58, 255)      # #D63AFF, background glows only
BLACK = (31, 30, 51)          # #1F1E33, the background
WHITE = (255, 255, 255)

# The dark theme of the videos: the aspire.dev dark theme.
BACKGROUND = BLACK
PANEL = (42, 41, 73)          # #2A2949, dark surface
PANEL_DIM = (36, 35, 62)
BORDER = (59, 58, 94)         # #3B3A5E, dark surface raised
CODE_BACKGROUND = (22, 21, 39)
TEXT = (246, 243, 255)        # #F6F3FF, dark text
MUTED = (205, 199, 232)       # #CDC7E8, dark text muted
DIM = (107, 105, 134)         # #6B6986, ink subtle

# Where a recording sits in the frame: 1728 x 972 (90%), centered, under the header.
VIDEO_W, VIDEO_H = 1728, 972
VIDEO_X, VIDEO_Y = (W - VIDEO_W) // 2, H - VIDEO_H - 14
MARGIN = 96


def font(size, weight="Regular"):
    """Poppins, the aspire.dev font, from the skill's assets: Regular, SemiBold or Bold."""
    return ImageFont.truetype(str(FONTS / f"Poppins-{weight}.ttf"), size)


def mono(size):
    """The code font: Fira Code, the aspire.dev code font, from the skill's assets."""
    code_font = ImageFont.truetype(str(FONTS / "FiraCode-Variable.ttf"), size)
    try:
        code_font.set_variation_by_name("Regular")
    except Exception:
        pass
    return code_font


_logos = {}


def logo(width, symbol_only=False):
    """The logo for a dark background: the Aspire symbol, followed by the name "Aspire" in white Poppins SemiBold.
    symbol_only: the symbol alone."""
    key = (width, symbol_only)
    if key not in _logos:
        symbol = Image.open(SYMBOL).convert("RGBA")
        symbol = symbol.crop(symbol.getbbox())
        if symbol_only:
            image = symbol
        else:
            # Composed at a large size, then scaled: the name's cap height is about 60% of the symbol's height.
            symbol = symbol.resize((int(symbol.width * 400 / symbol.height), 400), Image.LANCZOS)
            name_font = font(380, "SemiBold")
            box = name_font.getbbox("Aspire", anchor="ls")
            gap = 90
            image = Image.new("RGBA", (symbol.width + gap + box[2] - box[0], 400 + max(0, box[3])), (0, 0, 0, 0))
            image.paste(symbol, (0, 0), symbol)
            ImageDraw.Draw(image).text((symbol.width + gap - box[0], 400), "Aspire", font=name_font, fill=WHITE, anchor="ls")
            image = image.crop(image.getbbox())
        image = image.resize((width, int(image.height * width / image.width)), Image.LANCZOS)
        _logos[key] = image
    return _logos[key]


def background():
    """The dark background, with a soft purple glow in the top right corner and a magenta one in the bottom left."""
    image = Image.new("RGB", (W, H), BACKGROUND)
    glow = Image.new("RGB", (W, H), BACKGROUND)
    draw = ImageDraw.Draw(glow)
    draw.ellipse((W - 760, -560, W + 560, 520), fill=(66, 42, 150))
    draw.ellipse((-600, H - 380, 520, H + 620), fill=(70, 34, 104))
    return Image.blend(image, glow.filter(ImageFilter.GaussianBlur(180)), 0.95)


def header(image, chapter_title, label=None):
    """The header of recordings and slides: the logo, the chapter's title, and a purple pill such as "Chapter 3"."""
    draw = ImageDraw.Draw(image)
    mark = logo(190)
    image.paste(mark, (MARGIN, 47 - mark.height // 2), mark)
    x = MARGIN + mark.width + 24
    draw.line((x, 28, x, 66), fill=DIM, width=2)
    draw.text((x + 24, 47), chapter_title, font=font(32, "SemiBold"), fill=TEXT, anchor="lm")
    if label:
        text = label.upper()
        label_font = font(21, "Bold")
        box = draw.textbbox((0, 0), text, font=label_font)
        width = box[2] - box[0] + 40
        right = W - MARGIN
        draw.rounded_rectangle((right - width, 27, right, 67), radius=20, fill=PURPLE)
        draw.text((right - width / 2, 48), text, font=label_font, fill=WHITE, anchor="mm")


def frame(chapter_title, label=None):
    """The frame a recording is laid over: the header, and a shadowed purple border around the recording."""
    image = background()
    header(image, chapter_title, label)
    shadow = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle((VIDEO_X - 4, VIDEO_Y - 2, VIDEO_X + VIDEO_W + 4, VIDEO_Y + VIDEO_H + 8), radius=10, fill=(0, 0, 0, 170))
    image = Image.alpha_composite(image.convert("RGBA"), shadow.filter(ImageFilter.GaussianBlur(10))).convert("RGB")
    ImageDraw.Draw(image).rounded_rectangle((VIDEO_X - 3, VIDEO_Y - 3, VIDEO_X + VIDEO_W + 2, VIDEO_Y + VIDEO_H + 2), radius=8, outline=PRIMARY, width=3)
    return image


def card(title, subtitle, label=None, big=False):
    """A title card. big: the opening and closing cards, with the full logo. Otherwise a chapter card, with the
    symbol and a label such as "Chapter 3"."""
    image = background()
    draw = ImageDraw.Draw(image)
    if big:
        mark, title_font, gap = logo(640), font(72, "Bold"), 110
    else:
        mark, title_font, gap = logo(150, symbol_only=True), font(76, "Bold"), 70
    height = mark.height + gap + (64 if label else 0) + 96 + 56 + 50
    top = (H - height) // 2
    image.paste(mark, ((W - mark.width) // 2, top), mark)
    y = top + mark.height + gap
    if label:
        draw.text((W // 2, y), label.upper(), font=font(30, "Bold"), fill=SECONDARY, anchor="mm")
        y += 64
    draw.text((W // 2, y), title, font=title_font, fill=TEXT, anchor="mm")
    y += 72
    draw.rounded_rectangle((W // 2 - 60, y, W // 2 + 60, y + 6), radius=3, fill=PRIMARY)
    y += 56
    draw.text((W // 2, y), subtitle, font=font(38), fill=MUTED, anchor="mm")
    return image


# Slides.

def fits(condition, message):
    """Stops the build when a slide's text runs off its card, panel, or the slide."""
    if not condition:
        raise SystemExit(f"Slide layout: {message}.")


def slide_base(chapter_title, label, heading):
    """A slide: the header, and a heading underlined in purple. Content starts at y = 240."""
    image = background()
    header(image, chapter_title, label)
    draw = ImageDraw.Draw(image)
    draw.text((MARGIN, 150), heading, font=font(52, "Bold"), fill=TEXT, anchor="lm")
    draw.rounded_rectangle((MARGIN, 192, MARGIN + 100, 198), radius=3, fill=PRIMARY)
    return image


def bullets(image, items, top=250, active=None):
    """Up to four cards of (icon, title, text). icon is a short text, such as "1", or "check" for a check mark.
    active is the set of indexes shown bright, the others are dimmed; None shows them all bright."""
    draw = ImageDraw.Draw(image)
    y = top
    for index, (icon, title, text) in enumerate(items):
        on = active is None or index in active
        x = MARGIN
        fits(y + 150 <= H - 30, f"bullet card {title!r} runs off the bottom of the slide")
        fits(draw.textlength(title, font=font(36, "Bold")) <= W - MARGIN - x - 170, f"bullet title {title!r} is too long")
        fits(draw.textlength(text, font=font(28)) <= W - MARGIN - x - 170, f"bullet text {text!r} is too long")
        draw.rounded_rectangle((x, y, W - MARGIN, y + 150), radius=16, fill=PANEL if on else PANEL_DIM,
                               outline=PRIMARY if on else BORDER, width=2)
        draw.ellipse((x + 34, y + 39, x + 106, y + 111), fill=PRIMARY if on else (52, 60, 56))
        color = WHITE if on else MUTED
        if icon == "check":
            draw.line([(x + 52, y + 76), (x + 65, y + 90), (x + 89, y + 60)], fill=color, width=7, joint="curve")
        else:
            draw.text((x + 70, y + 75), icon, font=font(40, "Bold"), fill=color, anchor="mm")
        draw.text((x + 140, y + 50), title, font=font(36, "Bold"), fill=TEXT if on else DIM, anchor="lm")
        draw.text((x + 140, y + 102), text, font=font(28), fill=MUTED if on else DIM, anchor="lm")
        y += 176


def note_cards(image, notes, box):
    """Cards of (title, text) stacked in box = (x0, y0, x1, y1), next to a code panel."""
    draw = ImageDraw.Draw(image)
    x0, y, x1, y1 = box
    for title, text in notes:
        fits(y + 180 <= y1, f"note card {title!r} runs off its box (bottom {y + 180} > {y1})")
        fits(draw.textlength(title, font=font(34, "Bold")) <= x1 - x0 - 80, f"note title {title!r} is too long")
        fits(draw.textlength(text, font=font(30)) <= x1 - x0 - 80, f"note text {text!r} is too long")
        draw.rounded_rectangle((x0, y, x1, y + 180), radius=16, fill=PANEL, outline=PRIMARY, width=2)
        draw.text((x0 + 40, y + 64), title, font=font(34, "Bold"), fill=TEXT, anchor="lm")
        draw.text((x0 + 40, y + 118), text, font=font(30), fill=MUTED, anchor="lm")
        y += 220


# The syntax colors of code panels (the Visual Studio Code dark theme).
TOKEN_COLORS = [
    (Comment, (106, 153, 85)),
    (Keyword, (86, 156, 214)),
    (Name.Tag, (86, 156, 214)),
    (Name.Attribute, (156, 220, 254)),
    (Name.Class, (78, 201, 176)),
    (Name.Function, (220, 220, 170)),
    (String, (206, 145, 120)),
    (Number, (181, 206, 168)),
    (Name.Builtin, (86, 156, 214)),
    (Operator, (212, 212, 212)),
    (Punctuation, (212, 212, 212)),
]
TYPE_COLOR = (78, 201, 176)
LEXERS = {"csharp": CSharpLexer, "json": JsonLexer, "html": HtmlLexer, "cshtml": HtmlLexer, "bash": BashLexer,
          "powershell": PowerShellLexer, "javascript": JavascriptLexer, "typescript": TypeScriptLexer,
          "xml": XmlLexer, "yaml": YamlLexer}


def draw_code(image, box, title, language, code, types=(), highlight=None, size=25):
    """A code panel with a window bar and a file name. types: names colored as types, since the lexers can't tell.
    highlight: the 1-based lines shown bright, the others dimmed; None shows every line bright.
    Keep code at size 22 or more, and at most about 20 lines per panel."""
    x0, y0, x1, y1 = box
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle(box, radius=14, fill=CODE_BACKGROUND, outline=BORDER, width=2)
    draw.rounded_rectangle((x0, y0, x1, y0 + 52), radius=14, fill=PANEL)
    draw.rectangle((x0, y0 + 30, x1, y0 + 52), fill=PANEL)
    for index, color in enumerate([(237, 106, 94), (245, 191, 79), (98, 197, 84)]):
        draw.ellipse((x0 + 22 + index * 26, y0 + 19, x0 + 36 + index * 26, y0 + 33), fill=color)
    draw.text((x0 + 112, y0 + 26), title, font=font(21, "SemiBold"), fill=MUTED, anchor="lm")

    code_font = mono(size)
    line_height = int(size * 1.42)
    lines = code.strip("\n").split("\n")
    fits(y0 + 74 + len(lines) * line_height <= y1, f"{title}: {len(lines)} lines don't fit the panel; make it taller or the code shorter")
    for text in lines:
        fits(draw.textlength(text, font=code_font) <= x1 - x0 - 60, f"{title}: line {text.strip()!r} is wider than the panel")
    x, y = x0 + 30, y0 + 74
    line = 1
    for token, value in lex(code.strip("\n"), LEXERS[language]()):
        for part_index, part in enumerate(value.split("\n")):
            if part_index:
                line += 1
                x, y = x0 + 30, y + line_height
            if not part:
                continue
            color = TYPE_COLOR if token in Name and part in types else next(
                (c for kind, c in TOKEN_COLORS if token in kind), (220, 220, 220))
            if highlight is not None and line not in highlight:
                color = tuple(int(c * 0.38 + BACKGROUND[i] * 0.62) for i, c in enumerate(color))
            draw.text((x, y), part, font=code_font, fill=color)
            x += draw.textlength(part, font=code_font)
