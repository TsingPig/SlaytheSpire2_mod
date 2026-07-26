"""Generate Black Knight diagonal-slash A frames from the clean idle cutout.

The source is already transparent.  This script separates the weapon-side arm
and axe with a feathered art mask, rotates that layer around the shoulder, and
adds a hand-painted green/purple slash trail on the impact frames.  It is a
deterministic technical-art pass intended for rapid iteration.
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "NinjaMod/images/character/blackknight_idle/idle_00.png"
OUTPUT = ROOT / "NinjaMod/images/character/blackknight_attacks/diagonal_a"
PREVIEW = ROOT / "temp/blackknight_diagonal_a_preview.gif"
CONTACT = ROOT / "temp/blackknight_diagonal_a_contact.png"

SOURCE_SIZE = (768, 512)
CANVAS_SIZE = (1024, 768)
OFFSET = ((CANVAS_SIZE[0] - SOURCE_SIZE[0]) // 2, (CANVAS_SIZE[1] - SOURCE_SIZE[1]) // 2)
PIVOT_SOURCE = (326, 214)
PIVOT = (PIVOT_SOURCE[0] + OFFSET[0], PIVOT_SOURCE[1] + OFFSET[1])

# Key time, weapon angle, body counter-rotation, body vertical recoil.
# The order is deliberate: close distance first, raise to screen-right/up,
# then drive the axe through the body line into screen-left/down.
KEY_TIMES = (0.0, 0.10, 0.22, 0.34, 0.48, 0.62, 0.72, 0.80, 0.90, 1.02, 1.15)
KEY_POSES = (
    (0.0, 0.0, 0),
    (0.0, -1.0, 2),
    (4.0, -2.0, 3),
    (10.0, -3.0, 4),
    (35.0, -4.0, 8),
    (62.0, -5.0, 11),
    (68.0, -6.0, 10),
    (-132.0, 7.0, -14),
    (-138.0, 6.0, -12),
    (-45.0, 2.0, -4),
    (0.0, 0.0, 0),
)
FRAME_COUNT = 24
ANIMATION_SECONDS = 1.15


def canvas_from(image: Image.Image) -> Image.Image:
    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    canvas.alpha_composite(image, OFFSET)
    return canvas


def weapon_mask(source: Image.Image) -> Image.Image:
    """Select the right arm, hand, handle and axe while preserving soft edges."""
    alpha = source.getchannel("A")
    shape = Image.new("L", SOURCE_SIZE, 0)
    draw = ImageDraw.Draw(shape)
    draw.polygon(
        [
            (254, 124),
            (350, 112),
            (430, 145),
            (767, 150),
            (767, 388),
            (420, 360),
            (300, 310),
            (248, 252),
        ],
        fill=255,
    )
    # Keep the mask tight near the torso and softer at the rotating seam.
    shape = shape.filter(ImageFilter.GaussianBlur(2.2))
    return Image.composite(alpha, Image.new("L", SOURCE_SIZE, 0), shape)


def split_layers(source: Image.Image) -> tuple[Image.Image, Image.Image]:
    mask = weapon_mask(source)
    weapon = source.copy()
    weapon.putalpha(mask)

    body = source.copy()
    # Remove the original weapon silhouette so raising the axe cannot leave a
    # ghost image. A slightly expanded matte avoids a bright double edge.
    erase = mask.filter(ImageFilter.MaxFilter(5))
    body.putalpha(ImageChops.subtract(source.getchannel("A"), erase))
    return canvas_from(body), canvas_from(weapon)


def rotate_about(image: Image.Image, degrees: float, pivot: tuple[int, int]) -> Image.Image:
    return image.rotate(
        degrees,
        resample=Image.Resampling.BICUBIC,
        center=pivot,
        expand=False,
    )


def slash_trail(time_seconds: float) -> Image.Image:
    trail = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    if not 0.73 <= time_seconds <= 0.98:
        return trail

    draw = ImageDraw.Draw(trail, "RGBA")
    if time_seconds <= 0.80:
        opacity = (time_seconds - 0.73) / 0.07
    else:
        opacity = max(0.0, 1.0 - (time_seconds - 0.80) / 0.18)
    # Long, thick screen-right/up -> screen-left/down spectral axe trail.
    points = [
        (860, 96),
        (812, 132),
        (760, 174),
        (704, 220),
        (644, 270),
        (582, 324),
        (520, 380),
        (460, 438),
        (404, 496),
        (356, 554),
        (316, 612),
        (286, 662),
    ]

    # Draw every segment separately with a tapered width. Rounded joints and
    # non-uniform thickness make the result read as a spectral axe arc rather
    # than a modern laser beam.
    for index, (start, end) in enumerate(zip(points, points[1:])):
        t = (index + 0.5) / (len(points) - 1)
        taper = max(0.15, math.sin(math.pi * t))
        widths = (int(88 * taper), int(50 * taper), int(21 * taper), max(2, int(5 * taper)))
        colors = (
            (34, 18, 52, int(105 * opacity)),
            (118, 58, 184, int(175 * opacity)),
            (70, 226, 137, int(225 * opacity)),
            (226, 255, 236, int(235 * opacity)),
        )
        for width, color in zip(widths, colors):
            draw.line((start, end), fill=color, width=width)
            radius = max(1, width // 2)
            draw.ellipse(
                (end[0] - radius, end[1] - radius, end[0] + radius, end[1] + radius),
                fill=color,
            )

    # Tapered spectral fragments around the contact portion.
    for x, y, dx, dy in (
        (566, 338, 40, -14),
        (498, 410, 34, 22),
        (422, 492, 25, 36),
        (638, 274, 44, 10),
    ):
        draw.polygon(
            [(x, y), (x + dx, y + dy), (x + dx // 3, y + dy + 12)],
            fill=(83, 225, 137, int(170 * opacity)),
        )
    return trail.filter(ImageFilter.GaussianBlur(1.4))


def make_frame(
    body: Image.Image,
    weapon: Image.Image,
    time_seconds: float,
    weapon_angle: float,
    body_angle: float,
    recoil_y: int,
) -> Image.Image:
    pose = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))

    # A dark shoulder fill sits behind the articulated arm and hides the small
    # separation seam without inventing new silhouette outside the character.
    seam = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    seam_draw = ImageDraw.Draw(seam)
    seam_draw.ellipse(
        (PIVOT[0] - 62, PIVOT[1] - 58, PIVOT[0] + 48, PIVOT[1] + 68),
        fill=(19, 22, 29, 255),
        outline=(91, 76, 145, 220),
        width=4,
    )

    pose.alpha_composite(body)
    pose.alpha_composite(seam)
    pose.alpha_composite(rotate_about(weapon, weapon_angle, PIVOT))
    pose.alpha_composite(slash_trail(time_seconds))

    if body_angle:
        pose = rotate_about(pose, body_angle, (CANVAS_SIZE[0] // 2, CANVAS_SIZE[1] // 2))
    if recoil_y:
        shifted = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
        shifted.alpha_composite(pose, (0, recoil_y))
        pose = shifted
    return pose


def interpolate(values: tuple[float, ...], time_seconds: float) -> float:
    if time_seconds <= KEY_TIMES[0]:
        return values[0]
    if time_seconds >= KEY_TIMES[-1]:
        return values[-1]
    for index, (start, end) in enumerate(zip(KEY_TIMES, KEY_TIMES[1:])):
        if start <= time_seconds <= end:
            amount = (time_seconds - start) / (end - start)
            # Smoothstep preserves readable holds around the authored key poses.
            amount = amount * amount * (3.0 - 2.0 * amount)
            return values[index] + (values[index + 1] - values[index]) * amount
    return values[-1]


def pose_at(time_seconds: float) -> tuple[float, float, int]:
    weapon_angles = tuple(pose[0] for pose in KEY_POSES)
    body_angles = tuple(pose[1] for pose in KEY_POSES)
    recoils = tuple(float(pose[2]) for pose in KEY_POSES)
    return (
        interpolate(weapon_angles, time_seconds),
        interpolate(body_angles, time_seconds),
        round(interpolate(recoils, time_seconds)),
    )


def lunge_at(time_seconds: float) -> int:
    # Positive X is the player-facing direction in the current combat layout.
    positions = (0.0, 0.0, 55.0, 130.0, 175.0, 185.0, 185.0, 205.0, 205.0, 120.0, 0.0)
    return round(interpolate(positions, time_seconds))


def preview_background(size: tuple[int, int]) -> Image.Image:
    bg = Image.new("RGBA", size, (31, 31, 39, 255))
    draw = ImageDraw.Draw(bg)
    for y in range(0, size[1], 24):
        for x in range(0, size[0], 24):
            if (x // 24 + y // 24) % 2:
                draw.rectangle((x, y, x + 23, y + 23), fill=(42, 43, 54, 255))
    return bg


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)

    source = Image.open(SOURCE).convert("RGBA")
    body, weapon = split_layers(source)
    frame_times = [
        ANIMATION_SECONDS * index / (FRAME_COUNT - 1)
        for index in range(FRAME_COUNT)
    ]
    frames = [
        make_frame(body, weapon, time_seconds, *pose_at(time_seconds))
        for time_seconds in frame_times
    ]
    for index, frame in enumerate(frames):
        frame.save(OUTPUT / f"diagonal_a_{index:02d}.png")

    # Contact sheet is downscaled for quick visual review.
    thumb_size = (256, 192)
    contact = Image.new("RGB", (thumb_size[0] * 6, thumb_size[1] * 4), (28, 29, 36))
    for index, frame in enumerate(frames):
        bg = preview_background(CANVAS_SIZE)
        bg.alpha_composite(frame)
        thumb = bg.resize(thumb_size, Image.Resampling.LANCZOS).convert("RGB")
        contact.paste(thumb, ((index % 6) * thumb_size[0], (index // 6) * thumb_size[1]))
    contact.save(CONTACT)

    # Preview also shows the actual player-facing combat lunge. Runtime movement is
    # handled by the Godot Body:position track, not baked into final PNGs.
    preview_frames: list[Image.Image] = []
    for frame, time_seconds in zip(frames, frame_times):
        bg = preview_background(CANVAS_SIZE)
        bg.alpha_composite(frame, (lunge_at(time_seconds), 0))
        preview_frames.append(bg.convert("P", palette=Image.Palette.ADAPTIVE))
    preview_frames[0].save(
        PREVIEW,
        save_all=True,
        append_images=preview_frames[1:],
        duration=48,
        loop=0,
        disposal=2,
    )

    print(f"Wrote {len(frames)} attack frames to {OUTPUT}")
    print(f"Contact sheet: {CONTACT}")
    print(f"Loop preview: {PREVIEW}")


if __name__ == "__main__":
    main()
