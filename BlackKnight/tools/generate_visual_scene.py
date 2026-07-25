"""
Generate the Black Knight battle visual scene:
  NinjaMod/scenes/creature_visuals/blackknight.tscn

The scene mirrors the node layout BaseLib's NCreatureVisualsFactory expects
(unique-named %Visuals / %PhobiaModeVisuals / Bounds / %CenterPos / IntentPos /
%OrbPos / %TalkPos), matching the shipping ninja.tscn so the scene->NCreatureVisuals
conversion succeeds.

%Visuals is a Node2D rig holding:
  - Body   : the composited Black Knight sprite (blackknight_battle.png)
  - Phantom: the same texture, enlarged + purple-tinted + translucent, drawn
             behind Body, hidden by default (revealed in True Form)
  - AnimationPlayer: procedural transform/modulate animations named exactly as
             BlackKnightConfig expects (Idle, CurseCast, slashes, DarkArmor,
             TrueForm*, Hurt, Death). Driven from C# by BlackKnightAnimationController.

Re-runnable. Adjust BODY_* / animation tables below to retune.
"""
from __future__ import annotations

import os

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "NinjaMod", "scenes", "creature_visuals", "blackknight.tscn")
TEX = "res://NinjaMod/images/character/blackknight_battle.png"

BODY_POS = (0, -232)
BODY_SCALE = 0.744  # +20% larger than the original 0.62 (monster was too small)
PHANTOM_POS = (36, -300)
PHANTOM_SCALE = 1.72 * BODY_SCALE


# ── value formatting ────────────────────────────────────────────────────────
def v2(xy) -> str:
    return f"Vector2({_n(xy[0])}, {_n(xy[1])})"


def col(rgba) -> str:
    r, g, b, a = rgba
    return f"Color({_n(r)}, {_n(g)}, {_n(b)}, {_n(a)})"


def _n(x) -> str:
    if isinstance(x, float):
        s = f"{x:.4f}".rstrip("0").rstrip(".")
        return s if s else "0"
    return str(x)


def fval(x) -> str:
    if isinstance(x, tuple) and len(x) == 2:
        return v2(x)
    if isinstance(x, tuple) and len(x) == 4:
        return col(x)
    return _n(x)


# Each animation: (name, length, loop, [ (path, [(t, value), ...]), ... ])
BX, BY = BODY_POS


