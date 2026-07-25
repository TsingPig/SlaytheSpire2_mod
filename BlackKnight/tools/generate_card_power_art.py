"""
Generate Black Knight card / power art from the concept sheet + battle sprite:

  NinjaMod/images/card_portraits/nether_curse.png           (250x350)
  NinjaMod/images/card_portraits/big/nether_curse.png       (606x852)
  NinjaMod/images/powers/cowardice_power.png                (64x64)
  NinjaMod/images/powers/big/cowardice_power.png            (256x256)
  NinjaMod/images/powers/vertical_slash_protection_power.png(64x64)
  NinjaMod/images/powers/big/vertical_slash_protection_power.png (256x256)

- Nether Curse card: the extracted knight, duotoned to a spectral purple/green
  ghost over a dark soul-lit background + vignette.
- Cowardice debuff: the red-void helmet, background removed, on a dark red-glow disc.
- Warded Cleave buff: the silver axe blade, background removed, on a dark purple disc.

Re-runnable. Depends on extract_battle_sprite.py having produced blackknight_battle.png.
"""
from __future__ import annotations

import os
from PIL import Image, ImageDraw, ImageFilter, ImageChops
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SHEET = os.path.join(ROOT, "BlackKnight", "design.png")
BATTLE = os.path.join(ROOT, "NinjaMod", "images", "character", "blackknight_battle.png")
CARD_DIR = os.path.join(ROOT, "NinjaMod", "images", "card_portraits")
POWER_DIR = os.path.join(ROOT, "NinjaMod", "images", "powers")

HELMET_BOX = (752, 596, 920, 802)
BLADE_BOX = (203, 598, 330, 697)
SENTINEL = (255, 0, 255)


def remove_bg(img: Image.Image, thresh: int = 46, step: int = 12) -> Image.Image:
    """Border flood-fill background removal, keep only the largest blob."""
    rgb = img.convert("RGB")
    w, h = rgb.size
    flood = rgb.copy()
    seeds = [(x, 0) for x in range(0, w, step)] + [(x, h - 1) for x in range(0, w, step)]
    seeds += [(0, y) for y in range(0, h, step)] + [(w - 1, y) for y in range(0, h, step)]
    for s in seeds:
        ImageDraw.floodfill(flood, s, SENTINEL, thresh=thresh)
    arr = np.asarray(flood, dtype=np.int16)
    is_bg = (arr[:, :, 0] == SENTINEL[0]) & (arr[:, :, 1] == SENTINEL[1]) & (arr[:, :, 2] == SENTINEL[2])
    keep = largest_component(~is_bg)
    out = rgb.convert("RGBA")
    a = Image.fromarray(np.where(keep, 255, 0).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(0.6))
    out.putalpha(a)
    bbox = out.getbbox()
    return out.crop(bbox) if bbox else out


def largest_component(mask: np.ndarray) -> np.ndarray:
    h, w = mask.shape
    visited = np.zeros_like(mask, dtype=bool)
    best: list = []
    ys, xs = np.nonzero(mask)
    for sy, sx in zip(ys.tolist(), xs.tolist()):
        if visited[sy, sx]:
            continue
        stack = [(sy, sx)]
        visited[sy, sx] = True
        comp = []
        while stack:
            y, x = stack.pop()
            comp.append((y, x))
            for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not visited[ny, nx]:
                    visited[ny, nx] = True
                    stack.append((ny, nx))
        if len(comp) > len(best):
            best = comp
    out = np.zeros_like(mask, dtype=bool)
    if best:
        idx = np.array(best)
        out[idx[:, 0], idx[:, 1]] = True
    return out


def radial_bg(size, inner, outer):
    w, h = size
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    cx, cy = w / 2, h * 0.44
    d = np.sqrt(((xx - cx) / (w * 0.62)) ** 2 + ((yy - cy) / (h * 0.62)) ** 2)
    d = np.clip(d, 0, 1)
    bg = np.zeros((h, w, 3), np.float32)
    for c in range(3):
        bg[:, :, c] = inner[c] * (1 - d) + outer[c] * d
    return Image.fromarray(bg.astype(np.uint8), "RGB").convert("RGBA")


