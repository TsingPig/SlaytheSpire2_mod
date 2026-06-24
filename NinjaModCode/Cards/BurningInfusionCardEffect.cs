using System.Linq;
using Godot;
using NinjaMod.NinjaModCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace NinjaMod.NinjaModCode.Cards;

/// <summary>
/// 为卡牌挂载动态火焰边框。
/// 贴图负责火焰造型，运行时 canvas shader 负责轻微摆动和明暗呼吸，
/// 因此不依赖额外的 .gdshader/.tscn 资源。
/// 边框节点会按卡牌当前状态实时显隐：
///   • 带燃烧追加（<see cref="NinjaModCard.BurningInfusion"/> &gt; 0）的卡牌恒显；
///   • 攻击牌在玩家拥有淬火（Quenching）时显示，淬火失效后自动隐藏。
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

    internal static void Attach(Control root, NinjaModCard card)
    {
        var texture = GetTexture();
        if (texture == null)
        {
            return;
        }

        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.ClipContents = false;
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var frame = new BurningFrameNode
        {
            Card = card,
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

/// <summary>
/// 火焰边框节点：每帧根据所属卡牌的状态决定是否可见，
/// 实现「玩家拥有淬火时攻击牌实时高亮」的动态包裹效果。
/// </summary>
internal partial class BurningFrameNode : TextureRect
{
    public NinjaModCard? Card;

    public override void _Process(double delta)
    {
        Visible = ShouldShow();
    }

    private bool ShouldShow()
    {
        var card = Card;
        if (card == null)
        {
            return false;
        }

        // 带燃烧追加的卡牌恒显火焰边框。
        if (card.BurningInfusion > 0)
        {
            return true;
        }

        // 攻击牌：仅当玩家当前拥有淬火（Quenching）能力时，才动态包裹火焰边框。
        if (card.Type != CardType.Attack)
        {
            return false;
        }

        var owner = card.Owner?.Creature;
        if (owner == null)
        {
            return false;
        }

        return owner.Powers.Any(power => power.Id.Entry.Contains("Quenching"));
    }
}
