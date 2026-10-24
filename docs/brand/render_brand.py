import math, os, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ICONS = os.path.join(ROOT, 'Packages', 'com.nekuzaky.resonance', 'Editor', 'Icons')
OUT = os.path.join(ROOT, 'docs', 'images')
FONT = 'C:/Windows/Fonts/segoeuib.ttf'
FONT_REGULAR = 'C:/Windows/Fonts/segoeui.ttf'
BG = (20, 22, 26)
ACCENT = (47, 169, 184)
ROW = ['SoundEvent', 'MusicTrack', 'SoundParameter', 'SwitchGroup', 'StateGroup', 'AudioBus', 'MixSnapshot',
       'SoundEmitter', 'ParameterDriver', 'StateZone', 'MusicSource']


def wave(draw, width, height, top, bottom, alpha_scale):
    bars = 140
    step = width / bars
    middle = (top + bottom) / 2
    for i in range(bars):
        x = i * step + step / 2
        t = i / bars
        amp = (math.sin(t * 9.0) * 0.5 + 0.5) * (math.sin(t * 23.0 + 1.3) * 0.35 + 0.65) * math.sin(math.pi * t) ** 0.6
        h = max(2, amp * (bottom - top) / 2)
        shade = int(40 + 50 * amp * alpha_scale)
        color = (int(ACCENT[0] * shade / 90), int(ACCENT[1] * shade / 90), int(ACCENT[2] * shade / 90))
        draw.rounded_rectangle([x - step * 0.28, middle - h, x + step * 0.28, middle + h], radius=2, fill=color)


def render(width, height, name, title_size, show_row):
    image = Image.new('RGB', (width, height), BG)
    draw = ImageDraw.Draw(image)
    wave(draw, width, height, height * 0.08, height * 0.92, 1.0)
    veil = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    vdraw = ImageDraw.Draw(veil)
    vdraw.rounded_rectangle([width * 0.17, height * 0.18, width * 0.83, height * 0.82], radius=28, fill=(20, 22, 26, 225))
    veil = veil.filter(ImageFilter.GaussianBlur(18))
    image.paste(veil, (0, 0), veil)
    draw = ImageDraw.Draw(image)

    logo = Image.open(os.path.join(ICONS, 'Resonance.png')).convert('RGBA').resize((int(title_size * 1.25),) * 2, Image.LANCZOS)
    title_font = ImageFont.truetype(FONT, title_size)
    text = 'Resonance'
    tw = draw.textlength(text, font=title_font)
    total = logo.width + title_size * 0.4 + tw
    x = (width - total) / 2
    y = height * (0.19 if width / height > 2.5 else 0.27)
    image.paste(logo, (int(x), int(y)), logo)
    draw.text((x + logo.width + title_size * 0.4, y + logo.height / 2), text, font=title_font, fill=(236, 238, 242), anchor='lm')

    sub_font = ImageFont.truetype(FONT_REGULAR, int(title_size * 0.34))
    subtitle = 'Game audio middleware that lives inside Unity'
    draw.text((width / 2, y + logo.height + title_size * 0.55), subtitle, font=sub_font, fill=(170, 176, 186), anchor='mm')

    if show_row:
        size = int(title_size * 0.62)
        gap = int(size * 0.35)
        row_width = len(ROW) * size + (len(ROW) - 1) * gap
        rx = (width - row_width) / 2
        ry = y + logo.height + title_size * 1.15
        for i, icon in enumerate(ROW):
            tile = Image.open(os.path.join(ICONS, icon + '.png')).convert('RGBA').resize((size, size), Image.LANCZOS)
            image.paste(tile, (int(rx + i * (size + gap)), int(ry)), tile)
    image.save(os.path.join(OUT, name))


render(1280, 400, 'banner.png', 96, True)
render(1280, 640, 'social-preview.png', 120, True)
print('rendered')