def anims():
    return [
        ("Idle", 3.0, 1, [
            ("Body:position", [(0, (BX, BY)), (1.5, (BX, BY - 20)), (3.0, (BX, BY))]),
            ("Body:rotation", [(0, -0.02), (1.5, 0.02), (3.0, -0.02)]),
        ]),
        ("TrueFormIdle", 3.0, 1, [
            ("Body:position", [(0, (BX, BY)), (1.5, (BX, BY - 26)), (3.0, (BX, BY))]),
            ("Body:rotation", [(0, -0.025), (1.5, 0.025), (3.0, -0.025)]),
            ("Phantom:position", [(0, PHANTOM_POS), (1.5, (PHANTOM_POS[0], PHANTOM_POS[1] - 18)), (3.0, PHANTOM_POS)]),
        ]),
        ("CurseCast", 1.1, 0, [
            ("Body:position", [(0, (BX, BY)), (0.35, (BX, BY - 30)), (0.7, (BX, BY - 30)), (1.1, (BX, BY))]),
            ("Body:scale", [(0, (BODY_SCALE, BODY_SCALE)), (0.5, (BODY_SCALE * 1.06, BODY_SCALE * 1.06)), (1.1, (BODY_SCALE, BODY_SCALE))]),
            ("Body:modulate", [(0, (1, 1, 1, 1)), (0.5, (1.3, 0.7, 1.4, 1)), (1.1, (1, 1, 1, 1))]),
        ]),
        ("DiagonalSlashA", 0.5, 0, [
            ("Body:rotation", [(0, 0.0), (0.18, 0.28), (0.30, -0.34), (0.5, 0.0)]),
            ("Body:position", [(0, (BX, BY)), (0.30, (BX - 46, BY + 10)), (0.5, (BX, BY))]),
        ]),
        ("DiagonalSlashB", 0.5, 0, [
            ("Body:rotation", [(0, 0.0), (0.18, -0.28), (0.30, 0.34), (0.5, 0.0)]),
            ("Body:position", [(0, (BX, BY)), (0.30, (BX - 46, BY - 10)), (0.5, (BX, BY))]),
        ]),
        ("HorizontalSlash", 0.55, 0, [
            ("Body:rotation", [(0, 0.0), (0.2, 0.22), (0.34, -0.30), (0.55, 0.0)]),
            ("Body:position", [(0, (BX, BY)), (0.2, (BX + 26, BY)), (0.34, (BX - 60, BY)), (0.55, (BX, BY))]),
        ]),
        ("VerticalSlash", 0.7, 0, [
            ("Body:position", [(0, (BX, BY)), (0.34, (BX, BY - 54)), (0.46, (BX, BY + 16)), (0.7, (BX, BY))]),
            ("Body:rotation", [(0, 0.0), (0.34, -0.12), (0.46, 0.06), (0.7, 0.0)]),
            ("Body:scale", [(0, (BODY_SCALE, BODY_SCALE)), (0.46, (BODY_SCALE * 1.05, BODY_SCALE * 0.97)), (0.7, (BODY_SCALE, BODY_SCALE))]),
        ]),
        ("DarkArmor", 1.0, 0, [
            ("Body:scale", [(0, (BODY_SCALE, BODY_SCALE)), (0.4, (BODY_SCALE * 1.08, BODY_SCALE * 1.08)), (1.0, (BODY_SCALE, BODY_SCALE))]),
            ("Body:modulate", [(0, (1, 1, 1, 1)), (0.4, (0.75, 0.8, 1.25, 1)), (1.0, (1, 1, 1, 1))]),
        ]),
        ("TrueFormEnter", 1.2, 0, [
            ("Body:scale", [(0, (BODY_SCALE, BODY_SCALE)), (0.6, (BODY_SCALE * 1.12, BODY_SCALE * 1.12)), (1.2, (BODY_SCALE * 1.06, BODY_SCALE * 1.06))]),
            ("Body:modulate", [(0, (1, 1, 1, 1)), (0.6, (1.4, 0.8, 1.5, 1)), (1.2, (1.1, 0.95, 1.15, 1))]),
            ("Phantom:modulate", [(0, (0.42, 0.25, 0.62, 0.0)), (1.2, (0.5, 0.32, 0.72, 0.5))]),
            ("Phantom:scale", [(0, (PHANTOM_SCALE * 0.85, PHANTOM_SCALE * 0.85)), (1.2, (PHANTOM_SCALE, PHANTOM_SCALE))]),
        ]),
        ("TrueFormExit", 0.9, 0, [
            ("Phantom:modulate", [(0, (0.5, 0.32, 0.72, 0.5)), (0.9, (0.42, 0.25, 0.62, 0.0))]),
            ("Body:scale", [(0, (BODY_SCALE * 1.06, BODY_SCALE * 1.06)), (0.9, (BODY_SCALE, BODY_SCALE))]),
            ("Body:modulate", [(0, (1.1, 0.95, 1.15, 1)), (0.9, (1, 1, 1, 1))]),
        ]),
        ("Hurt", 0.3, 0, [
            ("Body:position", [(0, (BX, BY)), (0.06, (BX + 14, BY)), (0.14, (BX - 10, BY)), (0.22, (BX + 6, BY)), (0.3, (BX, BY))]),
            ("Body:modulate", [(0, (1, 1, 1, 1)), (0.08, (1.8, 0.6, 0.6, 1)), (0.3, (1, 1, 1, 1))]),
        ]),
        ("Death", 1.4, 0, [
            ("Body:position", [(0, (BX, BY)), (0.4, (BX, BY - 10)), (1.4, (BX, BY + 40))]),
            ("Body:rotation", [(0, 0.0), (1.4, 0.22)]),
            ("Body:modulate", [(0, (1, 1, 1, 1)), (0.3, (1.5, 0.5, 0.5, 1)), (1.4, (0.2, 0.2, 0.25, 0.0))]),
        ]),
    ]


