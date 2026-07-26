"""Generate the Black Knight's heavy horizontal-slash animation.

The animation deliberately follows the same deterministic technical-art
pipeline as ``generate_diagonal_slash_frames.py``: the clean idle cutout is
split into body and weapon-side layers, the axe is articulated around the
shoulder, and authored VFX are composited without repainting the knight.

The visual concept is "Shadow Sever": darkness collapses into the axe during
the windup, a hairline execution mark appears before the weapon moves, and the
mark ruptures into a broad delayed rift only after the knight commits.

Timing at 20 FPS (28 frames / 1.4 seconds):
  00-04  sink and wind up
  05-09  accelerate toward the target (runtime movement)
  10-11  hard stop / anticipation hold
  12-16  explosive horizontal swing
  17-18  impact freeze
  19-27  slower recovery and retreat
"""

from __future__ import annotations

import math
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageEnhance, ImageFilter, ImageOps


ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "NinjaMod/images/character/blackknight_idle/idle_00.png"
VFX_SOURCE = ROOT / "BlackKnight/assets/blackknight_horizontal_slash_vfx.png"
OUTPUT = ROOT / "NinjaMod/images/character/blackknight_attacks/horizontal"
PREVIEW = ROOT / "temp/blackknight_horizontal_preview.gif"
CONTACT = ROOT / "temp/blackknight_horizontal_contact.png"

