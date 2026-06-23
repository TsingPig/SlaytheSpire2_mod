using BaseLib.Abstracts;
using NinjaMod.NinjaModCode.Extensions;
using Godot;

namespace NinjaMod.NinjaModCode.Character;

public class NinjaModRelicPool : CustomRelicPoolModel
{
    public override Color LabOutlineColor => Ninja.NinjaColor;

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}