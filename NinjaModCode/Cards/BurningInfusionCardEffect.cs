using Godot;
using NinjaMod.NinjaModCode.Extensions;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 为带有燃烧追加的卡牌挂载动态火焰边框。
/// 贴图负责火焰造型，运行时 canvas shader 负责轻微摆动和明暗呼吸，
/// 因此不依赖额外的 .gdshader/.tscn 资源。
/// </summary>
internal static class BurningInfusionCardEffect
{
    private const string TextureRelativePath = "card_effects/burning_infusion_frame.png";

    private const string ShaderCode = """
        shader_type canvas_item;
        render_mode blend_add, unshaded;

        void fragment() {
            vec2 uv = UV;
            uv.x += sin(uv.y * 25.0 + TIME * 4.8) * 0.0032;
            uv.y += sin(uv.x * 21.0 - TIME * 3.6) * 0.0018;

            vec4 fire = texture(TEXTURE, uv);
            float local_flicker = 0.88 + 0.12 * sin(TIME * 7.0 + uv.y * 12.0 + uv.x * 5.0);
            float glow_breathe = 0.96 + 0.10 * sin(TIME * 2.9);

            COLOR = vec4(
                fire.rgb * local_flicker * glow_breathe,
                fire.a * (0.90 + 0.10 * local_flicker)
            );
        }
        """;

    private static Texture2D? _texture;
    private static ShaderMaterial? _material;

    internal static void Attach(Control root)
    {
        var texture = GetTexture();
        if (texture == null)
        {
            return;
        }

        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.ClipContents = false;
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var frame = new TextureRect
        {
            Name = "BurningInfusionFrame",
            Texture = texture,
            Material = GetMaterial(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ClipContents = false,
            ZIndex = 100
        };

        frame.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        frame.OffsetLeft = -20f;
        frame.OffsetTop = -28f;
        frame.OffsetRight = 20f;
        frame.OffsetBottom = 28f;
        root.AddChild(frame);
    }

    private static Texture2D? GetTexture()
    {
        if (_texture != null)
        {
            return _texture;
        }

        var path = TextureRelativePath.ImagePath();
        if (!ResourceLoader.Exists(path))
        {
            MainFile.Logger.Error($"Burning Infusion frame texture is missing: {path}");
            return null;
        }

        _texture = ResourceLoader.Load<Texture2D>(path);
        return _texture;
    }

    private static ShaderMaterial GetMaterial()
    {
        if (_material != null)
        {
            return _material;
        }

        _material = new ShaderMaterial
        {
            Shader = new Shader { Code = ShaderCode }
        };
        return _material;
    }
}