def build() -> str:
    animlist = anims()
    subs = []
    lib_entries = []
    for i, (name, length, loop, tracks) in enumerate(animlist):
        aid = f"Anim_{i}"
        lib_entries.append(f'"{name}": SubResource("{aid}")')
        lines = [
            f'[sub_resource type="Animation" id="{aid}"]',
            f'resource_name = "{name}"',
            f"length = {_n(float(length))}",
            f"loop_mode = {loop}",
        ]
        for ti, (path, keys) in enumerate(tracks):
            times = ", ".join(_n(float(t)) for t, _ in keys)
            trans = ", ".join("1" for _ in keys)
            vals = ", ".join(fval(v) for _, v in keys)
            lines += [
                f'tracks/{ti}/type = "value"',
                f"tracks/{ti}/imported = false",
                f"tracks/{ti}/enabled = true",
                f'tracks/{ti}/path = NodePath("{path}")',
                f"tracks/{ti}/interp = 1",
                f"tracks/{ti}/loop_wrap = true",
                'tracks/%d/keys = {' % ti,
                f'"times": PackedFloat32Array({times}),',
                f'"transitions": PackedFloat32Array({trans}),',
                '"update": 0,',
                f'"values": [{vals}]',
                "}",
            ]
        subs.append("\n".join(lines))

    lib = '[sub_resource type="AnimationLibrary" id="AnimLib"]\n_data = {\n' + ",\n".join(lib_entries) + "\n}"

    header = f'[gd_scene load_steps={len(animlist) + 3} format=3]\n\n[ext_resource type="Texture2D" path="{TEX}" id="1_body"]'

    nodes = f'''[node name="BlackKnightVisualRoot" type="Node2D"]

[node name="Visuals" type="Node2D" parent="."]
unique_name_in_owner = true

[node name="Phantom" type="Sprite2D" parent="Visuals"]
z_index = -2
texture = ExtResource("1_body")
position = {v2(PHANTOM_POS)}
scale = {v2((PHANTOM_SCALE, PHANTOM_SCALE))}
modulate = Color(0.42, 0.25, 0.62, 0)
visible = false

[node name="Body" type="Sprite2D" parent="Visuals"]
texture = ExtResource("1_body")
position = {v2(BODY_POS)}
scale = {v2((BODY_SCALE, BODY_SCALE))}

[node name="AnimationPlayer" type="AnimationPlayer" parent="Visuals"]
libraries = {{
"": SubResource("AnimLib")
}}
autoplay = "Idle"

[node name="PhobiaModeVisuals" type="Node2D" parent="."]
unique_name_in_owner = true
visible = false

[node name="Bounds" type="Control" parent="."]
offset_left = -210.0
offset_top = -470.0
offset_right = 210.0
offset_bottom = 0.0

[node name="CenterPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = {v2((0, -230))}

[node name="IntentPos" type="Marker2D" parent="."]
position = {v2((0, -520))}

[node name="OrbPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = {v2((170, -250))}

[node name="TalkPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = {v2((0, -440))}

[node name="VfxSpawnPos" type="Marker2D" parent="."]
unique_name_in_owner = true
position = {v2((0, -235))}
'''

    return header + "\n\n" + "\n\n".join(subs) + "\n\n" + lib + "\n\n" + nodes


def main() -> None:
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8") as f:
        f.write(build())
    print(f"wrote {OUT}")


if __name__ == "__main__":
    main()