def vignette(size, strength=0.85):
    w, h = size
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2)
    a = np.clip((d - 0.55) / 0.55, 0, 1) * strength
    v = np.zeros((h, w, 4), np.uint8)
    v[:, :, 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(v, "RGBA")


def duotone(img: Image.Image, shadow, mid, light) -> Image.Image:
    """Map luminance -> shadow/mid/light ramp, preserve alpha (spectral ghost)."""
    rgba = img.convert("RGBA")
    a = rgba.getchannel("A")
    lum = np.asarray(rgba.convert("L"), np.float32) / 255.0
    ramp = np.zeros((256, 3), np.float32)
    xs = np.linspace(0, 1, 256)
    for c in range(3):
        ramp[:, c] = np.interp(xs, [0, 0.5, 1], [shadow[c], mid[c], light[c]])
    idx = (lum * 255).astype(np.uint8)
    rgb = ramp[idx].astype(np.uint8)
    out = Image.fromarray(rgb, "RGB").convert("RGBA")
    out.putalpha(a)
    return out


def make_card():
    big = (606, 852)
    # Full-bleed background: deep purple top -> black, with a strong soul-green core
    # glow so the whole card face is lit (no dark empty bands).
    bg = radial_bg(big, inner=(58, 34, 84), outer=(10, 8, 18))
    glow = radial_bg(big, inner=(58, 176, 104), outer=(0, 0, 0))
    bg = Image.blend(bg, ImageChops.screen(bg, glow), 0.6)

    knight = Image.open(BATTLE).convert("RGBA")  # already faces left (flipped in extractor)
    ghost = duotone(knight, shadow=(70, 40, 104), mid=(128, 88, 168), light=(212, 250, 214))
    # Make the knight DOMINATE the card: its native ~1.57 aspect fills a wide (10:6-ish)
    # landscape band. Scale so width slightly overflows -> true full-bleed key visual.
    scale = (big[0] * 1.18) / ghost.width
    ghost = ghost.resize((int(ghost.width * scale), int(ghost.height * scale)), Image.LANCZOS)
    gx = (big[0] - ghost.width) // 2
    gy = int(big[1] * 0.40 - ghost.height * 0.5)
    card = bg.copy()

    # Bold diagonal cleave slash behind the knight for energy.
    slash = Image.new("RGBA", big, (0, 0, 0, 0))
    sd = ImageDraw.Draw(slash)
    sd.line([(int(big[0] * 0.08), int(big[1] * 0.62)), (int(big[0] * 0.94), int(big[1] * 0.20))],
            fill=(180, 255, 200, 220), width=18)
    sd.line([(int(big[0] * 0.08), int(big[1] * 0.62)), (int(big[0] * 0.94), int(big[1] * 0.20))],
            fill=(120, 230, 160, 160), width=42)
    slash = slash.filter(ImageFilter.GaussianBlur(7))
    card = ImageChops.screen(card, slash)

    # Spectral rim halo behind the ghost.
    halo = Image.new("RGBA", big, (0, 0, 0, 0))
    halo.alpha_composite(duotone(knight, (30, 96, 62), (54, 158, 104), (96, 226, 156)).resize(ghost.size, Image.LANCZOS), (gx, gy))
    halo = halo.filter(ImageFilter.GaussianBlur(11))
    card = ImageChops.screen(card, halo)
    card.alpha_composite(ghost, (gx, gy))

    # Soul embers filling the whole frame (top + bottom included).
    d = ImageDraw.Draw(card, "RGBA")
    rng = np.random.default_rng(7)
    for _ in range(150):
        x = rng.integers(0, big[0]); y = rng.integers(0, big[1])
        r = rng.integers(1, 5)
        g = int(rng.integers(150, 240))
        d.ellipse([x - r, y - r, x + r, y + r], fill=(90, g, 140, int(rng.integers(40, 120))))
    card = card.filter(ImageFilter.GaussianBlur(0.4))
    card.alpha_composite(vignette(big, 0.7))

    os.makedirs(os.path.join(CARD_DIR, "big"), exist_ok=True)
    card.convert("RGB").save(os.path.join(CARD_DIR, "big", "nether_curse.png"))
    card.resize((250, 350), Image.LANCZOS).convert("RGB").save(os.path.join(CARD_DIR, "nether_curse.png"))
    print("card nether_curse done")


def disc_icon(subject: Image.Image, disc_inner, disc_outer, rim, out_name, subj_scale=0.82):
    size = 256
    disc = radial_bg((size, size), disc_inner, disc_outer)
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).ellipse([4, 4, size - 4, size - 4], fill=255)
    disc.putalpha(mask)

    s = subject.copy()
    fit = int(size * subj_scale)
    r = min(fit / s.width, fit / s.height)
    s = s.resize((max(1, int(s.width * r)), max(1, int(s.height * r))), Image.LANCZOS)
    disc.alpha_composite(s, ((size - s.width) // 2, (size - s.height) // 2))
    disc.putalpha(ImageChops.multiply(disc.getchannel("A"), mask))

    d = ImageDraw.Draw(disc, "RGBA")
    d.ellipse([4, 4, size - 4, size - 4], outline=rim, width=8)

    os.makedirs(os.path.join(POWER_DIR, "big"), exist_ok=True)
    disc.save(os.path.join(POWER_DIR, "big", out_name))
    disc.resize((64, 64), Image.LANCZOS).save(os.path.join(POWER_DIR, out_name))
    print(f"power {out_name} done")


def main():
    sheet = Image.open(SHEET).convert("RGB")
    make_card()
    helmet = remove_bg(sheet.crop(HELMET_BOX))
    disc_icon(helmet, (86, 24, 30), (18, 6, 10), (210, 60, 70, 255),
              "cowardice_power.png", subj_scale=0.94)
    blade = remove_bg(sheet.crop(BLADE_BOX))
    disc_icon(blade, (54, 40, 86), (12, 8, 22), (176, 150, 210, 255),
              "vertical_slash_protection_power.png", subj_scale=0.9)


if __name__ == "__main__":
    main()
