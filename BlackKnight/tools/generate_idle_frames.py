"""Build a twelve-frame Black Knight idle loop from the current battle art.

The source art has a paper background, so this script first flood-fills that
background, keeps the largest connected foreground component, then applies
small non-rigid deformations around the torso.  Output frames share the exact
same canvas and root registration, which makes them safe to swap in Godot.
"""

from __future__ import annotations

from collections import deque
import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "NinjaMod/images/character/blackknight_battle.png"
OUTPUT = ROOT / "NinjaMod/images/character/blackknight_idle"
PREVIEW = ROOT / "temp/blackknight_idle_preview.gif"
CONTACT = ROOT / "temp/blackknight_idle_contact.png"
CANVAS_SIZE = (768, 512)


def connected_from_border(candidate: np.ndarray) -> np.ndarray:
    height, width = candidate.shape
    seen = np.zeros_like(candidate, dtype=bool)
    queue: deque[tuple[int, int]] = deque()

    for x in range(width):
        if candidate[0, x]:
            seen[0, x] = True
            queue.append((x, 0))
        if candidate[height - 1, x]:
            seen[height - 1, x] = True
            queue.append((x, height - 1))
    for y in range(height):
        if candidate[y, 0] and not seen[y, 0]:
            seen[y, 0] = True
            queue.append((0, y))
        if candidate[y, width - 1] and not seen[y, width - 1]:
            seen[y, width - 1] = True
            queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height and candidate[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                queue.append((nx, ny))
    return seen


def largest_component(mask: np.ndarray) -> np.ndarray:
    height, width = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    best: list[tuple[int, int]] = []

    for y in range(height):
        for x in range(width):
            if not mask[y, x] or seen[y, x]:
                continue
            component: list[tuple[int, int]] = []
            queue = deque([(x, y)])
            seen[y, x] = True
            while queue:
                px, py = queue.popleft()
                component.append((px, py))
                for nx, ny in ((px - 1, py), (px + 1, py), (px, py - 1), (px, py + 1)):
                    if 0 <= nx < width and 0 <= ny < height and mask[ny, nx] and not seen[ny, nx]:
                        seen[ny, nx] = True
                        queue.append((nx, ny))
            if len(component) > len(best):
                best = component

    result = np.zeros_like(mask, dtype=bool)
    if best:
        xs, ys = zip(*best)
        result[np.asarray(ys), np.asarray(xs)] = True
    return result


def extract_subject(source: Image.Image) -> Image.Image:
    rgb = np.asarray(source.convert("RGB"), dtype=np.int16)
    border = np.concatenate((rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]))
    paper = np.median(border, axis=0)
    distance = np.sqrt(np.sum((rgb - paper) ** 2, axis=2))

    # The paper is warm, bright and low-chroma. Requiring all three properties
    # avoids cutting pale armor and bone horns merely because they are bright.
    channel_spread = rgb.max(axis=2) - rgb.min(axis=2)
    candidate = (distance < 43) & (rgb.mean(axis=2) > 145) & (channel_spread < 52)
    background = connected_from_border(candidate)
    foreground = largest_component(~background)

    # The original illustration has a pale painted ground shadow connected to
    # the cape. Remove only bright, low-chroma pixels in that bottom strip.
    foreground[(np.indices(foreground.shape)[0] > 399) & (rgb.mean(axis=2) > 62) & (channel_spread < 55)] = False

    # Pull the matte two pixels inward instead of expanding it. The concept art
    # has a pale ink rim around the silhouette; expanding the old matte made
    # that rim even more visible in-game.
    matte = Image.fromarray((foreground * 255).astype(np.uint8), "L")
    matte = matte.filter(ImageFilter.MinFilter(5))

    # Remove the remaining warm, low-chroma rim only where it touches the new
    # outer edge. This preserves pale armor and axe detail away from the
    # silhouette while eliminating the cream/white sticker-like border.
    matte_values = np.asarray(matte).copy()
    inner = np.asarray(matte.filter(ImageFilter.MinFilter(5)))
    edge = (matte_values > 0) & (inner == 0)
    pale_rim = (
        (rgb.mean(axis=2) > 145)
        & (channel_spread < 58)
        & (rgb[:, :, 0] >= rgb[:, :, 2])
    )
    matte_values[edge & pale_rim] = 0
    matte = Image.fromarray(matte_values, "L").filter(ImageFilter.GaussianBlur(0.45))
    rgba = source.convert("RGBA")
    rgba.putalpha(matte)
    return rgba


