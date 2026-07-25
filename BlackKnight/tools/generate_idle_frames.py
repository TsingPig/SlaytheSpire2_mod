"""Build a four-frame Black Knight idle prototype from the current battle art.

The source art has a paper background, so this script first flood-fills that
background, keeps the largest connected foreground component, then applies
small non-rigid deformations around the torso.  Output frames share the exact
same canvas and root registration, which makes them safe to swap in Godot.
"""

from __future__ import annotations

from collections import deque
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

    # Close tiny antialias gaps so pale armor plates remain connected, then
    # feather only the outermost pixel. The source already uses a light ink rim.
    matte = Image.fromarray((foreground * 255).astype(np.uint8), "L")
    matte = matte.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(0.55))
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


def deform(subject: Image.Image, phase: str) -> Image.Image:
    pixels = np.asarray(subject, dtype=np.float32)
    height, width = pixels.shape[:2]
    yy, xx = np.mgrid[0:height, 0:width].astype(np.float32)

    torso = np.exp(-(((xx - 215) / 175) ** 2 + ((yy - 205) / 155) ** 2))
    chest = np.exp(-(((xx - 205) / 120) ** 2 + ((yy - 150) / 105) ** 2))
    head = np.exp(-(((xx - 180) / 75) ** 2 + ((yy - 76) / 72) ** 2))
    claw = np.exp(-(((xx - 63) / 90) ** 2 + ((yy - 205) / 90) ** 2))
    weapon = np.exp(-(((xx - 480) / 245) ** 2 + ((yy - 205) / 105) ** 2))
    cape = np.exp(-(((xx - 245) / 165) ** 2 + ((yy - 338) / 100) ** 2))

    dx = np.zeros_like(xx)
    dy = np.zeros_like(yy)
    if phase == "inhale":
        dx += -0.013 * (xx - 210) * chest
        dy += 6.0 * chest + 2.2 * head
        dy += -2.0 * cape
        dx += 1.8 * claw
        dy += 1.5 * weapon
    elif phase == "shift":
        dx += -4.2 * torso + 1.8 * head
        dy += 1.2 * chest - 1.4 * cape
        dx += 1.2 * weapon
    elif phase == "exhale":
        dx += 0.011 * (xx - 210) * chest
        dy += -5.0 * chest - 2.0 * head
        dx += -2.2 * claw
        dy += 2.7 * cape - 2.0 * weapon

    # The displacement describes where the visible feature moves, so inverse
    # sampling reads from the opposite direction.
    warped = bilinear_sample(pixels, xx - dx, yy - dy)
    return Image.fromarray(warped, "RGBA")


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
    raw_frames = [
        subject,
        deform(subject, "inhale"),
        deform(subject, "shift"),
        deform(subject, "exhale"),
    ]
    frames = [pad_frame(frame) for frame in raw_frames]

    for index, frame in enumerate(frames):
        frame.save(OUTPUT / f"idle_{index:02d}.png")

    contact = Image.new("RGBA", (CANVAS_SIZE[0] * 2, CANVAS_SIZE[1] * 2), (36, 37, 43, 255))
    for index, frame in enumerate(frames):
        contact.alpha_composite(frame, ((index % 2) * CANVAS_SIZE[0], (index // 2) * CANVAS_SIZE[1]))
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
        duration=500,
        loop=0,
        disposal=2,
    )

    print(f"Wrote {len(frames)} frames to {OUTPUT}")
    print(f"Contact sheet: {CONTACT}")
    print(f"Loop preview: {PREVIEW}")


if __name__ == "__main__":
    main()
