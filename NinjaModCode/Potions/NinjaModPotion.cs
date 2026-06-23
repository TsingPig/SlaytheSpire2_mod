using BaseLib.Abstracts;
using BaseLib.Utils;
using NinjaMod.NinjaModCode.Character;

namespace NinjaMod.NinjaModCode.Potions;

[Pool(typeof(NinjaModPotionPool))]
public abstract class NinjaModPotion : CustomPotionModel;