def bilinear_sample(image: np.ndarray, source_x: np.ndarray, source_y: np.ndarray) -> np.ndarray:
    height, width = image.shape[:2]
    x0 = np.floor(source_x).astype(np.int32)
    y0 = np.floor(source_y).astype(np.int32)
    x1 = x0 + 1
    y1 = y0 + 1

    valid = (x0 >= 0) & (x1 < width) & (y0 >= 0) & (y1 < height)
    x0c = np.clip(x0, 0, width - 1)
    x1c = np.clip(x1, 0, width - 1)
    y0c = np.clip(y0, 0, height - 1)
    y1c = np.clip(y1, 0, height - 1)

    wx = (source_x - x0)[..., None]
    wy = (source_y - y0)[..., None]
    top = image[y0c, x0c] * (1.0 - wx) + image[y0c, x1c] * wx
    bottom = image[y1c, x0c] * (1.0 - wx) + image[y1c, x1c] * wx
    sampled = top * (1.0 - wy) + bottom * wy
    sampled[~valid] = 0
    return np.clip(sampled, 0, 255).astype(np.uint8)


def deform(subject: Image.Image, angle: float) -> Image.Image:
    pixels = np.asarray(subject, dtype=np.float32)
    height, width = pixels.shape[:2]
    yy, xx = np.mgrid[0:height, 0:width].astype(np.float32)

    torso = np.exp(-(((xx - 215) / 175) ** 2 + ((yy - 205) / 155) ** 2))
    chest = np.exp(-(((xx - 205) / 120) ** 2 + ((yy - 150) / 105) ** 2))
    head = np.exp(-(((xx - 180) / 75) ** 2 + ((yy - 76) / 72) ** 2))
    claw = np.exp(-(((xx - 63) / 90) ** 2 + ((yy - 205) / 90) ** 2))
    weapon = np.exp(-(((xx - 480) / 245) ** 2 + ((yy - 205) / 105) ** 2))
    cape = np.exp(-(((xx - 245) / 165) ** 2 + ((yy - 338) / 100) ** 2))
    shoulders = np.exp(-(((xx - 215) / 185) ** 2 + ((yy - 125) / 72) ** 2))
    soul_tail = np.exp(-(((xx - 215) / 90) ** 2 + ((yy - 365) / 92) ** 2))

    breath = math.sin(angle)
    weight_shift = math.sin(angle * 2.0) * 0.42
    arm_lag = math.sin(angle - 0.32)
    cape_lag = math.sin(angle - 0.68) + 0.28 * math.sin(angle * 2.0 + 0.45)
    tail_flow = math.sin(angle - 0.95) + 0.35 * math.sin(angle * 3.0)
    compression = 0.0045 * math.sin(angle * 2.0 + 0.25)

    # Continuous breathing replaces the old four discrete named poses. The
    # weapon and cape use a phase delay so all parts do not reverse together.
    dx = -(0.012 * breath + compression) * (xx - 210) * chest
    dx += -3.8 * weight_shift * torso + 1.5 * weight_shift * head
    dx += 1.8 * arm_lag * claw + 1.25 * arm_lag * weapon
    dx += -2.6 * cape_lag * cape + 2.0 * tail_flow * soul_tail
    dx += 0.9 * arm_lag * shoulders

    dy = 5.6 * breath * chest + 2.0 * breath * head
    dy += 1.7 * arm_lag * weapon + 1.0 * arm_lag * shoulders
    dy += -2.4 * cape_lag * cape + 3.2 * tail_flow * soul_tail
    dy += compression * 190.0 * torso

    # The displacement describes where the visible feature moves, so inverse
    # sampling reads from the opposite direction.
    warped = bilinear_sample(pixels, xx - dx, yy - dy)
    return apply_face_glow(Image.fromarray(warped, "RGBA"), angle)