SOURCE_SIZE = (768, 512)
CANVAS_SIZE = (1024, 768)
OFFSET = ((CANVAS_SIZE[0] - SOURCE_SIZE[0]) // 2, (CANVAS_SIZE[1] - SOURCE_SIZE[1]) // 2)
PIVOT_SOURCE = (326, 214)
PIVOT = (PIVOT_SOURCE[0] + OFFSET[0], PIVOT_SOURCE[1] + OFFSET[1])

FRAME_COUNT = 28
FPS = 20
ANIMATION_SECONDS = FRAME_COUNT / FPS

# Frame, weapon angle, whole-pose counter rotation, vertical recoil.
# The swing travels clockwise below the torso and finishes left/up at about 7°.
POSE_KEYS = (
    (0, 0.0, 0.0, 0),
    (2, 5.0, -1.5, 5),
    (4, 12.0, -3.5, 11),
    (7, 15.0, -5.0, 15),
    (9, 16.0, -5.5, 16),
    (11, 16.0, -5.5, 16),
    (12, -24.0, -1.0, 12),
    (13, -92.0, 3.0, 4),
    (14, -154.0, 5.5, -3),
    (15, -187.0, 7.0, -7),
    (18, -187.0, 7.0, -7),
    (20, -145.0, 4.5, -3),
    (22, -88.0, 2.0, 2),
    (24, -28.0, 0.0, 5),
    (27, 0.0, 0.0, 0),
)


def canvas_from(image: Image.Image) -> Image.Image:
    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    canvas.alpha_composite(image, OFFSET)
    return canvas


def weapon_mask(source: Image.Image) -> Image.Image:
    """Select the right arm, hand, handle and axe with a feathered torso seam."""
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
    shape = shape.filter(ImageFilter.GaussianBlur(2.2))
    return Image.composite(alpha, Image.new("L", SOURCE_SIZE, 0), shape)


def split_layers(source: Image.Image) -> tuple[Image.Image, Image.Image]:
    mask = weapon_mask(source)
    weapon = source.copy()
    weapon.putalpha(mask)

    body = source.copy()
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


def interpolate_pose(frame_index: int) -> tuple[float, float, int]:
    for start, end in zip(POSE_KEYS, POSE_KEYS[1:]):
        if start[0] <= frame_index <= end[0]:
            span = max(1, end[0] - start[0])
            amount = (frame_index - start[0]) / span
            amount = amount * amount * (3.0 - 2.0 * amount)
            return (
                start[1] + (end[1] - start[1]) * amount,
                start[2] + (end[2] - start[2]) * amount,
                round(start[3] + (end[3] - start[3]) * amount),
            )
    last = POSE_KEYS[-1]
    return last[1], last[2], last[3]


def base_pose(
    body: Image.Image,
    weapon: Image.Image,
    weapon_angle: float,
    body_angle: float,
    recoil_y: int,
) -> Image.Image:
    pose = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))

    # The source is a flattened illustration, so removing the weapon-side arm
    # also uncovers a few pixels that originally belonged to the cape/torso.
    # A nearly black imprint of the original layer fills those pixels from
    # behind and doubles as a subtle supernatural weapon afterimage.
    underpaint = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    underpaint_color = Image.new("RGBA", CANVAS_SIZE, (25, 18, 35, 255))
    underpaint_color.putalpha(weapon.getchannel("A").point(lambda value: round(value * 0.68)))
    underpaint.alpha_composite(underpaint_color)

    seam = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    seam_draw = ImageDraw.Draw(seam)
    seam_draw.ellipse(
        (PIVOT[0] - 62, PIVOT[1] - 58, PIVOT[0] + 48, PIVOT[1] + 68),
        fill=(19, 22, 29, 255),
        outline=(91, 76, 145, 220),
        width=4,
    )

    pose.alpha_composite(underpaint)
    pose.alpha_composite(body)
    pose.alpha_composite(seam)
    pose.alpha_composite(rotate_about(weapon, weapon_angle, PIVOT))
    if body_angle:
        pose = rotate_about(pose, body_angle, (CANVAS_SIZE[0] // 2, CANVAS_SIZE[1] // 2))
    if recoil_y:
        shifted = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
        shifted.alpha_composite(pose, (0, recoil_y))
        pose = shifted
    return pose


def tint_shadow(image: Image.Image, opacity: float) -> Image.Image:
    """Turn a pose into a dark purple-black afterimage."""
    result = Image.new("RGBA", image.size, (0, 0, 0, 0))
    alpha = image.getchannel("A").point(lambda value: round(value * opacity))
    color = Image.new("RGBA", image.size, (28, 18, 43, 255))
    detail = ImageEnhance.Contrast(image.convert("RGB")).enhance(0.35)
    detail = ImageEnhance.Brightness(detail).enhance(0.24)
    color = Image.blend(color.convert("RGB"), detail, 0.32).convert("RGBA")
    color.putalpha(alpha)
    # A short horizontal smear preserves the silhouette while reading as speed.
    for dx, local_opacity in ((18, 0.18), (9, 0.32), (0, 1.0)):
        layer = color.copy()
        if local_opacity < 1.0:
            layer.putalpha(layer.getchannel("A").point(lambda value: round(value * local_opacity)))
        result.alpha_composite(layer, (dx, 0))
    return result.filter(ImageFilter.GaussianBlur(0.7))


def add_afterimages(frame: Image.Image, held_pose: Image.Image, frame_index: int) -> None:
    """Bake two path ghosts opposite the runtime lunge direction."""
    if not 6 <= frame_index <= 15:
        return
    if frame_index <= 10:
        strength = (frame_index - 5) / 5
    else:
        strength = max(0.0, 1.0 - (frame_index - 10) / 6)
    far = tint_shadow(held_pose, 0.18 * strength)
    near = tint_shadow(held_pose, 0.32 * strength)
    frame.alpha_composite(far, (190, 0))
    frame.alpha_composite(near, (94, 0))


def windup_collapse(frame_index: int) -> Image.Image:
    """Broken shadow orbits contract into the axe during anticipation."""
    layer = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    if not 1 <= frame_index <= 11:
        return layer

    draw = ImageDraw.Draw(layer, "RGBA")
    progress = frame_index / 11
    center_x = 785 - round(progress * 12)
    center_y = 405 + round(progress * 5)
    radius_x = round(138 - progress * 72)
    radius_y = round(82 - progress * 43)
    alpha = round(52 + progress * 105)
    phase = frame_index * 29

    for ring, width in ((0, 7), (18, 4), (34, 2)):
        box = (
            center_x - radius_x - ring,
            center_y - radius_y - ring // 2,
            center_x + radius_x + ring,
            center_y + radius_y + ring // 2,
        )
        for offset, sweep in ((0, 78), (121, 55), (232, 70)):
            draw.arc(
                box,
                start=(phase + offset) % 360,
                end=(phase + offset + sweep) % 360,
                fill=(78 + ring, 38, 112 + ring, max(18, alpha - ring * 2)),
                width=width,
            )

    # Fixed shards stream inward, avoiding unstable random animation.
    for index, angle in enumerate((18, 54, 108, 162, 218, 274, 326)):
        radians = math.radians(angle + phase * 0.35)
        distance = 58 + (index % 3) * 24 + round((1.0 - progress) * 52)
        x = center_x + math.cos(radians) * distance
        y = center_y + math.sin(radians) * distance * 0.58
        dx = (center_x - x) * 0.18
        dy = (center_y - y) * 0.18
        draw.polygon(
            [
                (round(x), round(y)),
                (round(x + dx), round(y + dy)),
                (round(x - dy * 0.35), round(y + dx * 0.18)),
            ],
            fill=(33, 19, 48, min(210, alpha + 20)),
        )
    return layer.filter(ImageFilter.GaussianBlur(0.65))


def execution_line(frame_index: int) -> Image.Image:
    """A still hairline marks the future cut before the broad rift exists."""
    layer = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    alpha_by_frame = {9: 30, 10: 88, 11: 178, 12: 218, 13: 150, 14: 72}
    alpha = alpha_by_frame.get(frame_index, 0)
    if alpha == 0:
        return layer

    draw = ImageDraw.Draw(layer, "RGBA")
    start = (22, 324)
    end = (892, 424)
    # A displaced dark-red underside makes the line feel like a cut in space,
    # while the white-lavender upper line stays only two pixels wide.
    draw.line((*start, *end), fill=(41, 10, 28, min(210, alpha)), width=9)
    draw.line((start[0], start[1] - 2, end[0], end[1] - 2), fill=(139, 67, 154, alpha), width=4)
    draw.line((start[0], start[1] - 3, end[0], end[1] - 3), fill=(246, 235, 255, alpha), width=2)

    # Deliberate gaps stop it reading as a sci-fi laser.
    eraser = Image.new("L", CANVAS_SIZE, 255)
    erase_draw = ImageDraw.Draw(eraser)
    for x, width in ((146, 15), (318, 9), (606, 18), (782, 11)):
        y = round(324 + (x - 22) * (100 / 870))
        erase_draw.rectangle((x, y - 11, x + width, y + 11), fill=0)
    layer.putalpha(ImageChops.multiply(layer.getchannel("A"), eraser))
    return layer.filter(ImageFilter.GaussianBlur(0.35))


def recovery_fragments(frame_index: int) -> Image.Image:
    """Rift splinters recoil toward the cape while the knight retreats."""
    layer = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    if not 19 <= frame_index <= 25:
        return layer
    progress = (frame_index - 19) / 6
    alpha = round(115 * (1.0 - progress))
    draw = ImageDraw.Draw(layer, "RGBA")
    for index, (x, y) in enumerate(((120, 340), (216, 365), (344, 386), (520, 408), (676, 429))):
        pull = round((510 - x) * progress * 0.62)
        px = x + pull
        py = y + round((392 - y) * progress * 0.55)
        length = max(5, round((26 - index * 3) * (1.0 - progress * 0.7)))
        draw.polygon(
            [(px, py), (px + length, py + 5), (px + length // 3, py + 11)],
            fill=(47, 22, 62, alpha),
        )
        if index % 2 == 0:
            draw.line((px, py, px + length + 8, py + 7), fill=(129, 44, 92, alpha), width=2)
    return layer.filter(ImageFilter.GaussianBlur(0.45))


def impact_overdrive(frame_index: int) -> Image.Image:
    """Extra contact rays and scattered chips on the rupture/hit-stop frames."""
    layer = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    if not 14 <= frame_index <= 18:
        return layer

    alpha_by_frame = {14: 210, 15: 255, 16: 225, 17: 165, 18: 95}
    expansion_by_frame = {14: 0.72, 15: 1.0, 16: 1.18, 17: 1.34, 18: 1.48}
    alpha = alpha_by_frame[frame_index]
    expansion = expansion_by_frame[frame_index]
    origin = (48, 328)
    draw = ImageDraw.Draw(layer, "RGBA")

    # Oversized impact spokes make the contact point read even at combat zoom.
    for index in range(28):
        angle = math.radians(-78 + index * (156 / 27))
        base_length = 48 + (index * 37) % 118
        length = round(base_length * expansion)
        start_radius = 18 + (index % 4) * 4
        x1 = origin[0] + math.cos(angle) * start_radius
        y1 = origin[1] + math.sin(angle) * start_radius
        x2 = origin[0] + math.cos(angle) * length
        y2 = origin[1] + math.sin(angle) * length
        color = (
            (255, 249, 255, alpha)
            if index % 5 == 0
            else (168, 74, 209, round(alpha * 0.82))
            if index % 2 == 0
            else (149, 24, 68, round(alpha * 0.72))
        )
        draw.line((round(x1), round(y1), round(x2), round(y2)), fill=color, width=1 + index % 3)

    # Shards peel away above and below the cut while the knight remains frozen.
    drift = frame_index - 14
    for index in range(22):
        x = 96 + index * 36 + drift * (8 + index % 4)
        center_y = 334 + round((x - 48) * 0.115)
        side = -1 if index % 2 == 0 else 1
        y = center_y + side * (20 + (index * 19) % 76 + drift * (5 + index % 3))
        length = 10 + (index * 11) % 31
        draw.polygon(
            [
                (x, y),
                (x + length, y + side * (3 + index % 7)),
                (x + length // 3, y + side * (9 + index % 5)),
            ],
            fill=(
                48 + index % 3 * 28,
                20,
                66 + index % 4 * 24,
                round(alpha * (0.62 if index % 3 else 0.9)),
            ),
        )

    # Two broken shock rings expand from the player-side contact point.
    ring_radius = round(52 * expansion)
    for extra, width in ((0, 5), (22, 2)):
        box = (
            origin[0] - ring_radius - extra,
            origin[1] - ring_radius - extra,
            origin[0] + ring_radius + extra,
            origin[1] + ring_radius + extra,
        )
        draw.arc(box, 205, 330, fill=(224, 187, 255, round(alpha * 0.72)), width=width)
        draw.arc(box, 24, 148, fill=(128, 27, 83, round(alpha * 0.58)), width=width)
    return layer.filter(ImageFilter.GaussianBlur(0.38))


def prepare_vfx() -> Image.Image:
    source = Image.open(VFX_SOURCE).convert("RGBA")
    bbox = source.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError(f"Horizontal slash VFX has no visible pixels: {VFX_SOURCE}")
    effect = source.crop(bbox)
    effect = ImageOps.flip(effect)
    effect.thumbnail((1020, 330), Image.Resampling.LANCZOS)

    canvas = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    x = -4
    # Align the white core with the axe's final leftward blade line.
    y = 204 + (330 - effect.height) // 2
    canvas.alpha_composite(effect, (x, y))
    return canvas


def vfx_frame(master: Image.Image, frame_index: int) -> Image.Image:
    if not 12 <= frame_index <= 20:
        return Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))

    reveal = {
        # The mark holds almost still for two frames, then tears fully open.
        12: 0.08,
        13: 0.28,
        14: 1.00,
        15: 1.00,
        16: 0.96,
        17: 0.82,
        18: 0.67,
        19: 0.38,
        20: 0.16,
    }[frame_index]
    opacity = {
        12: 0.18,
        13: 0.45,
        14: 1.00,
        15: 1.00,
        16: 0.92,
        17: 0.78,
        18: 0.62,
        19: 0.38,
        20: 0.18,
    }[frame_index]

    effect = master.copy()
    # Reveal from the knight/weapon side (right) toward the player side (left).
    cutoff = round(CANVAS_SIZE[0] * (1.0 - reveal))
    mask = effect.getchannel("A")
    reveal_mask = Image.new("L", CANVAS_SIZE, 0)
    ImageDraw.Draw(reveal_mask).rectangle((cutoff, 0, CANVAS_SIZE[0], CANVAS_SIZE[1]), fill=255)
    mask = ImageChops.multiply(mask, reveal_mask)
    mask = mask.point(lambda value: round(value * opacity))
    effect.putalpha(mask)

    # Guarantee a readable inner blade even after chroma-key extraction.
    core = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(core, "RGBA")
    start_x = max(cutoff, 36)
    end_x = 866
    start_y = 326 + round((start_x - 36) * 0.115)
    end_y = 421
    if start_x < end_x:
        draw.line((start_x, start_y, end_x, end_y), fill=(111, 65, 153, round(150 * opacity)), width=15)
        draw.line((start_x, start_y, end_x, end_y), fill=(214, 189, 255, round(235 * opacity)), width=6)
        draw.line((start_x, start_y, end_x, end_y), fill=(255, 255, 255, round(255 * opacity)), width=2)
    effect.alpha_composite(core.filter(ImageFilter.GaussianBlur(0.45)))
    return effect


def dim_anticipation(pose: Image.Image, frame_index: int) -> Image.Image:
    if frame_index not in (10, 11):
        return pose
    rgb = ImageEnhance.Brightness(pose.convert("RGB")).enhance(0.72)
    purple = Image.new("RGB", pose.size, (41, 32, 55))
    rgb = Image.blend(rgb, purple, 0.12)
    result = rgb.convert("RGBA")
    result.putalpha(pose.getchannel("A"))
    # The blade edge catches a narrow pale-lavender highlight. The separate
    # execution line marks the target space rather than merely tracing the axe.
    highlight = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
    draw = ImageDraw.Draw(highlight, "RGBA")
    draw.line((500, 330, 846, 374), fill=(230, 214, 255, 130 if frame_index == 10 else 205), width=3)
    result.alpha_composite(highlight.filter(ImageFilter.GaussianBlur(0.55)))
    return result


def make_frames(body: Image.Image, weapon: Image.Image) -> list[Image.Image]:
    vfx = prepare_vfx()
    held_pose = base_pose(body, weapon, 16.0, -5.5, 16)
    frames: list[Image.Image] = []

    for frame_index in range(FRAME_COUNT):
        weapon_angle, body_angle, recoil_y = interpolate_pose(frame_index)
        pose = base_pose(body, weapon, weapon_angle, body_angle, recoil_y)
        pose = dim_anticipation(pose, frame_index)

        frame = Image.new("RGBA", CANVAS_SIZE, (0, 0, 0, 0))
        frame.alpha_composite(windup_collapse(frame_index))
        add_afterimages(frame, held_pose, frame_index)
        frame.alpha_composite(pose)
        frame.alpha_composite(execution_line(frame_index))
        frame.alpha_composite(vfx_frame(vfx, frame_index))
        frame.alpha_composite(impact_overdrive(frame_index))
        frame.alpha_composite(recovery_fragments(frame_index))
        frames.append(frame)
    return frames


def lunge_at(frame_index: int) -> int:
    """Preview-only movement; runtime drives the Visuals root toward the target."""
    positions = {
        0: 0,
        4: 0,
        5: -42,
        6: -108,
        7: -174,
        8: -224,
        9: -244,
        18: -244,
        19: -228,
        21: -182,
        23: -124,
        25: -58,
        27: 0,
    }
    keys = sorted(positions)
    if frame_index <= keys[0]:
        return positions[keys[0]]
    for start, end in zip(keys, keys[1:]):
        if start <= frame_index <= end:
            amount = (frame_index - start) / max(1, end - start)
            amount = amount * amount * (3.0 - 2.0 * amount)
            return round(positions[start] + (positions[end] - positions[start]) * amount)
    return 0


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
    if source.size != SOURCE_SIZE:
        raise RuntimeError(f"Expected {SOURCE_SIZE} source, got {source.size}: {SOURCE}")
    body, weapon = split_layers(source)
    frames = make_frames(body, weapon)

    for index, frame in enumerate(frames):
        frame.save(OUTPUT / f"horizontal_{index:02d}.png")

    thumb_size = (256, 192)
    columns = 7
    rows = math.ceil(FRAME_COUNT / columns)
    contact = Image.new("RGB", (thumb_size[0] * columns, thumb_size[1] * rows), (28, 29, 36))
    for index, frame in enumerate(frames):
        bg = preview_background(CANVAS_SIZE)
        bg.alpha_composite(frame)
        thumb = bg.resize(thumb_size, Image.Resampling.LANCZOS).convert("RGB")
        contact.paste(thumb, ((index % columns) * thumb_size[0], (index // columns) * thumb_size[1]))
    contact.save(CONTACT)

    preview_frames: list[Image.Image] = []
    for index, frame in enumerate(frames):
        bg = preview_background(CANVAS_SIZE)
        bg.alpha_composite(frame, (lunge_at(index), 0))
        preview_frames.append(bg.convert("P", palette=Image.Palette.ADAPTIVE))
    preview_frames[0].save(
        PREVIEW,
        save_all=True,
        append_images=preview_frames[1:],
        duration=round(1000 / FPS),
        loop=0,
        disposal=2,
    )

    print(f"Wrote {len(frames)} attack frames to {OUTPUT}")
    print(f"Contact sheet: {CONTACT}")
    print(f"Loop preview: {PREVIEW}")


if __name__ == "__main__":
    main()
