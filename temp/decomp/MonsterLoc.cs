using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BaseLib.Abstracts;

/// <summary>
/// For use with ILocalizationProvider.<seealso cref="T:BaseLib.Abstracts.ILocalizationProvider" />
/// Localization for a monster.
/// </summary>
/// <param name="Name">The monster's name.</param>
/// <param name="MoveTitles">Sets of move keys and names. Keys should be ALL_CAPS. The name will be displayed when the monster acts.</param>
/// <param name="ExtraLoc">Any additional desired localization.</param>
public record MonsterLoc(string Name, IEnumerable<(string, string)> MoveTitles, params (string, string)[] ExtraLoc)
{
	public static implicit operator List<(string, string)>(MonsterLoc loc)
	{
		(string, string) tuple = ("name", loc.Name);
		(string, string)[] extraLoc = loc.ExtraLoc;
		int num = 1 + extraLoc.Length;
		List<(string, string)> list = new List<(string, string)>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<(string, string)> span = CollectionsMarshal.AsSpan(list);
		int num2 = 0;
		span[num2] = tuple;
		num2++;
		ReadOnlySpan<(string, string)> readOnlySpan = new ReadOnlySpan<(string, string)>(extraLoc);
		readOnlySpan.CopyTo(span.Slice(num2, readOnlySpan.Length));
		num2 += readOnlySpan.Length;
		List<(string, string)> list2 = list;
		foreach (var moveTitle in loc.MoveTitles)
		{
			list2.Add(("moves." + moveTitle.Item1 + ".title", moveTitle.Item2));
		}
		return list2;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '9.1.0.7988')
