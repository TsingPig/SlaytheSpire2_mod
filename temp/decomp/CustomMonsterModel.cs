using System;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

public abstract class CustomMonsterModel : MonsterModel, ICustomModel, ISceneConversions
{
	/// <summary>
	/// Override this or place your scene at res://scenes/creature_visuals/modname-class_name.tscn
	/// </summary>
	public virtual string? CustomVisualPath => null;

	public virtual string? CustomAttackSfx => null;

	public virtual string? CustomCastSfx => null;

	public virtual string? CustomDeathSfx => null;

	public CustomMonsterModel()
	{
		CustomContentDictionary.RegisterType(((object)this).GetType());
	}

	/// <summary>
	/// Use if you want to generate creature visuals entirely yourself.
	/// If you do, you'll also need to override AssetPaths to not include VisualPath.
	/// <code>public override IEnumerable&lt;string&gt; AssetPaths
	/// {
	///     get
	///     {
	///         List&lt;string&gt; assetPaths = [];
	///         foreach (AbstractIntent intent in GetIntents())
	///             assetPaths.AddRange(intent.AssetPaths);
	///        return assetPaths;
	///     }
	/// }</code>
	/// Otherwise, just override CustomVisualPath.
	/// </summary>
	/// <returns></returns>
	public virtual NCreatureVisuals? CreateCustomVisuals()
	{
		return null;
	}

	/// <summary>
	/// Override and return a CreatureAnimator if you need to set up states that differ from the default for the monster.
	/// Using <seealso cref="M:BaseLib.Abstracts.CustomMonsterModel.SetupAnimationState(MegaCrit.Sts2.Core.Bindings.MegaSpine.MegaSprite,System.String,System.String,System.Boolean,System.String,System.Boolean,System.String,System.Boolean,System.String,System.Boolean)" /> is suggested.
	/// </summary>
	/// <returns></returns>
	public virtual CreatureAnimator? SetupCustomAnimationStates(MegaSprite controller)
	{
		return null;
	}

	/// <summary>
	/// If you have a spine animation without all the required animations,
	/// use this method to set up a controller that will use animations of your choice for each animation.
	/// Any omitted animation parameters will default to the idle animation.
	/// </summary>
	/// <param name="controller"></param>
	/// <param name="idleName"></param>
	/// <param name="deadName"></param>
	/// <param name="deadLoop"></param>
	/// <param name="hitName"></param>
	/// <param name="hitLoop"></param>
	/// <param name="attackName"></param>
	/// <param name="attackLoop"></param>
	/// <param name="castName"></param>
	/// <param name="castLoop"></param>
	/// <returns></returns>
	public static CreatureAnimator SetupAnimationState(MegaSprite controller, string idleName, string? deadName = null, bool deadLoop = false, string? hitName = null, bool hitLoop = false, string? attackName = null, bool attackLoop = false, string? castName = null, bool castLoop = false)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Expected O, but got Unknown
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Expected O, but got Unknown
		AnimState val = new AnimState(idleName, true);
		AnimState val2 = (AnimState)((deadName == null) ? ((object)val) : ((object)new AnimState(deadName, deadLoop)));
		AnimState val3 = (AnimState)((hitName == null) ? ((object)val) : ((object)new AnimState(hitName, hitLoop)
		{
			NextState = val
		}));
		AnimState val4 = (AnimState)((attackName == null) ? ((object)val) : ((object)new AnimState(attackName, attackLoop)
		{
			NextState = val
		}));
		AnimState val5 = (AnimState)((castName == null) ? ((object)val) : ((object)new AnimState(castName, castLoop)
		{
			NextState = val
		}));
		CreatureAnimator val6 = new CreatureAnimator(val, controller);
		val6.AddAnyState("Idle", val, (Func<bool>)null);
		val6.AddAnyState("Dead", val2, (Func<bool>)null);
		val6.AddAnyState("Hit", val3, (Func<bool>)null);
		val6.AddAnyState("Attack", val4, (Func<bool>)null);
		val6.AddAnyState("Cast", val5, (Func<bool>)null);
		return val6;
	}

	public void RegisterSceneConversions()
	{
		CustomVisualPath?.RegisterSceneForConversion<NCreatureVisuals>((Action<NCreatureVisuals>?)null);
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '9.1.0.7988')