def apply_face_glow(frame: Image.Image, angle: float) -> Image.Image:
    """Add a localized breathing pulse without changing armor colors."""
    rgba = np.asarray(frame).copy()
    yy, xx = np.mgrid[0:rgba.shape[0], 0:rgba.shape[1]]
    red = rgba[:, :, 0].astype(np.int16)
    green = rgba[:, :, 1].astype(np.int16)
    blue = rgba[:, :, 2].astype(np.int16)
    face = (
        (xx > 105)
        & (xx < 245)
        & (yy > 38)
        & (yy < 175)
        & (red > 105)
        & (red > green * 1.35)
        & (red > blue * 1.25)
        & (rgba[:, :, 3] > 20)
    )
    pulse = 0.5 + 0.5 * math.sin(angle - 0.4)
    rgba[:, :, 0][face] = np.clip(red[face] * (1.08 + 0.28 * pulse), 0, 255)
    rgba[:, :, 1][face] = np.clip(green[face] * (0.9 + 0.12 * pulse), 0, 255)

    lit = Image.fromarray(rgba, "RGBA")
    mask = Image.fromarray((face.astype(np.uint8) * 255), "L").filter(ImageFilter.GaussianBlur(6.0))
    mask = mask.point(lambda value: int(value * (0.10 + 0.24 * pulse)))
    glow = Image.new("RGBA", frame.size, (255, 34, 38, 0))
    glow.putalpha(mask)
    lit.alpha_composite(glow)
    return lit


def pad_frame(frame: Image.Image) -> Image.Image:
    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    offset = ((CANVAS_SIZE[0] - frame.width) // 2, (CANVAS_SIZE[1] - frame.height) // 2)
    canvas.alpha_composite(frame, offset)
    return canvas


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)

    source = Image.open(SOURCE)
    subject = extract_subject(source)
    frame_count = 24
    raw_frames = [
        deform(subject, 2.0 * math.pi * index / frame_count)
        for index in range(frame_count)
    ]
    frames = [pad_frame(frame) for frame in raw_frames]

    for index, frame in enumerate(frames):
        frame.save(OUTPUT / f"idle_{index:02d}.png")

    thumb_size = (256, 171)
    contact = Image.new("RGBA", (thumb_size[0] * 6, thumb_size[1] * 4), (36, 37, 43, 255))
    for index, frame in enumerate(frames):
        preview = Image.new("RGBA", CANVAS_SIZE, (36, 37, 43, 255))
        preview.alpha_composite(frame)
        preview = preview.resize(thumb_size, Image.Resampling.LANCZOS)
        contact.alpha_composite(
            preview,
            ((index % 6) * thumb_size[0], (index // 6) * thumb_size[1]),
        )
    contact.convert("RGB").save(CONTACT)

    preview_frames = []
    for frame in frames:
        checker = Image.new("RGBA", frame.size, (42, 43, 50, 255))
        checker.alpha_composite(frame)
        preview_frames.append(checker.convert("P", palette=Image.Palette.ADAPTIVE))
    preview_frames[0].save(
        PREVIEW,
        save_all=True,
        append_images=preview_frames[1:],
        duration=125,
        loop=0,
        disposal=2,
    )

    print(f"Wrote {len(frames)} frames to {OUTPUT}")
    print(f"Contact sheet: {CONTACT}")
    print(f"Loop preview: {PREVIEW}")


if __name__ == "__main__":
    main()
