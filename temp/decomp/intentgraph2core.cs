using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Dfa;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using IntentGraph2.Antlr;
using IntentGraph2.Crossovers;
using IntentGraph2.DevConsole;
using IntentGraph2.Models;
using IntentGraph2.Patches;
using IntentGraph2.Scenes;
using IntentGraph2.Utils;
using IntentGraph2.Utils.GraphGenerator;
using IntentGraph2.Utils.JsonConverters;
using IntentGraph2.Utils.Rule;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.Fonts;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("intentgraph2core")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+bd0515c7734d50d6bc22fb8510efec153af8cf9e")]
[assembly: AssemblyProduct("intentgraph2core")]
[assembly: AssemblyTitle("intentgraph2core")]
[assembly: AssemblyHasScripts(new Type[]
{
	typeof(NIntentGraph),
	typeof(NIntentGraphCanvas),
	typeof(NIntentGraphEditor),
	typeof(NIntentGraphPanel)
})]
[assembly: AssemblyVersion("1.0.0.0")]
[module: RefSafetyRules(11)]
namespace GodotPlugins.Game
{
	internal static class Main
	{
		[UnmanagedCallersOnly(EntryPoint = "godotsharp_game_main_init")]
		private static godot_bool InitializeFromGameProject(nint godotDllHandle, nint outManagedCallbacks, nint unmanagedCallbacks, int unmanagedCallbacksSize)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				DllImportResolver resolver = new GodotDllImportResolver((IntPtr)godotDllHandle).OnResolveDllImport;
				NativeLibrary.SetDllImportResolver(typeof(GodotObject).Assembly, resolver);
				NativeFuncs.Initialize((IntPtr)unmanagedCallbacks, unmanagedCallbacksSize);
				ManagedCallbacks.Create((IntPtr)outManagedCallbacks);
				ScriptManagerBridge.LookupScriptsInAssembly(typeof(Main).Assembly);
				return (godot_bool)1;
			}
			catch (Exception value)
			{
				Console.Error.WriteLine(value);
				return GodotBoolExtensions.ToGodotBool(false);
			}
		}
	}
}
namespace IntentGraph2
{
	public class IntentGraphMod
	{
		public const string ModId = "intentgraph2";

		public static Dictionary<string, string> IntentGraphStrings = new Dictionary<string, string>();

		public static Dictionary<string, IntentDefinitionList> IntentDefinitions = new Dictionary<string, IntentDefinitionList>();

		public static readonly ConditionalWeakTable<MonsterModel, Graph> GeneratedGraphs = new ConditionalWeakTable<MonsterModel, Graph>();

		private static IntentGraphModConfig defaultConfig = new IntentGraphModConfig();

		private static IBaseLibHelper? baseLibHelper;

		private static IRitsuLibHelper? ritsuLibHelper;

		public static JsonSerializerOptions SerializeOptions { get; } = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			Converters = { (JsonConverter)new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
		};

		public static IntentGraphModConfig Config => ritsuLibHelper?.Config ?? baseLibHelper?.Config ?? defaultConfig;

		public static void InitializeMod()
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			IgLogger.Info("IntentGraphMod initialize");
			Assembly assembly = typeof(IntentGraphMod).Assembly;
			ScriptManagerBridge.LookupScriptsInAssembly(assembly);
			IgLogger.Info("Patching...");
			new Harmony("chaofan.sts2.intentgraph2").PatchAll(assembly);
			IgLogger.Info("IntentGraphMod initialize done.");
		}

		public static void PostInitializeMod()
		{
			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
			IgLogger.Info("IntentGraphMod post initialize");
			if (GetLoadedMods().Any((Mod m) => m.manifest?.id == "BaseLib"))
			{
				try
				{
					Assembly assembly = LoadDll("intentgraph2baselib");
					if (assembly != null)
					{
						baseLibHelper = (IBaseLibHelper)AccessToolsExtensions.CreateInstance(assembly.GetType("IntentGraph2.BaseLib.BaseLibHelper"));
						baseLibHelper.RegisterConfig();
					}
					else
					{
						IgLogger.Warn("Failed to get assembly load context for IntentGraphMod.");
					}
				}
				catch (Exception ex)
				{
					baseLibHelper = null;
					IgLogger.Warn("Failed to load BaseLib helper: " + ex);
				}
			}
			if (GetLoadedMods().Any((Mod m) => m.manifest?.id == "STS2-RitsuLib"))
			{
				try
				{
					Assembly assembly2 = LoadDll("intentgraph2ritsulib");
					if (assembly2 != null)
					{
						ritsuLibHelper = (IRitsuLibHelper)AccessToolsExtensions.CreateInstance(assembly2.GetType("IntentGraph2.RitsuLib.RitsuLibHelper"));
						ritsuLibHelper.RegisterConfig();
					}
					else
					{
						IgLogger.Warn("Failed to get assembly load context for IntentGraphMod.");
					}
				}
				catch (Exception ex2)
				{
					ritsuLibHelper = null;
					IgLogger.Warn("Failed to load RitsuLib helper: " + ex2);
				}
			}
			if (ritsuLibHelper != null && baseLibHelper != null)
			{
				baseLibHelper.Config.SetFrom(ritsuLibHelper.Config);
				baseLibHelper.SaveConfig();
				baseLibHelper.Config.OnUpdated += delegate
				{
					ritsuLibHelper.Config.SetFrom(baseLibHelper.Config);
					ritsuLibHelper.SaveConfig();
				};
				ritsuLibHelper.Config.OnUpdated += delegate
				{
					baseLibHelper.Config.SetFrom(ritsuLibHelper.Config);
					baseLibHelper.SaveConfig();
				};
			}
			try
			{
				string text = new GodotFileIo(UserDataPathProvider.GetAccountScopedBasePath((string)null, (PlatformType?)null, (ulong?)null) + "/mod_data/intentgraph2").ReadFile("settings.json");
				if (text != null)
				{
					defaultConfig = JsonSerializer.Deserialize<IntentGraphModConfig>(text) ?? new IntentGraphModConfig();
				}
			}
			catch (Exception ex3)
			{
				IgLogger.Warn("Failed to load default config: " + ex3);
			}
			LoadIntentDefinitions();
			IgLogger.Info("IntentGraphMod post initialize done.");
		}

		public static void LoadIntentDefinitions()
		{
			IntentDefinitions.Clear();
			LoadIntentDefinitionForMod("intentgraph2");
			foreach (Mod loadedMod in GetLoadedMods())
			{
				if (loadedMod != null && loadedMod.manifest?.id != null && loadedMod.manifest.id != "intentgraph2")
				{
					LoadIntentDefinitionForMod(loadedMod.manifest.id);
				}
			}
			LoadIntentDefinitionForDev();
		}

		public static void ReloadIntentDefinitionsAndGraphs()
		{
			LoadIntentDefinitions();
			LoadIntentStrings(LocManager.Instance.Language);
			CombatState val = CombatManager.Instance.DebugOnlyGetState();
			if (val == null || val.Encounter == null)
			{
				return;
			}
			GeneratedGraphs.Clear();
			foreach (Creature enemy in val.Enemies)
			{
				MonsterSetupPatch.Postfix(CombatManager.Instance, enemy);
			}
		}

		public static void LoadIntentStrings(string language)
		{
			IntentGraphStrings.Clear();
			LoadIntentStringsFromMod("intentgraph2", language);
			foreach (Mod loadedMod in GetLoadedMods())
			{
				if (loadedMod != null && loadedMod.manifest?.id != null && loadedMod.manifest.id != "intentgraph2")
				{
					LoadIntentStringsFromMod(loadedMod.manifest.id, language);
				}
			}
			LoadIntentStringsForDev(language);
		}

		public static string GetDevIntentDefinitionFilePath()
		{
			return Path.GetFullPath(Path.Join(Path.GetDirectoryName(typeof(ModManager).Assembly.Location), "..", "intentgraph-intents-dev.json"));
		}

		public static string GetDevIntentStringFilePath(string language)
		{
			return Path.GetFullPath(Path.Join(Path.GetDirectoryName(typeof(ModManager).Assembly.Location), "..", "intentgraph-strings-" + language + "-dev.json"));
		}

		public static Dictionary<string, IntentDefinitionList> LoadIntentDefinitionsFromFile(string filePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					return new Dictionary<string, IntentDefinitionList>();
				}
				return JsonSerializer.Deserialize<Dictionary<string, IntentDefinitionList>>(File.ReadAllText(filePath), SerializeOptions) ?? new Dictionary<string, IntentDefinitionList>();
			}
			catch (Exception value)
			{
				IgLogger.Warn($"Failed to load intent definitions from {filePath}: {value}");
				return new Dictionary<string, IntentDefinitionList>();
			}
		}

		public static void SaveIntentDefinitionsToFile(string filePath, Dictionary<string, IntentDefinitionList> definitions)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
			JsonSerializerOptions options = new JsonSerializerOptions(SerializeOptions)
			{
				WriteIndented = true
			};
			string contents = JsonSerializer.Serialize(definitions, options);
			File.WriteAllText(filePath, contents);
		}

		public static Dictionary<string, string> LoadIntentStringsFromFile(string filePath)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					return new Dictionary<string, string>();
				}
				return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(filePath)) ?? new Dictionary<string, string>();
			}
			catch (Exception value)
			{
				IgLogger.Warn($"Failed to load intent strings from {filePath}: {value}");
				return new Dictionary<string, string>();
			}
		}

		public static void SaveIntentStringsToFile(string filePath, Dictionary<string, string> strings)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
			JsonSerializerOptions options = new JsonSerializerOptions(SerializeOptions)
			{
				WriteIndented = true
			};
			string contents = JsonSerializer.Serialize(strings, options);
			File.WriteAllText(filePath, contents);
		}

		private static IEnumerable<Mod> GetLoadedMods()
		{
			return ModManager.GetLoadedMods();
		}

		private static void LoadIntentDefinitionForMod(string modId)
		{
			IgLogger.Info("Searching intent definitions for mod " + modId);
			string errorMessage = "Failed to load intent definitions for mod " + modId;
			LoadIntentDefinitionsFromResource($"res://{"intentgraph2"}/intentgraph-{modId}.json", errorMessage);
			LoadIntentDefinitionsFromResource("res://" + modId + "/intentgraph.json", errorMessage);
			if (ReleaseInfoManager.Instance.ReleaseInfo != null)
			{
				LoadIntentDefinitionsFromResource($"res://{modId}/intentgraph-{ReleaseInfoManager.Instance.ReleaseInfo.Version}.json", "Failed to load version-specific intent definitions for mod " + modId);
			}
		}

		private static void LoadIntentDefinitionsFromResource(string file, string errorMessage)
		{
			try
			{
				if (!FileAccess.FileExists(file))
				{
					return;
				}
				IgLogger.Info("Loading intent definitions from " + file);
				FileAccess val = FileAccess.Open(file, (ModeFlags)1);
				try
				{
					foreach (KeyValuePair<string, IntentDefinitionList> item in JsonSerializer.Deserialize<Dictionary<string, IntentDefinitionList>>(val.GetAsText(false), SerializeOptions) ?? new Dictionary<string, IntentDefinitionList>())
					{
						IntentDefinitions[item.Key] = item.Value;
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch (Exception value)
			{
				IgLogger.Warn($"{errorMessage}: {value}");
			}
		}

		private static string GetLocalizedResourcePath(string preferredPath, string fallbackPath)
		{
			if (!FileAccess.FileExists(preferredPath))
			{
				return fallbackPath;
			}
			return preferredPath;
		}

		private static void LoadIntentDefinitionForDev()
		{
			string devIntentDefinitionFilePath = GetDevIntentDefinitionFilePath();
			IgLogger.Info("Loading intent definitions from " + devIntentDefinitionFilePath);
			foreach (KeyValuePair<string, IntentDefinitionList> item in LoadIntentDefinitionsFromFile(devIntentDefinitionFilePath))
			{
				IntentDefinitions[item.Key] = item.Value;
			}
		}

		private static void LoadIntentStringsFromMod(string modId, string language)
		{
			IgLogger.Info("Searching intent strings for mod " + modId + ", language " + language);
			string errorMessage = "Failed to load intent strings for mod " + modId + ", language " + language;
			LoadIntentStringsFromResource(GetLocalizedResourcePath($"res://{"intentgraph2"}/localization/{language}/intentgraph-{modId}.json", $"res://{"intentgraph2"}/localization/eng/intentgraph-{modId}.json"), errorMessage);
			LoadIntentStringsFromResource(GetLocalizedResourcePath($"res://{modId}/localization/{language}/intentgraph.json", "res://" + modId + "/localization/eng/intentgraph.json"), errorMessage);
			if (ReleaseInfoManager.Instance.ReleaseInfo != null)
			{
				LoadIntentStringsFromResource(GetLocalizedResourcePath($"res://{modId}/localization/{language}/intentgraph-{ReleaseInfoManager.Instance.ReleaseInfo.Version}.json", $"res://{modId}/localization/eng/intentgraph-{ReleaseInfoManager.Instance.ReleaseInfo.Version}.json"), "Failed to load version-specific intent strings for mod " + modId + ", language " + language);
			}
		}

		private static void LoadIntentStringsFromResource(string file, string errorMessage)
		{
			try
			{
				if (!FileAccess.FileExists(file))
				{
					return;
				}
				IgLogger.Info("Loading intent strings from " + file);
				FileAccess val = FileAccess.Open(file, (ModeFlags)1);
				try
				{
					foreach (KeyValuePair<string, string> item in JsonSerializer.Deserialize<Dictionary<string, string>>(val.GetAsText(false)) ?? new Dictionary<string, string>())
					{
						IntentGraphStrings[item.Key] = item.Value;
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
			}
			catch (Exception value)
			{
				IgLogger.Warn($"{errorMessage}: {value}");
			}
		}

		private static void LoadIntentStringsForDev(string language)
		{
			string devIntentStringFilePath = GetDevIntentStringFilePath(language);
			IgLogger.Info("Loading intent strings from " + devIntentStringFilePath);
			foreach (KeyValuePair<string, string> item in LoadIntentStringsFromFile(devIntentStringFilePath))
			{
				IntentGraphStrings[item.Key] = item.Value;
			}
		}

		private static Assembly? LoadDll(string dllName)
		{
			try
			{
				Assembly assembly = typeof(IntentGraphMod).Assembly;
				AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(assembly);
				if (loadContext != null)
				{
					string assemblyPath = Path.Join(Path.GetDirectoryName(assembly.Location), dllName + ".dll");
					return loadContext.LoadFromAssemblyPath(assemblyPath);
				}
				IgLogger.Info("Failed to get assembly load context for IntentGraphMod.");
			}
			catch (Exception value)
			{
				IgLogger.Info($"Failed to load dll {dllName}: {value}");
			}
			return null;
		}
	}
}
namespace IntentGraph2.Antlr
{
	[GeneratedCode("ANTLR", "4.13.1")]
	[DebuggerNonUserCode]
	[CLSCompliant(false)]
	public class RuleBaseListener : IRuleListener, IParseTreeListener
	{
		public virtual void EnterProg([Antlr4.Runtime.Misc.NotNull] RuleParser.ProgContext context)
		{
		}

		public virtual void ExitProg([Antlr4.Runtime.Misc.NotNull] RuleParser.ProgContext context)
		{
		}

		public virtual void EnterExpr([Antlr4.Runtime.Misc.NotNull] RuleParser.ExprContext context)
		{
		}

		public virtual void ExitExpr([Antlr4.Runtime.Misc.NotNull] RuleParser.ExprContext context)
		{
		}

		public virtual void EnterEveryRule([Antlr4.Runtime.Misc.NotNull] ParserRuleContext context)
		{
		}

		public virtual void ExitEveryRule([Antlr4.Runtime.Misc.NotNull] ParserRuleContext context)
		{
		}

		public virtual void VisitTerminal([Antlr4.Runtime.Misc.NotNull] ITerminalNode node)
		{
		}

		public virtual void VisitErrorNode([Antlr4.Runtime.Misc.NotNull] IErrorNode node)
		{
		}
	}
	[GeneratedCode("ANTLR", "4.13.1")]
	[DebuggerNonUserCode]
	[CLSCompliant(false)]
	public class RuleBaseVisitor<Result> : AbstractParseTreeVisitor<Result>, IRuleVisitor<Result>, IParseTreeVisitor<Result>
	{
		public virtual Result VisitProg([Antlr4.Runtime.Misc.NotNull] RuleParser.ProgContext context)
		{
			return VisitChildren(context);
		}

		public virtual Result VisitExpr([Antlr4.Runtime.Misc.NotNull] RuleParser.ExprContext context)
		{
			return VisitChildren(context);
		}
	}
	[GeneratedCode("ANTLR", "4.13.1")]
	[CLSCompliant(false)]
	public class RuleLexer : Lexer
	{
		protected static DFA[] decisionToDFA;

		protected static PredictionContextCache sharedContextCache;

		public const int T__0 = 1;

		public const int T__1 = 2;

		public const int T__2 = 3;

		public const int T__3 = 4;

		public const int T__4 = 5;

		public const int T__5 = 6;

		public const int T__6 = 7;

		public const int T__7 = 8;

		public const int T__8 = 9;

		public const int T__9 = 10;

		public const int T__10 = 11;

		public const int SPACE = 12;

		public const int BOOL = 13;

		public const int INT = 14;

		public const int VAR = 15;

		public static string[] channelNames;

		public static string[] modeNames;

		public static readonly string[] ruleNames;

		private static readonly string[] _LiteralNames;

		private static readonly string[] _SymbolicNames;

		public static readonly IVocabulary DefaultVocabulary;

		private static int[] _serializedATN;

		public static readonly ATN _ATN;

		[Antlr4.Runtime.Misc.NotNull]
		public override IVocabulary Vocabulary => DefaultVocabulary;

		public override string GrammarFileName => "Rule.g4";

		public override string[] RuleNames => ruleNames;

		public override string[] ChannelNames => channelNames;

		public override string[] ModeNames => modeNames;

		public override int[] SerializedAtn => _serializedATN;

		public RuleLexer(ICharStream input)
			: this(input, Console.Out, Console.Error)
		{
		}

		public RuleLexer(ICharStream input, TextWriter output, TextWriter errorOutput)
			: base(input, output, errorOutput)
		{
			Interpreter = new LexerATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
		}

		static RuleLexer()
		{
			sharedContextCache = new PredictionContextCache();
			channelNames = new string[2] { "DEFAULT_TOKEN_CHANNEL", "HIDDEN" };
			modeNames = new string[1] { "DEFAULT_MODE" };
			ruleNames = new string[15]
			{
				"T__0", "T__1", "T__2", "T__3", "T__4", "T__5", "T__6", "T__7", "T__8", "T__9",
				"T__10", "SPACE", "BOOL", "INT", "VAR"
			};
			_LiteralNames = new string[12]
			{
				null, "'!'", "'>'", "'<'", "'>='", "'<='", "'=='", "'!='", "'&&'", "'||'",
				"'('", "')'"
			};
			_SymbolicNames = new string[16]
			{
				null, null, null, null, null, null, null, null, null, null,
				null, null, "SPACE", "BOOL", "INT", "VAR"
			};
			DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);
			_serializedATN = new int[947]
			{
				4, 0, 15, 102, 6, -1, 2, 0, 7, 0,
				2, 1, 7, 1, 2, 2, 7, 2, 2, 3,
				7, 3, 2, 4, 7, 4, 2, 5, 7, 5,
				2, 6, 7, 6, 2, 7, 7, 7, 2, 8,
				7, 8, 2, 9, 7, 9, 2, 10, 7, 10,
				2, 11, 7, 11, 2, 12, 7, 12, 2, 13,
				7, 13, 2, 14, 7, 14, 1, 0, 1, 0,
				1, 1, 1, 1, 1, 2, 1, 2, 1, 3,
				1, 3, 1, 3, 1, 4, 1, 4, 1, 4,
				1, 5, 1, 5, 1, 5, 1, 6, 1, 6,
				1, 6, 1, 7, 1, 7, 1, 7, 1, 8,
				1, 8, 1, 8, 1, 9, 1, 9, 1, 10,
				1, 10, 1, 11, 4, 11, 61, 8, 11, 11,
				11, 12, 11, 62, 1, 11, 1, 11, 1, 12,
				1, 12, 1, 12, 1, 12, 1, 12, 1, 12,
				1, 12, 1, 12, 1, 12, 3, 12, 76, 8,
				12, 1, 13, 3, 13, 79, 8, 13, 1, 13,
				4, 13, 82, 8, 13, 11, 13, 12, 13, 83,
				1, 14, 1, 14, 5, 14, 88, 8, 14, 10,
				14, 12, 14, 91, 9, 14, 1, 14, 3, 14,
				94, 8, 14, 1, 14, 1, 14, 5, 14, 98,
				8, 14, 10, 14, 12, 14, 101, 9, 14, 0,
				0, 15, 1, 1, 3, 2, 5, 3, 7, 4,
				9, 5, 11, 6, 13, 7, 15, 8, 17, 9,
				19, 10, 21, 11, 23, 12, 25, 13, 27, 14,
				29, 15, 1, 0, 4, 3, 0, 9, 10, 13,
				13, 32, 32, 1, 0, 48, 57, 3, 0, 65,
				90, 95, 95, 97, 122, 4, 0, 48, 57, 65,
				90, 95, 95, 97, 122, 108, 0, 1, 1, 0,
				0, 0, 0, 3, 1, 0, 0, 0, 0, 5,
				1, 0, 0, 0, 0, 7, 1, 0, 0, 0,
				0, 9, 1, 0, 0, 0, 0, 11, 1, 0,
				0, 0, 0, 13, 1, 0, 0, 0, 0, 15,
				1, 0, 0, 0, 0, 17, 1, 0, 0, 0,
				0, 19, 1, 0, 0, 0, 0, 21, 1, 0,
				0, 0, 0, 23, 1, 0, 0, 0, 0, 25,
				1, 0, 0, 0, 0, 27, 1, 0, 0, 0,
				0, 29, 1, 0, 0, 0, 1, 31, 1, 0,
				0, 0, 3, 33, 1, 0, 0, 0, 5, 35,
				1, 0, 0, 0, 7, 37, 1, 0, 0, 0,
				9, 40, 1, 0, 0, 0, 11, 43, 1, 0,
				0, 0, 13, 46, 1, 0, 0, 0, 15, 49,
				1, 0, 0, 0, 17, 52, 1, 0, 0, 0,
				19, 55, 1, 0, 0, 0, 21, 57, 1, 0,
				0, 0, 23, 60, 1, 0, 0, 0, 25, 75,
				1, 0, 0, 0, 27, 78, 1, 0, 0, 0,
				29, 93, 1, 0, 0, 0, 31, 32, 5, 33,
				0, 0, 32, 2, 1, 0, 0, 0, 33, 34,
				5, 62, 0, 0, 34, 4, 1, 0, 0, 0,
				35, 36, 5, 60, 0, 0, 36, 6, 1, 0,
				0, 0, 37, 38, 5, 62, 0, 0, 38, 39,
				5, 61, 0, 0, 39, 8, 1, 0, 0, 0,
				40, 41, 5, 60, 0, 0, 41, 42, 5, 61,
				0, 0, 42, 10, 1, 0, 0, 0, 43, 44,
				5, 61, 0, 0, 44, 45, 5, 61, 0, 0,
				45, 12, 1, 0, 0, 0, 46, 47, 5, 33,
				0, 0, 47, 48, 5, 61, 0, 0, 48, 14,
				1, 0, 0, 0, 49, 50, 5, 38, 0, 0,
				50, 51, 5, 38, 0, 0, 51, 16, 1, 0,
				0, 0, 52, 53, 5, 124, 0, 0, 53, 54,
				5, 124, 0, 0, 54, 18, 1, 0, 0, 0,
				55, 56, 5, 40, 0, 0, 56, 20, 1, 0,
				0, 0, 57, 58, 5, 41, 0, 0, 58, 22,
				1, 0, 0, 0, 59, 61, 7, 0, 0, 0,
				60, 59, 1, 0, 0, 0, 61, 62, 1, 0,
				0, 0, 62, 60, 1, 0, 0, 0, 62, 63,
				1, 0, 0, 0, 63, 64, 1, 0, 0, 0,
				64, 65, 6, 11, 0, 0, 65, 24, 1, 0,
				0, 0, 66, 67, 5, 116, 0, 0, 67, 68,
				5, 114, 0, 0, 68, 69, 5, 117, 0, 0,
				69, 76, 5, 101, 0, 0, 70, 71, 5, 102,
				0, 0, 71, 72, 5, 97, 0, 0, 72, 73,
				5, 108, 0, 0, 73, 74, 5, 115, 0, 0,
				74, 76, 5, 101, 0, 0, 75, 66, 1, 0,
				0, 0, 75, 70, 1, 0, 0, 0, 76, 26,
				1, 0, 0, 0, 77, 79, 5, 45, 0, 0,
				78, 77, 1, 0, 0, 0, 78, 79, 1, 0,
				0, 0, 79, 81, 1, 0, 0, 0, 80, 82,
				7, 1, 0, 0, 81, 80, 1, 0, 0, 0,
				82, 83, 1, 0, 0, 0, 83, 81, 1, 0,
				0, 0, 83, 84, 1, 0, 0, 0, 84, 28,
				1, 0, 0, 0, 85, 89, 7, 2, 0, 0,
				86, 88, 7, 3, 0, 0, 87, 86, 1, 0,
				0, 0, 88, 91, 1, 0, 0, 0, 89, 87,
				1, 0, 0, 0, 89, 90, 1, 0, 0, 0,
				90, 92, 1, 0, 0, 0, 91, 89, 1, 0,
				0, 0, 92, 94, 5, 46, 0, 0, 93, 85,
				1, 0, 0, 0, 93, 94, 1, 0, 0, 0,
				94, 95, 1, 0, 0, 0, 95, 99, 7, 2,
				0, 0, 96, 98, 7, 3, 0, 0, 97, 96,
				1, 0, 0, 0, 98, 101, 1, 0, 0, 0,
				99, 97, 1, 0, 0, 0, 99, 100, 1, 0,
				0, 0, 100, 30, 1, 0, 0, 0, 101, 99,
				1, 0, 0, 0, 8, 0, 62, 75, 78, 83,
				89, 93, 99, 1, 6, 0, 0
			};
			_ATN = new ATNDeserializer().Deserialize(_serializedATN);
			decisionToDFA = new DFA[_ATN.NumberOfDecisions];
			for (int i = 0; i < _ATN.NumberOfDecisions; i++)
			{
				decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
			}
		}
	}
	[GeneratedCode("ANTLR", "4.13.1")]
	[CLSCompliant(false)]
	public interface IRuleListener : IParseTreeListener
	{
		void EnterProg([Antlr4.Runtime.Misc.NotNull] RuleParser.ProgContext context);

		void ExitProg([Antlr4.Runtime.Misc.NotNull] RuleParser.ProgContext context);

		void EnterExpr([Antlr4.Runtime.Misc.NotNull] RuleParser.ExprContext context);

		void ExitExpr([Antlr4.Runtime.Misc.NotNull] RuleParser.ExprContext context);
	}
	[GeneratedCode("ANTLR", "4.13.1")]
	[CLSCompliant(false)]
	public class RuleParser : Parser
	{
		public class ProgContext : ParserRuleContext
		{
			public override int RuleIndex => 0;

			[DebuggerNonUserCode]
			public ExprContext expr()
			{
				return GetRuleContext<ExprContext>(0);
			}

			[DebuggerNonUserCode]
			public ITerminalNode Eof()
			{
				return GetToken(-1, 0);
			}

			public ProgContext(ParserRuleContext parent, int invokingState)
				: base(parent, invokingState)
			{
			}

			[DebuggerNonUserCode]
			public override void EnterRule(IParseTreeListener listener)
			{
				if (listener is IRuleListener ruleListener)
				{
					ruleListener.EnterProg(this);
				}
			}

			[DebuggerNonUserCode]
			public override void ExitRule(IParseTreeListener listener)
			{
				if (listener is IRuleListener ruleListener)
				{
					ruleListener.ExitProg(this);
				}
			}

			[DebuggerNonUserCode]
			public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
			{
				if (visitor is IRuleVisitor<TResult> ruleVisitor)
				{
					return ruleVisitor.VisitProg(this);
				}
				return visitor.VisitChildren(this);
			}
		}

		public class ExprContext : ParserRuleContext
		{
			public override int RuleIndex => 1;

			[DebuggerNonUserCode]
			public ExprContext[] expr()
			{
				return GetRuleContexts<ExprContext>();
			}

			[DebuggerNonUserCode]
			public ExprContext expr(int i)
			{
				return GetRuleContext<ExprContext>(i);
			}

			[DebuggerNonUserCode]
			public ITerminalNode BOOL()
			{
				return GetToken(13, 0);
			}

			[DebuggerNonUserCode]
			public ITerminalNode INT()
			{
				return GetToken(14, 0);
			}

			[DebuggerNonUserCode]
			public ITerminalNode VAR()
			{
				return GetToken(15, 0);
			}

			public ExprContext(ParserRuleContext parent, int invokingState)
				: base(parent, invokingState)
			{
			}

			[DebuggerNonUserCode]
			public override void EnterRule(IParseTreeListener listener)
			{
				if (listener is IRuleListener ruleListener)
				{
					ruleListener.EnterExpr(this);
				}
			}

			[DebuggerNonUserCode]
			public override void ExitRule(IParseTreeListener listener)
			{
				if (listener is IRuleListener ruleListener)
				{
					ruleListener.ExitExpr(this);
				}
			}

			[DebuggerNonUserCode]
			public override TResult Accept<TResult>(IParseTreeVisitor<TResult> visitor)
			{
				if (visitor is IRuleVisitor<TResult> ruleVisitor)
				{
					return ruleVisitor.VisitExpr(this);
				}
				return visitor.VisitChildren(this);
			}
		}

		protected static DFA[] decisionToDFA;

		protected static PredictionContextCache sharedContextCache;

		public const int T__0 = 1;

		public const int T__1 = 2;

		public const int T__2 = 3;

		public const int T__3 = 4;

		public const int T__4 = 5;

		public const int T__5 = 6;

		public const int T__6 = 7;

		public const int T__7 = 8;

		public const int T__8 = 9;

		public const int T__9 = 10;

		public const int T__10 = 11;

		public const int SPACE = 12;

		public const int BOOL = 13;

		public const int INT = 14;

		public const int VAR = 15;

		public const int RULE_prog = 0;

		public const int RULE_expr = 1;

		public static readonly string[] ruleNames;

		private static readonly string[] _LiteralNames;

		private static readonly string[] _SymbolicNames;

		public static readonly IVocabulary DefaultVocabulary;

		private static int[] _serializedATN;

		public static readonly ATN _ATN;

		[Antlr4.Runtime.Misc.NotNull]
		public override IVocabulary Vocabulary => DefaultVocabulary;

		public override string GrammarFileName => "Rule.g4";

		public override string[] RuleNames => ruleNames;

		public override int[] SerializedAtn => _serializedATN;

		static RuleParser()
		{
			sharedContextCache = new PredictionContextCache();
			ruleNames = new string[2] { "prog", "expr" };
			_LiteralNames = new string[12]
			{
				null, "'!'", "'>'", "'<'", "'>='", "'<='", "'=='", "'!='", "'&&'", "'||'",
				"'('", "')'"
			};
			_SymbolicNames = new string[16]
			{
				null, null, null, null, null, null, null, null, null, null,
				null, null, "SPACE", "BOOL", "INT", "VAR"
			};
			DefaultVocabulary = new Vocabulary(_LiteralNames, _SymbolicNames);
			_serializedATN = new int[320]
			{
				4, 1, 15, 34, 2, 0, 7, 0, 2, 1,
				7, 1, 1, 0, 1, 0, 1, 0, 1, 1,
				1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
				1, 1, 1, 1, 1, 1, 1, 1, 3, 1,
				18, 8, 1, 1, 1, 1, 1, 1, 1, 1,
				1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
				1, 5, 1, 29, 8, 1, 10, 1, 12, 1,
				32, 9, 1, 1, 1, 0, 1, 2, 2, 0,
				2, 0, 1, 1, 0, 2, 7, 38, 0, 4,
				1, 0, 0, 0, 2, 17, 1, 0, 0, 0,
				4, 5, 3, 2, 1, 0, 5, 6, 5, 0,
				0, 1, 6, 1, 1, 0, 0, 0, 7, 8,
				6, 1, -1, 0, 8, 9, 5, 1, 0, 0,
				9, 18, 3, 2, 1, 8, 10, 18, 5, 13,
				0, 0, 11, 18, 5, 14, 0, 0, 12, 18,
				5, 15, 0, 0, 13, 14, 5, 10, 0, 0,
				14, 15, 3, 2, 1, 0, 15, 16, 5, 11,
				0, 0, 16, 18, 1, 0, 0, 0, 17, 7,
				1, 0, 0, 0, 17, 10, 1, 0, 0, 0,
				17, 11, 1, 0, 0, 0, 17, 12, 1, 0,
				0, 0, 17, 13, 1, 0, 0, 0, 18, 30,
				1, 0, 0, 0, 19, 20, 10, 7, 0, 0,
				20, 21, 7, 0, 0, 0, 21, 29, 3, 2,
				1, 8, 22, 23, 10, 6, 0, 0, 23, 24,
				5, 8, 0, 0, 24, 29, 3, 2, 1, 7,
				25, 26, 10, 5, 0, 0, 26, 27, 5, 9,
				0, 0, 27, 29, 3, 2, 1, 6, 28, 19,
				1, 0, 0, 0, 28, 22, 1, 0, 0, 0,
				28, 25, 1, 0, 0, 0, 29, 32, 1, 0,
				0, 0, 30, 28, 1, 0, 0, 0, 30, 31,
				1, 0, 0, 0, 31, 3, 1, 0, 0, 0,
				32, 30, 1, 0, 0, 0, 3, 17, 28, 30
			};
			_ATN = new ATNDeserializer().Deserialize(_serializedATN);
			decisionToDFA = new DFA[_ATN.NumberOfDecisions];
			for (int i = 0; i < _ATN.NumberOfDecisions; i++)
			{
				decisionToDFA[i] = new DFA(_ATN.GetDecisionState(i), i);
			}
		}

		public RuleParser(ITokenStream input)
			: this(input, Console.Out, Console.Error)
		{
		}

		public RuleParser(ITokenStream input, TextWriter output, TextWriter errorOutput)
			: base(input, output, errorOutput)
		{
			Interpreter = new ParserATNSimulator(this, _ATN, decisionToDFA, sharedContextCache);
		}

		[RuleVersion(0)]
		public ProgContext prog()
		{
			ProgContext progContext = new ProgContext(Context, base.State);
			EnterRule(progContext, 0, 0);
			try
			{
				EnterOuterAlt(progContext, 1);
				base.State = 4;
				expr(0);
				base.State = 5;
				Match(-1);
			}
			catch (RecognitionException exception)
			{
				RecognitionException e = (progContext.exception = exception);
				ErrorHandler.ReportError(this, e);
				ErrorHandler.Recover(this, e);
			}
			finally
			{
				ExitRule();
			}
			return progContext;
		}

		[RuleVersion(0)]
		public ExprContext expr()
		{
			return expr(0);
		}

		private ExprContext expr(int _p)
		{
			ParserRuleContext context = Context;
			int state = base.State;
			ExprContext exprContext = new ExprContext(Context, state);
			int state2 = 2;
			EnterRecursionRule(exprContext, 2, 1, _p);
			try
			{
				EnterOuterAlt(exprContext, 1);
				base.State = 17;
				ErrorHandler.Sync(this);
				switch (base.TokenStream.LA(1))
				{
				case 1:
					base.State = 8;
					Match(1);
					base.State = 9;
					expr(8);
					break;
				case 13:
					base.State = 10;
					Match(13);
					break;
				case 14:
					base.State = 11;
					Match(14);
					break;
				case 15:
					base.State = 12;
					Match(15);
					break;
				case 10:
					base.State = 13;
					Match(10);
					base.State = 14;
					expr(0);
					base.State = 15;
					Match(11);
					break;
				default:
					throw new NoViableAltException(this);
				}
				Context.Stop = base.TokenStream.LT(-1);
				base.State = 30;
				ErrorHandler.Sync(this);
				int num = Interpreter.AdaptivePredict(base.TokenStream, 2, Context);
				while (true)
				{
					switch (num)
					{
					case 1:
						if (ParseListeners != null)
						{
							TriggerExitRuleEvent();
						}
						base.State = 28;
						ErrorHandler.Sync(this);
						switch (Interpreter.AdaptivePredict(base.TokenStream, 1, Context))
						{
						case 1:
						{
							exprContext = new ExprContext(context, state);
							PushNewRecursionContext(exprContext, state2, 1);
							base.State = 19;
							if (!Precpred(Context, 7))
							{
								throw new FailedPredicateException(this, "Precpred(Context, 7)");
							}
							base.State = 20;
							int num2 = base.TokenStream.LA(1);
							if ((num2 & -64) != 0 || ((1L << num2) & 0xFC) == 0L)
							{
								ErrorHandler.RecoverInline(this);
							}
							else
							{
								ErrorHandler.ReportMatch(this);
								Consume();
							}
							base.State = 21;
							expr(8);
							break;
						}
						case 2:
							exprContext = new ExprContext(context, state);
							PushNewRecursionContext(exprContext, state2, 1);
							base.State = 22;
							if (!Precpred(Context, 6))
							{
								throw new FailedPredicateException(this, "Precpred(Context, 6)");
							}
							base.State = 23;
							Match(8);
							base.State = 24;
							expr(7);
							break;
						case 3:
							exprContext = new ExprContext(context, state);
							PushNewRecursionContext(exprContext, state2, 1);
							base.State = 25;
							if (!Precpred(Context, 5))
							{
								throw new FailedPredicateException(this, "Precpred(Context, 5)");
							}
							base.State = 26;
							Match(9);
							base.State = 27;
							expr(6);
							break;
						}
						break;
					case 0:
					case 2:
						goto end_IL_032c;
					}
					base.State = 32;
					ErrorHandler.Sync(this);
					num = Interpreter.AdaptivePredict(base.TokenStream, 2, Context);
					continue;
					end_IL_032c:
					break;
				}
			}
			catch (RecognitionException exception)
			{
				RecognitionException e = (exprContext.exception = exception);
				ErrorHandler.ReportError(this, e);
				ErrorHandler.Recover(this, e);
			}
			finally
			{
				UnrollRecursionContexts(context);
			}
			return exprContext;
		}

		public override bool Sempred(Antlr4.Runtime.RuleContext _localctx, int ruleIndex, int predIndex)
		{
			if (ruleIndex == 1)
			{
				return expr_sempred((ExprContext)_localctx, predIndex);
			}
			return true;
		}

		private bool expr_sempred(ExprContext _localctx, int predIndex)
		{
			return predIndex switch
			{
				0 => Precpred(Context, 7), 
				1 => Precpred(Context, 6), 
				2 => Precpred(Context, 5), 
				_ => true, 
			};
		}
	}
	[GeneratedCode("ANTLR", "4.13.1")]
	[CLSCompliant(false)]
	public interface IRuleVisitor<Result> : IParseTreeVisitor<Result>
	{
		Result VisitProg([Antlr4.Runtime.Misc.NotNull] RuleParser.ProgContext context);

		Result VisitExpr([Antlr4.Runtime.Misc.NotNull] RuleParser.ExprContext context);
	}
}
namespace IntentGraph2.Utils
{
	public static class HoverTipSetExtensions
	{
		public static Control GetTextHoverTipContainer(this NHoverTipSet hoverTipSet)
		{
			object? obj = ((object)hoverTipSet).GetType().GetField("_textHoverTipContainer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(hoverTipSet);
			Control val = (Control)((obj is Control) ? obj : null);
			if (val == null)
			{
				throw new Exception("Failed to get _textHoverTipContainer from hoverTipSet");
			}
			return val;
		}
	}
	public static class IntentDefinitionEditorService
	{
		public static IntentDefinitionList LoadEditableDefinitions(string monsterModelFullName, string? devOverrideFilePath = null, IReadOnlyDictionary<string, IntentDefinitionList>? runtimeDefinitions = null)
		{
			if (IntentGraphMod.LoadIntentDefinitionsFromFile(devOverrideFilePath ?? IntentGraphMod.GetDevIntentDefinitionFilePath()).TryGetValue(monsterModelFullName, out IntentDefinitionList value))
			{
				return Clone(value) ?? new IntentDefinitionList();
			}
			if (runtimeDefinitions == null)
			{
				runtimeDefinitions = IntentGraphMod.IntentDefinitions;
			}
			if (runtimeDefinitions.TryGetValue(monsterModelFullName, out IntentDefinitionList value2))
			{
				return Clone(value2) ?? new IntentDefinitionList();
			}
			return new IntentDefinitionList();
		}

		public static void SaveEditableDefinitions(string monsterModelFullName, IntentDefinitionList definitions, string? filePath = null)
		{
			string? filePath2 = filePath ?? IntentGraphMod.GetDevIntentDefinitionFilePath();
			Dictionary<string, IntentDefinitionList> dictionary = IntentGraphMod.LoadIntentDefinitionsFromFile(filePath2);
			dictionary[monsterModelFullName] = Clone(definitions) ?? new IntentDefinitionList();
			IntentGraphMod.SaveIntentDefinitionsToFile(filePath2, dictionary);
		}

		public static T? Clone<T>(T? value)
		{
			if (value == null)
			{
				return default(T);
			}
			return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, IntentGraphMod.SerializeOptions), IntentGraphMod.SerializeOptions);
		}

		public static string SerializeJson<T>(T value)
		{
			return JsonSerializer.Serialize(value, GetIndentedSerializerOptions());
		}

		public static T? DeserializeJson<T>(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return default(T);
			}
			return JsonSerializer.Deserialize<T>(text, IntentGraphMod.SerializeOptions);
		}

		public static string BuildReadOnlySummary(IntentDefinition definition)
		{
			List<string> list = new List<string> { IntentGraphMod.IntentGraphStrings.GetValueOrDefault("ui.editor.read_only.summary.none", "Not editable here: graph.") };
			if (definition.Graph != null)
			{
				list.Add(IntentGraphMod.IntentGraphStrings.GetValueOrDefault("ui.editor.read_only.summary.graph_present", "graph is present and remains read-only in this editor."));
			}
			return string.Join(Environment.NewLine, list.Distinct());
		}

		private static JsonSerializerOptions GetIndentedSerializerOptions()
		{
			return new JsonSerializerOptions(IntentGraphMod.SerializeOptions)
			{
				WriteIndented = true
			};
		}
	}
	public static class IntentGraphEditorHost
	{
		private const string EditorScenePath = "res://intentgraph2/scenes/intent_graph_editor.tscn";

		private static NIntentGraphEditor? currentEditor;

		public static bool TryOpenEditor(MonsterModel monster, string monsterDisplayName, out string message)
		{
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			if (IsEditorAlive(currentEditor))
			{
				if (currentEditor.HasUnsavedChanges)
				{
					message = "An intent editor is already open with unsaved changes. Close it before opening another editor.";
					return false;
				}
				((Node)currentEditor).QueueFree();
				currentEditor = null;
			}
			Node val = ResolveEditorParent();
			if (val == null)
			{
				message = "Failed to find a UI parent for the intent editor.";
				return false;
			}
			PackedScene val2 = ResourceLoader.Load<PackedScene>("res://intentgraph2/scenes/intent_graph_editor.tscn", (string)null, (CacheMode)1);
			if (val2 == null)
			{
				message = "Failed to load editor scene 'res://intentgraph2/scenes/intent_graph_editor.tscn'.";
				return false;
			}
			NIntentGraphEditor editor = val2.Instantiate<NIntentGraphEditor>((GenEditState)0);
			val.AddChild((Node)(object)editor, false, (InternalMode)0);
			((CanvasItem)editor).TopLevel = true;
			((Control)editor).Position = Vector2.Zero;
			NIntentGraphEditor nIntentGraphEditor = editor;
			Control val3 = (Control)(object)((val is Control) ? val : null);
			Vector2 size;
			if (val3 == null)
			{
				Rect2 viewportRect = ((CanvasItem)editor).GetViewportRect();
				size = ((Rect2)(ref viewportRect)).Size;
			}
			else
			{
				size = val3.Size;
			}
			((Control)nIntentGraphEditor).Size = size;
			((CanvasItem)editor).ZIndex = 1000;
			editor.Initialize(monster, monsterDisplayName);
			((Node)editor).TreeExited += delegate
			{
				if (currentEditor == editor)
				{
					currentEditor = null;
				}
			};
			currentEditor = editor;
			message = "Opened intent editor for " + monsterDisplayName + ".";
			return true;
		}

		public static void CloseEditor(NIntentGraphEditor editor)
		{
			if (currentEditor == editor)
			{
				currentEditor = null;
			}
			if (GodotObject.IsInstanceValid((GodotObject)(object)editor))
			{
				((Node)editor).QueueFree();
			}
		}

		private static Node? ResolveEditorParent()
		{
			if (NGame.Instance != null)
			{
				return (Node?)(object)NGame.Instance;
			}
			MainLoop mainLoop = Engine.GetMainLoop();
			MainLoop obj = ((mainLoop is SceneTree) ? mainLoop : null);
			if (obj == null)
			{
				return null;
			}
			return (Node?)(object)((SceneTree)obj).Root;
		}

		private static bool IsEditorAlive(NIntentGraphEditor? editor)
		{
			if (editor != null)
			{
				return GodotObject.IsInstanceValid((GodotObject)(object)editor);
			}
			return false;
		}
	}
	public static class IntentGraphHost
	{
		private class IntentGraphItem
		{
			public required NIntentGraphPanel IntentGraphPanel { get; set; }

			public required Action RemoveIntentGraphPanel { get; set; }
		}

		private const float IntentGraphPanelTop = 90f;

		private const float IntentGraphPanelTopPinned = 160f;

		private static Dictionary<NCreature, IntentGraphItem> availableIntentGraphs = new Dictionary<NCreature, IntentGraphItem>();

		private static bool intentGraphVisible = true;

		public static void ToggleIntentGraphVisibility()
		{
			intentGraphVisible = !intentGraphVisible;
			if (intentGraphVisible)
			{
				foreach (IntentGraphItem value in availableIntentGraphs.Values)
				{
					((CanvasItem)value.IntentGraphPanel).Show();
				}
				return;
			}
			foreach (IntentGraphItem value2 in availableIntentGraphs.Values)
			{
				((CanvasItem)value2.IntentGraphPanel).Hide();
			}
		}

		public static void Create(NCreature nCreature)
		{
			if (availableIntentGraphs.TryGetValue(nCreature, out IntentGraphItem value))
			{
				GodotTreeExtensions.MoveToFrontSafely((CanvasItem)(object)value.IntentGraphPanel);
				return;
			}
			NGame instance = NGame.Instance;
			if (((instance != null) ? instance.HoverTipsContainer : null) == null)
			{
				return;
			}
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 == null || instance2.Ui.Hand.InCardPlay)
			{
				return;
			}
			Creature entity = nCreature.Entity;
			if (entity == null || !entity.IsMonster)
			{
				return;
			}
			Creature entity2 = nCreature.Entity;
			if (entity2.Monster == null || !IntentGraphMod.GeneratedGraphs.TryGetValue(entity2.Monster, out Graph value2))
			{
				return;
			}
			IRule? condition = value2.Condition;
			if (condition != null && !condition.GetBool())
			{
				value2 = IntentGraphGenerator.GenerateAndCacheGraphForCreature(entity2);
				if (value2 == null)
				{
					return;
				}
			}
			foreach (var (key, intentGraphItem2) in availableIntentGraphs.ToList())
			{
				if (!intentGraphItem2.IntentGraphPanel.Pinned)
				{
					intentGraphItem2.RemoveIntentGraphPanel();
					availableIntentGraphs.Remove(key);
				}
			}
			PackedScene scene = PreloadManager.Cache.GetScene("res://intentgraph2/scenes/intent_graph_panel.tscn");
			NIntentGraphPanel intentGraphPanel = scene.Instantiate<NIntentGraphPanel>((GenEditState)0);
			intentGraphPanel.NCreature = nCreature;
			Label node = ((Node)intentGraphPanel).GetNode<Label>(NodePath.op_Implicit("%MonsterName"));
			node.Text = entity2.Name;
			FontControlUtils.ApplyLocaleFontSubstitution((Control)(object)node, (FontType)0, StringName.op_Implicit("font"));
			FontControlUtils.ApplyLocaleFontSubstitution((Control)(object)node, (FontType)1, StringName.op_Implicit("font"));
			NIntentGraph node2 = ((Node)intentGraphPanel).GetNode<NIntentGraph>(NodePath.op_Implicit("%IntentGraph"));
			node2.Graph = value2;
			node2.Monster = entity2.Monster;
			bool pinableIntentGraph = IntentGraphMod.Config.PinableIntentGraph;
			Action handleResized = OnIntentGraphPanelResized(nCreature, (MarginContainer)(object)intentGraphPanel, pinableIntentGraph);
			if (!pinableIntentGraph)
			{
				((Control)nCreature).Resized += handleResized;
			}
			((Control)intentGraphPanel).Resized += handleResized;
			if (value2.Warning != null)
			{
				MarginContainer node3 = ((Node)intentGraphPanel).GetNode<MarginContainer>(NodePath.op_Implicit("%OutdatedMarkContainer"));
				Label node4 = ((Node)node3).GetNode<Label>(NodePath.op_Implicit("OutdatedMark"));
				((CanvasItem)node3).Show();
				node4.Text = "?\ufe0f" + value2.Warning;
				FontControlUtils.ApplyLocaleFontSubstitution((Control)(object)node4, (FontType)0, StringName.op_Implicit("font"));
				FontControlUtils.ApplyLocaleFontSubstitution((Control)(object)node4, (FontType)1, StringName.op_Implicit("font"));
			}
			((Control)intentGraphPanel).ResetSize();
			if (pinableIntentGraph)
			{
				NCombatRoom instance3 = NCombatRoom.Instance;
				if (instance3 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance3.Ui, (Node)(object)intentGraphPanel);
				}
			}
			else
			{
				GodotTreeExtensions.AddChildSafely(NGame.Instance.HoverTipsContainer, (Node)(object)intentGraphPanel);
			}
			if (!intentGraphVisible)
			{
				((CanvasItem)intentGraphPanel).Hide();
			}
			availableIntentGraphs[nCreature] = new IntentGraphItem
			{
				IntentGraphPanel = intentGraphPanel,
				RemoveIntentGraphPanel = delegate
				{
					try
					{
						((Control)intentGraphPanel).Resized -= handleResized;
						if (!pinableIntentGraph)
						{
							((Control)nCreature).Resized -= handleResized;
						}
					}
					catch (Exception ex)
					{
						IgLogger.Error("Error unregistering resized event handlers: " + ex);
					}
					GodotTreeExtensions.QueueFreeSafely((Node)(object)intentGraphPanel);
				}
			};
		}

		public static void Remove(NCreature creature)
		{
			if (availableIntentGraphs.TryGetValue(creature, out IntentGraphItem value))
			{
				value.RemoveIntentGraphPanel();
				availableIntentGraphs.Remove(creature);
			}
		}

		private static Action OnIntentGraphPanelResized(NCreature __instance, MarginContainer intentGraphPanel, bool pinableIntentGraph)
		{
			return delegate
			{
				//IL_0005: Unknown result type (might be due to invalid IL or missing references)
				//IL_000a: Unknown result type (might be due to invalid IL or missing references)
				//IL_000e: Unknown result type (might be due to invalid IL or missing references)
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_002f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0084: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00df: Unknown result type (might be due to invalid IL or missing references)
				//IL_0141: Unknown result type (might be due to invalid IL or missing references)
				//IL_0154: Unknown result type (might be due to invalid IL or missing references)
				//IL_010a: Unknown result type (might be due to invalid IL or missing references)
				//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
				//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
				//IL_012f: Unknown result type (might be due to invalid IL or missing references)
				//IL_01be: Expected O, but got Unknown
				//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
				//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
				//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
				//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
				//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
				//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
				//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
				//IL_0202: Unknown result type (might be due to invalid IL or missing references)
				//IL_02d4: Unknown result type (might be due to invalid IL or missing references)
				//IL_0210: Unknown result type (might be due to invalid IL or missing references)
				//IL_021e: Unknown result type (might be due to invalid IL or missing references)
				//IL_022e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0235: Unknown result type (might be due to invalid IL or missing references)
				//IL_024a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0281: Unknown result type (might be due to invalid IL or missing references)
				//IL_028e: Unknown result type (might be due to invalid IL or missing references)
				//IL_025d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0264: Unknown result type (might be due to invalid IL or missing references)
				//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
				//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
				//IL_026f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0276: Unknown result type (might be due to invalid IL or missing references)
				Rect2 viewportRect = ((CanvasItem)NGame.Instance).GetViewportRect();
				float x = ((Rect2)(ref viewportRect)).Size.X;
				float num = ((Control)__instance).GlobalPosition.X + ((Control)__instance).Size.X / 2f;
				if (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopLeft || (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopLeftOrRight && num < x / 2f))
				{
					((Control)intentGraphPanel).Position = new Vector2(8f, pinableIntentGraph ? 160f : 90f);
				}
				else if (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopRight || (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopLeftOrRight && num >= x / 2f))
				{
					((Control)intentGraphPanel).Position = new Vector2(x - ((Control)intentGraphPanel).Size.X, pinableIntentGraph ? 160f : 90f);
				}
				else if (IntentGraphMod.Config.IntentGraphPosition == IntentGraphPosition.TopCenter)
				{
					((Control)intentGraphPanel).Position = new Vector2(x / 2f - ((Control)intentGraphPanel).Size.X / 2f, pinableIntentGraph ? 160f : 90f);
				}
				else
				{
					float num2 = x - ((Control)intentGraphPanel).Size.X;
					float num3 = Math.Clamp(num - ((Control)intentGraphPanel).Size.X / 2f, 0f, num2);
					Node parent = ((Node)intentGraphPanel).GetParent();
					NHoverTipSet val = (NHoverTipSet)((parent != null) ? ((IEnumerable<Node>)parent.GetChildren(false)).LastOrDefault((Func<Node, bool>)((Node c) => c is NHoverTipSet)) : null);
					Control val2 = (((int)val != 0) ? HoverTipSetExtensions.GetTextHoverTipContainer(val) : null);
					if (val2 != null)
					{
						Vector2 globalPosition = val2.GlobalPosition;
						Vector2 size = val2.Size;
						if (globalPosition.Y < 90f + ((Control)intentGraphPanel).Size.Y && globalPosition.X + size.X > num3 && globalPosition.X < num3 + ((Control)intentGraphPanel).Size.X)
						{
							if (globalPosition.X + size.X / 2f < num3 + ((Control)intentGraphPanel).Size.X / 2f && globalPosition.X + size.X <= num2)
							{
								num3 = globalPosition.X + size.X;
							}
							else if (globalPosition.X - ((Control)intentGraphPanel).Size.X >= 0f)
							{
								num3 = globalPosition.X - ((Control)intentGraphPanel).Size.X;
							}
						}
					}
					((Control)intentGraphPanel).Position = new Vector2(num3, pinableIntentGraph ? 160f : 90f);
				}
			};
		}
	}
	public class IntentGraphModConfig
	{
		public Key ToggleIntentGraphKey { get; set; } = (Key)4194332;

		public bool ShowMonsterMoveNames { get; set; } = true;

		public bool UseAnimatedIntentIcon { get; set; } = true;

		public bool ShowCurrentMove { get; set; } = true;

		public bool PinableIntentGraph { get; set; }

		public IntentGraphPosition IntentGraphPosition { get; set; }

		public float IntentGraphScale { get; set; } = 1f;

		public event EventHandler<string>? OnUpdated;

		public void NotifyUpdated(string propertyName)
		{
			this.OnUpdated?.Invoke(this, propertyName);
		}

		public void SetFrom(IntentGraphModConfig config)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			ToggleIntentGraphKey = config.ToggleIntentGraphKey;
			ShowMonsterMoveNames = config.ShowMonsterMoveNames;
			UseAnimatedIntentIcon = config.UseAnimatedIntentIcon;
			ShowCurrentMove = config.ShowCurrentMove;
			PinableIntentGraph = config.PinableIntentGraph;
			IntentGraphPosition = config.IntentGraphPosition;
			IntentGraphScale = config.IntentGraphScale;
		}
	}
	public enum IntentGraphPosition
	{
		Default,
		TopLeft,
		TopCenter,
		TopRight,
		TopLeftOrRight
	}
	internal class IgLogger
	{
		public static void Info(string message)
		{
			Log.Info("[IntentGraph] " + message, 2);
		}

		public static void Warn(string message)
		{
			Log.Warn("[IntentGraph] " + message, 2);
		}

		public static void Error(string message)
		{
			Log.Error("[IntentGraph] " + message, 2);
		}

		public static void Debug(string message)
		{
			Log.Debug("[IntentGraph] " + message, 2);
		}

		public static void Verbose(string message)
		{
			Log.VeryDebug("[IntentGraph] " + message, 2);
		}
	}
	public static class StateMachineExtensions
	{
		public static MonsterState GetInitialState(this MonsterMoveStateMachine stateMachine)
		{
			object? obj = ((object)stateMachine).GetType().GetField("_initialState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(stateMachine);
			MonsterState val = (MonsterState)((obj is MonsterState) ? obj : null);
			if (val == null)
			{
				throw new Exception("Failed to get initial state from state machine.");
			}
			return val;
		}

		public static MonsterState GetCurrentState(this MonsterMoveStateMachine stateMachine)
		{
			object? obj = ((object)stateMachine).GetType().GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(stateMachine);
			MonsterState val = (MonsterState)((obj is MonsterState) ? obj : null);
			if (val == null)
			{
				throw new Exception("Failed to get current state from state machine.");
			}
			return val;
		}

		public static List<string> GetStates(this ConditionalBranchState conditionalBranchState)
		{
			IList list = ((object)conditionalBranchState).GetType().GetProperty("States", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(conditionalBranchState) as IList;
			List<string> list2 = new List<string>();
			if (list != null)
			{
				foreach (object item2 in list)
				{
					if (item2.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item2) is string item)
					{
						list2.Add(item);
					}
				}
			}
			return list2;
		}

		public static string? EvaluateStates(this ConditionalBranchState conditionalBranchState)
		{
			if (((object)conditionalBranchState).GetType().GetProperty("States", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(conditionalBranchState) is IList list)
			{
				foreach (object item in list)
				{
					float? num = item.GetType().GetMethod("Evaluate", BindingFlags.Instance | BindingFlags.Public)?.Invoke(item, null) as float?;
					if (num.HasValue && num > 0f)
					{
						return item.GetType().GetField("id", BindingFlags.Instance | BindingFlags.Public)?.GetValue(item) as string;
					}
				}
			}
			return null;
		}
	}
}
namespace IntentGraph2.Utils.Rule
{
	public interface IRule
	{
		public enum Operator
		{
			EQ,
			LT,
			GT,
			LE,
			GE,
			NE,
			AND,
			OR,
			NOT
		}

		int GetInt();

		bool GetBool();

		static IRule? Parse(string expression, IRuleContext ruleContext)
		{
			return RuleParserHelper.Parse(expression, ruleContext);
		}
	}
	public interface IRuleContext
	{
		int GetIntVariable(string variableName);
	}
	public class OneOperandRule : IRule
	{
		private readonly IRule.Operator compareOperator;

		private readonly IRule operandA;

		public OneOperandRule(IRule.Operator compareOperator, IRule operandA)
		{
			this.compareOperator = compareOperator;
			this.operandA = operandA;
		}

		private bool Check()
		{
			if (compareOperator == IRule.Operator.NOT)
			{
				return !operandA.GetBool();
			}
			return false;
		}

		public int GetInt()
		{
			return Check() ? 1 : 0;
		}

		public bool GetBool()
		{
			return Check();
		}
	}
	public class RuleContext : IRuleContext
	{
		public MonsterModel Monster { get; }

		public RuleContext(MonsterModel monster)
		{
			Monster = monster;
		}

		public int GetIntVariable(string variableName)
		{
			try
			{
				if (variableName.StartsWith("m."))
				{
					string text = variableName.Substring(2);
					Traverse val = Traverse.Create((object)Monster);
					return Convert.ToInt32(val.Property(text, (object[])null).GetValue() ?? val.Field(text).GetValue() ?? ((object)0));
				}
				if (variableName.StartsWith("mm.") && Monster.MoveStateMachine != null)
				{
					MonsterMoveStateMachine moveStateMachine = Monster.MoveStateMachine;
					string text2 = variableName.Substring(3);
					if (text2 == "count")
					{
						return moveStateMachine.States.Count;
					}
					if (text2.StartsWith("hasMove_"))
					{
						string moveName = text2.Substring(8);
						return moveStateMachine.States.Any((KeyValuePair<string, MonsterState> s) => s.Key == moveName) ? 1 : 0;
					}
					if (text2.StartsWith("startsWith_"))
					{
						string text3 = text2.Substring(11);
						return (moveStateMachine.GetInitialState().Id == text3) ? 1 : 0;
					}
					if (text2.StartsWith("nextMoveOf_") && text2.Contains("_is_"))
					{
						int num = text2.IndexOf("_is_");
						if (num == -1)
						{
							return 0;
						}
						string moveName2 = text2.Substring(11, num - 11);
						string nextMoveName = text2.Substring(num + 4);
						MonsterState value = moveStateMachine.States.FirstOrDefault((KeyValuePair<string, MonsterState> s) => s.Key == moveName2).Value;
						MonsterState value2 = moveStateMachine.States.FirstOrDefault((KeyValuePair<string, MonsterState> s) => s.Key == nextMoveName).Value;
						MoveState val2 = (MoveState)(object)((value is MoveState) ? value : null);
						if (val2 != null && value2 != null)
						{
							return (val2.FollowUpStateId == nextMoveName || val2.FollowUpState == value2) ? 1 : 0;
						}
						return 0;
					}
					return 0;
				}
				int result;
				switch (variableName)
				{
				case "act":
					result = Monster.CombatState.RunState.CurrentActIndex;
					break;
				case "slotIndex":
				{
					EncounterModel encounter = Monster.CombatState.Encounter;
					result = ((encounter != null) ? ListExtensions.IndexOf<string>(encounter.Slots, Monster.Creature.SlotName) : 0);
					break;
				}
				case "ascension":
					result = Traverse.Create((object)RunManager.Instance.AscensionManager).Field("_level").GetValue<int>();
					break;
				case "showMoveNames":
					result = (IntentGraphMod.Config.ShowMonsterMoveNames ? 1 : 0);
					break;
				default:
					result = 0;
					break;
				}
				return result;
			}
			catch (Exception value3)
			{
				IgLogger.Warn($"Error getting variable '{variableName}': {value3}. Return 0.");
				return 0;
			}
		}
	}
	public static class RuleParserHelper
	{
		private static readonly Dictionary<string, IRule.Operator> OperatorMap = new Dictionary<string, IRule.Operator>
		{
			[">"] = IRule.Operator.GT,
			["<"] = IRule.Operator.LT,
			["=="] = IRule.Operator.EQ,
			[">="] = IRule.Operator.GE,
			["<="] = IRule.Operator.LE,
			["!="] = IRule.Operator.NE,
			["&&"] = IRule.Operator.AND,
			["||"] = IRule.Operator.OR
		};

		public static IRule? Parse(string expression, IRuleContext ruleContext)
		{
			return Expr(new RuleParser(new CommonTokenStream(new RuleLexer(CharStreams.fromString(expression)))).prog().expr(), ruleContext);
		}

		private static IRule? Expr(IParseTree tree, IRuleContext ruleContext)
		{
			if (tree.ChildCount == 1)
			{
				if (tree.GetChild(0) is ITerminalNode { Symbol: var symbol })
				{
					if (symbol.Type == 15)
					{
						return new VariableOperand(symbol.Text, ruleContext);
					}
					if (symbol.Type == 14)
					{
						return new ValueOperand(int.Parse(symbol.Text));
					}
					if (symbol.Type == 13)
					{
						return new ValueOperand(bool.Parse(symbol.Text));
					}
				}
			}
			else if (tree.ChildCount == 2)
			{
				if (tree.GetChild(0) is ITerminalNode terminalNode2 && terminalNode2.Symbol.Text == "!")
				{
					IRule rule = Expr(tree.GetChild(1), ruleContext);
					if (rule != null)
					{
						return new OneOperandRule(IRule.Operator.NOT, rule);
					}
				}
			}
			else if (tree.ChildCount == 3)
			{
				IParseTree child = tree.GetChild(0);
				IParseTree child2 = tree.GetChild(1);
				IRule.Operator value;
				if (child is ITerminalNode terminalNode3)
				{
					if (terminalNode3.Symbol.Text == "(")
					{
						return Expr(tree.GetChild(1), ruleContext);
					}
				}
				else if (child2 is ITerminalNode terminalNode4 && OperatorMap.TryGetValue(terminalNode4.Symbol.Text, out value))
				{
					IRule rule2 = Expr(child, ruleContext);
					IRule rule3 = Expr(tree.GetChild(2), ruleContext);
					if (rule2 != null && rule3 != null)
					{
						return new TwoOperandRule(rule2, value, rule3);
					}
				}
			}
			return null;
		}
	}
	public class TwoOperandRule : IRule
	{
		private readonly IRule.Operator compareOperator;

		private readonly IRule operandA;

		private readonly IRule operandB;

		public TwoOperandRule(IRule operandA, IRule.Operator compareOperator, IRule operandB)
		{
			this.compareOperator = compareOperator;
			this.operandA = operandA;
			this.operandB = operandB;
		}

		private bool Check()
		{
			return compareOperator switch
			{
				IRule.Operator.EQ => operandA.GetInt() == operandB.GetInt(), 
				IRule.Operator.LT => operandA.GetInt() < operandB.GetInt(), 
				IRule.Operator.GT => operandA.GetInt() > operandB.GetInt(), 
				IRule.Operator.LE => operandA.GetInt() <= operandB.GetInt(), 
				IRule.Operator.GE => operandA.GetInt() >= operandB.GetInt(), 
				IRule.Operator.NE => operandA.GetInt() != operandB.GetInt(), 
				IRule.Operator.AND => operandA.GetBool() && operandB.GetBool(), 
				IRule.Operator.OR => operandA.GetBool() || operandB.GetBool(), 
				_ => false, 
			};
		}

		public int GetInt()
		{
			return Check() ? 1 : 0;
		}

		public bool GetBool()
		{
			return Check();
		}
	}
	public class ValueOperand : IRule
	{
		private readonly int intValue;

		private readonly bool boolValue;

		public ValueOperand(int value)
		{
			intValue = value;
			boolValue = false;
		}

		public ValueOperand(bool value)
		{
			boolValue = value;
			intValue = 0;
		}

		public int GetInt()
		{
			return intValue;
		}

		public bool GetBool()
		{
			return boolValue;
		}
	}
	public class VariableOperand : IRule
	{
		private readonly string variableName;

		private readonly IRuleContext context;

		public VariableOperand(string variableName, IRuleContext context)
		{
			this.variableName = variableName;
			this.context = context;
		}

		public int GetInt()
		{
			return context.GetIntVariable(variableName);
		}

		public bool GetBool()
		{
			return GetInt() != 0;
		}
	}
}
namespace IntentGraph2.Utils.JsonConverters
{
	public class MoveReplacementJsonConverter : JsonConverter<MoveReplacement>
	{
		public override MoveReplacement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.StartArray)
			{
				return new MoveReplacement(JsonSerializer.Deserialize<IntentOverride[]>(ref reader, options), null);
			}
			if (reader.TokenType == JsonTokenType.StartObject)
			{
				using (JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader))
				{
					JsonElement rootElement = jsonDocument.RootElement;
					IntentOverride[] intentOverrides = null;
					if (rootElement.TryGetProperty("intentOverrides", out var value))
					{
						intentOverrides = value.Deserialize<IntentOverride[]>(options);
					}
					ArrowOverride arrowOverride = null;
					if (rootElement.TryGetProperty("arrowOverride", out var value2))
					{
						arrowOverride = value2.Deserialize<ArrowOverride>(options);
					}
					return new MoveReplacement(intentOverrides, arrowOverride);
				}
			}
			throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		public override void Write(Utf8JsonWriter writer, MoveReplacement value, JsonSerializerOptions options)
		{
			if (value.ArrowOverride == null && value.IntentOverrides != null)
			{
				JsonSerializer.Serialize(writer, value.IntentOverrides, options);
				return;
			}
			writer.WriteStartObject();
			if (value.IntentOverrides != null)
			{
				writer.WritePropertyName("intentOverrides");
				JsonSerializer.Serialize(writer, value.IntentOverrides, options);
			}
			if (value.ArrowOverride != null)
			{
				writer.WritePropertyName("arrowOverride");
				JsonSerializer.Serialize(writer, value.ArrowOverride, options);
			}
			writer.WriteEndObject();
		}
	}
	public class SecondaryInitialStateJsonConverter : JsonConverter<SecondaryInitialState>
	{
		public override SecondaryInitialState? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.String)
			{
				return new SecondaryInitialState(reader.GetString());
			}
			if (reader.TokenType == JsonTokenType.StartObject)
			{
				using (JsonDocument jsonDocument = JsonDocument.ParseValue(ref reader))
				{
					JsonElement rootElement = jsonDocument.RootElement;
					if (!rootElement.TryGetProperty("id", out var value))
					{
						throw new JsonException("Missing required property: Id");
					}
					string? text = value.GetString();
					if (string.IsNullOrEmpty(text))
					{
						throw new JsonException("Invalid value for property: Id");
					}
					Position offset = default(Position);
					if (rootElement.TryGetProperty("offset", out var value2))
					{
						offset = value2.Deserialize<Position>(options);
					}
					return new SecondaryInitialState(text, offset);
				}
			}
			throw new JsonException($"Unexpected token type: {reader.TokenType}");
		}

		public override void Write(Utf8JsonWriter writer, SecondaryInitialState value, JsonSerializerOptions options)
		{
			if (value.Offset == default(Position))
			{
				writer.WriteStringValue(value.Id);
				return;
			}
			writer.WriteStartObject();
			writer.WriteString("id", value.Id);
			writer.WritePropertyName("offset");
			JsonSerializer.Serialize(writer, value.Offset, options);
			writer.WriteEndObject();
		}
	}
}
namespace IntentGraph2.Utils.GraphGenerator
{
	public class IntentGraphGenerator
	{
		public const float IconPaddingInMove = -0.33f;

		public const float IconGroupPadding = 0.1f;

		public const float IconGroupLabelHeight = 0.25f;

		public static bool ShowMonsterMoveNames => IntentGraphMod.Config.ShowMonsterMoveNames;

		public static float IconGroupSingleMovePadding
		{
			get
			{
				if (!ShowMonsterMoveNames)
				{
					return -0.15f;
				}
				return 0f;
			}
		}

		public static Graph? GenerateAndCacheGraphForCreature(Creature creature)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			try
			{
				MonsterModel monster = creature.Monster;
				IgLogger.Info("Generating intent graph for monster: " + creature.Name + ".");
				Graph graph = GenerateGraph(monster);
				if (monster != null && graph != null)
				{
					IntentGraphMod.GeneratedGraphs.AddOrUpdate(monster, graph);
				}
				return graph;
			}
			catch (Exception ex)
			{
				Log.Warn(ex.ToString(), 2);
				return null;
			}
			finally
			{
				stopwatch.Stop();
				IgLogger.Info($"Finished generating intent graph for monster: {creature.Name} in {stopwatch.ElapsedMilliseconds} ms.");
			}
		}

		public static Graph? GenerateGraph(MonsterModel? monster, IntentDefinition? overwriteIntentDefinition = null, IReadOnlyDictionary<string, string>? overwriteIntentStrings = null)
		{
			if (((monster != null) ? monster.MoveStateMachine : null) == null)
			{
				return null;
			}
			MonsterMoveStateMachine moveStateMachine = monster.MoveStateMachine;
			MonsterState initialState = moveStateMachine.GetInitialState();
			IntentDefinition intentDefinition = overwriteIntentDefinition;
			IRule condition = null;
			if (intentDefinition == null)
			{
				IntentDefinitionList valueOrDefault = IntentGraphMod.IntentDefinitions.GetValueOrDefault(((object)monster).GetType().FullName ?? string.Empty);
				if (valueOrDefault != null)
				{
					(intentDefinition, condition) = valueOrDefault.FindFirstMatchCondition(monster);
				}
			}
			IntentGraphLocalizer intentGraphLocalizer = new IntentGraphLocalizer(overwriteIntentStrings);
			string warning = null;
			if (intentDefinition != null && intentDefinition.UpToDateCondition != null)
			{
				try
				{
					IRule? rule = RuleParserHelper.Parse(intentDefinition.UpToDateCondition, new IntentGraph2.Utils.Rule.RuleContext(monster));
					if (rule != null && !rule.GetBool())
					{
						warning = intentGraphLocalizer.GetOrElse("ui.Outdated", "Outdated");
					}
				}
				catch (Exception ex)
				{
					IgLogger.Warn($"Failed to evaluate up to date condition '{intentDefinition.UpToDateCondition}' for monster '{((AbstractModel)monster).Id}', error message: {ex.Message}");
				}
			}
			Font font = ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres", (string)null, (CacheMode)1);
			IntentGraphLayouter intentGraphLayouter = new IntentGraphLayouter(monster, intentGraphLocalizer);
			Graph graph;
			if (intentDefinition?.Graph != null)
			{
				graph = intentGraphLayouter.MakeGraphFromIntentDefinition(moveStateMachine, intentDefinition.Graph, intentDefinition, font);
				graph.Warning = warning;
				graph.Condition = condition;
				return graph;
			}
			MonsterStateNodeConverter monsterStateNodeConverter = new MonsterStateNodeConverter(intentGraphLocalizer);
			List<MonsterStateNode> stateNodes = ((intentDefinition?.StateMachine == null) ? monsterStateNodeConverter.FromMonsterMoveStateMachine(((object)monster).GetType().FullName ?? "_unknownMonster", font, moveStateMachine, initialState, intentDefinition, ref warning) : monsterStateNodeConverter.FromStateMachineNodes(moveStateMachine, intentDefinition.StateMachine, font));
			graph = intentGraphLayouter.StateNodesToGraph(stateNodes, intentDefinition);
			graph.Warning = warning;
			graph.Condition = condition;
			if (intentDefinition?.GraphPatch != null)
			{
				Graph graph2 = intentGraphLayouter.MakeGraphFromIntentDefinition(moveStateMachine, intentDefinition.GraphPatch, intentDefinition, font, stateNodes);
				graph.Width = Math.Max(graph.Width, graph2.Width);
				graph.Height = Math.Max(graph.Height, graph2.Height);
				graph.Icons.AddRange(graph2.Icons);
				graph.Moves.AddRange(graph2.Moves);
				graph.IconGroups.AddRange(graph2.IconGroups);
				graph.Labels.AddRange(graph2.Labels);
				graph.Arrows.AddRange(graph2.Arrows);
			}
			if (graph.Moves.Sum(delegate(Move m)
			{
				Icon[]? icons = m.Icons;
				return (icons != null) ? icons.Length : 0;
			}) == 0 && graph.IconGroups.Count == 0 && graph.Labels.Count == 0 && graph.Icons.Count == 0)
			{
				return null;
			}
			return graph;
		}
	}
	internal class IntentGraphLayouter
	{
		private class GraphGenerationContext
		{
			public int NextXIndex { get; set; }

			public int YIndex { get; set; }

			public float NextX { get; set; }

			public float Y { get; set; }

			public Dictionary<float, MonsterStateNode> HLineTargetNode { get; set; } = new Dictionary<float, MonsterStateNode>();

			public Dictionary<float, MonsterStateNode> VLineTargetNode { get; set; } = new Dictionary<float, MonsterStateNode>();

			public Dictionary<(int x, int y), MonsterStateNode> IndexToNode { get; set; } = new Dictionary<(int, int), MonsterStateNode>();

			public IntentDefinition? IntentDefinition { get; init; }

			public Dictionary<Arrow, MonsterStateNode> ArrowTarget { get; set; } = new Dictionary<Arrow, MonsterStateNode>();

			public Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> PreviousStateNodes { get; set; } = new Dictionary<MonsterStateNode, HashSet<MonsterStateNode>>();

			public Dictionary<MonsterStateNode, Move> StateNodeToMove { get; set; } = new Dictionary<MonsterStateNode, Move>();

			public Dictionary<Move, MonsterStateNode> MoveToStateNode { get; set; } = new Dictionary<Move, MonsterStateNode>();

			public Dictionary<int, SubGraph> SubGraphs { get; set; } = new Dictionary<int, SubGraph>();

			internal void NewLine(float y)
			{
				NextXIndex = 0;
				NextX = 0f;
				YIndex++;
				VLineTargetNode.Clear();
				Y = y;
			}
		}

		private class SubGraph
		{
			public float Y { get; set; }

			public List<int> MoveIndices { get; set; } = new List<int>();

			public List<int> LabelIndices { get; set; } = new List<int>();

			public List<int> IconGroupIndices { get; set; } = new List<int>();

			public List<(Arrow arrow, int startIndex, int length)> Arrows { get; set; } = new List<(Arrow, int, int)>();

			public List<MonsterStateNode> Nodes { get; set; } = new List<MonsterStateNode>();

			internal void MoveY(Graph graph, float yOffset)
			{
				Y += yOffset;
				foreach (int moveIndex in MoveIndices)
				{
					Move move = graph.Moves[moveIndex];
					graph.Moves[moveIndex] = move with
					{
						Y = move.Y + yOffset,
						Icons = move.Icons?.Select((Icon icon) => icon with
						{
							Y = icon.Y + yOffset
						}).ToArray()
					};
				}
				foreach (int labelIndex in LabelIndices)
				{
					Label label = graph.Labels[labelIndex];
					graph.Labels[labelIndex] = label with
					{
						Y = label.Y + yOffset
					};
				}
				foreach (int iconGroupIndex in IconGroupIndices)
				{
					IconGroup iconGroup = graph.IconGroups[iconGroupIndex];
					graph.IconGroups[iconGroupIndex] = iconGroup with
					{
						Y = iconGroup.Y + yOffset
					};
				}
				foreach (MonsterStateNode node in Nodes)
				{
					node.Y += yOffset;
				}
				foreach (var (arrow, num, num2) in Arrows)
				{
					if (2 >= num && 2 < num + num2)
					{
						arrow.Path[2] += yOffset;
					}
					for (int num3 = Math.Max(3, num); num3 < num + num2; num3++)
					{
						if ((float)(num3 % 2) == arrow.Path[0])
						{
							arrow.Path[num3] += yOffset;
						}
					}
				}
			}
		}

		private readonly MonsterModel monster;

		private readonly IntentGraphLocalizer localizer;

		public IntentGraphLayouter(MonsterModel monster, IntentGraphLocalizer localizer)
		{
			this.monster = monster;
			this.localizer = localizer;
		}

		public Graph MakeGraphFromIntentDefinition(MonsterMoveStateMachine stateMachine, Graph graph, IntentDefinition intentDefinition, Font font, List<MonsterStateNode>? stateNodes = null)
		{
			Dictionary<string, MonsterStateNode> allStateNodes = (from n in stateNodes?.GetAllNodes()
				where n.FullId != null
				select n).ToDictionary((MonsterStateNode n) => n.FullId);
			Graph graph2 = new Graph
			{
				Width = graph.Width,
				Height = graph.Height,
				Icons = (from i in graph.Icons
					select (i: i, ResolveRelative(i, allStateNodes)) into t
					select t.i with
					{
						X = t.Item2.x,
						Y = t.Item2.y,
						RelativeTo = null
					}).ToList(),
				IconGroups = (from i in graph.IconGroups
					select (i: i, ResolveRelative(i, allStateNodes)) into t
					select t.i with
					{
						X = t.Item2.x,
						Y = t.Item2.y,
						RelativeTo = null
					}).ToList(),
				Arrows = graph.Arrows.Select((Arrow i) => ResolveRelative(i, allStateNodes)).ToList()
			};
			foreach (Label label in graph.Labels)
			{
				(float x, float y) tuple = ResolveRelative(label, allStateNodes);
				float item = tuple.x;
				float item2 = tuple.y;
				Label resolvedLabel = label with
				{
					X = item,
					Y = item2,
					Text = localizer.GetOrElse(label.Text, label.Text),
					RelativeTo = null
				};
				graph2.Labels.Add(resolvedLabel);
				if (graph.Expand)
				{
					string[] array = resolvedLabel.Text.Split('\n');
					float num = array.Select((string l) => font.GetStringSize(l, (HorizontalAlignment)0, -1f, resolvedLabel.FontSize, (JustificationFlag)3, (Direction)0, (Orientation)0).X).Max() / 80f;
					graph2.Height = Math.Max(graph2.Height, resolvedLabel.Y + ((float)resolvedLabel.FontSize + 2f) * (float)(array.Length - 1) / 80f);
					if (resolvedLabel.Align != "right")
					{
						graph2.Width = ((resolvedLabel.Align == "left") ? Math.Max(graph2.Width, resolvedLabel.X + num) : Math.Max(graph2.Width, resolvedLabel.X + num / 2f));
					}
				}
			}
			foreach (Move move in graph.Moves)
			{
				MonsterState val = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == move.Id));
				if (val != null)
				{
					MoveState val2 = (MoveState)(object)((val is MoveState) ? val : null);
					if (val2 != null)
					{
						(float x, float y) tuple2 = ResolveRelative(move, allStateNodes);
						float item3 = tuple2.x;
						float item4 = tuple2.y;
						string id = move.Id;
						string[] array2 = move.Ids ?? Array.Empty<string>();
						int num2 = 0;
						string[] array3 = new string[1 + array2.Length];
						array3[num2] = id;
						num2++;
						ReadOnlySpan<string> readOnlySpan = new ReadOnlySpan<string>(array2);
						readOnlySpan.CopyTo(new Span<string>(array3).Slice(num2, readOnlySpan.Length));
						num2 += readOnlySpan.Length;
						AddMove(val2, new HashSet<string>(new <>z__ReadOnlyArray<string>(array3)).ToArray(), graph2, item3, item4, null, move.PossiblePreviousMoveNodeIndices, null);
					}
				}
			}
			return graph2;
		}

		public Graph StateNodesToGraph(List<MonsterStateNode> stateNodes, IntentDefinition? intentDefinition)
		{
			Graph graph = new Graph();
			float num = intentDefinition?.Offset.X ?? 0f;
			GraphGenerationContext graphGenerationContext = new GraphGenerationContext
			{
				IntentDefinition = intentDefinition,
				PreviousStateNodes = MonsterStateNodeSimplifier.GetPrecessorDict(stateNodes.GetAllNodes()),
				Y = (intentDefinition?.Offset.Y ?? 0f)
			};
			foreach (MonsterStateNode stateNode in stateNodes)
			{
				graphGenerationContext.SubGraphs[graphGenerationContext.YIndex] = new SubGraph
				{
					Y = graphGenerationContext.Y
				};
				graphGenerationContext.Y += stateNode.Offset.Y;
				if (stateNodes.Count == 1 && stateNode.NextState == stateNode && stateNode.Children == null)
				{
					stateNode.NextState = null;
					stateNode.NextStateCount = 0;
				}
				AddStateNodeToGraph(stateNode, null, graph, graphGenerationContext, num + stateNode.Offset.X);
				AddPossiblePreviousMoveIds(graph.Moves, graphGenerationContext);
				graphGenerationContext.NewLine(graph.Height + 0.25f);
			}
			TuneArrowPosition(graph.Arrows, graphGenerationContext.ArrowTarget);
			return graph;
		}

		private (float x, float y) ResolveRelative(IRelativeToPosition relativeToPosition, Dictionary<string, MonsterStateNode>? stateNodes)
		{
			if (stateNodes == null || relativeToPosition.RelativeTo == null || !stateNodes.TryGetValue(relativeToPosition.RelativeTo, out MonsterStateNode value))
			{
				return (x: relativeToPosition.X, y: relativeToPosition.Y);
			}
			return (x: value.X + relativeToPosition.X, y: value.Y + relativeToPosition.Y);
		}

		private Arrow ResolveRelative(Arrow relativeToPosition, Dictionary<string, MonsterStateNode>? stateNodes)
		{
			if (stateNodes == null || relativeToPosition.RelativeTo == null || !stateNodes.TryGetValue(relativeToPosition.RelativeTo, out MonsterStateNode value))
			{
				return relativeToPosition;
			}
			float[] array = (float[])relativeToPosition.Path.Clone();
			array[1] += value.X;
			array[2] += value.Y;
			for (int i = 3; i < array.Length; i++)
			{
				array[i] += ((array[0] == (float)(i % 2)) ? value.Y : value.X);
			}
			return new Arrow(array);
		}

		private void AddStateNodeToGraph(MonsterStateNode stateNode, MonsterStateNode? precessorNode, Graph graph, GraphGenerationContext context, float x)
		{
			if (!stateNode.AddedToGraph)
			{
				if (stateNode.SimpleLoopStart && stateNode.SimpleLoopPrecessorCount == 1 && (stateNode.SimpleLoopLength >= 4 || (stateNode.SimpleLoopLength == 3 && (context.NextX >= 3f || graph.Height - context.Y >= 2f))))
				{
					AddSimpleLoopToGraph(stateNode, precessorNode, graph, context, x);
				}
				else
				{
					AddStateNodeToGraphNormal(stateNode, graph, context, x, 0f);
				}
			}
		}

		private void AddStateNodeToGraphNormal(MonsterStateNode stateNode, Graph graph, GraphGenerationContext context, float x, float yOffset)
		{
			if (stateNode.AddedToGraph)
			{
				return;
			}
			stateNode.AddedToGraph = true;
			stateNode.X = x;
			stateNode.Y = context.Y + yOffset;
			float y = stateNode.Y;
			if (stateNode.Parent != null)
			{
				stateNode.XIndex = stateNode.Parent.XIndex;
				stateNode.YIndex = stateNode.Parent.YIndex;
			}
			else
			{
				stateNode.XIndex = context.NextXIndex++;
				stateNode.YIndex = context.YIndex;
				context.IndexToNode[(stateNode.XIndex, stateNode.YIndex)] = stateNode;
			}
			context.SubGraphs[stateNode.YIndex].Nodes.Add(stateNode);
			if (context.NextX < x + stateNode.Width + 0.25f + 0.25f * (float)stateNode.NextStateCount)
			{
				context.NextX = x + stateNode.Width + 0.25f + 0.25f * (float)stateNode.NextStateCount;
			}
			if (x + stateNode.Width > graph.Width)
			{
				graph.Width = x + stateNode.Width;
			}
			if (y + stateNode.Height > graph.Height)
			{
				graph.Height = y + stateNode.Height;
			}
			if (stateNode.Parent == null)
			{
				stateNode.ArrowRight = x + stateNode.Width + 0.25f;
				stateNode.ArrowBottom = y + stateNode.Height + 0.25f;
			}
			else
			{
				stateNode.ArrowRight = stateNode.Parent.ArrowRight;
				stateNode.ArrowBottom = stateNode.Parent.ArrowBottom;
			}
			if (stateNode.Children == null)
			{
				MonsterState? state = stateNode.State;
				MoveState val = (MoveState)(object)((state is MoveState) ? state : null);
				if (val != null)
				{
					AddMove(val, stateNode, graph, x, y, context);
				}
			}
			else
			{
				SubGraph subGraph = context.SubGraphs[stateNode.YIndex];
				float num = yOffset + 0.25f + 0.1f;
				float num2 = x + 0.1f;
				for (int i = 0; i < stateNode.Children.Count; i++)
				{
					MonsterStateNode monsterStateNode = stateNode.Children[i];
					graph.Labels.Add(new Label(num2, context.Y + num - 0.04f, monsterStateNode.Label ?? string.Empty));
					subGraph.LabelIndices.Add(graph.Labels.Count - 1);
					if (stateNode.HorizontalLayout)
					{
						AddStateNodeToGraphNormal(monsterStateNode, graph, context, num2, num + ((monsterStateNode.Children == null) ? IntentGraphGenerator.IconGroupSingleMovePadding : 0f));
						num2 += monsterStateNode.Width + 0.1f;
						continue;
					}
					if (monsterStateNode.Children == null)
					{
						num += IntentGraphGenerator.IconGroupSingleMovePadding;
					}
					AddStateNodeToGraphNormal(monsterStateNode, graph, context, num2, num);
					num += monsterStateNode.Height + 0.25f;
				}
				graph.IconGroups.Add(new IconGroup(stateNode.X, stateNode.Y, stateNode.Width, stateNode.Height));
				subGraph.IconGroupIndices.Add(graph.IconGroups.Count - 1);
			}
			if (stateNode.NextState != null)
			{
				MonsterStateNode monsterStateNode2 = stateNode;
				while (monsterStateNode2.Parent != null)
				{
					monsterStateNode2 = monsterStateNode2.Parent;
				}
				MonsterStateNode nextState = stateNode.NextState;
				AddStateNodeToGraph(nextState, stateNode, graph, context, context.NextX);
				AddArrow(stateNode, nextState, graph, context);
			}
		}

		private void AddMove(MoveState moveState, MonsterStateNode node, Graph graph, float x, float y, GraphGenerationContext context)
		{
			SubGraph subGraph = context.SubGraphs[node.YIndex];
			MoveReplacement value = null;
			Dictionary<string, MoveReplacement> dictionary = context.IntentDefinition?.MoveReplacements;
			dictionary?.TryGetValue(((MonsterState)moveState).Id, out value);
			if (node.FullId != null && dictionary != null && dictionary.TryGetValue(node.FullId, out var value2))
			{
				value = value2;
			}
			IntentOverride[] intentOverrides = value?.IntentOverrides;
			Move move = AddMove(moveState, node.MoveStateIds.ToArray(), graph, x, y, intentOverrides, null, subGraph);
			context.StateNodeToMove[node] = move;
			context.MoveToStateNode[move] = node;
		}

		private Move AddMove(MoveState moveState, string[] ids, Graph graph, float x, float y, IntentOverride[]? intentOverrides, int?[]? possiblePreviousMoveIndices, SubGraph? subGraph)
		{
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			IReadOnlyList<AbstractIntent> intents = moveState.Intents;
			Icon[] array = new Icon[intents.Count];
			for (int i = 0; i < intents.Count; i++)
			{
				AbstractIntent val = intents[i];
				IntentOverride intentOverride = ((i < intentOverrides?.Length) ? intentOverrides[i] : null);
				AttackIntent val2 = (AttackIntent)(object)((val is AttackIntent) ? val : null);
				if (val2 != null)
				{
					array[i] = new Icon((float)i * 0.66999996f + x, y, val.IntentType, (int?)val2.DamageCalc?.Invoke(), val2.Repeats, intentOverride?.ValueText ?? string.Empty, intentOverride?.TimesText ?? string.Empty);
					continue;
				}
				StatusIntent val3 = (StatusIntent)(object)((val is StatusIntent) ? val : null);
				if (val3 != null)
				{
					array[i] = new Icon((float)i * 0.66999996f + x, y, val.IntentType, val3.CardCount, 1, intentOverride?.ValueText ?? string.Empty);
				}
				else
				{
					array[i] = new Icon((float)i * 0.66999996f + x, y, val.IntentType);
				}
			}
			Move move = new Move(ids[0], ids, x, y, array, possiblePreviousMoveIndices);
			graph.Moves.Add(move);
			subGraph?.MoveIndices.Add(graph.Moves.Count - 1);
			if (IntentGraphGenerator.ShowMonsterMoveNames)
			{
				AddMoveName(moveState, graph, x, y, subGraph);
			}
			return move;
		}

		private void AddMoveName(MoveState moveState, Graph graph, float x, float y, SubGraph? subGraph)
		{
			LocTable table = LocManager.Instance.GetTable("monsters");
			string text = ((AbstractModel)monster).Id.Entry;
			if (text.StartsWith("DECIMILLIPEDE_SEGMENT_"))
			{
				text = "DECIMILLIPEDE_SEGMENT";
			}
			string id = ((MonsterState)moveState).Id;
			if (!TryGetTitle(table, text, id, out string title) && id.Contains('_'))
			{
				int num = id.LastIndexOf('_');
				int num2 = id.IndexOf('_');
				if (TryGetTitle(table, text, id, out title, 0, num) || TryGetTitle(table, text, id, out title, 0, id.LastIndexOf('_', num - 1)) || TryGetTitle(table, text, id, out title, num2 + 1))
				{
					_ = 1;
				}
				else
					TryGetTitle(table, text, id, out title, num2 + 1, num);
			}
			if (title != null)
			{
				float num3 = (float)moveState.Intents.Count + (float)(moveState.Intents.Count - 1) * -0.33f;
				Label item = new Label(x + num3 / 2f, y + 0.2f, title, "center", 15);
				graph.Labels.Add(item);
				subGraph?.LabelIndices.Add(graph.Labels.Count - 1);
			}
		}

		private bool TryGetTitle(LocTable table, string monsterName, string moveId, out string? title, int startIndex = 0, int? endIndex = null)
		{
			title = null;
			int num = endIndex ?? moveId.Length;
			if (startIndex == -1 || num == -1 || num <= startIndex)
			{
				return false;
			}
			string text = monsterName + ".moves." + moveId.Substring(startIndex, num - startIndex) + ".title";
			if (table.HasEntry(text))
			{
				title = table.GetRawText(text);
				return true;
			}
			return false;
		}

		private void AddSimpleLoopToGraph(MonsterStateNode loopStart, MonsterStateNode? precessorNode, Graph graph, GraphGenerationContext context, float x)
		{
			if (loopStart.AddedToGraph)
			{
				return;
			}
			float y = context.Y;
			bool flag = precessorNode == null || (precessorNode.XIndex == context.NextXIndex - 1 && precessorNode.Y < y + 0.5f);
			List<MonsterStateNode> list = new List<MonsterStateNode>();
			MonsterStateNode monsterStateNode = loopStart;
			while (monsterStateNode != null && !list.Contains(monsterStateNode))
			{
				list.Add(monsterStateNode);
				monsterStateNode = monsterStateNode.NextState;
			}
			int simpleLoopLength = loopStart.SimpleLoopLength;
			float num = x;
			int num2 = list.IndexOf(loopStart);
			int i;
			for (i = num2 + (simpleLoopLength + 1) / 2; (double)(list.Skip(num2).Take(i - num2).Sum((MonsterStateNode n) => n.Width) + 0.5f * (float)(i - num2 - 1) + (flag ? 0.5f : ((0f - loopStart.Width) / 2f - 0.5f))) < (double)list.Skip(i).Sum((MonsterStateNode n) => n.Width) + 0.5 * (double)(list.Count - i - 1); i++)
			{
			}
			if (list.Count - i <= 0)
			{
				AddStateNodeToGraphNormal(loopStart, graph, context, x, y);
				return;
			}
			if (list.Count - i == 1 && list.Count > 3 && list[i].Width < 1.5f && context.NextX < 2f)
			{
				AddStateNodeToGraphNormal(loopStart, graph, context, x, y);
				return;
			}
			bool flag2 = list.Count - i <= 1 && flag;
			float num3 = y + 1.35f;
			for (int num4 = 0; num4 < i; num4++)
			{
				MonsterStateNode monsterStateNode2 = list[num4];
				monsterStateNode2.X = x;
				monsterStateNode2.Y = y;
				if (num4 == num2)
				{
					num = x;
				}
				MonsterState? state = monsterStateNode2.State;
				MoveState val = (MoveState)(object)((state is MoveState) ? state : null);
				if (val != null)
				{
					AddMove(val, monsterStateNode2, graph, monsterStateNode2.X, monsterStateNode2.Y, context);
				}
				if (monsterStateNode2.NextState != null)
				{
					if (num4 == i - 1)
					{
						if (!flag2)
						{
							AddArrow(graph, new Arrow(new float[4]
							{
								1f,
								x + monsterStateNode2.Width - Math.Min(monsterStateNode2.Width, monsterStateNode2.NextState.Width) / 2f,
								monsterStateNode2.Y + monsterStateNode2.Height,
								num3
							}), context, monsterStateNode2.NextState);
						}
					}
					else
					{
						AddArrow(graph, new Arrow(new float[4]
						{
							0f,
							x + monsterStateNode2.Width,
							monsterStateNode2.Y + 0.5f,
							x + monsterStateNode2.Width + 0.5f
						}), context, monsterStateNode2.NextState);
					}
				}
				x += monsterStateNode2.Width + 0.5f;
			}
			x -= 0.5f;
			context.NextX = x + 0.25f;
			graph.Width = x;
			graph.Height = Math.Max(num3 + 1f, graph.Height);
			if (!flag2)
			{
				float num5 = x - num;
				float num6 = ((flag && list.Count - i != 1) ? ((num5 - list.Skip(i).Sum((MonsterStateNode n) => n.Width)) / (float)(list.Count - i - 1)) : 0.5f);
				for (int num7 = i; num7 < list.Count; num7++)
				{
					MonsterStateNode monsterStateNode3 = list[num7];
					monsterStateNode3.X = x - monsterStateNode3.Width;
					monsterStateNode3.Y = num3;
					MonsterState? state2 = monsterStateNode3.State;
					MoveState val2 = (MoveState)(object)((state2 is MoveState) ? state2 : null);
					if (val2 != null)
					{
						AddMove(val2, monsterStateNode3, graph, monsterStateNode3.X, monsterStateNode3.Y, context);
					}
					if (monsterStateNode3.NextState != null)
					{
						if (num7 == list.Count - 1)
						{
							if (flag)
							{
								AddArrow(graph, new Arrow(new float[4]
								{
									1f,
									monsterStateNode3.X + Math.Min(monsterStateNode3.Width, monsterStateNode3.NextState.Width) / 2f,
									monsterStateNode3.Y,
									loopStart.Y + loopStart.Height
								}), context, monsterStateNode3.NextState);
							}
							else
							{
								AddArrow(graph, new Arrow(new float[5]
								{
									0f,
									monsterStateNode3.X,
									monsterStateNode3.Y + 0.5f,
									num + loopStart.Width / 2f,
									loopStart.Y + loopStart.Height
								}), context, monsterStateNode3.NextState);
							}
						}
						else
						{
							AddArrow(graph, new Arrow(new float[4]
							{
								0f,
								monsterStateNode3.X,
								monsterStateNode3.Y + 0.5f,
								monsterStateNode3.X - num6
							}), context, monsterStateNode3.NextState);
						}
					}
					x -= monsterStateNode3.Width + num6;
				}
			}
			else
			{
				MonsterStateNode monsterStateNode4 = list[i - 1];
				MonsterStateNode monsterStateNode5 = list[i];
				monsterStateNode5.X = num + (x - num) / 2f - monsterStateNode5.Width / 2f;
				monsterStateNode5.Y = num3;
				MonsterState? state3 = monsterStateNode5.State;
				MoveState val3 = (MoveState)(object)((state3 is MoveState) ? state3 : null);
				if (val3 != null)
				{
					AddMove(val3, monsterStateNode5, graph, monsterStateNode5.X, monsterStateNode5.Y, context);
				}
				if (monsterStateNode4.X + monsterStateNode4.Width / 2f < monsterStateNode5.X + monsterStateNode5.Width - 0.25f)
				{
					AddArrow(graph, new Arrow(new float[4]
					{
						1f,
						monsterStateNode4.X + monsterStateNode4.Width / 2f,
						monsterStateNode4.Y + monsterStateNode4.Height,
						monsterStateNode5.Y
					}), context, monsterStateNode5);
				}
				else
				{
					AddArrow(graph, new Arrow(new float[5]
					{
						1f,
						Math.Max(monsterStateNode4.X + monsterStateNode4.Width / 2f, monsterStateNode5.X + monsterStateNode5.Width + 0.25f),
						monsterStateNode4.Y + monsterStateNode4.Height,
						monsterStateNode5.Y + 0.5f,
						monsterStateNode5.X + monsterStateNode5.Width
					}), context, monsterStateNode5);
				}
				if (loopStart.X + loopStart.Width / 2f > monsterStateNode5.X + 0.25f)
				{
					AddArrow(graph, new Arrow(new float[4]
					{
						1f,
						loopStart.X + loopStart.Width / 2f,
						monsterStateNode5.Y,
						loopStart.Y + loopStart.Height
					}), context, loopStart);
				}
				else
				{
					AddArrow(graph, new Arrow(new float[5]
					{
						0f,
						monsterStateNode5.X,
						monsterStateNode5.Y + 0.5f,
						Math.Min(loopStart.X + loopStart.Width / 2f, monsterStateNode5.X - 0.25f),
						loopStart.Y + loopStart.Height
					}), context, loopStart);
				}
			}
			for (int num8 = 0; num8 < list.Count; num8++)
			{
				MonsterStateNode monsterStateNode6 = list[num8];
				monsterStateNode6.AddedToGraph = true;
				monsterStateNode6.AddedArrow = true;
				monsterStateNode6.XIndex = context.NextXIndex++;
				monsterStateNode6.YIndex = context.YIndex;
				context.IndexToNode[(monsterStateNode6.XIndex, monsterStateNode6.YIndex)] = monsterStateNode6;
				context.SubGraphs[monsterStateNode6.YIndex].Nodes.Add(monsterStateNode6);
			}
		}

		private void AddArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			if (stateNode.AddedArrow)
			{
				return;
			}
			stateNode.AddedArrow = true;
			if (TryAddOverrideArrow(stateNode, nextStateNode, graph, context))
			{
				return;
			}
			if (stateNode.YIndex == nextStateNode.YIndex)
			{
				if ((Math.Abs(stateNode.XIndex - nextStateNode.XIndex) != 1 || !TryAddHorizontalStraightArrow(stateNode, nextStateNode, graph, context)) && (stateNode.XIndex == nextStateNode.XIndex || !TryAddHorizontalThenUpArrow(stateNode, nextStateNode, graph, context)))
				{
					AddDefaultSameYArrow(stateNode, nextStateNode, graph, context);
				}
			}
			else if ((stateNode.YIndex != nextStateNode.YIndex + 1 || !TryAddVerticalStraightArrow(stateNode, nextStateNode, graph, context)) && !TryAddVerticalStartDifferentYArrow(stateNode, nextStateNode, graph, context))
			{
				AddDefaultDifferentYArrow(stateNode, nextStateNode, graph, context);
			}
		}

		private bool TryAddOverrideArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			if (!TryGetArrowOverride(stateNode, context, out ArrowOverride arrowOverride))
			{
				return false;
			}
			if (arrowOverride.Path.Length <= 3)
			{
				return true;
			}
			float?[] array = (float?[])arrowOverride.Path.Clone();
			int num = 0;
			int num2 = array.Length;
			if (array[0].HasValue)
			{
				if (array[0] == 0f)
				{
					if (!array[1].HasValue && array[3].HasValue)
					{
						float valueOrDefault = array[3].GetValueOrDefault();
						array[1] = ((valueOrDefault > stateNode.X + stateNode.Width / 2f) ? (stateNode.X + stateNode.Width) : stateNode.X);
						num = 1;
					}
					if (!array[2].HasValue)
					{
						array[2] = stateNode.Y + stateNode.Height / 2f;
						num = 2;
					}
				}
				else
				{
					if (!array[1].HasValue)
					{
						array[1] = stateNode.X + stateNode.Width / 2f;
						num = 1;
					}
					if (!array[2].HasValue && array[3].HasValue)
					{
						float valueOrDefault2 = array[3].GetValueOrDefault();
						array[2] = ((valueOrDefault2 > stateNode.Y + stateNode.Height / 2f) ? (stateNode.Y + stateNode.Height) : stateNode.Y);
						num = 2;
					}
				}
				if ((float)(array.Length % 2) == array[0])
				{
					int num3 = ((array.Length <= 5) ? 1 : (array.Length - 3));
					if (!array[^1].HasValue && array[num3].HasValue)
					{
						float valueOrDefault3 = array[num3].GetValueOrDefault();
						array[^1] = ((valueOrDefault3 > nextStateNode.X + nextStateNode.Width / 2f) ? (nextStateNode.X + nextStateNode.Width) : nextStateNode.X);
						num2 = array.Length - 1;
					}
					if (!array[^2].HasValue)
					{
						array[^2] = nextStateNode.Y + nextStateNode.Height / 2f;
						num2 = array.Length - 2;
					}
				}
				else
				{
					int num4 = ((array.Length > 4) ? (array.Length - 3) : 2);
					if (!array[^1].HasValue && array[num4].HasValue)
					{
						float valueOrDefault4 = array[num4].GetValueOrDefault();
						array[^1] = ((valueOrDefault4 > nextStateNode.Y + nextStateNode.Height / 2f) ? (nextStateNode.Y + nextStateNode.Height) : nextStateNode.Y);
						num2 = array.Length - 1;
					}
					if (!array[^2].HasValue && array.Length > 4)
					{
						array[^2] = nextStateNode.X + nextStateNode.Width / 2f;
						num2 = array.Length - 2;
					}
				}
			}
			Arrow arrow = new Arrow(array.Select((float? p) => p.GetValueOrDefault()).ToArray());
			AddArrow(graph, arrow, context, nextStateNode, addSubGraph: false);
			if (num > 0)
			{
				context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, num + 1));
			}
			if (num2 < array.Length)
			{
				context.SubGraphs[nextStateNode.YIndex].Arrows.Add((arrow, num2, array.Length - num2));
			}
			return true;
		}

		private bool TryAddHorizontalStraightArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			float num = Math.Max(stateNode.Y + 0.25f, nextStateNode.Y + 0.25f);
			float num2 = Math.Min(stateNode.Y + stateNode.Height - 0.25f, nextStateNode.Y + nextStateNode.Height - 0.25f);
			if (num <= num2)
			{
				float num3 = (num + num2) / 2f;
				ArrowOverride arrowOverride;
				if (stateNode.X < nextStateNode.X)
				{
					if (nextStateNode.NextState == stateNode && !nextStateNode.AddedArrow && !TryGetArrowOverride(nextStateNode, context, out arrowOverride))
					{
						AddArrow(graph, new Arrow(new float[4]
						{
							0f,
							stateNode.X + stateNode.Width,
							num3 - 0.2f,
							nextStateNode.X
						}), context, nextStateNode);
						AddArrow(graph, new Arrow(new float[4]
						{
							0f,
							nextStateNode.X,
							num3 + 0.2f,
							stateNode.X + stateNode.Width
						}), context, stateNode);
						nextStateNode.AddedArrow = true;
						return true;
					}
					Arrow arrow = new Arrow(new float[4]
					{
						0f,
						stateNode.X + stateNode.Width,
						num3,
						nextStateNode.X
					});
					if (stateNode.Parent != null)
					{
						arrow.Path[1] -= 0.1f;
					}
					AddArrow(graph, arrow, context, nextStateNode);
					return true;
				}
				if (nextStateNode.NextState == stateNode && !nextStateNode.AddedArrow && !TryGetArrowOverride(nextStateNode, context, out arrowOverride))
				{
					AddArrow(graph, new Arrow(new float[4]
					{
						0f,
						stateNode.X,
						num3 + 0.2f,
						nextStateNode.X + nextStateNode.Width
					}), context, nextStateNode);
					AddArrow(graph, new Arrow(new float[4]
					{
						0f,
						nextStateNode.X + nextStateNode.Width,
						num3 - 0.2f,
						stateNode.X
					}), context, stateNode);
					nextStateNode.AddedArrow = true;
					return true;
				}
				Arrow arrow2 = new Arrow(new float[4]
				{
					0f,
					stateNode.X,
					num3,
					nextStateNode.X + nextStateNode.Width
				});
				if (stateNode.Parent != null)
				{
					arrow2.Path[1] += 0.1f;
				}
				AddArrow(graph, arrow2, context, nextStateNode);
				return true;
			}
			return false;
		}

		private bool TryAddHorizontalThenUpArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			float num = stateNode.Y + stateNode.Height / 2f;
			if (num < nextStateNode.Y + nextStateNode.Height + 0.25f)
			{
				return false;
			}
			bool flag = true;
			for (int i = Math.Min(stateNode.XIndex, nextStateNode.XIndex) + 1; i < Math.Max(stateNode.XIndex, nextStateNode.XIndex); i++)
			{
				MonsterStateNode monsterStateNode = context.IndexToNode[(i, stateNode.YIndex)];
				if (monsterStateNode.Y + monsterStateNode.Height + 0.2f > num)
				{
					flag = false;
					break;
				}
			}
			if (flag && !context.HLineTargetNode.ContainsKey(num))
			{
				context.HLineTargetNode[num] = nextStateNode;
				Arrow arrow;
				if (stateNode.X < nextStateNode.X)
				{
					arrow = new Arrow(new float[5]
					{
						0f,
						stateNode.X + stateNode.Width,
						num,
						nextStateNode.X + nextStateNode.Width / 2f,
						nextStateNode.Y + nextStateNode.Height
					});
					if (stateNode.Parent != null)
					{
						arrow.Path[1] -= 0.1f;
					}
				}
				else
				{
					arrow = new Arrow(new float[5]
					{
						0f,
						stateNode.X,
						num,
						nextStateNode.X + nextStateNode.Width / 2f,
						nextStateNode.Y + nextStateNode.Height
					});
					if (stateNode.Parent != null)
					{
						arrow.Path[1] += 0.1f;
					}
				}
				AddArrow(graph, arrow, context, nextStateNode);
				return true;
			}
			return false;
		}

		private void AddDefaultSameYArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			float num;
			MonsterStateNode value;
			for (num = stateNode.ArrowRight; context.VLineTargetNode.TryGetValue(num, out value); num += 0.25f)
			{
				if (value == nextStateNode)
				{
					break;
				}
			}
			float num2 = ((nextStateNode.XIndex <= stateNode.XIndex) ? stateNode.ArrowBottom : 0f);
			for (int i = Math.Min(stateNode.XIndex, nextStateNode.XIndex) + 1; i < Math.Max(stateNode.XIndex, nextStateNode.XIndex); i++)
			{
				MonsterStateNode monsterStateNode = context.IndexToNode[(i, stateNode.YIndex)];
				if (num2 < monsterStateNode.Y + monsterStateNode.Height + 0.25f)
				{
					num2 = monsterStateNode.Y + monsterStateNode.Height + 0.25f;
				}
			}
			if (num2 < nextStateNode.Y + nextStateNode.Height + 0.25f)
			{
				num2 = nextStateNode.Y + nextStateNode.Height + 0.25f;
			}
			MonsterStateNode value2;
			for (; context.HLineTargetNode.TryGetValue(num2, out value2); num2 += 0.25f)
			{
				if (value2 == nextStateNode)
				{
					break;
				}
			}
			context.VLineTargetNode[num] = nextStateNode;
			context.HLineTargetNode[num2] = nextStateNode;
			Arrow arrow = new Arrow(new float[7]
			{
				0f,
				stateNode.X + stateNode.Width,
				stateNode.Y + stateNode.Height / 2f,
				num,
				num2,
				nextStateNode.X + nextStateNode.Width / 2f,
				nextStateNode.Y + nextStateNode.Height
			});
			if (stateNode.Parent != null && stateNode.Children == null)
			{
				arrow.Path[1] -= 0.1f;
			}
			AddArrow(graph, arrow, context, nextStateNode);
			if (num > graph.Width)
			{
				graph.Width = num;
			}
			if (num2 > graph.Height)
			{
				graph.Height = num2;
			}
		}

		private bool TryAddVerticalStraightArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			if (stateNode.Parent != null && !stateNode.Parent.HorizontalLayout)
			{
				return false;
			}
			float num = Math.Max(stateNode.X + 0.25f, nextStateNode.X + 0.25f);
			float num2 = Math.Min(stateNode.X + stateNode.Width - 0.25f, nextStateNode.X + nextStateNode.Width - 0.25f);
			if (num <= num2)
			{
				float num3 = (num + num2) / 2f;
				Arrow arrow = new Arrow(new float[4]
				{
					1f,
					num3,
					stateNode.Y,
					nextStateNode.Y + nextStateNode.Height
				});
				AddArrow(graph, arrow, context, nextStateNode, addSubGraph: false);
				context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, 3));
				context.SubGraphs[nextStateNode.YIndex].Arrows.Add((arrow, 3, 1));
				return true;
			}
			return false;
		}

		private bool TryAddVerticalStartDifferentYArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			if (stateNode.Parent != null && !stateNode.Parent.HorizontalLayout)
			{
				return false;
			}
			float num = stateNode.X + stateNode.Width / 2f;
			float num2 = nextStateNode.X + nextStateNode.Width / 2f;
			float? midY = ((IEnumerable<float>)(from p in context.HLineTargetNode
				where p.Value == nextStateNode
				select p.Key into p
				orderby p
				select p)).Select((Func<float, float?>)((float k) => k)).FirstOrDefault();
			SubGraph subGraph = context.SubGraphs[stateNode.YIndex];
			if (!midY.HasValue)
			{
				float num3 = 0.25f;
				midY = subGraph.Y;
				context.Y += num3;
				graph.Height += num3;
				subGraph.MoveY(graph, num3);
				context.HLineTargetNode[midY.Value] = nextStateNode;
				foreach (var (num5, value) in context.HLineTargetNode.Where<KeyValuePair<float, MonsterStateNode>>((KeyValuePair<float, MonsterStateNode> p) => p.Key > midY.Value).ToList())
				{
					context.HLineTargetNode[num5 + num3] = value;
					context.HLineTargetNode.Remove(num5);
				}
			}
			Arrow arrow = new Arrow(new float[6]
			{
				1f,
				num,
				stateNode.Y,
				midY.Value,
				num2,
				nextStateNode.Y + nextStateNode.Height
			});
			AddArrow(graph, arrow, context, nextStateNode, addSubGraph: false);
			context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, 3));
			GetSubGraphByY(context.SubGraphs, midY.Value).Arrows.Add((arrow, 3, 2));
			context.SubGraphs[nextStateNode.YIndex].Arrows.Add((arrow, 5, 1));
			return true;
		}

		private void AddDefaultDifferentYArrow(MonsterStateNode stateNode, MonsterStateNode nextStateNode, Graph graph, GraphGenerationContext context)
		{
			float num;
			MonsterStateNode value;
			for (num = stateNode.ArrowRight; context.VLineTargetNode.TryGetValue(num, out value); num += 0.25f)
			{
				if (value == nextStateNode)
				{
					break;
				}
			}
			float num2 = nextStateNode.X + nextStateNode.Width / 2f;
			float? midY = (from p in context.HLineTargetNode
				where p.Value == nextStateNode
				select p.Key).Select((Func<float, float?>)((float k) => k)).FirstOrDefault();
			SubGraph subGraph = context.SubGraphs[stateNode.YIndex];
			if (!midY.HasValue)
			{
				float num3 = 0.25f;
				midY = subGraph.Y;
				context.Y += num3;
				graph.Height += num3;
				subGraph.MoveY(graph, num3);
				context.HLineTargetNode[midY.Value] = nextStateNode;
				foreach (var (num5, value2) in context.HLineTargetNode.Where<KeyValuePair<float, MonsterStateNode>>((KeyValuePair<float, MonsterStateNode> p) => p.Key > midY.Value).ToList())
				{
					context.HLineTargetNode[num5 + num3] = value2;
					context.HLineTargetNode.Remove(num5);
				}
			}
			context.VLineTargetNode[num] = nextStateNode;
			Arrow arrow = new Arrow(new float[7]
			{
				0f,
				stateNode.X + stateNode.Width,
				stateNode.Y + stateNode.Height / 2f,
				num,
				midY.Value,
				num2,
				nextStateNode.Y + nextStateNode.Height
			});
			if (stateNode.Parent != null && stateNode.Children == null)
			{
				arrow.Path[1] -= 0.1f;
			}
			AddArrow(graph, arrow, context, nextStateNode, addSubGraph: false);
			context.SubGraphs[stateNode.YIndex].Arrows.Add((arrow, 0, 4));
			GetSubGraphByY(context.SubGraphs, midY.Value).Arrows.Add((arrow, 4, 2));
			context.SubGraphs[nextStateNode.YIndex].Arrows.Add((arrow, 6, 1));
			if (num > graph.Width)
			{
				graph.Width = num;
			}
		}

		private SubGraph GetSubGraphByY(Dictionary<int, SubGraph> subGraphs, float value)
		{
			SubGraph result = subGraphs[0];
			SubGraph value2;
			for (int i = 0; subGraphs.TryGetValue(i, out value2); i++)
			{
				if (value2.Y >= value)
				{
					break;
				}
				result = value2;
			}
			return result;
		}

		private bool TryGetArrowOverride(MonsterStateNode stateNode, GraphGenerationContext context, [NotNullWhen(true)] out ArrowOverride? arrowOverride)
		{
			arrowOverride = null;
			if (stateNode.FullId != null)
			{
				IntentDefinition? intentDefinition = context.IntentDefinition;
				MoveReplacement value = default(MoveReplacement);
				if (intentDefinition != null && intentDefinition.MoveReplacements?.TryGetValue(stateNode.FullId, out value) == true)
				{
					return (arrowOverride = value?.ArrowOverride) != null;
				}
			}
			return false;
		}

		private void AddArrow(Graph graph, Arrow arrow, GraphGenerationContext context, MonsterStateNode target, bool addSubGraph = true)
		{
			graph.Arrows.Add(arrow);
			context.ArrowTarget[arrow] = target;
			if (addSubGraph)
			{
				context.SubGraphs[target.YIndex].Arrows.Add((arrow, 0, arrow.Path.Length));
			}
		}

		private void AddPossiblePreviousMoveIds(List<Move> moves, GraphGenerationContext context)
		{
			List<Move> list = new List<Move>(moves.Count);
			foreach (Move move in moves)
			{
				MonsterStateNode valueOrDefault = context.MoveToStateNode.GetValueOrDefault(move);
				if (valueOrDefault == null)
				{
					list.Add(move);
					continue;
				}
				HashSet<MonsterStateNode> valueOrDefault2 = context.PreviousStateNodes.GetValueOrDefault(valueOrDefault);
				int?[] possiblePreviousMoveNodeIndices;
				if (valueOrDefault2 == null)
				{
					possiblePreviousMoveNodeIndices = ((!valueOrDefault.IsInitialState) ? null : new int?[1]);
				}
				else
				{
					HashSet<int?> hashSet = (from v in (from n in valueOrDefault2.SelectMany(delegate(MonsterStateNode n)
							{
								List<MonsterStateNode> list2 = new List<MonsterStateNode>();
								list2.Add(n);
								list2.AddRange(n.GetAllDescendants());
								return new <>z__ReadOnlyList<MonsterStateNode>(list2);
							})
							where n.Children == null
							select n).Select((Func<MonsterStateNode, int?>)((MonsterStateNode n) => moves.IndexOf(context.StateNodeToMove.GetValueOrDefault(n))))
						where v != -1 && v.HasValue
						select v).ToHashSet();
					if (valueOrDefault.IsInitialState)
					{
						int? num = null;
						HashSet<int?> hashSet2 = hashSet;
						int num2 = 0;
						int?[] array = new int?[1 + hashSet2.Count];
						array[num2] = num;
						num2++;
						foreach (int? item in hashSet2)
						{
							array[num2] = item;
							num2++;
						}
						possiblePreviousMoveNodeIndices = array;
					}
					else
					{
						possiblePreviousMoveNodeIndices = hashSet.ToArray();
					}
				}
				list.Add(move with
				{
					PossiblePreviousMoveNodeIndices = possiblePreviousMoveNodeIndices
				});
			}
			moves.Clear();
			moves.AddRange(list);
		}

		private void TuneArrowPosition(List<Arrow> arrows, Dictionary<Arrow, MonsterStateNode> arrowTarget)
		{
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_026b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0285: Unknown result type (might be due to invalid IL or missing references)
			//IL_0293: Unknown result type (might be due to invalid IL or missing references)
			//IL_029a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02af: Unknown result type (might be due to invalid IL or missing references)
			//IL_021e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_0189: Unknown result type (might be due to invalid IL or missing references)
			//IL_0190: Unknown result type (might be due to invalid IL or missing references)
			//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0239: Unknown result type (might be due to invalid IL or missing references)
			//IL_0240: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0318: Unknown result type (might be due to invalid IL or missing references)
			//IL_031f: Unknown result type (might be due to invalid IL or missing references)
			for (int i = 0; i < arrows.Count; i++)
			{
				Arrow arrow = arrows[i];
				for (int j = i + 1; j < arrows.Count; j++)
				{
					Arrow arrow2 = arrows[j];
					bool flag = arrowTarget[arrow] == arrowTarget[arrow2];
					foreach (var (flag2, val, val2, num) in ArrowSegments(arrow))
					{
						foreach (var (flag3, val3, val4, num2) in ArrowSegments(arrow2))
						{
							if (flag2 != flag3)
							{
								break;
							}
							if (flag2 && Math.Abs(val.Y - val3.Y) < 0.2f)
							{
								if (flag && Math.Abs(val2.X - val4.X) < 0.001f)
								{
									float num3 = (val.Y + val3.Y) / 2f;
									arrow.Path[num] = num3;
									arrow2.Path[num2] = num3;
									continue;
								}
								float val5 = Math.Min(val.X, val2.X);
								float val6 = Math.Min(val3.X, val4.X);
								float val7 = Math.Max(val.X, val2.X);
								float val8 = Math.Max(val3.X, val4.X);
								if (Math.Max(val5, val6) < Math.Min(val7, val8))
								{
									float num4 = (val.Y + val3.Y) / 2f;
									arrow.Path[num] = num4 + ((val.X < val3.X) ? (-0.15f) : 0.15f);
									arrow2.Path[num2] = num4 + ((val.X < val3.X) ? 0.15f : (-0.15f));
								}
							}
							if (flag2 || !((double)Math.Abs(val.X - val3.X) < 0.12))
							{
								continue;
							}
							if (flag && Math.Abs(val2.Y - val4.Y) < 0.001f)
							{
								float num5 = (val.X + val3.X) / 2f;
								arrow.Path[num] = num5;
								arrow2.Path[num2] = num5;
								continue;
							}
							float val9 = Math.Min(val.Y, val2.Y);
							float val10 = Math.Min(val3.Y, val4.Y);
							float val11 = Math.Max(val.Y, val2.Y);
							float val12 = Math.Max(val3.Y, val4.Y);
							if (Math.Max(val9, val10) < Math.Min(val11, val12))
							{
								float num6 = (val.X + val3.X) / 2f;
								arrow.Path[num] = num6 + ((val.Y < val3.Y) ? (-0.15f) : 0.15f);
								arrow2.Path[num2] = num6 + ((val.Y < val3.Y) ? 0.15f : (-0.15f));
							}
						}
					}
				}
			}
		}

		private IEnumerable<(bool horizontal, Vector2 start, Vector2 end, int pathIndex)> ArrowSegments(Arrow arrow)
		{
			bool horizontal = arrow.Path[0] == 0f;
			float x = arrow.Path[1];
			float y = arrow.Path[2];
			int xIndex = 1;
			int yIndex = 2;
			for (int i = 3; i < arrow.Path.Length; i++)
			{
				if (horizontal)
				{
					yield return (horizontal: horizontal, start: new Vector2(x, y), end: new Vector2(arrow.Path[i], y), pathIndex: yIndex);
					x = arrow.Path[i];
					xIndex = i;
				}
				else
				{
					yield return (horizontal: horizontal, start: new Vector2(x, y), end: new Vector2(x, arrow.Path[i]), pathIndex: xIndex);
					y = arrow.Path[i];
					yIndex = i;
				}
				horizontal = !horizontal;
			}
		}
	}
	internal class IntentGraphLocalizer
	{
		private readonly IReadOnlyDictionary<string, string>? overwriteIntentStrings;

		public IntentGraphLocalizer(IReadOnlyDictionary<string, string>? overwriteIntentStrings)
		{
			this.overwriteIntentStrings = overwriteIntentStrings;
		}

		public bool TryGet(string key, [NotNullWhen(true)] out string? value)
		{
			if (overwriteIntentStrings != null && overwriteIntentStrings.TryGetValue(key, out value))
			{
				return true;
			}
			return IntentGraphMod.IntentGraphStrings.TryGetValue(key, out value);
		}

		public string GetOrElse(string key, string fallbackValue)
		{
			if (!TryGet(key, out string value))
			{
				return fallbackValue;
			}
			return value;
		}
	}
	internal class MonsterStateNode
	{
		public string? Id { get; set; }

		public float Width { get; set; }

		public float Height { get; set; }

		public MonsterStateNode? Parent { get; set; }

		public List<MonsterStateNode>? Children { get; set; }

		public bool HorizontalLayout { get; set; }

		public string? Label { get; set; }

		public bool IsLabelGenerated { get; set; }

		public MonsterState? State { get; set; }

		public MonsterStateNode? NextState { get; set; }

		public List<string> MoveStateIds { get; set; } = new List<string>();

		public bool IsInitialState { get; set; }

		public int NextStateCount { get; set; }

		public bool UnrecognizedStateType { get; set; }

		public bool ForceNotSimpleLoop { get; set; }

		public bool SimpleLoopStart { get; set; }

		public int SimpleLoopLength { get; set; }

		public int SimpleLoopPrecessorCount { get; set; }

		public Position Offset { get; set; }

		public float X { get; set; }

		public float Y { get; set; }

		public bool AddedToGraph { get; set; }

		public int XIndex { get; set; }

		public int YIndex { get; set; }

		public bool AddedArrow { get; set; }

		public float ArrowRight { get; set; }

		public float ArrowBottom { get; set; }

		public string? FullId
		{
			get
			{
				if (Parent == null)
				{
					if (Id != null)
					{
						return "/" + Id;
					}
					return null;
				}
				string fullId = Parent.FullId;
				if (fullId != null && Id != null)
				{
					return fullId + "/" + Id;
				}
				return null;
			}
		}

		public void CalculateNodeSize()
		{
			List<MonsterStateNode> children = Children;
			if (children == null)
			{
				return;
			}
			if (HorizontalLayout)
			{
				Width = children.Select((MonsterStateNode c) => c.Width).Sum() + 0.1f * (float)(children.Count - 1) + 0.2f;
				Height = children.Select((MonsterStateNode c) => c.Height).DefaultIfEmpty(1f).Max() + 0.25f + 0.2f + (children.All((MonsterStateNode c) => c.Children == null) ? IntentGraphGenerator.IconGroupSingleMovePadding : 0f);
			}
			else
			{
				Width = children.Select((MonsterStateNode c) => c.Width).DefaultIfEmpty(1f).Max() + 0.2f;
				Height = 0.25f * (float)children.Count + 0.2f + children.Select((MonsterStateNode c) => c.Height).Sum() + IntentGraphGenerator.IconGroupSingleMovePadding * (float)children.Where((MonsterStateNode c) => c.Children == null).Count();
			}
		}

		public HashSet<MonsterStateNode> GetAllNodes()
		{
			HashSet<MonsterStateNode> hashSet = new HashSet<MonsterStateNode>();
			Queue<MonsterStateNode> queue = new Queue<MonsterStateNode>();
			queue.Enqueue(this);
			while (queue.Count > 0)
			{
				MonsterStateNode monsterStateNode = queue.Dequeue();
				if (!hashSet.Add(monsterStateNode))
				{
					continue;
				}
				if (monsterStateNode.Children != null)
				{
					foreach (MonsterStateNode child in monsterStateNode.Children)
					{
						queue.Enqueue(child);
					}
				}
				if (monsterStateNode.NextState != null)
				{
					queue.Enqueue(monsterStateNode.NextState);
				}
			}
			return hashSet;
		}

		public void AddMoveStateIdsFrom(MonsterStateNode b)
		{
			MoveStateIds.AddRange(b.MoveStateIds);
			if (Children != null && b.Children != null)
			{
				for (int i = 0; i < Children.Count; i++)
				{
					Children[i].AddMoveStateIdsFrom(b.Children[i]);
				}
			}
		}

		public void SetIsInitialState(bool value)
		{
			IsInitialState = value;
			if (Children == null)
			{
				return;
			}
			foreach (MonsterStateNode child in Children)
			{
				child.SetIsInitialState(value);
			}
		}

		public IEnumerable<MonsterStateNode> GetAllDescendants()
		{
			if (Children == null)
			{
				yield break;
			}
			foreach (MonsterStateNode child in Children)
			{
				yield return child;
				foreach (MonsterStateNode allDescendant in child.GetAllDescendants())
				{
					yield return allDescendant;
				}
			}
		}
	}
	public static class MonsterStateNodeExtensions
	{
		internal static HashSet<MonsterStateNode> GetAllNodes(this IEnumerable<MonsterStateNode> nodes)
		{
			HashSet<MonsterStateNode> hashSet = new HashSet<MonsterStateNode>();
			foreach (MonsterStateNode node in nodes)
			{
				foreach (MonsterStateNode allNode in node.GetAllNodes())
				{
					hashSet.Add(allNode);
				}
			}
			return hashSet;
		}
	}
	internal class MonsterStateNodeConverter
	{
		private const string OtherwiseMark = "{otherwise}";

		private readonly IntentGraphLocalizer localizer;

		public MonsterStateNodeConverter(IntentGraphLocalizer localizer)
		{
			this.localizer = localizer;
		}

		public List<MonsterStateNode> FromStateMachineNodes(MonsterMoveStateMachine stateMachine, StateMachineNode[] overwriteStateMachine, Font font)
		{
			Dictionary<string, MonsterStateNode> existingNodes = new Dictionary<string, MonsterStateNode>();
			List<(int, MonsterStateNode)> list = new List<(int, MonsterStateNode)>();
			foreach (StateMachineNode stateMachineNode in overwriteStateMachine)
			{
				if (stateMachineNode.IsInitialState)
				{
					MonsterStateNode monsterStateNode = StateMachineNodeToMonsterStateNode(font, stateMachine, overwriteStateMachine, stateMachineNode, existingNodes, null);
					if (monsterStateNode != null)
					{
						list.Add((stateMachineNode.InitialStatePriority, monsterStateNode));
					}
				}
			}
			List<MonsterStateNode> list2 = (from t in list
				orderby t.Item1
				select t.Item2).ToList();
			MonsterStateNodeSimplifier.FindAndSetSimpleLoops(list2);
			MonsterStateNode? monsterStateNode2 = list2.FirstOrDefault();
			if (monsterStateNode2 != null)
			{
				monsterStateNode2.SetIsInitialState(value: true);
				return list2;
			}
			return list2;
		}

		public List<MonsterStateNode> FromMonsterMoveStateMachine(string monsterName, Font font, MonsterMoveStateMachine stateMachine, MonsterState initialState, IntentDefinition? intentDefinition, ref string? warning)
		{
			Dictionary<MonsterState, MonsterStateNode> dictionary = new Dictionary<MonsterState, MonsterStateNode>();
			List<MonsterStateNode> list = new List<MonsterStateNode>();
			MonsterStateNode monsterStateNode = null;
			ConditionalBranchState val = (ConditionalBranchState)(object)((initialState is ConditionalBranchState) ? initialState : null);
			if (val != null)
			{
				string stateName = val.EvaluateStates();
				MonsterState val2 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == stateName));
				if (val2 != null)
				{
					monsterStateNode = MonsterStateToMonsterStateNode(monsterName, font, stateMachine, val2, dictionary, null, ref warning);
					monsterStateNode.SetIsInitialState(value: true);
					if (monsterStateNode.NextState?.State == initialState)
					{
						monsterStateNode = null;
					}
				}
			}
			if (monsterStateNode == null)
			{
				monsterStateNode = MonsterStateToMonsterStateNode(monsterName, font, stateMachine, initialState, dictionary, null, ref warning);
				monsterStateNode.SetIsInitialState(value: true);
			}
			HashSet<MonsterStateNode> allNodes = monsterStateNode.GetAllNodes();
			MonsterStateNodeSimplifier.SimplifyStateNodes(monsterStateNode, allNodes, this);
			list.Add(monsterStateNode);
			SecondaryInitialState[] array = intentDefinition?.SecondaryInitialStates;
			if (array != null)
			{
				SecondaryInitialState[] array2 = array;
				foreach (SecondaryInitialState secondaryState in array2)
				{
					MonsterState val3 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == secondaryState.Id));
					if (val3 == null || dictionary.ContainsKey(val3))
					{
						continue;
					}
					MonsterStateNode monsterStateNode2 = MonsterStateToMonsterStateNode(monsterName, font, stateMachine, val3, dictionary, null, ref warning);
					HashSet<MonsterStateNode> allNodes2 = monsterStateNode2.GetAllNodes();
					MonsterStateNodeSimplifier.SimplifyStateNodes(monsterStateNode2, allNodes2, this);
					foreach (MonsterStateNode item in allNodes2)
					{
						allNodes.Add(item);
					}
					monsterStateNode2.Offset = secondaryState.Offset;
					list.Add(monsterStateNode2);
				}
			}
			foreach (string stateId in stateMachine.States.Keys)
			{
				if (stateId != "INIT_MOVE" && !allNodes.Any(delegate(MonsterStateNode n)
				{
					MonsterState? state = n.State;
					return ((state != null) ? state.Id : null) == stateId;
				}))
				{
					IgLogger.Warn($"State '{stateId}' is not included in the graph for monster '{monsterName}'.");
					break;
				}
			}
			if (warning == null && allNodes.Any((MonsterStateNode n) => n.UnrecognizedStateType))
			{
				warning = localizer.GetOrElse("ui.Incomplete", "Incomplete");
			}
			MonsterStateNodeSimplifier.FindAndSetSimpleLoops(list);
			return list;
		}

		public string MakeText(StateWeight s, float sumWeight)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Expected I4, but got Unknown
			string text = ((int)(((StateWeight)(ref s)).GetWeight() / sumWeight * 100f)).ToString();
			string text2;
			if (s.cooldown > 0)
			{
				text2 = ", ?" + s.cooldown;
			}
			else
			{
				MoveRepeatType repeatType = s.repeatType;
				text2 = (int)repeatType switch
				{
					0 => "", 
					1 => ", ≤" + s.maxTimes, 
					2 => ", ≤1", 
					3 => ", " + localizer.GetOrElse("ui.UseOnlyOnce", "one use"), 
					_ => "", 
				};
			}
			return text + "%" + text2;
		}

		private MonsterStateNode? StateMachineNodeToMonsterStateNode(Font font, MonsterMoveStateMachine stateMachine, StateMachineNode[] overwriteStateMachine, StateMachineNode? node, Dictionary<string, MonsterStateNode> existingNodes, MonsterStateNode? parent)
		{
			//IL_0314: Unknown result type (might be due to invalid IL or missing references)
			if (node == null)
			{
				return null;
			}
			string name = node.Name;
			if (parent == null && existingNodes.TryGetValue(name, out MonsterStateNode value))
			{
				return value;
			}
			if (node.Children == null || node.Children.Length == 0)
			{
				MonsterState? obj = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState s) => s.Id == (node.MoveName ?? node.Name)));
				MoveState val = (MoveState)(object)((obj is MoveState) ? obj : null);
				MonsterStateNode monsterStateNode;
				if (val != null)
				{
					monsterStateNode = new MonsterStateNode
					{
						Id = node.Name,
						State = (MonsterState?)(object)val,
						Width = (float)val.Intents.Count + (float)(val.Intents.Count - 1) * -0.33f,
						Height = 1f,
						NextStateCount = 1,
						Parent = parent,
						ForceNotSimpleLoop = node.NotSimpleLoopStart,
						Offset = node.Offset
					};
					monsterStateNode.MoveStateIds.Add(((MonsterState)val).Id);
					if (node.AlternativeMoveNames != null)
					{
						monsterStateNode.MoveStateIds.AddRange(node.AlternativeMoveNames);
					}
				}
				else
				{
					if (node.PlaceholderIntentCount <= 0)
					{
						return null;
					}
					monsterStateNode = new MonsterStateNode
					{
						Id = node.Name,
						State = null,
						Width = (float)node.PlaceholderIntentCount + (float)(node.PlaceholderIntentCount - 1) * -0.33f,
						Height = 1f,
						NextStateCount = 1,
						Parent = parent,
						ForceNotSimpleLoop = node.NotSimpleLoopStart,
						Offset = node.Offset
					};
					if (node.AlternativeMoveNames != null)
					{
						monsterStateNode.MoveStateIds.AddRange(node.AlternativeMoveNames);
					}
				}
				if (parent == null)
				{
					existingNodes[name] = monsterStateNode;
				}
				if (node.FollowUpState != null)
				{
					monsterStateNode.NextState = StateMachineNodeToMonsterStateNode(font, stateMachine, overwriteStateMachine, overwriteStateMachine.FirstOrDefault((StateMachineNode n) => n.Name == node.FollowUpState), existingNodes, null);
				}
				return monsterStateNode;
			}
			MonsterStateNode monsterStateNode2 = new MonsterStateNode
			{
				Id = node.Name,
				State = null,
				Parent = parent,
				ForceNotSimpleLoop = node.NotSimpleLoopStart,
				Offset = node.Offset
			};
			if (parent == null)
			{
				existingNodes[name] = monsterStateNode2;
			}
			List<MonsterStateNode> list = new List<MonsterStateNode>();
			for (int num = 0; num < node.Children.Length; num++)
			{
				StateMachineNode node2 = node.Children[num].Node;
				string label = node.Children[num].Label;
				label = localizer.GetOrElse(label, label);
				MonsterStateNode monsterStateNode3 = StateMachineNodeToMonsterStateNode(font, stateMachine, overwriteStateMachine, node2, existingNodes, monsterStateNode2);
				if (monsterStateNode3 != null)
				{
					monsterStateNode3.Label = label;
					monsterStateNode3.Width = Math.Max(monsterStateNode3.Width, font.GetStringSize(label, (HorizontalAlignment)0, -1f, 18, (JustificationFlag)3, (Direction)0, (Orientation)0).X / 80f);
					list.Add(monsterStateNode3);
					monsterStateNode2.MoveStateIds.AddRange(monsterStateNode3.MoveStateIds);
				}
			}
			if (node.AlternativeMoveNames != null)
			{
				monsterStateNode2.MoveStateIds.AddRange(node.AlternativeMoveNames);
			}
			monsterStateNode2.Children = list;
			monsterStateNode2.HorizontalLayout = node.HorizontalLayout;
			monsterStateNode2.CalculateNodeSize();
			if (node.FollowUpState != null)
			{
				monsterStateNode2.NextState = StateMachineNodeToMonsterStateNode(font, stateMachine, overwriteStateMachine, overwriteStateMachine.FirstOrDefault((StateMachineNode n) => n.Name == node.FollowUpState), existingNodes, null);
			}
			monsterStateNode2.NextStateCount = ((monsterStateNode2.NextState != null) ? 1 : 0) + list.Select((MonsterStateNode c) => c.NextStateCount).DefaultIfEmpty(0).Max();
			return monsterStateNode2;
		}

		[return: NotNullIfNotNull("state")]
		private MonsterStateNode? MonsterStateToMonsterStateNode(string monsterName, Font font, MonsterMoveStateMachine stateMachine, MonsterState? state, Dictionary<MonsterState, MonsterStateNode> existingNodes, MonsterStateNode? parent, ref string? warning)
		{
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0192: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c9: Unknown result type (might be due to invalid IL or missing references)
			if (state == null)
			{
				return null;
			}
			if (parent == null && existingNodes.TryGetValue(state, out MonsterStateNode value))
			{
				return value;
			}
			MoveState val = (MoveState)(object)((state is MoveState) ? state : null);
			if (val != null)
			{
				MonsterStateNode monsterStateNode = new MonsterStateNode
				{
					Id = state.Id,
					State = state,
					Width = (float)val.Intents.Count + (float)(val.Intents.Count - 1) * -0.33f,
					Height = 1f,
					NextStateCount = 1,
					Parent = parent
				};
				monsterStateNode.MoveStateIds.Add(((MonsterState)val).Id);
				if (parent == null)
				{
					existingNodes[state] = monsterStateNode;
				}
				monsterStateNode.NextState = MonsterStateToMonsterStateNode(monsterName, font, stateMachine, val.FollowUpState, existingNodes, null, ref warning);
				return monsterStateNode;
			}
			bool unrecognizedStateType = false;
			List<(string, string, bool)> list = new List<(string, string, bool)>();
			RandomBranchState val2 = (RandomBranchState)(object)((state is RandomBranchState) ? state : null);
			if (val2 != null)
			{
				float sumWeight = val2.States.Sum((StateWeight val6) => ((StateWeight)(ref val6)).GetWeight());
				foreach (StateWeight state2 in val2.States)
				{
					string item;
					bool item2;
					if (localizer.TryGet($"branch.{monsterName}.{state.Id}.{state2.stateId}", out string value2))
					{
						item = value2;
						item2 = true;
					}
					else
					{
						item = MakeText(state2, sumWeight);
						item2 = false;
					}
					list.Add((state2.stateId, item, item2));
				}
			}
			else
			{
				ConditionalBranchState val3 = (ConditionalBranchState)(object)((state is ConditionalBranchState) ? state : null);
				if (val3 != null)
				{
					if (state.Id == "INIT_MOVE")
					{
						string evaluatedSstateName = val3.EvaluateStates();
						MonsterState val4 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState val6) => val6.Id == evaluatedSstateName));
						if (val4 != null)
						{
							return MonsterStateToMonsterStateNode(monsterName, font, stateMachine, val4, existingNodes, parent, ref warning);
						}
					}
					foreach (string s in val3.GetStates())
					{
						if (list.Any<(string, string, bool)>(((string state, string label, bool overwriteLabel) c) => c.state == s))
						{
							continue;
						}
						if (localizer.TryGet($"branch.{monsterName}.{state.Id}.{s}", out string value3))
						{
							list.Add((s, value3, true));
							continue;
						}
						if (warning == null)
						{
							warning = localizer.GetOrElse("ui.UnknownConditions", "Unknown conditions");
						}
						list.Add((s, localizer.GetOrElse("ui.UnknownCondition", "condition?"), true));
					}
				}
				else
				{
					unrecognizedStateType = true;
				}
			}
			int num = list.Count;
			for (int num2 = 0; num2 < num; num2++)
			{
				(string, string, bool) tuple = list[num2];
				if (tuple.Item2 == "{otherwise}")
				{
					list.RemoveAt(num2);
					list.Add((tuple.Item1, localizer.GetOrElse("ui.Otherwise", "Otherwise"), tuple.Item3));
					num2--;
					num--;
				}
			}
			MonsterStateNode monsterStateNode2 = new MonsterStateNode
			{
				Id = state.Id,
				State = state,
				Parent = parent,
				UnrecognizedStateType = unrecognizedStateType
			};
			if (parent == null)
			{
				existingNodes[state] = monsterStateNode2;
			}
			List<MonsterStateNode> list2 = new List<MonsterStateNode>();
			for (int num3 = 0; num3 < list.Count; num3++)
			{
				(string, string, bool) tuple2 = list[num3];
				string childStateId = tuple2.Item1;
				string item3 = tuple2.Item2;
				bool item4 = tuple2.Item3;
				MonsterState val5 = ((IEnumerable<MonsterState>)stateMachine.States.Values).FirstOrDefault((Func<MonsterState, bool>)((MonsterState val6) => val6.Id == childStateId));
				if (val5 != null)
				{
					MonsterStateNode monsterStateNode3 = MonsterStateToMonsterStateNode(monsterName, font, stateMachine, val5, existingNodes, monsterStateNode2, ref warning);
					if (monsterStateNode3 != null)
					{
						monsterStateNode3.Label = item3;
						monsterStateNode3.IsLabelGenerated = !item4;
						monsterStateNode3.Width = Math.Max(monsterStateNode3.Width, font.GetStringSize(item3, (HorizontalAlignment)0, -1f, 18, (JustificationFlag)3, (Direction)0, (Orientation)0).X / 80f);
						list2.Add(monsterStateNode3);
						monsterStateNode2.MoveStateIds.AddRange(monsterStateNode3.MoveStateIds);
					}
				}
			}
			List<MonsterStateNode> list3 = list2.Select((MonsterStateNode c) => c.NextState).Distinct().ToList();
			if (list3.Count == 1)
			{
				foreach (MonsterStateNode item5 in list2)
				{
					item5.NextState = null;
					item5.NextStateCount = 0;
				}
			}
			monsterStateNode2.Children = list2;
			monsterStateNode2.CalculateNodeSize();
			monsterStateNode2.NextState = ((list3.Count == 1) ? list3[0] : null);
			monsterStateNode2.NextStateCount = ((monsterStateNode2.NextState != null) ? 1 : 0) + list2.Select((MonsterStateNode c) => c.NextStateCount).DefaultIfEmpty(0).Max();
			return monsterStateNode2;
		}
	}
	internal class MonsterStateNodeSimplifier
	{
		public static void SimplifyStateNodes(MonsterStateNode stateNode, HashSet<MonsterStateNode> allNodes, MonsterStateNodeConverter converter)
		{
			RemoveUnreachableNoRepeat(allNodes, converter);
			List<MonsterStateNode> rootNodes = new List<MonsterStateNode>(allNodes.Where((MonsterStateNode n) => n.Parent == null));
			MergeSameNodes(allNodes, rootNodes);
			ChangeToHorizontalLayout(allNodes, rootNodes);
		}

		public static void FindAndSetSimpleLoops(List<MonsterStateNode> initNodes)
		{
			Dictionary<MonsterStateNode, int> precessorCount = new Dictionary<MonsterStateNode, int>();
			foreach (MonsterStateNode initNode in initNodes)
			{
				precessorCount[initNode] = 1;
			}
			HashSet<MonsterStateNode> allNodes = initNodes.GetAllNodes();
			foreach (MonsterStateNode item in allNodes)
			{
				if (item.NextState != null)
				{
					precessorCount[item.NextState] = precessorCount.GetValueOrDefault(item.NextState) + 1;
				}
			}
			foreach (MonsterStateNode item2 in allNodes.Where((MonsterStateNode n) => n.Parent == null && n.Children == null && !n.ForceNotSimpleLoop && precessorCount.GetValueOrDefault(n) > 1).ToList())
			{
				HashSet<MonsterStateNode> hashSet = new HashSet<MonsterStateNode>();
				MonsterStateNode monsterStateNode = item2;
				while (true)
				{
					if (monsterStateNode != null && !hashSet.Contains(monsterStateNode))
					{
						if (monsterStateNode != item2 && (monsterStateNode.Children != null || precessorCount.GetValueOrDefault(monsterStateNode) > 1))
						{
							break;
						}
						hashSet.Add(monsterStateNode);
						monsterStateNode = monsterStateNode.NextState;
						continue;
					}
					if (monsterStateNode == item2 && hashSet.Count > 1)
					{
						item2.SimpleLoopStart = true;
						item2.SimpleLoopLength = hashSet.Count;
						item2.SimpleLoopPrecessorCount = precessorCount.GetValueOrDefault(item2) - 1;
					}
					break;
				}
			}
		}

		private static void MergeSameNodes(HashSet<MonsterStateNode> allNodes, List<MonsterStateNode> rootNodes)
		{
			List<(MonsterStateNode, MonsterStateNode)> list = new List<(MonsterStateNode, MonsterStateNode)>();
			for (int i = 0; i < rootNodes.Count; i++)
			{
				MonsterStateNode monsterStateNode = rootNodes[i];
				for (int j = i + 1; j < rootNodes.Count; j++)
				{
					MonsterStateNode monsterStateNode2 = rootNodes[j];
					if (AreSameNode(monsterStateNode, monsterStateNode2, list))
					{
						list.Add((monsterStateNode, monsterStateNode2));
						DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(40, 2);
						defaultInterpolatedStringHandler.AppendLiteral("MergeSameNodes detected same node ");
						MonsterState? state = monsterStateNode.State;
						defaultInterpolatedStringHandler.AppendFormatted((state != null) ? state.Id : null);
						defaultInterpolatedStringHandler.AppendLiteral(" and ");
						MonsterState? state2 = monsterStateNode2.State;
						defaultInterpolatedStringHandler.AppendFormatted((state2 != null) ? state2.Id : null);
						defaultInterpolatedStringHandler.AppendLiteral(".");
						IgLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
					}
				}
			}
			Dictionary<MonsterStateNode, MonsterStateNode> dictionary = new Dictionary<MonsterStateNode, MonsterStateNode>();
			foreach (var (monsterStateNode3, monsterStateNode4) in list)
			{
				if (!dictionary.ContainsKey(monsterStateNode3) && !dictionary.ContainsKey(monsterStateNode4))
				{
					dictionary[monsterStateNode4] = monsterStateNode3;
					monsterStateNode3.AddMoveStateIdsFrom(monsterStateNode4);
				}
			}
			foreach (MonsterStateNode allNode in allNodes)
			{
				if (allNode.NextState != null && dictionary.TryGetValue(allNode.NextState, out var value))
				{
					allNode.NextState = value;
				}
			}
		}

		private static bool AreSameNode(MonsterStateNode a, MonsterStateNode b, List<(MonsterStateNode, MonsterStateNode)> exisitingSameNodes)
		{
			if (exisitingSameNodes.Contains((a, b)) || exisitingSameNodes.Contains((b, a)))
			{
				return true;
			}
			if (a == b)
			{
				return true;
			}
			if (a.Children != null != (b.Children != null))
			{
				return false;
			}
			if (a.NextState != null != (b.NextState != null))
			{
				return false;
			}
			if (a.Label != b.Label)
			{
				return false;
			}
			exisitingSameNodes.Add((a, b));
			try
			{
				if (a.Children != null)
				{
					if (a.Children.Count != b.Children.Count)
					{
						return false;
					}
					for (int i = 0; i < a.Children.Count; i++)
					{
						if (!AreSameNode(a.Children[i], b.Children[i], exisitingSameNodes))
						{
							return false;
						}
					}
				}
				else if (a.State != b.State)
				{
					return false;
				}
				if (a.NextState != null && !AreSameNode(a.NextState, b.NextState, exisitingSameNodes))
				{
					return false;
				}
				return true;
			}
			finally
			{
				exisitingSameNodes.Remove((a, b));
			}
		}

		private static void RemoveUnreachableNoRepeat(HashSet<MonsterStateNode> allNodes, MonsterStateNodeConverter converter)
		{
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			bool flag;
			do
			{
				flag = false;
				Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> precessorDict = GetPrecessorDict(allNodes);
				List<MonsterStateNode> list = new List<MonsterStateNode>();
				foreach (MonsterStateNode allNode in allNodes)
				{
					MonsterStateNode parent = allNode.Parent;
					if (parent == null)
					{
						continue;
					}
					MonsterState state = allNode.State;
					MoveState moveState = (MoveState)(object)((state is MoveState) ? state : null);
					if (moveState == null || allNode.Children != null)
					{
						continue;
					}
					MonsterState? state2 = parent.State;
					RandomBranchState val = (RandomBranchState)(object)((state2 is RandomBranchState) ? state2 : null);
					if (val == null || parent.Children == null || parent.Children.Any((MonsterStateNode c) => !c.IsLabelGenerated))
					{
						continue;
					}
					StateWeight val2 = ((IEnumerable<StateWeight>)val.States).FirstOrDefault((Func<StateWeight, bool>)((StateWeight s) => s.stateId == ((MonsterState)moveState).Id));
					if (val2.stateId != null)
					{
						if (TryRemoveNoRepeatNodePrecededBySame(allNode, precessorDict, list, converter, moveState, val, val2))
						{
							flag = true;
						}
						else if (TryRemoveNoRepeatTextIfPrecededByOther(allNode, precessorDict, converter, val2))
						{
							flag = true;
						}
					}
				}
				foreach (MonsterStateNode item in list)
				{
					allNodes.Remove(item);
				}
			}
			while (flag);
		}

		public static Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> GetPrecessorDict(HashSet<MonsterStateNode> allNodes)
		{
			Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> dictionary = new Dictionary<MonsterStateNode, HashSet<MonsterStateNode>>();
			foreach (MonsterStateNode allNode in allNodes)
			{
				if (allNode.NextState != null)
				{
					if (!dictionary.ContainsKey(allNode.NextState))
					{
						dictionary[allNode.NextState] = new HashSet<MonsterStateNode>();
					}
					dictionary[allNode.NextState].Add(allNode);
				}
			}
			foreach (MonsterStateNode allNode2 in allNodes)
			{
				MonsterStateNode monsterStateNode = allNode2;
				while (monsterStateNode.Parent != null)
				{
					monsterStateNode = monsterStateNode.Parent;
				}
				if (monsterStateNode != allNode2 && dictionary.TryGetValue(monsterStateNode, out var value))
				{
					dictionary[allNode2] = value;
				}
			}
			return dictionary;
		}

		private static bool TryRemoveNoRepeatNodePrecededBySame(MonsterStateNode node, Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> allPrecessors, List<MonsterStateNode> nodesToRemove, MonsterStateNodeConverter converter, MoveState moveState, RandomBranchState parentBranchState, StateWeight stateWeight)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Invalid comparison between Unknown and I4
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Invalid comparison between Unknown and I4
			//IL_0314: Unknown result type (might be due to invalid IL or missing references)
			//IL_0319: Unknown result type (might be due to invalid IL or missing references)
			//IL_031b: Unknown result type (might be due to invalid IL or missing references)
			//IL_032c: Unknown result type (might be due to invalid IL or missing references)
			MonsterStateNode parent = node.Parent;
			if ((int)stateWeight.repeatType != 2 && (int)stateWeight.repeatType != 3)
			{
				return false;
			}
			if (!allPrecessors.ContainsKey(node))
			{
				return false;
			}
			HashSet<MonsterStateNode> source = allPrecessors[node];
			if (source.Any((MonsterStateNode n) => n.Children != null || !(n.State is MoveState)))
			{
				return false;
			}
			List<string> list = source.Select((MonsterStateNode n) => n.State.Id).Distinct().ToList();
			if (list.Count != 1 || list[0] != ((MonsterState)moveState).Id)
			{
				return false;
			}
			nodesToRemove.Add(node);
			parent.Children.Remove(node);
			foreach (KeyValuePair<MonsterStateNode, HashSet<MonsterStateNode>> allPrecessor in allPrecessors)
			{
				allPrecessor.Deconstruct(out var _, out var value);
				value.Remove(node);
			}
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(54, 2);
			defaultInterpolatedStringHandler.AppendLiteral("TryRemoveNoRepeatNodePrecededBySame removed node ");
			MonsterState? state = node.State;
			defaultInterpolatedStringHandler.AppendFormatted((state != null) ? state.Id : null);
			defaultInterpolatedStringHandler.AppendLiteral(" in ");
			MonsterState? state2 = parent.State;
			defaultInterpolatedStringHandler.AppendFormatted((state2 != null) ? state2.Id : null);
			defaultInterpolatedStringHandler.AppendLiteral(".");
			IgLogger.Info(defaultInterpolatedStringHandler.ToStringAndClear());
			if (parent.Children.Count == 1)
			{
				MonsterStateNode monsterStateNode = parent.Children[0];
				foreach (MonsterStateNode item in allPrecessors[parent])
				{
					if (item.NextState == parent)
					{
						item.NextState = monsterStateNode;
					}
				}
				nodesToRemove.Add(parent);
				monsterStateNode.Parent = parent.Parent;
				monsterStateNode.Label = parent.Label;
				monsterStateNode.IsLabelGenerated = parent.IsLabelGenerated;
				if (monsterStateNode.NextState == null)
				{
					monsterStateNode.NextState = parent.NextState;
					monsterStateNode.NextStateCount = ((monsterStateNode.NextState != null) ? 1 : 0);
					if (monsterStateNode.NextState != null)
					{
						HashSet<MonsterStateNode> hashSet = allPrecessors[monsterStateNode.NextState];
						hashSet.Add(monsterStateNode);
						hashSet.Remove(parent);
					}
				}
			}
			else
			{
				float sumWeight = parentBranchState.States.Where((StateWeight s) => parent.Children.Any(delegate(MonsterStateNode c)
				{
					MonsterState? state3 = c.State;
					return ((state3 != null) ? state3.Id : null) == s.stateId;
				})).Sum((StateWeight w) => ((StateWeight)(ref w)).GetWeight());
				foreach (MonsterStateNode child in parent.Children)
				{
					StateWeight val = ((IEnumerable<StateWeight>)parentBranchState.States).FirstOrDefault((Func<StateWeight, bool>)delegate(StateWeight s)
					{
						//IL_0000: Unknown result type (might be due to invalid IL or missing references)
						string stateId = s.stateId;
						MonsterState? state3 = child.State;
						return stateId == ((state3 != null) ? state3.Id : null);
					});
					if (val.stateId != null)
					{
						child.Label = converter.MakeText(val, sumWeight);
						child.IsLabelGenerated = true;
					}
				}
				List<MonsterStateNode> list2 = parent.Children.Select((MonsterStateNode c) => c.NextState).Distinct().ToList();
				if (list2.Count == 1)
				{
					foreach (MonsterStateNode child2 in parent.Children)
					{
						child2.NextState = null;
						child2.NextStateCount = 0;
					}
					parent.NextState = list2[0];
				}
				parent.NextStateCount = ((parent.NextState != null) ? 1 : 0) + parent.Children.Select((MonsterStateNode c) => c.NextStateCount).DefaultIfEmpty(0).Max();
				parent.CalculateNodeSize();
			}
			return true;
		}

		private static bool TryRemoveNoRepeatTextIfPrecededByOther(MonsterStateNode node, Dictionary<MonsterStateNode, HashSet<MonsterStateNode>> allPrecessors, MonsterStateNodeConverter converter, StateWeight stateWeight)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Invalid comparison between Unknown and I4
			_ = node.Parent;
			if ((int)stateWeight.repeatType != 2)
			{
				return false;
			}
			if (allPrecessors.TryGetValue(node, out HashSet<MonsterStateNode> value) && value.Any(delegate(MonsterStateNode n)
			{
				if (n.Children == null && n.State is MoveState)
				{
					string id = n.State.Id;
					MonsterState? state2 = node.State;
					return id == ((state2 != null) ? state2.Id : null);
				}
				return true;
			}))
			{
				return false;
			}
			string? label = node.Label;
			if (label == null || !label.Contains("≤1"))
			{
				return false;
			}
			MonsterState? state = node.State;
			IgLogger.Info("TryRemoveNoRepeatTextIfPrecededByOther removed repeat restriction on " + ((state != null) ? state.Id : null) + ".");
			node.Label = node.Label?.Replace(", ≤1", string.Empty);
			return true;
		}

		private static void ChangeToHorizontalLayout(HashSet<MonsterStateNode> allNodes, List<MonsterStateNode> rootNodes)
		{
			foreach (MonsterStateNode allNode in allNodes)
			{
				List<MonsterStateNode> children = allNode.Children;
				if (children == null || children.Count <= 2)
				{
					continue;
				}
				MonsterStateNode? parent = allNode.Parent;
				if ((parent == null || !parent.HorizontalLayout) && !children.Any((MonsterStateNode c) => c.HorizontalLayout || c.NextState != null || c.Width > 1.5f) && ((allNode.Parent != null && !(allNode.Parent.Width < children.Sum((MonsterStateNode c) => c.Width))) || rootNodes.Count <= 4))
				{
					allNode.HorizontalLayout = true;
					allNode.CalculateNodeSize();
					for (MonsterStateNode parent2 = allNode.Parent; parent2 != null; parent2 = parent2.Parent)
					{
						parent2.CalculateNodeSize();
					}
				}
			}
		}
	}
}
namespace IntentGraph2.Scenes
{
	[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraph.cs")]
	public class NIntentGraph : Control
	{
		public class MethodName : MethodName
		{
			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName canvas = StringName.op_Implicit("canvas");
		}

		public class SignalName : SignalName
		{
		}

		public const float GridSize = 80f;

		public const int LabelFontSize = 18;

		public const float LabelLinePadding = 2f;

		private Graph? graph;

		private MonsterModel? monster;

		private NIntentGraphCanvas? canvas;

		public Graph? Graph
		{
			get
			{
				return graph;
			}
			set
			{
				//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
				//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
				graph = value;
				((Control)this).CustomMinimumSize = new Vector2((80f * graph?.Width) ?? 80f, (80f * graph?.Height) ?? 80f) * new Vector2(IntentGraphMod.Config.IntentGraphScale, IntentGraphMod.Config.IntentGraphScale);
				if (canvas != null)
				{
					canvas.Graph = value;
				}
			}
		}

		public MonsterModel? Monster
		{
			get
			{
				return monster;
			}
			set
			{
				monster = value;
				if (canvas != null)
				{
					canvas.Monster = value;
				}
			}
		}

		public override void _Ready()
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			canvas = ((Node)this).GetNode<NIntentGraphCanvas>(NodePath.op_Implicit("%IntentGraphCanvas"));
			((Control)canvas).Scale = new Vector2(IntentGraphMod.Config.IntentGraphScale, IntentGraphMod.Config.IntentGraphScale);
			canvas.Graph = graph;
			canvas.Monster = monster;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(1)
			{
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._Ready();
				ret = default(godot_variant);
				return true;
			}
			return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName._Ready)
			{
				return true;
			}
			return ((Control)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((ref name) == PropertyName.canvas)
			{
				canvas = VariantUtils.ConvertTo<NIntentGraphCanvas>(ref value);
				return true;
			}
			return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.canvas)
			{
				value = VariantUtils.CreateFrom<NIntentGraphCanvas>(ref canvas);
				return true;
			}
			return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, PropertyName.canvas, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName.canvas, Variant.From<NIntentGraphCanvas>(ref canvas));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			((GodotObject)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName.canvas, ref val))
			{
				canvas = ((Variant)(ref val)).As<NIntentGraphCanvas>();
			}
		}
	}
	[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphCanvas.cs")]
	public class NIntentGraphCanvas : Control
	{
		public class MethodName : MethodName
		{
			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

			public static readonly StringName _Process = StringName.op_Implicit("_Process");

			public static readonly StringName _Draw = StringName.op_Implicit("_Draw");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName ShowCurrentMove = StringName.op_Implicit("ShowCurrentMove");

			public static readonly StringName arrowTexture = StringName.op_Implicit("arrowTexture");

			public static readonly StringName groupBorderTexture = StringName.op_Implicit("groupBorderTexture");

			public static readonly StringName glowTexture = StringName.op_Implicit("glowTexture");

			public static readonly StringName font = StringName.op_Implicit("font");

			public static readonly StringName labelFont = StringName.op_Implicit("labelFont");

			public static readonly StringName glowColor = StringName.op_Implicit("glowColor");

			public static readonly StringName hasAnimatedIcons = StringName.op_Implicit("hasAnimatedIcons");

			public static readonly StringName previousStateLogLength = StringName.op_Implicit("previousStateLogLength");
		}

		public class SignalName : SignalName
		{
		}

		private const int ArrowWidth = 10;

		private const int ArrowEndLength = 15;

		private const int AnimatedIconFrameDurationMs = 80;

		private readonly Move InitMove = new Move("intentgraph2_special_init_move_", null, 0f, 0f, null, Array.Empty<int?>());

		private static readonly Dictionary<IntentType, string> IntentImageResourcePath = new Dictionary<IntentType, string>
		{
			{
				(IntentType)0,
				"res://images/packed/intents/attack/intent_attack_1.png"
			},
			{
				(IntentType)1,
				"res://images/packed/intents/intent_buff.png"
			},
			{
				(IntentType)2,
				"res://images/packed/intents/debuff/intent_megadebuff_01.png"
			},
			{
				(IntentType)3,
				"res://images/packed/intents/debuff/intent_megadebuff_01.png"
			},
			{
				(IntentType)4,
				"res://images/packed/intents/intent_defend.png"
			},
			{
				(IntentType)5,
				"res://images/packed/intents/intent_escape.png"
			},
			{
				(IntentType)6,
				"res://images/packed/intents/intent_heal.png"
			},
			{
				(IntentType)7,
				"res://images/packed/intents/intent_hidden.png"
			},
			{
				(IntentType)8,
				"res://images/packed/intents/intent_summon.png"
			},
			{
				(IntentType)9,
				"res://images/packed/intents/intent_sleep.png"
			},
			{
				(IntentType)10,
				"res://images/packed/intents/intent_stun.png"
			},
			{
				(IntentType)11,
				"res://images/packed/intents/intent_status_card.png"
			},
			{
				(IntentType)12,
				"res://images/packed/intents/intent_card_debuff.png"
			},
			{
				(IntentType)13,
				"res://images/packed/intents/intent_death_blow.png"
			},
			{
				(IntentType)14,
				"res://images/packed/intents/intent_unknown.png"
			}
		};

		private static readonly Dictionary<IntentType, string> IntentImageAnimationResourcePath = new Dictionary<IntentType, string>
		{
			{
				(IntentType)1,
				"res://images/packed/intents/buff/intent_buff_{0:00}.png"
			},
			{
				(IntentType)2,
				"res://images/packed/intents/debuff/intent_megadebuff_{0:00}.png"
			},
			{
				(IntentType)3,
				"res://images/packed/intents/debuff/intent_megadebuff_{0:00}.png"
			},
			{
				(IntentType)4,
				"res://images/packed/intents/defend/intent_defend_{0:00}.png"
			},
			{
				(IntentType)5,
				"res://images/packed/intents/escape/intent_escape_{0:00}.png"
			},
			{
				(IntentType)6,
				"res://images/packed/intents/heal/intent_heal_{0:00}.png"
			},
			{
				(IntentType)8,
				"res://images/packed/intents/summon/intent_summon_{0:00}.png"
			},
			{
				(IntentType)9,
				"res://images/packed/intents/sleep/intent_sleep_{0:00}.png"
			},
			{
				(IntentType)10,
				"res://images/packed/intents/stun/intent_stunned_{0:00}.png"
			},
			{
				(IntentType)11,
				"res://images/packed/intents/status/intent_statuscard_{0:00}.png"
			},
			{
				(IntentType)12,
				"res://images/packed/intents/card_debuff/intent_carddebuff_{0:00}.png"
			},
			{
				(IntentType)14,
				"res://images/packed/intents/unknown/intent_unknown_{0:00}.png"
			}
		};

		private static readonly Dictionary<IntentType, int> IntentImageAnimationFrameCounts = new Dictionary<IntentType, int>
		{
			{
				(IntentType)1,
				30
			},
			{
				(IntentType)2,
				11
			},
			{
				(IntentType)3,
				11
			},
			{
				(IntentType)4,
				45
			},
			{
				(IntentType)5,
				40
			},
			{
				(IntentType)6,
				45
			},
			{
				(IntentType)8,
				25
			},
			{
				(IntentType)9,
				16
			},
			{
				(IntentType)10,
				16
			},
			{
				(IntentType)11,
				19
			},
			{
				(IntentType)12,
				15
			},
			{
				(IntentType)14,
				30
			}
		};

		private static readonly Rect2 IconGroupLT = new Rect2(0f, 0f, 3f, 3f);

		private static readonly Rect2 IconGroupTop = new Rect2(3f, 0f, 26f, 3f);

		private static readonly Rect2 IconGroupTR = new Rect2(29f, 0f, 3f, 3f);

		private static readonly Rect2 IconGroupLeft = new Rect2(0f, 3f, 3f, 26f);

		private static readonly Rect2 IconGroupRight = new Rect2(29f, 3f, 3f, 26f);

		private static readonly Rect2 IconGroupBL = new Rect2(0f, 29f, 3f, 3f);

		private static readonly Rect2 IconGroupBottom = new Rect2(3f, 29f, 26f, 3f);

		private static readonly Rect2 IconGroupBR = new Rect2(29f, 29f, 3f, 3f);

		private static readonly Rect2 ArrowHorizontal = new Rect2(1f, 0f, 62f, 10f);

		private static readonly Rect2 ArrowVertical = new Rect2(65f, 1f, 10f, 62f);

		private static readonly Rect2 ArrowDR = new Rect2(0f, 11f, 10f, 10f);

		private static readonly Rect2 ArrowDL = new Rect2(11f, 11f, 10f, 10f);

		private static readonly Rect2 ArrowUR = new Rect2(0f, 22f, 10f, 10f);

		private static readonly Rect2 ArrowUL = new Rect2(11f, 22f, 10f, 10f);

		private static readonly Rect2 ArrowU = new Rect2(91f, 0f, 20f, 15f);

		private static readonly Rect2 ArrowD = new Rect2(91f, 35f, 20f, 15f);

		private static readonly Rect2 ArrowR = new Rect2(111f, 15f, 15f, 20f);

		private static readonly Rect2 ArrowL = new Rect2(76f, 15f, 15f, 20f);

		private Texture2D? arrowTexture;

		private Texture2D? groupBorderTexture;

		private Texture2D? glowTexture;

		private Dictionary<string, Texture2D> intentTextures = new Dictionary<string, Texture2D>();

		private Font? font;

		private Font? labelFont;

		private Color glowColor;

		private Graph? graph;

		private bool hasAnimatedIcons;

		private int previousStateLogLength;

		private List<Move> glowingMoves = new List<Move>();

		public Graph? Graph
		{
			get
			{
				return graph;
			}
			set
			{
				//IL_010f: Unknown result type (might be due to invalid IL or missing references)
				graph = value;
				Graph? obj = graph;
				hasAnimatedIcons = obj != null && obj.Moves?.Any((Move m) => m.Icons?.Any((Icon icon) => IntentImageAnimationFrameCounts.ContainsKey(icon.IntentType)) ?? false) == true;
				((Control)this).CustomMinimumSize = new Vector2((80f * graph?.Width) ?? 80f, (80f * graph?.Height) ?? 80f);
				((CanvasItem)this).QueueRedraw();
			}
		}

		public MonsterModel? Monster { get; set; }

		private bool ShowCurrentMove => IntentGraphMod.Config.ShowCurrentMove;

		public override void _Ready()
		{
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Expected O, but got Unknown
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Expected O, but got Unknown
			labelFont = (font = ResourceLoader.Load<Font>("res://themes/kreon_bold_glyph_space_one.tres", (string)null, (CacheMode)1));
			if (LocManager.Instance.Language == "zhs")
			{
				labelFont = ResourceLoader.Load<Font>("res://intentgraph2/themes/kreon_bold_glyph_space_one_zhs.tres", (string)null, (CacheMode)1);
			}
			else if (!Engine.IsEditorHint() && !TestMode.IsOn && FontManager.NeedsFontSubstitution(LocManager.Instance.Language))
			{
				FontVariation val = new FontVariation();
				Font substituteFont = FontManager.GetSubstituteFont(LocManager.Instance.Language, (FontType)1);
				if (substituteFont != null)
				{
					val.BaseFont = substituteFont;
					((Font)val).Fallbacks.Add(ResourceLoader.Load<Font>("res://intentgraph2/images/ui/icon.png", (string)null, (CacheMode)1));
					labelFont = (Font?)(object)val;
				}
			}
			else
			{
				FontVariation val2 = new FontVariation();
				val2.BaseFont = labelFont;
				((Font)val2).Fallbacks.Add(ResourceLoader.Load<Font>("res://intentgraph2/images/ui/icon.png", (string)null, (CacheMode)1));
				labelFont = (Font?)(object)val2;
			}
		}

		public override void _Process(double delta)
		{
			if ((hasAnimatedIcons || ShowCurrentMove) && graph != null && ((CanvasItem)this).Visible && IntentGraphMod.Config.UseAnimatedIntentIcon)
			{
				((CanvasItem)this).QueueRedraw();
			}
		}

		public override void _Draw()
		{
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			if (graph == null)
			{
				return;
			}
			if (ShowCurrentMove)
			{
				float num = (IntentGraphMod.Config.UseAnimatedIntentIcon ? (0.3f + 0.4f * Mathf.Sin((float)Time.GetTicksMsec() / 1000f * (float)Math.PI)) : 0.5f);
				glowColor = new Color(1f, 1f, 1f, num);
			}
			if (ShowCurrentMove)
			{
				DrawGlow(graph.Moves);
			}
			IEnumerable<Icon> icons = graph.Icons;
			foreach (Icon item in icons ?? Enumerable.Empty<Icon>())
			{
				DrawIcon(item);
			}
			IEnumerable<Move> moves = graph.Moves;
			foreach (Move item2 in moves ?? Enumerable.Empty<Move>())
			{
				DrawMove(item2);
			}
			IEnumerable<IconGroup> iconGroups = graph.IconGroups;
			foreach (IconGroup item3 in iconGroups ?? Enumerable.Empty<IconGroup>())
			{
				DrawIconGroup(item3);
			}
			IEnumerable<Arrow> arrows = graph.Arrows;
			foreach (Arrow item4 in arrows ?? Enumerable.Empty<Arrow>())
			{
				DrawArrow(item4);
			}
			IEnumerable<Label> labels = graph.Labels;
			foreach (Label item5 in labels ?? Enumerable.Empty<Label>())
			{
				DrawLabel(item5);
			}
		}

		private void DrawGlow(List<Move> moves)
		{
			if (moves == null)
			{
				return;
			}
			MonsterModel? monster = Monster;
			if (((monster != null) ? monster.MoveStateMachine : null) == null)
			{
				return;
			}
			MonsterMoveStateMachine moveStateMachine = Monster.MoveStateMachine;
			if (!StateLogPatches.FullStateLog.TryGetValue(moveStateMachine, out List<MonsterState> value))
			{
				return;
			}
			if (previousStateLogLength == value.Count)
			{
				foreach (Move glowingMove in glowingMoves)
				{
					DrawMoveGlow(glowingMove);
				}
				return;
			}
			previousStateLogLength = value.Count;
			glowingMoves.Clear();
			int num = 1;
			List<(Move, Move)> source = moves.Select((Move m) => (curr: m, final: m)).ToList();
			for (; value.Count >= num; num++)
			{
				List<MonsterState> list = value;
				int num2 = num;
				MonsterState state = list[list.Count - num2];
				List<(Move, Move)> list2 = source.Where<(Move, Move)>(((Move curr, Move final) m) => m.curr == null || (m.curr.Ids?.Contains(state.Id) ?? false)).ToList();
				if (list2.Count == 1)
				{
					glowingMoves.Add(list2[0].Item2);
					DrawMoveGlow(list2[0].Item2);
					return;
				}
				if (list2.Count == 0)
				{
					break;
				}
				source = list2.SelectMany<(Move, Move), (Move, Move)>(((Move curr, Move final) m) => (!(m.curr == null)) ? ((m.curr.PossiblePreviousMoveNodeIndices != null) ? m.curr.PossiblePreviousMoveNodeIndices.Select((int? i) => i.HasValue ? (curr: moves[i.Value], final: m.final) : (curr: InitMove, final: m.final)) : new <>z__ReadOnlySingleElementList<(Move, Move)>((null, m.final))) : new <>z__ReadOnlySingleElementList<(Move, Move)>(m)).ToList();
			}
			foreach (Move item in (from m in source
				where m.curr == null || m.curr == InitMove
				select m.final).Distinct())
			{
				glowingMoves.Add(item);
				DrawMoveGlow(item);
			}
		}

		private void DrawMoveGlow(Move move)
		{
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			if (glowTexture == null)
			{
				glowTexture = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/glow.png", (string)null, (CacheMode)1);
			}
			IEnumerable<Icon> icons = move.Icons;
			foreach (Icon item in icons ?? Enumerable.Empty<Icon>())
			{
				float num = 20f;
				((CanvasItem)this).DrawTextureRect(glowTexture, new Rect2(item.X * 80f - num, item.Y * 80f - num, 80f + 2f * num, 80f + 2f * num), false, (Color?)glowColor, false);
			}
		}

		private void DrawMove(Move move)
		{
			IEnumerable<Icon> icons = move.Icons;
			foreach (Icon item in icons ?? Enumerable.Empty<Icon>())
			{
				DrawIcon(item);
			}
		}

		private void DrawIcon(Icon icon)
		{
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			DrawIconIntent(icon);
			string text = string.Empty;
			string text2 = ((!string.IsNullOrEmpty(icon.ValueText)) ? icon.ValueText.Replace("{}", icon.Value?.ToString()) : (icon.Value?.ToString() ?? string.Empty));
			if (!string.IsNullOrEmpty(text2))
			{
				if (icon.Times <= 1 && string.IsNullOrEmpty(icon.TimesText))
				{
					text = text2;
				}
				else
				{
					string text3 = ((!string.IsNullOrEmpty(icon.TimesText)) ? icon.TimesText.Replace("{}", icon.Times.ToString()) : icon.Times.ToString());
					text = text2 + "x" + text3;
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				Vector2 val = default(Vector2);
				((Vector2)(ref val))..ctor(icon.X * 80f + 12f, icon.Y * 80f + 71f);
				((CanvasItem)this).DrawStringOutline(font, val, text, (HorizontalAlignment)0, -1f, 22, 16, (Color?)new Color(0f, 0f, 0f, 0.5f), (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
				((CanvasItem)this).DrawString(font, val, text, (HorizontalAlignment)0, -1f, 22, (Color?)null, (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
			}
		}

		private void DrawIconIntent(Icon icon)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			if ((int)icon.IntentType == 0)
			{
				int num = icon.Value.GetValueOrDefault() * icon.Times;
				int num2 = ((num < 20) ? ((num < 5) ? 1 : ((num >= 10) ? 3 : 2)) : ((num >= 40) ? 5 : 4));
				int value = num2;
				string key = ((object)(IntentType)0/*cast due to .constrained prefix*/).ToString() + value;
				if (!intentTextures.TryGetValue(key, out Texture2D value2))
				{
					Texture2D val = (intentTextures[key] = ResourceLoader.Load<Texture2D>($"res://images/packed/intents/attack/intent_attack_{value}.png", (string)null, (CacheMode)1));
					value2 = val;
				}
				((CanvasItem)this).DrawTextureRect(value2, new Rect2(icon.X * 80f + 4f, icon.Y * 80f + 4f, 72f, 72f), false, (Color?)null, false);
			}
			else
			{
				if (!TryGetAnimatedIntentTexture(icon.IntentType, out Texture2D texture) && !intentTextures.TryGetValue(((object)icon.IntentType/*cast due to .constrained prefix*/).ToString(), out texture))
				{
					Texture2D val = (intentTextures[((object)icon.IntentType/*cast due to .constrained prefix*/).ToString()] = ResourceLoader.Load<Texture2D>(IntentImageResourcePath[icon.IntentType], (string)null, (CacheMode)1));
					texture = val;
				}
				((CanvasItem)this).DrawTextureRect(texture, new Rect2(icon.X * 80f + 4f, icon.Y * 80f, 72f, 72f), false, (Color?)null, false);
			}
		}

		private bool TryGetAnimatedIntentTexture(IntentType intentType, out Texture2D? texture)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			texture = null;
			if (!IntentGraphMod.Config.UseAnimatedIntentIcon || !IntentImageAnimationFrameCounts.TryGetValue(intentType, out var value) || value <= 0 || !IntentImageAnimationResourcePath.TryGetValue(intentType, out string value2))
			{
				return false;
			}
			int num = (int)(Time.GetTicksMsec() / 80 % (ulong)value);
			string key = $"{intentType}_{num}";
			if (!intentTextures.TryGetValue(key, out texture))
			{
				Texture2D val = (intentTextures[key] = ResourceLoader.Load<Texture2D>(string.Format(value2, num), (string)null, (CacheMode)1));
				texture = val;
			}
			return texture != null;
		}

		private void DrawArrow(Arrow arrow)
		{
			//IL_033c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0354: Unknown result type (might be due to invalid IL or missing references)
			//IL_0363: Unknown result type (might be due to invalid IL or missing references)
			//IL_036d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0372: Unknown result type (might be due to invalid IL or missing references)
			//IL_039f: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0416: Unknown result type (might be due to invalid IL or missing references)
			//IL_0425: Unknown result type (might be due to invalid IL or missing references)
			//IL_042f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0434: Unknown result type (might be due to invalid IL or missing references)
			//IL_045b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0471: Unknown result type (might be due to invalid IL or missing references)
			//IL_0480: Unknown result type (might be due to invalid IL or missing references)
			//IL_048a: Unknown result type (might be due to invalid IL or missing references)
			//IL_048f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_023d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0242: Unknown result type (might be due to invalid IL or missing references)
			//IL_0285: Unknown result type (might be due to invalid IL or missing references)
			//IL_028a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			if (arrowTexture == null)
			{
				arrowTexture = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/arrow.png", (string)null, (CacheMode)1);
			}
			float[] path = arrow.Path;
			if (path.Length <= 3)
			{
				return;
			}
			bool flag = path[0] == 0f;
			float num = path[1];
			float num2 = path[2];
			int num3 = -1;
			float num6;
			float num7;
			for (int i = 3; i < path.Length; i++)
			{
				bool flag2 = i == 3;
				bool flag3 = i == path.Length - 1;
				float num4 = (flag ? path[i] : num);
				float num5 = (flag ? num2 : path[i]);
				num6 = num2 * 80f - 5f;
				num7 = num * 80f - 5f;
				int num8;
				if (flag)
				{
					bool flag4 = num4 > num;
					num8 = (flag4 ? 1 : 3);
					int num9 = ((!flag2) ? (flag4 ? 1 : (-1)) : 0) * 10 / 2;
					int num10 = (flag3 ? 15 : 5) * ((!flag4) ? 1 : (-1));
					float num11 = num * 80f + (float)num9;
					float num12 = num4 * 80f + (float)num10;
					float num13 = Math.Abs(num11 - num12);
					((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(Math.Min(num11, num12), num6, num13, 10f), ArrowHorizontal, (Color?)null, false, true);
				}
				else
				{
					bool flag5 = num5 > num2;
					num8 = (flag5 ? 2 : 0);
					int num14 = ((!flag2) ? (flag5 ? 1 : (-1)) : 0) * 10 / 2;
					int num15 = (flag3 ? 15 : 5) * ((!flag5) ? 1 : (-1));
					float num16 = num2 * 80f + (float)num14;
					float num17 = num5 * 80f + (float)num15;
					float num18 = Math.Abs(num16 - num17);
					((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, Math.Min(num16, num17), 10f, num18), ArrowVertical, (Color?)null, false, true);
				}
				if (!flag2)
				{
					if ((num3 == 2 && num8 == 1) || (num3 == 3 && num8 == 0))
					{
						((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowUR, (Color?)null, false, true);
					}
					else if ((num3 == 2 && num8 == 3) || (num3 == 1 && num8 == 0))
					{
						((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowUL, (Color?)null, false, true);
					}
					else if ((num3 == 0 && num8 == 3) || (num3 == 1 && num8 == 2))
					{
						((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowDL, (Color?)null, false, true);
					}
					else if ((num3 == 0 && num8 == 1) || (num3 == 3 && num8 == 2))
					{
						((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6, 10f, 10f), ArrowDR, (Color?)null, false, true);
					}
				}
				flag = !flag;
				num = num4;
				num2 = num5;
				num3 = num8;
			}
			num6 = num2 * 80f;
			num7 = num * 80f;
			switch (num3)
			{
			case 0:
				((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7 - ((Rect2)(ref ArrowU)).Size.X / 2f, num6, ((Rect2)(ref ArrowU)).Size.X, ((Rect2)(ref ArrowU)).Size.Y), ArrowU, (Color?)null, false, true);
				break;
			case 1:
				((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7 - 15f, num6 - ((Rect2)(ref ArrowR)).Size.Y / 2f, ((Rect2)(ref ArrowR)).Size.X, ((Rect2)(ref ArrowR)).Size.Y), ArrowR, (Color?)null, false, true);
				break;
			case 2:
				((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7 - ((Rect2)(ref ArrowD)).Size.X / 2f, num6 - 15f, ((Rect2)(ref ArrowD)).Size.X, ((Rect2)(ref ArrowD)).Size.Y), ArrowD, (Color?)null, false, true);
				break;
			case 3:
				((CanvasItem)this).DrawTextureRectRegion(arrowTexture, new Rect2(num7, num6 - ((Rect2)(ref ArrowL)).Size.Y / 2f, ((Rect2)(ref ArrowL)).Size.X, ((Rect2)(ref ArrowL)).Size.Y), ArrowL, (Color?)null, false, true);
				break;
			}
		}

		private void DrawLabel(Label label)
		{
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			string text = label.Text;
			int fontSize = label.FontSize;
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			Vector2 val = default(Vector2);
			((Vector2)(ref val))..ctor(label.X * 80f, label.Y * 80f);
			string[] array = text.Split('\n');
			foreach (string text2 in array)
			{
				Vector2 val2 = val;
				if (label.Align == "right")
				{
					Vector2 stringSize = labelFont.GetStringSize(text, (HorizontalAlignment)0, -1f, fontSize, (JustificationFlag)3, (Direction)0, (Orientation)0);
					val2.X -= stringSize.X;
				}
				else if (label.Align != "left")
				{
					Vector2 stringSize2 = labelFont.GetStringSize(text, (HorizontalAlignment)0, -1f, fontSize, (JustificationFlag)3, (Direction)0, (Orientation)0);
					val2.X -= stringSize2.X / 2f;
				}
				((CanvasItem)this).DrawStringOutline(labelFont, val2, text2, (HorizontalAlignment)0, -1f, fontSize, 12, (Color?)new Color(0f, 0f, 0f, 0.5f), (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
				((CanvasItem)this).DrawString(labelFont, val2, text2, (HorizontalAlignment)0, -1f, fontSize, (Color?)null, (JustificationFlag)3, (Direction)0, (Orientation)0, 0f);
				val.Y += (float)fontSize + 2f;
			}
		}

		private void DrawIconGroup(IconGroup iconGroup)
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Unknown result type (might be due to invalid IL or missing references)
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_014b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0180: Unknown result type (might be due to invalid IL or missing references)
			//IL_0191: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01da: Unknown result type (might be due to invalid IL or missing references)
			//IL_01df: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0201: Unknown result type (might be due to invalid IL or missing references)
			//IL_020f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0214: Unknown result type (might be due to invalid IL or missing references)
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_022a: Unknown result type (might be due to invalid IL or missing references)
			//IL_022f: Unknown result type (might be due to invalid IL or missing references)
			//IL_024c: Unknown result type (might be due to invalid IL or missing references)
			//IL_025c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0261: Unknown result type (might be due to invalid IL or missing references)
			//IL_0270: Unknown result type (might be due to invalid IL or missing references)
			//IL_0275: Unknown result type (might be due to invalid IL or missing references)
			//IL_027a: Unknown result type (might be due to invalid IL or missing references)
			if (groupBorderTexture == null)
			{
				groupBorderTexture = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/groupborder.png", (string)null, (CacheMode)1);
			}
			float num = iconGroup.X * 80f;
			float num2 = iconGroup.Y * 80f;
			float num3 = iconGroup.Width * 80f;
			float num4 = iconGroup.Height * 80f;
			Vector2 val = default(Vector2);
			((Vector2)(ref val))..ctor(num, num2);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val, new Vector2(3f, 3f)), IconGroupLT, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(3f, 0f), new Vector2(num3 - 6f, 3f)), IconGroupTop, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(num3 - 3f, 0f), new Vector2(3f, 3f)), IconGroupTR, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(0f, 3f), new Vector2(3f, num4 - 6f)), IconGroupLeft, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(num3 - 3f, 3f), new Vector2(3f, num4 - 6f)), IconGroupRight, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(0f, num4 - 3f), new Vector2(3f, 3f)), IconGroupBL, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(3f, num4 - 3f), new Vector2(num3 - 6f, 3f)), IconGroupBottom, (Color?)null, false, true);
			((CanvasItem)this).DrawTextureRectRegion(groupBorderTexture, new Rect2(val + new Vector2(num3 - 3f, num4 - 3f), new Vector2(3f, 3f)), IconGroupBR, (Color?)null, false, true);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(3)
			{
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName._Draw, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._Draw && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((CanvasItem)this)._Draw();
				ret = default(godot_variant);
				return true;
			}
			return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName._Ready)
			{
				return true;
			}
			if ((ref method) == MethodName._Process)
			{
				return true;
			}
			if ((ref method) == MethodName._Draw)
			{
				return true;
			}
			return ((Control)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.arrowTexture)
			{
				arrowTexture = VariantUtils.ConvertTo<Texture2D>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.groupBorderTexture)
			{
				groupBorderTexture = VariantUtils.ConvertTo<Texture2D>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.glowTexture)
			{
				glowTexture = VariantUtils.ConvertTo<Texture2D>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.font)
			{
				font = VariantUtils.ConvertTo<Font>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.labelFont)
			{
				labelFont = VariantUtils.ConvertTo<Font>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.glowColor)
			{
				glowColor = VariantUtils.ConvertTo<Color>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.hasAnimatedIcons)
			{
				hasAnimatedIcons = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.previousStateLogLength)
			{
				previousStateLogLength = VariantUtils.ConvertTo<int>(ref value);
				return true;
			}
			return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.ShowCurrentMove)
			{
				bool showCurrentMove = ShowCurrentMove;
				value = VariantUtils.CreateFrom<bool>(ref showCurrentMove);
				return true;
			}
			if ((ref name) == PropertyName.arrowTexture)
			{
				value = VariantUtils.CreateFrom<Texture2D>(ref arrowTexture);
				return true;
			}
			if ((ref name) == PropertyName.groupBorderTexture)
			{
				value = VariantUtils.CreateFrom<Texture2D>(ref groupBorderTexture);
				return true;
			}
			if ((ref name) == PropertyName.glowTexture)
			{
				value = VariantUtils.CreateFrom<Texture2D>(ref glowTexture);
				return true;
			}
			if ((ref name) == PropertyName.font)
			{
				value = VariantUtils.CreateFrom<Font>(ref font);
				return true;
			}
			if ((ref name) == PropertyName.labelFont)
			{
				value = VariantUtils.CreateFrom<Font>(ref labelFont);
				return true;
			}
			if ((ref name) == PropertyName.glowColor)
			{
				value = VariantUtils.CreateFrom<Color>(ref glowColor);
				return true;
			}
			if ((ref name) == PropertyName.hasAnimatedIcons)
			{
				value = VariantUtils.CreateFrom<bool>(ref hasAnimatedIcons);
				return true;
			}
			if ((ref name) == PropertyName.previousStateLogLength)
			{
				value = VariantUtils.CreateFrom<int>(ref previousStateLogLength);
				return true;
			}
			return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, PropertyName.arrowTexture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.groupBorderTexture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.glowTexture, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.font, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.labelFont, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)20, PropertyName.glowColor, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.hasAnimatedIcons, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)2, PropertyName.previousStateLogLength, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.ShowCurrentMove, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName.arrowTexture, Variant.From<Texture2D>(ref arrowTexture));
			info.AddProperty(PropertyName.groupBorderTexture, Variant.From<Texture2D>(ref groupBorderTexture));
			info.AddProperty(PropertyName.glowTexture, Variant.From<Texture2D>(ref glowTexture));
			info.AddProperty(PropertyName.font, Variant.From<Font>(ref font));
			info.AddProperty(PropertyName.labelFont, Variant.From<Font>(ref labelFont));
			info.AddProperty(PropertyName.glowColor, Variant.From<Color>(ref glowColor));
			info.AddProperty(PropertyName.hasAnimatedIcons, Variant.From<bool>(ref hasAnimatedIcons));
			info.AddProperty(PropertyName.previousStateLogLength, Variant.From<int>(ref previousStateLogLength));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName.arrowTexture, ref val))
			{
				arrowTexture = ((Variant)(ref val)).As<Texture2D>();
			}
			Variant val2 = default(Variant);
			if (info.TryGetProperty(PropertyName.groupBorderTexture, ref val2))
			{
				groupBorderTexture = ((Variant)(ref val2)).As<Texture2D>();
			}
			Variant val3 = default(Variant);
			if (info.TryGetProperty(PropertyName.glowTexture, ref val3))
			{
				glowTexture = ((Variant)(ref val3)).As<Texture2D>();
			}
			Variant val4 = default(Variant);
			if (info.TryGetProperty(PropertyName.font, ref val4))
			{
				font = ((Variant)(ref val4)).As<Font>();
			}
			Variant val5 = default(Variant);
			if (info.TryGetProperty(PropertyName.labelFont, ref val5))
			{
				labelFont = ((Variant)(ref val5)).As<Font>();
			}
			Variant val6 = default(Variant);
			if (info.TryGetProperty(PropertyName.glowColor, ref val6))
			{
				glowColor = ((Variant)(ref val6)).As<Color>();
			}
			Variant val7 = default(Variant);
			if (info.TryGetProperty(PropertyName.hasAnimatedIcons, ref val7))
			{
				hasAnimatedIcons = ((Variant)(ref val7)).As<bool>();
			}
			Variant val8 = default(Variant);
			if (info.TryGetProperty(PropertyName.previousStateLogLength, ref val8))
			{
				previousStateLogLength = ((Variant)(ref val8)).As<int>();
			}
		}
	}
	[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphEditor.cs")]
	public class NIntentGraphEditor : Control
	{
		private enum JsonEditorKind
		{
			SecondaryInitialStates,
			StateMachine,
			MoveReplacements,
			GraphPatch
		}

		private enum PendingAction
		{
			None,
			Close,
			Reload
		}

		private readonly record struct CompletionItem(string MatchText, string DisplayText, string InsertText, CodeCompletionKind KindValue)
		{
			[CompilerGenerated]
			public void Deconstruct(out string MatchText, out string DisplayText, out string InsertText, out CodeCompletionKind KindValue)
			{
				//IL_001b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0021: Expected I8, but got Unknown
				MatchText = this.MatchText;
				DisplayText = this.DisplayText;
				InsertText = this.InsertText;
				KindValue = (CodeCompletionKind)(long)this.KindValue;
			}
		}

		public class MethodName : MethodName
		{
			public static readonly StringName LocalizeText = StringName.op_Implicit("LocalizeText");

			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

			public static readonly StringName _UnhandledInput = StringName.op_Implicit("_UnhandledInput");

			public static readonly StringName ApplyViewportLayout = StringName.op_Implicit("ApplyViewportLayout");

			public static readonly StringName LoadDefinitionsFromDiskOrRuntime = StringName.op_Implicit("LoadDefinitionsFromDiskOrRuntime");

			public static readonly StringName ApplyLocalization = StringName.op_Implicit("ApplyLocalization");

			public static readonly StringName SetTabTitle = StringName.op_Implicit("SetTabTitle");

			public static readonly StringName RefreshUiFromState = StringName.op_Implicit("RefreshUiFromState");

			public static readonly StringName RefreshVariantList = StringName.op_Implicit("RefreshVariantList");

			public static readonly StringName RefreshStateIdList = StringName.op_Implicit("RefreshStateIdList");

			public static readonly StringName LoadSelectedDefinitionIntoEditors = StringName.op_Implicit("LoadSelectedDefinitionIntoEditors");

			public static readonly StringName LoadIntentStringsIntoEditor = StringName.op_Implicit("LoadIntentStringsIntoEditor");

			public static readonly StringName UpdateActionState = StringName.op_Implicit("UpdateActionState");

			public static readonly StringName SetEditorsEnabled = StringName.op_Implicit("SetEditorsEnabled");

			public static readonly StringName OnEditorFieldsChanged = StringName.op_Implicit("OnEditorFieldsChanged");

			public static readonly StringName OnIntentStringsChanged = StringName.op_Implicit("OnIntentStringsChanged");

			public static readonly StringName ConfigureCodeEditor = StringName.op_Implicit("ConfigureCodeEditor");

			public static readonly StringName OnCodeEditorTextChanged = StringName.op_Implicit("OnCodeEditorTextChanged");

			public static readonly StringName OnCodeCompletionRequested = StringName.op_Implicit("OnCodeCompletionRequested");

			public static readonly StringName RegisterEditableInput = StringName.op_Implicit("RegisterEditableInput");

			public static readonly StringName OnVariantSelected = StringName.op_Implicit("OnVariantSelected");

			public static readonly StringName OnAddVariantPressed = StringName.op_Implicit("OnAddVariantPressed");

			public static readonly StringName OnStateIdClicked = StringName.op_Implicit("OnStateIdClicked");

			public static readonly StringName OnDuplicateVariantPressed = StringName.op_Implicit("OnDuplicateVariantPressed");

			public static readonly StringName OnDeleteVariantPressed = StringName.op_Implicit("OnDeleteVariantPressed");

			public static readonly StringName OnMoveVariantUpPressed = StringName.op_Implicit("OnMoveVariantUpPressed");

			public static readonly StringName OnMoveVariantDownPressed = StringName.op_Implicit("OnMoveVariantDownPressed");

			public static readonly StringName MoveSelectedVariant = StringName.op_Implicit("MoveSelectedVariant");

			public static readonly StringName OnSavePressed = StringName.op_Implicit("OnSavePressed");

			public static readonly StringName OnReloadPressed = StringName.op_Implicit("OnReloadPressed");

			public static readonly StringName OnClosePressed = StringName.op_Implicit("OnClosePressed");

			public static readonly StringName RequestClose = StringName.op_Implicit("RequestClose");

			public static readonly StringName OnUnsavedChangesConfirmed = StringName.op_Implicit("OnUnsavedChangesConfirmed");

			public static readonly StringName ReloadDefinitions = StringName.op_Implicit("ReloadDefinitions");

			public static readonly StringName CloseEditor = StringName.op_Implicit("CloseEditor");

			public static readonly StringName RefreshPreview = StringName.op_Implicit("RefreshPreview");

			public static readonly StringName UpdateConditionStatus = StringName.op_Implicit("UpdateConditionStatus");

			public static readonly StringName UpdateUpToDateConditionStatus = StringName.op_Implicit("UpdateUpToDateConditionStatus");

			public static readonly StringName UpdateHeaderStatus = StringName.op_Implicit("UpdateHeaderStatus");

			public static readonly StringName UpdateIntentStringsStatus = StringName.op_Implicit("UpdateIntentStringsStatus");

			public static readonly StringName InsertStateIdIntoActiveInput = StringName.op_Implicit("InsertStateIdIntoActiveInput");

			public static readonly StringName GetCompletionPrefix = StringName.op_Implicit("GetCompletionPrefix");

			public static readonly StringName IsCompletionCharacter = StringName.op_Implicit("IsCompletionCharacter");

			public static readonly StringName IsCaretInsideString = StringName.op_Implicit("IsCaretInsideString");

			public static readonly StringName ShowMessage = StringName.op_Implicit("ShowMessage");

			public static readonly StringName NormalizeSecondaryInitialStates = StringName.op_Implicit("NormalizeSecondaryInitialStates");

			public static readonly StringName NormalizeConditionText = StringName.op_Implicit("NormalizeConditionText");

			public static readonly StringName FormatPositionComponent = StringName.op_Implicit("FormatPositionComponent");

			public static readonly StringName NormalizeOptionalRuleText = StringName.op_Implicit("NormalizeOptionalRuleText");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName HasUnsavedChanges = StringName.op_Implicit("HasUnsavedChanges");

			public static readonly StringName monsterNameLabel = StringName.op_Implicit("monsterNameLabel");

			public static readonly StringName monsterModelLabel = StringName.op_Implicit("monsterModelLabel");

			public static readonly StringName headerStatusLabel = StringName.op_Implicit("headerStatusLabel");

			public static readonly StringName variantList = StringName.op_Implicit("variantList");

			public static readonly StringName stateIdList = StringName.op_Implicit("stateIdList");

			public static readonly StringName addVariantButton = StringName.op_Implicit("addVariantButton");

			public static readonly StringName duplicateVariantButton = StringName.op_Implicit("duplicateVariantButton");

			public static readonly StringName deleteVariantButton = StringName.op_Implicit("deleteVariantButton");

			public static readonly StringName moveVariantUpButton = StringName.op_Implicit("moveVariantUpButton");

			public static readonly StringName moveVariantDownButton = StringName.op_Implicit("moveVariantDownButton");

			public static readonly StringName saveButton = StringName.op_Implicit("saveButton");

			public static readonly StringName reloadButton = StringName.op_Implicit("reloadButton");

			public static readonly StringName closeButton = StringName.op_Implicit("closeButton");

			public static readonly StringName conditionEdit = StringName.op_Implicit("conditionEdit");

			public static readonly StringName conditionStatusLabel = StringName.op_Implicit("conditionStatusLabel");

			public static readonly StringName upToDateConditionEdit = StringName.op_Implicit("upToDateConditionEdit");

			public static readonly StringName upToDateConditionStatusLabel = StringName.op_Implicit("upToDateConditionStatusLabel");

			public static readonly StringName offsetXEdit = StringName.op_Implicit("offsetXEdit");

			public static readonly StringName offsetYEdit = StringName.op_Implicit("offsetYEdit");

			public static readonly StringName secondaryInitialStatesEdit = StringName.op_Implicit("secondaryInitialStatesEdit");

			public static readonly StringName stateMachineEdit = StringName.op_Implicit("stateMachineEdit");

			public static readonly StringName moveReplacementsEdit = StringName.op_Implicit("moveReplacementsEdit");

			public static readonly StringName graphPatchEdit = StringName.op_Implicit("graphPatchEdit");

			public static readonly StringName intentStringsStatusLabel = StringName.op_Implicit("intentStringsStatusLabel");

			public static readonly StringName intentStringsEdit = StringName.op_Implicit("intentStringsEdit");

			public static readonly StringName readOnlySummaryEdit = StringName.op_Implicit("readOnlySummaryEdit");

			public static readonly StringName previewStatusLabel = StringName.op_Implicit("previewStatusLabel");

			public static readonly StringName previewGraph = StringName.op_Implicit("previewGraph");

			public static readonly StringName unsavedChangesDialog = StringName.op_Implicit("unsavedChangesDialog");

			public static readonly StringName messageDialog = StringName.op_Implicit("messageDialog");

			public static readonly StringName windowPanel = StringName.op_Implicit("windowPanel");

			public static readonly StringName editorTabs = StringName.op_Implicit("editorTabs");

			public static readonly StringName monsterDisplayName = StringName.op_Implicit("monsterDisplayName");

			public static readonly StringName monsterModelFullName = StringName.op_Implicit("monsterModelFullName");

			public static readonly StringName loadedIntentStringsLanguage = StringName.op_Implicit("loadedIntentStringsLanguage");

			public static readonly StringName selectedVariantIndex = StringName.op_Implicit("selectedVariantIndex");

			public static readonly StringName isReady = StringName.op_Implicit("isReady");

			public static readonly StringName isRefreshingUi = StringName.op_Implicit("isRefreshingUi");

			public static readonly StringName hasUnsavedChanges = StringName.op_Implicit("hasUnsavedChanges");

			public static readonly StringName pendingAction = StringName.op_Implicit("pendingAction");

			public static readonly StringName lastActiveTextInput = StringName.op_Implicit("lastActiveTextInput");
		}

		public class SignalName : SignalName
		{
		}

		private Label? monsterNameLabel;

		private Label? monsterModelLabel;

		private Label? headerStatusLabel;

		private ItemList? variantList;

		private ItemList? stateIdList;

		private Button? addVariantButton;

		private Button? duplicateVariantButton;

		private Button? deleteVariantButton;

		private Button? moveVariantUpButton;

		private Button? moveVariantDownButton;

		private Button? saveButton;

		private Button? reloadButton;

		private Button? closeButton;

		private LineEdit? conditionEdit;

		private Label? conditionStatusLabel;

		private LineEdit? upToDateConditionEdit;

		private Label? upToDateConditionStatusLabel;

		private LineEdit? offsetXEdit;

		private LineEdit? offsetYEdit;

		private CodeEdit? secondaryInitialStatesEdit;

		private CodeEdit? stateMachineEdit;

		private CodeEdit? moveReplacementsEdit;

		private CodeEdit? graphPatchEdit;

		private Label? intentStringsStatusLabel;

		private CodeEdit? intentStringsEdit;

		private CodeEdit? readOnlySummaryEdit;

		private Label? previewStatusLabel;

		private NIntentGraph? previewGraph;

		private ConfirmationDialog? unsavedChangesDialog;

		private AcceptDialog? messageDialog;

		private PanelContainer? windowPanel;

		private TabContainer? editorTabs;

		private MonsterModel? monster;

		private string monsterDisplayName = string.Empty;

		private string monsterModelFullName = string.Empty;

		private string loadedIntentStringsLanguage = "eng";

		private IntentDefinitionList draftDefinitions = new IntentDefinitionList();

		private Dictionary<string, string> draftIntentStrings = new Dictionary<string, string>(StringComparer.Ordinal);

		private List<string> availableStateIds = new List<string>();

		private int selectedVariantIndex = -1;

		private bool isReady;

		private bool isRefreshingUi;

		private bool hasUnsavedChanges;

		private PendingAction pendingAction;

		private Control? lastActiveTextInput;

		public bool HasUnsavedChanges => hasUnsavedChanges;

		private static string LocalizeText(string key, string fallback)
		{
			return IntentGraphMod.IntentGraphStrings.GetValueOrDefault(key, fallback);
		}

		private static string LocalizeText(string key, string fallback, params object[] args)
		{
			return string.Format(LocalizeText(key, fallback), args);
		}

		public void Initialize(MonsterModel monster, string monsterDisplayName)
		{
			this.monster = monster;
			this.monsterDisplayName = monsterDisplayName;
			monsterModelFullName = ((object)monster).GetType().FullName ?? ((object)((AbstractModel)monster).Id).ToString();
			if (isReady)
			{
				LoadDefinitionsFromDiskOrRuntime();
			}
		}

		public override void _Ready()
		{
			//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02dd: Expected O, but got Unknown
			//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f4: Expected O, but got Unknown
			//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c3: Expected O, but got Unknown
			//IL_03d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03da: Expected O, but got Unknown
			//IL_03e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f1: Expected O, but got Unknown
			//IL_03fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0408: Expected O, but got Unknown
			//IL_060a: Unknown result type (might be due to invalid IL or missing references)
			monsterNameLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%MonsterName"));
			monsterModelLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%MonsterModelName"));
			headerStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%HeaderStatus"));
			variantList = ((Node)this).GetNode<ItemList>(NodePath.op_Implicit("%VariantList"));
			stateIdList = ((Node)this).GetNode<ItemList>(NodePath.op_Implicit("%StateIdList"));
			addVariantButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%AddVariantButton"));
			duplicateVariantButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%DuplicateVariantButton"));
			deleteVariantButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%DeleteVariantButton"));
			moveVariantUpButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%MoveVariantUpButton"));
			moveVariantDownButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%MoveVariantDownButton"));
			saveButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%SaveButton"));
			reloadButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%ReloadButton"));
			closeButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%CloseButton"));
			conditionEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%ConditionEdit"));
			conditionStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%ConditionStatus"));
			upToDateConditionEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%UpToDateConditionEdit"));
			upToDateConditionStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%UpToDateConditionStatus"));
			offsetXEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%OffsetXEdit"));
			offsetYEdit = ((Node)this).GetNode<LineEdit>(NodePath.op_Implicit("%OffsetYEdit"));
			secondaryInitialStatesEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%SecondaryInitialStatesEdit"));
			stateMachineEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%StateMachineEdit"));
			moveReplacementsEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%MoveReplacementsEdit"));
			graphPatchEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%GraphPatchEdit"));
			intentStringsStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%IntentStringsStatus"));
			intentStringsEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%IntentStringsEdit"));
			readOnlySummaryEdit = ((Node)this).GetNode<CodeEdit>(NodePath.op_Implicit("%ReadOnlySummaryEdit"));
			previewStatusLabel = ((Node)this).GetNode<Label>(NodePath.op_Implicit("%PreviewStatus"));
			previewGraph = ((Node)this).GetNode<NIntentGraph>(NodePath.op_Implicit("%PreviewGraph"));
			unsavedChangesDialog = ((Node)this).GetNode<ConfirmationDialog>(NodePath.op_Implicit("%UnsavedChangesDialog"));
			messageDialog = ((Node)this).GetNode<AcceptDialog>(NodePath.op_Implicit("%MessageDialog"));
			windowPanel = ((Node)this).GetNode<PanelContainer>(NodePath.op_Implicit("WindowMargin/Window"));
			editorTabs = ((Node)this).GetNode<TabContainer>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs"));
			ApplyLocalization();
			variantList.ItemSelected += new ItemSelectedEventHandler(OnVariantSelected);
			stateIdList.ItemClicked += new ItemClickedEventHandler(OnStateIdClicked);
			((BaseButton)addVariantButton).Pressed += OnAddVariantPressed;
			((BaseButton)duplicateVariantButton).Pressed += OnDuplicateVariantPressed;
			((BaseButton)deleteVariantButton).Pressed += OnDeleteVariantPressed;
			((BaseButton)moveVariantUpButton).Pressed += OnMoveVariantUpPressed;
			((BaseButton)moveVariantDownButton).Pressed += OnMoveVariantDownPressed;
			((BaseButton)saveButton).Pressed += OnSavePressed;
			((BaseButton)reloadButton).Pressed += OnReloadPressed;
			((BaseButton)closeButton).Pressed += OnClosePressed;
			conditionEdit.TextChanged += (TextChangedEventHandler)delegate
			{
				OnEditorFieldsChanged();
			};
			upToDateConditionEdit.TextChanged += (TextChangedEventHandler)delegate
			{
				OnEditorFieldsChanged();
			};
			offsetXEdit.TextChanged += (TextChangedEventHandler)delegate
			{
				OnEditorFieldsChanged();
			};
			offsetYEdit.TextChanged += (TextChangedEventHandler)delegate
			{
				OnEditorFieldsChanged();
			};
			((TextEdit)secondaryInitialStatesEdit).TextChanged += OnEditorFieldsChanged;
			((TextEdit)stateMachineEdit).TextChanged += OnEditorFieldsChanged;
			((TextEdit)moveReplacementsEdit).TextChanged += OnEditorFieldsChanged;
			((TextEdit)graphPatchEdit).TextChanged += OnEditorFieldsChanged;
			((TextEdit)intentStringsEdit).TextChanged += OnIntentStringsChanged;
			((TextEdit)secondaryInitialStatesEdit).TextChanged += delegate
			{
				OnCodeEditorTextChanged(secondaryInitialStatesEdit);
			};
			((TextEdit)stateMachineEdit).TextChanged += delegate
			{
				OnCodeEditorTextChanged(stateMachineEdit);
			};
			((TextEdit)moveReplacementsEdit).TextChanged += delegate
			{
				OnCodeEditorTextChanged(moveReplacementsEdit);
			};
			((TextEdit)graphPatchEdit).TextChanged += delegate
			{
				OnCodeEditorTextChanged(graphPatchEdit);
			};
			secondaryInitialStatesEdit.CodeCompletionRequested += delegate
			{
				OnCodeCompletionRequested(secondaryInitialStatesEdit, JsonEditorKind.SecondaryInitialStates);
			};
			stateMachineEdit.CodeCompletionRequested += delegate
			{
				OnCodeCompletionRequested(stateMachineEdit, JsonEditorKind.StateMachine);
			};
			moveReplacementsEdit.CodeCompletionRequested += delegate
			{
				OnCodeCompletionRequested(moveReplacementsEdit, JsonEditorKind.MoveReplacements);
			};
			graphPatchEdit.CodeCompletionRequested += delegate
			{
				OnCodeCompletionRequested(graphPatchEdit, JsonEditorKind.GraphPatch);
			};
			((AcceptDialog)unsavedChangesDialog).Confirmed += OnUnsavedChangesConfirmed;
			RegisterEditableInput((Control?)(object)conditionEdit);
			RegisterEditableInput((Control?)(object)upToDateConditionEdit);
			RegisterEditableInput((Control?)(object)secondaryInitialStatesEdit);
			RegisterEditableInput((Control?)(object)stateMachineEdit);
			RegisterEditableInput((Control?)(object)moveReplacementsEdit);
			RegisterEditableInput((Control?)(object)graphPatchEdit);
			RegisterEditableInput((Control?)(object)intentStringsEdit);
			ConfigureCodeEditor(secondaryInitialStatesEdit);
			ConfigureCodeEditor(stateMachineEdit);
			ConfigureCodeEditor(moveReplacementsEdit);
			ConfigureCodeEditor(graphPatchEdit);
			ConfigureCodeEditor(intentStringsEdit, enableCompletion: false);
			ConfigureCodeEditor(readOnlySummaryEdit, enableCompletion: false);
			((TextEdit)readOnlySummaryEdit).Editable = false;
			isReady = true;
			((GodotObject)this).CallDeferred(MethodName.ApplyViewportLayout, Array.Empty<Variant>());
			if (monster != null)
			{
				LoadDefinitionsFromDiskOrRuntime();
				return;
			}
			UpdateHeaderStatus(LocalizeText("ui.editor.status.no_monster", "No monster selected."));
			SetEditorsEnabled(enabled: false);
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Invalid comparison between Unknown and I8
			InputEventKey val = (InputEventKey)(object)((@event is InputEventKey) ? @event : null);
			if (val != null && val.Pressed && !val.Echo && (long)val.Keycode == 4194305)
			{
				RequestClose();
				((Node)this).GetViewport().SetInputAsHandled();
			}
		}

		private void ApplyViewportLayout()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
			Vector2 size = ((Rect2)(ref viewportRect)).Size;
			((Control)this).Position = Vector2.Zero;
			((Control)this).Size = size;
			if (windowPanel != null)
			{
				((Control)windowPanel).CustomMinimumSize = size;
				((Control)windowPanel).Size = size;
			}
		}

		private void LoadDefinitionsFromDiskOrRuntime()
		{
			MonsterModel? obj = monster;
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				MonsterMoveStateMachine moveStateMachine = obj.MoveStateMachine;
				obj2 = ((moveStateMachine != null) ? (from state in moveStateMachine.States.Values
					select state.Id into id
					where !string.IsNullOrWhiteSpace(id)
					select id).Distinct<string>(StringComparer.Ordinal).OrderBy<string, string>((string id) => id, StringComparer.Ordinal).ToList() : null);
			}
			if (obj2 == null)
			{
				obj2 = new List<string>();
			}
			availableStateIds = (List<string>)obj2;
			draftDefinitions = ((monster == null) ? new IntentDefinitionList() : IntentDefinitionEditorService.LoadEditableDefinitions(monsterModelFullName));
			loadedIntentStringsLanguage = LocManager.Instance.Language;
			draftIntentStrings = IntentGraphMod.LoadIntentStringsFromFile(IntentGraphMod.GetDevIntentStringFilePath(loadedIntentStringsLanguage));
			ApplyLocalization();
			selectedVariantIndex = ((draftDefinitions.Count > 0) ? Math.Clamp(selectedVariantIndex, 0, draftDefinitions.Count - 1) : (-1));
			if (draftDefinitions.Count > 0 && selectedVariantIndex < 0)
			{
				selectedVariantIndex = 0;
			}
			hasUnsavedChanges = false;
			pendingAction = PendingAction.None;
			RefreshUiFromState();
			UpdateHeaderStatus(LocalizeText("ui.editor.status.editing_monster", "Editing {0}", monsterModelFullName));
		}

		private void ApplyLocalization()
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_018b: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Unknown result type (might be due to invalid IL or missing references)
			Label? obj = monsterNameLabel;
			if (obj != null)
			{
				((GodotObject)obj).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.header.monster_name", "Monster Name")));
			}
			Label? obj2 = monsterModelLabel;
			if (obj2 != null)
			{
				((GodotObject)obj2).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.header.monster_model_name", "Monster model full name")));
			}
			Label? obj3 = headerStatusLabel;
			if (!string.IsNullOrWhiteSpace((obj3 != null) ? obj3.Text : null))
			{
				Label? obj4 = headerStatusLabel;
				if (!(((obj4 != null) ? obj4.Text : null) == "Ready"))
				{
					goto IL_00aa;
				}
			}
			UpdateHeaderStatus(LocalizeText("ui.editor.status.ready", "Ready"));
			goto IL_00aa;
			IL_00aa:
			Button? obj5 = saveButton;
			if (obj5 != null)
			{
				((GodotObject)obj5).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.save", "Save")));
			}
			Button? obj6 = reloadButton;
			if (obj6 != null)
			{
				((GodotObject)obj6).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.reload", "Reload")));
			}
			Button? obj7 = closeButton;
			if (obj7 != null)
			{
				((GodotObject)obj7).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.close", "Close")));
			}
			Button? obj8 = addVariantButton;
			if (obj8 != null)
			{
				((GodotObject)obj8).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.add", "Add")));
			}
			Button? obj9 = duplicateVariantButton;
			if (obj9 != null)
			{
				((GodotObject)obj9).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.duplicate", "Duplicate")));
			}
			Button? obj10 = deleteVariantButton;
			if (obj10 != null)
			{
				((GodotObject)obj10).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.delete", "Delete")));
			}
			Button? obj11 = moveVariantUpButton;
			if (obj11 != null)
			{
				((GodotObject)obj11).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.move_up", "Move Up")));
			}
			Button? obj12 = moveVariantDownButton;
			if (obj12 != null)
			{
				((GodotObject)obj12).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(LocalizeText("ui.editor.button.move_down", "Move Down")));
			}
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/VariantsLabel")).Text = LocalizeText("ui.editor.sidebar.variants", "Variants");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/VariantHint")).Text = LocalizeText("ui.editor.sidebar.variant_hint", "Later items win when multiple conditions match.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/StateIdsLabel")).Text = LocalizeText("ui.editor.sidebar.state_ids", "State IDs");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/Sidebar/StateIdHint")).Text = LocalizeText("ui.editor.sidebar.state_ids_hint", "Click a state ID to insert it into the last focused editable field.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/ConditionLabel")).Text = LocalizeText("ui.editor.condition.label", "Selection condition");
			conditionEdit.PlaceholderText = LocalizeText("ui.editor.condition.placeholder", "true");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/UpToDateConditionLabel")).Text = LocalizeText("ui.editor.up_to_date_condition.label", "Up-to-date condition");
			upToDateConditionEdit.PlaceholderText = LocalizeText("ui.editor.up_to_date_condition.placeholder", "Leave blank to skip outdated warnings.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/OffsetRow/OffsetXLabel")).Text = LocalizeText("ui.editor.offset_x.label", "offsetX");
			offsetXEdit.PlaceholderText = LocalizeText("ui.editor.offset_x.placeholder", "0");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/Condition/ConditionSection/OffsetRow/OffsetYLabel")).Text = LocalizeText("ui.editor.offset_y.label", "offsetY");
			offsetYEdit.PlaceholderText = LocalizeText("ui.editor.offset_y.placeholder", "0");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/secondaryInitialStates JSON/SecondaryInitialStatesSection/SecondaryInitialStatesHelp")).Text = LocalizeText("ui.editor.help.secondary_initial_states", "Edit secondaryInitialStates as a JSON array of state ids.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/stateMachine JSON/StateMachineSection/StateMachineHelp")).Text = LocalizeText("ui.editor.help.state_machine", "Edit the StateMachineNode array for this variant.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/moveReplacements JSON/MoveReplacementsSection/MoveReplacementHelp")).Text = LocalizeText("ui.editor.help.move_replacements", "Edit move replacement overrides keyed by move id.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/EditorScroll/EditorVBox/EditorTabs/graphPatch JSON/GraphPatchSection/GraphPatchHelp")).Text = LocalizeText("ui.editor.help.graph_patch", "Edit the full graphPatch object for this variant.");
			((Node)this).GetNode<Label>(NodePath.op_Implicit("WindowMargin/Window/RootMargin/RootVBox/BodySplit/ContentSplit/PreviewVBox/PreviewTitle")).Text = LocalizeText("ui.editor.preview.title", "Live Preview");
			if (editorTabs != null)
			{
				SetTabTitle("Condition", LocalizeText("ui.editor.tab.condition", "Basic Value"));
				SetTabTitle("secondaryInitialStates JSON", LocalizeText("ui.editor.tab.secondary_initial_states", "secondaryInitialStates JSON"));
				SetTabTitle("stateMachine JSON", LocalizeText("ui.editor.tab.state_machine", "stateMachine JSON"));
				SetTabTitle("moveReplacements JSON", LocalizeText("ui.editor.tab.move_replacements", "moveReplacements JSON"));
				SetTabTitle("graphPatch JSON", LocalizeText("ui.editor.tab.graph_patch", "graphPatch JSON"));
				SetTabTitle("intentgraph strings JSON", LocalizeText("ui.editor.tab.intent_strings", "intentgraph strings JSON"));
				SetTabTitle("Read-only fields", LocalizeText("ui.editor.tab.read_only", "Read-only fields"));
			}
			((Window)unsavedChangesDialog).Title = LocalizeText("ui.editor.dialog.unsaved.title", "Discard Changes?");
			((AcceptDialog)unsavedChangesDialog).DialogText = LocalizeText("ui.editor.dialog.unsaved.text", "You have unsaved changes. Continue and discard them?");
			((Window)messageDialog).Title = LocalizeText("ui.editor.dialog.message.title", "Intent Graph Editor");
			if (conditionStatusLabel != null && conditionStatusLabel.Text == "Condition syntax is validated against the current monster.")
			{
				conditionStatusLabel.Text = LocalizeText("ui.editor.condition.status.initial", "Condition syntax is validated against the current monster.");
			}
			if (upToDateConditionStatusLabel != null && upToDateConditionStatusLabel.Text == "Leave blank to skip outdated warnings. Syntax is validated against the current monster.")
			{
				upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.status.initial", "Leave blank to skip outdated warnings. Syntax is validated against the current monster.");
			}
			if (intentStringsStatusLabel != null && intentStringsStatusLabel.Text == "Editing dev intent strings for the current language.")
			{
				intentStringsStatusLabel.Text = LocalizeText("ui.editor.intent_strings.status.initial", "Editing dev intent strings for the current language.");
			}
			if (previewStatusLabel != null && previewStatusLabel.Text == "Preview updates as fields become valid.")
			{
				previewStatusLabel.Text = LocalizeText("ui.editor.preview.status.initial", "Preview updates as fields become valid.");
			}
		}

		private void SetTabTitle(string childName, string title)
		{
			if (editorTabs != null && ((Node)editorTabs).HasNode(NodePath.op_Implicit(childName)))
			{
				Control node = ((Node)editorTabs).GetNode<Control>(NodePath.op_Implicit(childName));
				editorTabs.SetTabTitle(((Node)node).GetIndex(false), title);
			}
		}

		private void RefreshUiFromState()
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			if (!isReady)
			{
				return;
			}
			isRefreshingUi = true;
			try
			{
				Label? obj = monsterNameLabel;
				if (obj != null)
				{
					((GodotObject)obj).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(monsterDisplayName));
				}
				Label? obj2 = monsterModelLabel;
				if (obj2 != null)
				{
					((GodotObject)obj2).SetDeferred(StringName.op_Implicit("text"), Variant.op_Implicit(monsterModelFullName));
				}
				RefreshVariantList();
				RefreshStateIdList();
				LoadSelectedDefinitionIntoEditors();
				LoadIntentStringsIntoEditor();
				UpdateActionState();
			}
			finally
			{
				isRefreshingUi = false;
			}
			RefreshPreview();
		}

		private void RefreshVariantList()
		{
			if (variantList != null)
			{
				variantList.Clear();
				for (int i = 0; i < draftDefinitions.Count; i++)
				{
					variantList.AddItem(BuildVariantLabel(i, draftDefinitions[i]), (Texture2D)null, true);
				}
				if (selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
				{
					variantList.Select(selectedVariantIndex, true);
				}
			}
		}

		private void RefreshStateIdList()
		{
			if (stateIdList == null)
			{
				return;
			}
			stateIdList.Clear();
			foreach (string availableStateId in availableStateIds)
			{
				stateIdList.AddItem(availableStateId, (Texture2D)null, true);
			}
		}

		private static string BuildVariantLabel(int index, IntentDefinition definition)
		{
			string text = (string.IsNullOrWhiteSpace(definition.Condition) ? "true" : definition.Condition.Trim());
			if (text.Length > 42)
			{
				text = text.Substring(0, 39) + "...";
			}
			return $"#{index + 1} {text}";
		}

		private void LoadSelectedDefinitionIntoEditors()
		{
			IntentDefinition intentDefinition = ((selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count) ? draftDefinitions[selectedVariantIndex] : null);
			SetEditorsEnabled(intentDefinition != null);
			if (conditionEdit != null && upToDateConditionEdit != null && offsetXEdit != null && offsetYEdit != null && secondaryInitialStatesEdit != null && stateMachineEdit != null && moveReplacementsEdit != null && graphPatchEdit != null && readOnlySummaryEdit != null)
			{
				if (intentDefinition == null)
				{
					conditionEdit.Text = string.Empty;
					upToDateConditionEdit.Text = string.Empty;
					offsetXEdit.Text = string.Empty;
					offsetYEdit.Text = string.Empty;
					((TextEdit)secondaryInitialStatesEdit).Text = string.Empty;
					((TextEdit)stateMachineEdit).Text = string.Empty;
					((TextEdit)moveReplacementsEdit).Text = string.Empty;
					((TextEdit)graphPatchEdit).Text = string.Empty;
					((TextEdit)readOnlySummaryEdit).Text = LocalizeText("ui.editor.message.no_variant_selected", "No variant selected. Add one to begin editing.");
					UpdateConditionStatus(null);
					UpdateUpToDateConditionStatus(null);
				}
				else
				{
					conditionEdit.Text = intentDefinition.Condition ?? "true";
					upToDateConditionEdit.Text = intentDefinition.UpToDateCondition ?? string.Empty;
					offsetXEdit.Text = FormatPositionComponent(intentDefinition.Offset.X);
					offsetYEdit.Text = FormatPositionComponent(intentDefinition.Offset.Y);
					((TextEdit)secondaryInitialStatesEdit).Text = ((intentDefinition.SecondaryInitialStates == null || intentDefinition.SecondaryInitialStates.Length == 0) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.SecondaryInitialStates));
					((TextEdit)stateMachineEdit).Text = ((intentDefinition.StateMachine == null) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.StateMachine));
					((TextEdit)moveReplacementsEdit).Text = ((intentDefinition.MoveReplacements == null) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.MoveReplacements));
					((TextEdit)graphPatchEdit).Text = ((intentDefinition.GraphPatch == null) ? string.Empty : IntentDefinitionEditorService.SerializeJson(intentDefinition.GraphPatch));
					((TextEdit)readOnlySummaryEdit).Text = IntentDefinitionEditorService.BuildReadOnlySummary(intentDefinition);
					UpdateConditionStatus(intentDefinition.Condition);
					UpdateUpToDateConditionStatus(intentDefinition.UpToDateCondition);
				}
			}
		}

		private void LoadIntentStringsIntoEditor()
		{
			if (intentStringsEdit != null)
			{
				((TextEdit)intentStringsEdit).Text = ((draftIntentStrings.Count == 0) ? string.Empty : IntentDefinitionEditorService.SerializeJson(draftIntentStrings));
				UpdateIntentStringsStatus(null);
			}
		}

		private void UpdateActionState()
		{
			bool flag = selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count;
			if (duplicateVariantButton != null)
			{
				((BaseButton)duplicateVariantButton).Disabled = !flag;
			}
			if (deleteVariantButton != null)
			{
				((BaseButton)deleteVariantButton).Disabled = !flag;
			}
			if (moveVariantUpButton != null)
			{
				((BaseButton)moveVariantUpButton).Disabled = !flag || selectedVariantIndex <= 0;
			}
			if (moveVariantDownButton != null)
			{
				((BaseButton)moveVariantDownButton).Disabled = !flag || selectedVariantIndex >= draftDefinitions.Count - 1;
			}
		}

		private void SetEditorsEnabled(bool enabled)
		{
			if (conditionEdit != null)
			{
				conditionEdit.Editable = enabled;
			}
			if (upToDateConditionEdit != null)
			{
				upToDateConditionEdit.Editable = enabled;
			}
			if (offsetXEdit != null)
			{
				offsetXEdit.Editable = enabled;
			}
			if (offsetYEdit != null)
			{
				offsetYEdit.Editable = enabled;
			}
			if (secondaryInitialStatesEdit != null)
			{
				((TextEdit)secondaryInitialStatesEdit).Editable = enabled;
			}
			if (stateMachineEdit != null)
			{
				((TextEdit)stateMachineEdit).Editable = enabled;
			}
			if (moveReplacementsEdit != null)
			{
				((TextEdit)moveReplacementsEdit).Editable = enabled;
			}
			if (graphPatchEdit != null)
			{
				((TextEdit)graphPatchEdit).Editable = enabled;
			}
		}

		private void OnEditorFieldsChanged()
		{
			if (!isRefreshingUi)
			{
				hasUnsavedChanges = true;
				UpdateHeaderStatus(LocalizeText("ui.editor.status.unsaved_changes", "Unsaved changes"));
				RefreshPreview();
			}
		}

		private void OnIntentStringsChanged()
		{
			OnEditorFieldsChanged();
			if (TryReadIntentStringsFromEditor(out Dictionary<string, string> _, out string error))
			{
				UpdateIntentStringsStatus(null);
			}
			else
			{
				UpdateIntentStringsStatus(error);
			}
		}

		private void ConfigureCodeEditor(CodeEdit? editor, bool enableCompletion = true)
		{
			if (editor != null)
			{
				editor.CodeCompletionEnabled = enableCompletion;
				editor.AutoBraceCompletionEnabled = true;
				editor.IndentAutomatic = true;
				editor.IndentUseSpaces = true;
				editor.IndentSize = 4;
				editor.GuttersDrawLineNumbers = true;
			}
		}

		private void OnCodeEditorTextChanged(CodeEdit? editor)
		{
			if (editor != null && !isRefreshingUi && ((Control)editor).HasFocus())
			{
				string line = ((TextEdit)editor).GetLine(((TextEdit)editor).GetCaretLine(0));
				int caretColumn = ((TextEdit)editor).GetCaretColumn(0);
				char c = ((caretColumn > 0 && caretColumn <= line.Length) ? line[caretColumn - 1] : '\0');
				string completionPrefix = GetCompletionPrefix(editor);
				if (c == '"' || c == ':' || c == '{' || c == '[' || completionPrefix.Length >= 1)
				{
					editor.RequestCodeCompletion(true);
				}
			}
		}

		private void OnCodeCompletionRequested(CodeEdit? editor, JsonEditorKind kind)
		{
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			if (editor == null)
			{
				return;
			}
			bool isInsideString = IsCaretInsideString(editor);
			string prefix = GetCompletionPrefix(editor);
			foreach (CompletionItem item in from c in BuildCompletionItems(kind, isInsideString)
				where string.IsNullOrEmpty(prefix) || c.MatchText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				group c by (DisplayText: c.DisplayText, InsertText: c.InsertText) into @group
				select @group.First())
			{
				editor.AddCodeCompletionOption(item.KindValue, item.DisplayText, item.InsertText, (Color?)null, (Resource)null, default(Variant), 1024);
			}
			editor.UpdateCodeCompletionOptions(true);
		}

		private void RegisterEditableInput(Control? control)
		{
			if (control != null)
			{
				control.FocusEntered += delegate
				{
					lastActiveTextInput = control;
				};
			}
		}

		private void OnVariantSelected(long index)
		{
			if (isRefreshingUi)
			{
				return;
			}
			int num = (int)index;
			if (num == selectedVariantIndex)
			{
				return;
			}
			if (!TryCommitSelectedVariant(validateCondition: false, out string error))
			{
				ShowMessage(LocalizeText("ui.editor.message.fix_before_switch", "Fix the current variant before switching.\n\n{0}", error));
				isRefreshingUi = true;
				try
				{
					if (variantList != null && selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
					{
						variantList.Select(selectedVariantIndex, true);
					}
					return;
				}
				finally
				{
					isRefreshingUi = false;
				}
			}
			selectedVariantIndex = num;
			isRefreshingUi = true;
			try
			{
				LoadSelectedDefinitionIntoEditors();
				UpdateActionState();
			}
			finally
			{
				isRefreshingUi = false;
			}
			RefreshPreview();
		}

		private void OnAddVariantPressed()
		{
			if (!TryCommitSelectedVariant(validateCondition: false, out string error))
			{
				ShowMessage(LocalizeText("ui.editor.message.fix_before_add", "Fix the current variant before adding another.\n\n{0}", error));
				return;
			}
			draftDefinitions.Add(new IntentDefinition());
			selectedVariantIndex = draftDefinitions.Count - 1;
			hasUnsavedChanges = true;
			RefreshUiFromState();
		}

		private void OnStateIdClicked(long index, Vector2 atPosition, long mouseButtonIndex)
		{
			if (mouseButtonIndex == 1 && index >= 0 && index < availableStateIds.Count)
			{
				InsertStateIdIntoActiveInput(availableStateIds[(int)index]);
			}
		}

		private void OnDuplicateVariantPressed()
		{
			if (selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
			{
				if (!TryCommitSelectedVariant(validateCondition: false, out string error))
				{
					ShowMessage(LocalizeText("ui.editor.message.fix_before_duplicate", "Fix the current variant before duplicating it.\n\n{0}", error));
					return;
				}
				IntentDefinition item = IntentDefinitionEditorService.Clone(draftDefinitions[selectedVariantIndex]) ?? new IntentDefinition();
				draftDefinitions.Insert(selectedVariantIndex + 1, item);
				selectedVariantIndex++;
				hasUnsavedChanges = true;
				RefreshUiFromState();
			}
		}

		private void OnDeleteVariantPressed()
		{
			if (selectedVariantIndex >= 0 && selectedVariantIndex < draftDefinitions.Count)
			{
				draftDefinitions.RemoveAt(selectedVariantIndex);
				if (draftDefinitions.Count == 0)
				{
					selectedVariantIndex = -1;
				}
				else if (selectedVariantIndex >= draftDefinitions.Count)
				{
					selectedVariantIndex = draftDefinitions.Count - 1;
				}
				hasUnsavedChanges = true;
				RefreshUiFromState();
			}
		}

		private void OnMoveVariantUpPressed()
		{
			MoveSelectedVariant(-1);
		}

		private void OnMoveVariantDownPressed()
		{
			MoveSelectedVariant(1);
		}

		private void MoveSelectedVariant(int delta)
		{
			if (selectedVariantIndex < 0 || selectedVariantIndex >= draftDefinitions.Count)
			{
				return;
			}
			if (!TryCommitSelectedVariant(validateCondition: false, out string error))
			{
				ShowMessage(LocalizeText("ui.editor.message.fix_before_reorder", "Fix the current variant before reordering.\n\n{0}", error));
				return;
			}
			int num = selectedVariantIndex + delta;
			if (num >= 0 && num < draftDefinitions.Count)
			{
				IntentDefinitionList intentDefinitionList = draftDefinitions;
				int index = selectedVariantIndex;
				IntentDefinitionList intentDefinitionList2 = draftDefinitions;
				int index2 = num;
				IntentDefinition value = draftDefinitions[num];
				IntentDefinition value2 = draftDefinitions[selectedVariantIndex];
				intentDefinitionList[index] = value;
				intentDefinitionList2[index2] = value2;
				selectedVariantIndex = num;
				hasUnsavedChanges = true;
				RefreshUiFromState();
			}
		}

		private void OnSavePressed()
		{
			if (!TryCommitSelectedVariant(validateCondition: true, out string error))
			{
				ShowMessage(LocalizeText("ui.editor.message.save_invalid_current", "Cannot save until the current variant is valid.\n\n{0}", error));
				return;
			}
			if (!ValidateAllVariantConditions(out error))
			{
				ShowMessage(LocalizeText("ui.editor.message.save_invalid_all_conditions", "Cannot save until every variant has a valid condition.\n\n{0}", error));
				return;
			}
			if (!TryCommitIntentStrings(out error))
			{
				ShowMessage(LocalizeText("ui.editor.message.save_invalid_intent_strings", "Cannot save until the current language intent strings JSON is valid.\n\n{0}", error));
				return;
			}
			IntentDefinitionEditorService.SaveEditableDefinitions(monsterModelFullName, draftDefinitions);
			IntentGraphMod.SaveIntentStringsToFile(IntentGraphMod.GetDevIntentStringFilePath(loadedIntentStringsLanguage), draftIntentStrings);
			IntentGraphMod.ReloadIntentDefinitionsAndGraphs();
			hasUnsavedChanges = false;
			UpdateHeaderStatus(LocalizeText("ui.editor.status.saved", "Saved definitions and {0} intent strings.", loadedIntentStringsLanguage));
			RefreshUiFromState();
		}

		private void OnReloadPressed()
		{
			if (hasUnsavedChanges)
			{
				pendingAction = PendingAction.Reload;
				ConfirmationDialog? obj = unsavedChangesDialog;
				if (obj != null)
				{
					((Window)obj).PopupCentered((Vector2I?)null);
				}
			}
			else
			{
				ReloadDefinitions();
			}
		}

		private void OnClosePressed()
		{
			RequestClose();
		}

		private void RequestClose()
		{
			if (hasUnsavedChanges)
			{
				pendingAction = PendingAction.Close;
				ConfirmationDialog? obj = unsavedChangesDialog;
				if (obj != null)
				{
					((Window)obj).PopupCentered((Vector2I?)null);
				}
			}
			else
			{
				CloseEditor();
			}
		}

		private void OnUnsavedChangesConfirmed()
		{
			switch (pendingAction)
			{
			case PendingAction.Close:
				CloseEditor();
				break;
			case PendingAction.Reload:
				ReloadDefinitions();
				break;
			}
			pendingAction = PendingAction.None;
		}

		private void ReloadDefinitions()
		{
			LoadDefinitionsFromDiskOrRuntime();
		}

		private void CloseEditor()
		{
			IntentGraphEditorHost.CloseEditor(this);
		}

		private void RefreshPreview()
		{
			if (previewGraph == null || previewStatusLabel == null)
			{
				return;
			}
			if (monster == null)
			{
				previewGraph.Graph = null;
				previewStatusLabel.Text = LocalizeText("ui.editor.preview.no_monster", "No monster selected.");
				return;
			}
			if (!TryReadIntentStringsFromEditor(out Dictionary<string, string> intentStrings, out string error))
			{
				previewGraph.Graph = null;
				previewStatusLabel.Text = LocalizeText("ui.editor.preview.blocked", "Preview blocked: {0}", error);
				return;
			}
			if (selectedVariantIndex < 0 || selectedVariantIndex >= draftDefinitions.Count)
			{
				previewGraph.Graph = IntentGraphGenerator.GenerateGraph(monster, null, intentStrings);
				previewStatusLabel.Text = ((previewGraph.Graph == null) ? LocalizeText("ui.editor.preview.no_graph_current_monster", "Preview generated no graph for the current monster.") : LocalizeText("ui.editor.preview.updated_runtime", "Preview updated from current runtime graph."));
				UpdateConditionStatus(null);
				UpdateUpToDateConditionStatus(null);
				return;
			}
			if (!TryCommitSelectedVariant(validateCondition: false, out string error2))
			{
				previewStatusLabel.Text = LocalizeText("ui.editor.preview.blocked", "Preview blocked: {0}", error2);
				return;
			}
			IntentDefinition intentDefinition = draftDefinitions[selectedVariantIndex];
			UpdateConditionStatus(intentDefinition.Condition);
			UpdateUpToDateConditionStatus(intentDefinition.UpToDateCondition);
			try
			{
				previewGraph.Graph = IntentGraphGenerator.GenerateGraph(monster, intentDefinition, intentStrings);
				previewStatusLabel.Text = ((previewGraph.Graph == null) ? LocalizeText("ui.editor.preview.no_graph_draft", "Preview generated no graph for this draft.") : LocalizeText("ui.editor.preview.updated", "Preview updated."));
			}
			catch (Exception ex)
			{
				previewGraph.Graph = null;
				previewStatusLabel.Text = LocalizeText("ui.editor.preview.error", "Preview error: {0}", ex.Message);
			}
		}

		private bool TryCommitSelectedVariant(bool validateCondition, out string error)
		{
			error = string.Empty;
			if (selectedVariantIndex < 0 || selectedVariantIndex >= draftDefinitions.Count)
			{
				return true;
			}
			IntentDefinition intentDefinition = IntentDefinitionEditorService.Clone(draftDefinitions[selectedVariantIndex]) ?? new IntentDefinition();
			LineEdit? obj = conditionEdit;
			intentDefinition.Condition = NormalizeConditionText((obj != null) ? obj.Text : null);
			LineEdit? obj2 = upToDateConditionEdit;
			intentDefinition.UpToDateCondition = NormalizeOptionalRuleText((obj2 != null) ? obj2.Text : null);
			LineEdit? obj3 = offsetXEdit;
			if (!TryParsePositionComponent((obj3 != null) ? obj3.Text : null, LocalizeText("ui.editor.field.offset_x", "offsetX"), out var value, out error))
			{
				return false;
			}
			LineEdit? obj4 = offsetYEdit;
			if (!TryParsePositionComponent((obj4 != null) ? obj4.Text : null, LocalizeText("ui.editor.field.offset_y", "offsetY"), out var value2, out error))
			{
				return false;
			}
			CodeEdit? obj5 = secondaryInitialStatesEdit;
			if (!TryDeserializeField<SecondaryInitialState[]>((obj5 != null) ? ((TextEdit)obj5).Text : null, LocalizeText("ui.editor.field.secondary_initial_states", "secondaryInitialStates"), out SecondaryInitialState[] value3, out error))
			{
				return false;
			}
			CodeEdit? obj6 = stateMachineEdit;
			if (!TryDeserializeField<StateMachineNode[]>((obj6 != null) ? ((TextEdit)obj6).Text : null, LocalizeText("ui.editor.field.state_machine", "stateMachine"), out StateMachineNode[] value4, out error))
			{
				return false;
			}
			CodeEdit? obj7 = moveReplacementsEdit;
			if (!TryDeserializeField<Dictionary<string, MoveReplacement>>((obj7 != null) ? ((TextEdit)obj7).Text : null, LocalizeText("ui.editor.field.move_replacements", "moveReplacements"), out Dictionary<string, MoveReplacement> value5, out error))
			{
				return false;
			}
			CodeEdit? obj8 = graphPatchEdit;
			if (!TryDeserializeField<Graph>((obj8 != null) ? ((TextEdit)obj8).Text : null, LocalizeText("ui.editor.field.graph_patch", "graphPatch"), out Graph value6, out error))
			{
				return false;
			}
			intentDefinition.SecondaryInitialStates = value3;
			intentDefinition.StateMachine = value4;
			intentDefinition.MoveReplacements = value5;
			intentDefinition.GraphPatch = NormalizeGraphPatch(value6);
			intentDefinition.Offset = new Position(value, value2);
			if (validateCondition && !ValidateVariantRuleExpressions(intentDefinition, out error))
			{
				return false;
			}
			draftDefinitions[selectedVariantIndex] = intentDefinition;
			return true;
		}

		private bool TryCommitIntentStrings(out string error)
		{
			if (!TryReadIntentStringsFromEditor(out Dictionary<string, string> intentStrings, out error))
			{
				return false;
			}
			draftIntentStrings = intentStrings;
			return true;
		}

		private bool ValidateAllVariantConditions(out string error)
		{
			for (int i = 0; i < draftDefinitions.Count; i++)
			{
				if (!ValidateVariantRuleExpressions(draftDefinitions[i], out error))
				{
					error = $"Variant #{i + 1}: {error}";
					return false;
				}
			}
			error = string.Empty;
			return true;
		}

		private bool ValidateVariantRuleExpressions(IntentDefinition definition, out string error)
		{
			if (!ValidateCondition(definition.Condition, out error))
			{
				error = LocalizeText("ui.editor.error.rule_field", "{0}: {1}", LocalizeText("ui.editor.field.condition", "condition"), error);
				return false;
			}
			if (!ValidateUpToDateCondition(definition.UpToDateCondition, out error))
			{
				error = LocalizeText("ui.editor.error.rule_field", "{0}: {1}", LocalizeText("ui.editor.field.up_to_date_condition", "upToDateCondition"), error);
				return false;
			}
			return true;
		}

		private bool ValidateCondition(string condition, out string error)
		{
			return ValidateRuleExpression(NormalizeConditionText(condition), LocalizeText("ui.editor.field.condition", "condition"), out error);
		}

		private bool ValidateUpToDateCondition(string? condition, out string error)
		{
			string text = NormalizeOptionalRuleText(condition);
			if (text == null)
			{
				error = string.Empty;
				return true;
			}
			return ValidateRuleExpression(text, LocalizeText("ui.editor.field.up_to_date_condition", "upToDateCondition"), out error);
		}

		private bool ValidateRuleExpression(string condition, string fieldName, out string error)
		{
			error = string.Empty;
			if (monster == null)
			{
				return true;
			}
			try
			{
				if (IRule.Parse(condition, new IntentGraph2.Utils.Rule.RuleContext(monster)) == null)
				{
					error = LocalizeText("ui.editor.rule.parse_error", "{0} could not be parsed.", fieldName);
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		private void UpdateConditionStatus(string? condition)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			if (conditionStatusLabel != null)
			{
				string error;
				if (string.IsNullOrWhiteSpace(condition))
				{
					conditionStatusLabel.Text = LocalizeText("ui.editor.condition.defaults_true", "Condition defaults to true.");
					((CanvasItem)conditionStatusLabel).Modulate = Colors.White;
				}
				else if (ValidateCondition(condition, out error))
				{
					conditionStatusLabel.Text = LocalizeText("ui.editor.condition.valid", "Condition syntax is valid.");
					((CanvasItem)conditionStatusLabel).Modulate = new Color(0.7f, 0.95f, 0.75f, 1f);
				}
				else
				{
					conditionStatusLabel.Text = LocalizeText("ui.editor.condition.warning", "Condition warning: {0}", error);
					((CanvasItem)conditionStatusLabel).Modulate = new Color(1f, 0.7f, 0.65f, 1f);
				}
			}
		}

		private void UpdateUpToDateConditionStatus(string? condition)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			if (upToDateConditionStatusLabel != null)
			{
				string error;
				if (string.IsNullOrWhiteSpace(condition))
				{
					upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.defaults_empty", "No up-to-date check. Outdated warnings are disabled.");
					((CanvasItem)upToDateConditionStatusLabel).Modulate = Colors.White;
				}
				else if (ValidateUpToDateCondition(condition, out error))
				{
					upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.valid", "Up-to-date condition syntax is valid.");
					((CanvasItem)upToDateConditionStatusLabel).Modulate = new Color(0.7f, 0.95f, 0.75f, 1f);
				}
				else
				{
					upToDateConditionStatusLabel.Text = LocalizeText("ui.editor.up_to_date_condition.warning", "Up-to-date condition warning: {0}", error);
					((CanvasItem)upToDateConditionStatusLabel).Modulate = new Color(1f, 0.7f, 0.65f, 1f);
				}
			}
		}

		private void UpdateHeaderStatus(string status)
		{
			if (headerStatusLabel != null)
			{
				headerStatusLabel.Text = status;
			}
		}

		private void UpdateIntentStringsStatus(string? error)
		{
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			if (intentStringsStatusLabel != null)
			{
				string devIntentStringFilePath = IntentGraphMod.GetDevIntentStringFilePath(loadedIntentStringsLanguage);
				if (string.IsNullOrWhiteSpace(error))
				{
					intentStringsStatusLabel.Text = LocalizeText("ui.editor.intent_strings.status.saved_to", "Editing dev intent strings for '{0}'. Saved to {1}.", loadedIntentStringsLanguage, devIntentStringFilePath);
					((CanvasItem)intentStringsStatusLabel).Modulate = Colors.White;
				}
				else
				{
					intentStringsStatusLabel.Text = LocalizeText("ui.editor.intent_strings.status.warning", "Intent strings warning: {0}", error);
					((CanvasItem)intentStringsStatusLabel).Modulate = new Color(1f, 0.7f, 0.65f, 1f);
				}
			}
		}

		private void InsertStateIdIntoActiveInput(string stateId)
		{
			Control val = lastActiveTextInput;
			LineEdit val2 = (LineEdit)(object)((val is LineEdit) ? val : null);
			if (val2 == null)
			{
				TextEdit val3 = (TextEdit)(object)((val is TextEdit) ? val : null);
				if (val3 == null || !val3.Editable)
				{
					goto IL_0065;
				}
				val3.DeleteSelection(-1);
				val3.InsertTextAtCaret(stateId, -1);
				((Control)val3).GrabFocus();
			}
			else
			{
				if (!val2.Editable)
				{
					goto IL_0065;
				}
				val2.DeleteText(val2.GetSelectionFromColumn(), val2.GetSelectionToColumn());
				val2.InsertTextAtCaret(stateId);
				((Control)val2).GrabFocus();
			}
			UpdateHeaderStatus(LocalizeText("ui.editor.status.inserted_state_id", "Inserted state ID '{0}'.", stateId));
			OnEditorFieldsChanged();
			return;
			IL_0065:
			UpdateHeaderStatus(LocalizeText("ui.editor.status.click_editable_first", "Click in an editable text box first, then choose a state ID."));
		}

		private IEnumerable<CompletionItem> BuildCompletionItems(JsonEditorKind kind, bool isInsideString)
		{
			return kind switch
			{
				JsonEditorKind.SecondaryInitialStates => BuildSecondaryInitialStatesCompletionItems(isInsideString), 
				JsonEditorKind.StateMachine => BuildStateMachineCompletionItems(isInsideString), 
				JsonEditorKind.MoveReplacements => BuildMoveReplacementCompletionItems(isInsideString), 
				JsonEditorKind.GraphPatch => BuildGraphPatchCompletionItems(isInsideString), 
				_ => Enumerable.Empty<CompletionItem>(), 
			};
		}

		private IEnumerable<CompletionItem> BuildSecondaryInitialStatesCompletionItems(bool isInsideString)
		{
			string[] array = new string[4] { "id", "offset", "x", "y" };
			foreach (string propertyName in array)
			{
				yield return PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
			}
			yield return SnippetCompletion("secondary initial states", "[\n  \"\"\n]");
			foreach (string availableStateId in availableStateIds)
			{
				yield return StringValueCompletion(availableStateId, isInsideString, (CodeCompletionKind)3);
			}
		}

		private IEnumerable<CompletionItem> BuildStateMachineCompletionItems(bool isInsideString)
		{
			string[] array = new string[15]
			{
				"name", "moveName", "isInitialState", "initialStatePriority", "children", "followUpState", "label", "node", "horizontalLayout", "placeholderIntentCount",
				"notSimpleLoopStart", "alternativeMoveNames", "offset", "x", "y"
			};
			foreach (string propertyName in array)
			{
				yield return PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
			}
			yield return SnippetCompletion("state machine node", "{\n  \"name\": \"\",\n  \"moveName\": \"\",\n  \"isInitialState\": false,\n  \"initialStatePriority\": 0,\n  \"followUpState\": \"\"\n}");
			yield return SnippetCompletion("state machine branch node", "{\n  \"name\": \"\",\n  \"children\": [\n    {\n      \"label\": \"\",\n      \"node\": {\n        \"name\": \"\"\n      }\n    }\n  ]\n}");
			yield return BooleanCompletion(value: true);
			yield return BooleanCompletion(value: false);
			foreach (string availableStateId in availableStateIds)
			{
				yield return StringValueCompletion(availableStateId, isInsideString, (CodeCompletionKind)3);
			}
		}

		private IEnumerable<CompletionItem> BuildMoveReplacementCompletionItems(bool isInsideString)
		{
			string[] array = new string[3] { "intentOverrides", "arrowOverride", "path" };
			foreach (string propertyName in array)
			{
				yield return PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
			}
			foreach (string availableStateId in availableStateIds)
			{
				yield return PropertyCompletion(availableStateId, isInsideString, (CodeCompletionKind)3);
			}
			yield return PropertyCompletion("valueText", isInsideString, (CodeCompletionKind)4);
			yield return PropertyCompletion("timesText", isInsideString, (CodeCompletionKind)4);
			yield return SnippetCompletion("move replacement entry", "[\n  {\n    \"valueText\": \"\",\n    \"timesText\": \"\"\n  }\n]");
			yield return PlainTextCompletion("null", "null");
		}

		private IEnumerable<CompletionItem> BuildGraphPatchCompletionItems(bool isInsideString)
		{
			string[] array = new string[21]
			{
				"width", "height", "moves", "icons", "iconGroups", "labels", "arrows", "x", "y", "id",
				"intentType", "value", "times", "valueText", "timesText", "text", "align", "path", "fontSize", "expand",
				"relativeTo"
			};
			foreach (string propertyName in array)
			{
				yield return PropertyCompletion(propertyName, isInsideString, (CodeCompletionKind)4);
			}
			yield return SnippetCompletion("graphPatch", "{\n  \"labels\": [\n    {\n      \"x\": 0.0,\n      \"y\": 0.0,\n      \"text\": \"\",\n      \"align\": \"left\"\n    }\n  ]\n}");
			yield return SnippetCompletion("graphPatch label", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"text\": \"\",\n  \"align\": \"left\"\n}");
			yield return SnippetCompletion("graphPatch move", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"id\": \"\"\n}");
			yield return SnippetCompletion("graphPatch icon", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"intentType\": \"Attack\",\n  \"value\": 0,\n  \"times\": 1\n}");
			yield return SnippetCompletion("graphPatch iconGroup", "{\n  \"x\": 0.0,\n  \"y\": 0.0,\n  \"width\": 1.0,\n  \"height\": 1.0\n}");
			yield return SnippetCompletion("graphPatch arrow", "{\n  \"path\": [0, 0.0, 0.0, 1.0]\n}");
			yield return StringValueCompletion("left", isInsideString, (CodeCompletionKind)5);
			yield return StringValueCompletion("center", isInsideString, (CodeCompletionKind)5);
			yield return StringValueCompletion("right", isInsideString, (CodeCompletionKind)5);
			array = Enum.GetNames<IntentType>();
			foreach (string value in array)
			{
				yield return StringValueCompletion(value, isInsideString, (CodeCompletionKind)5);
			}
			foreach (string availableStateId in availableStateIds)
			{
				yield return StringValueCompletion(availableStateId, isInsideString, (CodeCompletionKind)3);
			}
		}

		private static CompletionItem PropertyCompletion(string propertyName, bool isInsideString, CodeCompletionKind kindValue = (CodeCompletionKind)4L)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			if (!isInsideString)
			{
				return new CompletionItem(propertyName, "\"" + propertyName + "\"", "\"" + propertyName + "\": ", kindValue);
			}
			return new CompletionItem(propertyName, propertyName, propertyName, kindValue);
		}

		private static CompletionItem StringValueCompletion(string value, bool isInsideString, CodeCompletionKind kindValue)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			if (!isInsideString)
			{
				return new CompletionItem(value, "\"" + value + "\"", "\"" + value + "\"", kindValue);
			}
			return new CompletionItem(value, value, value, kindValue);
		}

		private static CompletionItem BooleanCompletion(bool value)
		{
			string obj = (value ? "true" : "false");
			return new CompletionItem(obj, obj, obj, (CodeCompletionKind)6);
		}

		private static CompletionItem SnippetCompletion(string displayText, string insertText)
		{
			return new CompletionItem(displayText, displayText, insertText, (CodeCompletionKind)9);
		}

		private static CompletionItem PlainTextCompletion(string displayText, string insertText)
		{
			return new CompletionItem(displayText, displayText, insertText, (CodeCompletionKind)9);
		}

		private static string GetCompletionPrefix(CodeEdit editor)
		{
			string line = ((TextEdit)editor).GetLine(((TextEdit)editor).GetCaretLine(0));
			int num = Math.Min(((TextEdit)editor).GetCaretColumn(0), line.Length);
			int num2 = num;
			while (num2 > 0 && IsCompletionCharacter(line[num2 - 1]))
			{
				num2--;
			}
			return line.Substring(num2, num - num2);
		}

		private static bool IsCompletionCharacter(char character)
		{
			if (!char.IsLetterOrDigit(character) && character != '_')
			{
				return character == '-';
			}
			return true;
		}

		private static bool IsCaretInsideString(CodeEdit editor)
		{
			int caretLine = ((TextEdit)editor).GetCaretLine(0);
			int caretColumn = ((TextEdit)editor).GetCaretColumn(0);
			if (editor.IsInString(caretLine, caretColumn) < 0)
			{
				if (caretColumn > 0)
				{
					return editor.IsInString(caretLine, caretColumn - 1) >= 0;
				}
				return false;
			}
			return true;
		}

		private void ShowMessage(string text)
		{
			if (messageDialog != null)
			{
				messageDialog.DialogText = text;
				((Window)messageDialog).PopupCentered((Vector2I?)null);
			}
		}

		private static Graph? NormalizeGraphPatch(Graph? graphPatch)
		{
			if (graphPatch == null)
			{
				return null;
			}
			if (graphPatch.Width == 1f && graphPatch.Height == 1f && graphPatch.Arrows.Count == 0 && graphPatch.Moves.Count == 0 && graphPatch.Icons.Count == 0 && graphPatch.IconGroups.Count == 0 && graphPatch.Labels.Count == 0)
			{
				return null;
			}
			return graphPatch;
		}

		private static string[]? NormalizeSecondaryInitialStates(string[]? secondaryInitialStates)
		{
			if (secondaryInitialStates == null)
			{
				return null;
			}
			string[] array = (from stateId in secondaryInitialStates
				where !string.IsNullOrWhiteSpace(stateId)
				select stateId.Trim()).Distinct<string>(StringComparer.Ordinal).ToArray();
			if (array.Length != 0)
			{
				return array;
			}
			return null;
		}

		private static string NormalizeConditionText(string? condition)
		{
			return NormalizeOptionalRuleText(condition) ?? "true";
		}

		private static string FormatPositionComponent(float value)
		{
			if (value != 0f)
			{
				return value.ToString(CultureInfo.InvariantCulture);
			}
			return string.Empty;
		}

		private static string? NormalizeOptionalRuleText(string? condition)
		{
			if (!string.IsNullOrWhiteSpace(condition))
			{
				return condition.Trim();
			}
			return null;
		}

		private bool TryParsePositionComponent(string? text, string fieldName, out float value, out string error)
		{
			error = string.Empty;
			value = 0f;
			if (string.IsNullOrWhiteSpace(text))
			{
				return true;
			}
			if (float.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value))
			{
				return true;
			}
			error = LocalizeText("ui.editor.error.invalid_number", "Invalid {0}: expected a number using '.' for decimals.", fieldName);
			return false;
		}

		private bool TryDeserializeField<T>(string? text, string fieldName, out T? value, out string error)
		{
			error = string.Empty;
			value = default(T);
			if (string.IsNullOrWhiteSpace(text))
			{
				return true;
			}
			try
			{
				value = IntentDefinitionEditorService.DeserializeJson<T>(text);
				return true;
			}
			catch (Exception ex)
			{
				error = LocalizeText("ui.editor.error.invalid_json", "Invalid {0} JSON: {1}", fieldName, ex.Message);
				return false;
			}
		}

		private bool TryReadIntentStringsFromEditor(out Dictionary<string, string> intentStrings, out string error)
		{
			intentStrings = new Dictionary<string, string>(StringComparer.Ordinal);
			CodeEdit? obj = intentStringsEdit;
			if (string.IsNullOrWhiteSpace((obj != null) ? ((TextEdit)obj).Text : null))
			{
				error = string.Empty;
				return true;
			}
			if (!TryDeserializeField<Dictionary<string, string>>(((TextEdit)intentStringsEdit).Text, LocalizeText("ui.editor.field.intent_strings", "intent strings"), out Dictionary<string, string> value, out error))
			{
				return false;
			}
			intentStrings = value ?? new Dictionary<string, string>(StringComparer.Ordinal);
			return true;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Expected O, but got Unknown
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Unknown result type (might be due to invalid IL or missing references)
			//IL_0156: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0185: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0203: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Unknown result type (might be due to invalid IL or missing references)
			//IL_0232: Unknown result type (might be due to invalid IL or missing references)
			//IL_0258: Unknown result type (might be due to invalid IL or missing references)
			//IL_0261: Unknown result type (might be due to invalid IL or missing references)
			//IL_0287: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_0314: Unknown result type (might be due to invalid IL or missing references)
			//IL_031d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0343: Unknown result type (might be due to invalid IL or missing references)
			//IL_0366: Unknown result type (might be due to invalid IL or missing references)
			//IL_0371: Unknown result type (might be due to invalid IL or missing references)
			//IL_0397: Unknown result type (might be due to invalid IL or missing references)
			//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_041d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0428: Expected O, but got Unknown
			//IL_0423: Unknown result type (might be due to invalid IL or missing references)
			//IL_0444: Unknown result type (might be due to invalid IL or missing references)
			//IL_044f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0475: Unknown result type (might be due to invalid IL or missing references)
			//IL_049d: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a8: Expected O, but got Unknown
			//IL_04a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0507: Expected O, but got Unknown
			//IL_0502: Unknown result type (might be due to invalid IL or missing references)
			//IL_0523: Unknown result type (might be due to invalid IL or missing references)
			//IL_052e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0554: Unknown result type (might be due to invalid IL or missing references)
			//IL_057c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0587: Expected O, but got Unknown
			//IL_0582: Unknown result type (might be due to invalid IL or missing references)
			//IL_058d: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_05e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0607: Unknown result type (might be due to invalid IL or missing references)
			//IL_0610: Unknown result type (might be due to invalid IL or missing references)
			//IL_0636: Unknown result type (might be due to invalid IL or missing references)
			//IL_0659: Unknown result type (might be due to invalid IL or missing references)
			//IL_067a: Unknown result type (might be due to invalid IL or missing references)
			//IL_069b: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_06cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_06d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_06fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0704: Unknown result type (might be due to invalid IL or missing references)
			//IL_072a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0733: Unknown result type (might be due to invalid IL or missing references)
			//IL_0759: Unknown result type (might be due to invalid IL or missing references)
			//IL_0762: Unknown result type (might be due to invalid IL or missing references)
			//IL_0788: Unknown result type (might be due to invalid IL or missing references)
			//IL_07ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_07b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_07dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_07e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_080b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0814: Unknown result type (might be due to invalid IL or missing references)
			//IL_083a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0843: Unknown result type (might be due to invalid IL or missing references)
			//IL_0869: Unknown result type (might be due to invalid IL or missing references)
			//IL_0872: Unknown result type (might be due to invalid IL or missing references)
			//IL_0898: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_08c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_08d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_08f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_08ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0925: Unknown result type (might be due to invalid IL or missing references)
			//IL_092e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0954: Unknown result type (might be due to invalid IL or missing references)
			//IL_0977: Unknown result type (might be due to invalid IL or missing references)
			//IL_0982: Unknown result type (might be due to invalid IL or missing references)
			//IL_09a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_09cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_09d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_09fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a1f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a2a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a50: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a73: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a7e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0aa4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ac7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ad2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0af8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b21: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b2c: Expected O, but got Unknown
			//IL_0b27: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b32: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b58: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b7c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0b87: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bad: Unknown result type (might be due to invalid IL or missing references)
			//IL_0bd6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0be1: Expected O, but got Unknown
			//IL_0bdc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0be7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c0d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c30: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c3b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c62: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c87: Unknown result type (might be due to invalid IL or missing references)
			//IL_0c92: Unknown result type (might be due to invalid IL or missing references)
			//IL_0cb8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0cdc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0ce7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d0d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d31: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d3c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d62: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d86: Unknown result type (might be due to invalid IL or missing references)
			//IL_0d91: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(49)
			{
				new MethodInfo(MethodName.LocalizeText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("key"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
					new PropertyInfo((Type)4, StringName.op_Implicit("fallback"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName._UnhandledInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.ApplyViewportLayout, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.LoadDefinitionsFromDiskOrRuntime, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ApplyLocalization, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.SetTabTitle, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("childName"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
					new PropertyInfo((Type)4, StringName.op_Implicit("title"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshUiFromState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshVariantList, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshStateIdList, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.LoadSelectedDefinitionIntoEditors, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.LoadIntentStringsIntoEditor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.UpdateActionState, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.SetEditorsEnabled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)1, StringName.op_Implicit("enabled"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnEditorFieldsChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnIntentStringsChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ConfigureCodeEditor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false),
					new PropertyInfo((Type)1, StringName.op_Implicit("enableCompletion"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnCodeEditorTextChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnCodeCompletionRequested, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false),
					new PropertyInfo((Type)2, StringName.op_Implicit("kind"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.RegisterEditableInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("control"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnVariantSelected, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnAddVariantPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnStateIdClicked, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
					new PropertyInfo((Type)5, StringName.op_Implicit("atPosition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
					new PropertyInfo((Type)2, StringName.op_Implicit("mouseButtonIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnDuplicateVariantPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnDeleteVariantPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnMoveVariantUpPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnMoveVariantDownPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.MoveSelectedVariant, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnSavePressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnReloadPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnClosePressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RequestClose, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnUnsavedChangesConfirmed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.ReloadDefinitions, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.CloseEditor, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.RefreshPreview, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.UpdateConditionStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.UpdateUpToDateConditionStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.UpdateHeaderStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("status"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.UpdateIntentStringsStatus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("error"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.InsertStateIdIntoActiveInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("stateId"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.GetCompletionPrefix, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.IsCompletionCharacter, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)2, StringName.op_Implicit("character"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.IsCaretInsideString, new PropertyInfo((Type)1, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("editor"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("CodeEdit"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.ShowMessage, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("text"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.NormalizeSecondaryInitialStates, new PropertyInfo((Type)34, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)34, StringName.op_Implicit("secondaryInitialStates"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.NormalizeConditionText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.FormatPositionComponent, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)3, StringName.op_Implicit("value"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.NormalizeOptionalRuleText, new PropertyInfo((Type)4, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
				{
					new PropertyInfo((Type)4, StringName.op_Implicit("condition"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_018f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0223: Unknown result type (might be due to invalid IL or missing references)
			//IL_0256: Unknown result type (might be due to invalid IL or missing references)
			//IL_027b: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0313: Unknown result type (might be due to invalid IL or missing references)
			//IL_0353: Unknown result type (might be due to invalid IL or missing references)
			//IL_0386: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03de: Unknown result type (might be due to invalid IL or missing references)
			//IL_0413: Unknown result type (might be due to invalid IL or missing references)
			//IL_042b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0450: Unknown result type (might be due to invalid IL or missing references)
			//IL_0475: Unknown result type (might be due to invalid IL or missing references)
			//IL_049a: Unknown result type (might be due to invalid IL or missing references)
			//IL_04bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0517: Unknown result type (might be due to invalid IL or missing references)
			//IL_053c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0561: Unknown result type (might be due to invalid IL or missing references)
			//IL_0586: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_05f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_061a: Unknown result type (might be due to invalid IL or missing references)
			//IL_064d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0680: Unknown result type (might be due to invalid IL or missing references)
			//IL_06b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_06e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0719: Unknown result type (might be due to invalid IL or missing references)
			//IL_074e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0753: Unknown result type (might be due to invalid IL or missing references)
			//IL_0787: Unknown result type (might be due to invalid IL or missing references)
			//IL_078c: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_07c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_07f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_082d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0832: Unknown result type (might be due to invalid IL or missing references)
			//IL_0867: Unknown result type (might be due to invalid IL or missing references)
			//IL_086c: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_08a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_08db: Unknown result type (might be due to invalid IL or missing references)
			//IL_08e0: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.LocalizeText && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				string text = LocalizeText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = VariantUtils.CreateFrom<string>(ref text);
				return true;
			}
			if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._UnhandledInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				((Node)this)._UnhandledInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ApplyViewportLayout && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ApplyViewportLayout();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.LoadDefinitionsFromDiskOrRuntime && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				LoadDefinitionsFromDiskOrRuntime();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ApplyLocalization && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ApplyLocalization();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.SetTabTitle && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				SetTabTitle(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshUiFromState && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshUiFromState();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshVariantList && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshVariantList();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshStateIdList && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshStateIdList();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.LoadSelectedDefinitionIntoEditors && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				LoadSelectedDefinitionIntoEditors();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.LoadIntentStringsIntoEditor && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				LoadIntentStringsIntoEditor();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.UpdateActionState && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				UpdateActionState();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.SetEditorsEnabled && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				SetEditorsEnabled(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnEditorFieldsChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnEditorFieldsChanged();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnIntentStringsChanged && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnIntentStringsChanged();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ConfigureCodeEditor && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				ConfigureCodeEditor(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnCodeEditorTextChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnCodeEditorTextChanged(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnCodeCompletionRequested && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				OnCodeCompletionRequested(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<JsonEditorKind>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RegisterEditableInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				RegisterEditableInput(VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnVariantSelected && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnVariantSelected(VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnAddVariantPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnAddVariantPressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnStateIdClicked && ((NativeVariantPtrArgs)(ref args)).Count == 3)
			{
				OnStateIdClicked(VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Vector2>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<long>(ref ((NativeVariantPtrArgs)(ref args))[2]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnDuplicateVariantPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnDuplicateVariantPressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnDeleteVariantPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnDeleteVariantPressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnMoveVariantUpPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnMoveVariantUpPressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnMoveVariantDownPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnMoveVariantDownPressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.MoveSelectedVariant && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				MoveSelectedVariant(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnSavePressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnSavePressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnReloadPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnReloadPressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnClosePressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnClosePressed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RequestClose && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RequestClose();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnUnsavedChangesConfirmed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnUnsavedChangesConfirmed();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.ReloadDefinitions && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				ReloadDefinitions();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.CloseEditor && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				CloseEditor();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.RefreshPreview && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				RefreshPreview();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.UpdateConditionStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				UpdateConditionStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.UpdateUpToDateConditionStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				UpdateUpToDateConditionStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.UpdateHeaderStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				UpdateHeaderStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.UpdateIntentStringsStatus && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				UpdateIntentStringsStatus(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.InsertStateIdIntoActiveInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				InsertStateIdIntoActiveInput(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.GetCompletionPrefix && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string completionPrefix = GetCompletionPrefix(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref completionPrefix);
				return true;
			}
			if ((ref method) == MethodName.IsCompletionCharacter && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag = IsCompletionCharacter(VariantUtils.ConvertTo<char>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag);
				return true;
			}
			if ((ref method) == MethodName.IsCaretInsideString && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag2 = IsCaretInsideString(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag2);
				return true;
			}
			if ((ref method) == MethodName.ShowMessage && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				ShowMessage(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.NormalizeSecondaryInitialStates && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string[] array = NormalizeSecondaryInitialStates(VariantUtils.ConvertTo<string[]>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string[]>(ref array);
				return true;
			}
			if ((ref method) == MethodName.NormalizeConditionText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string text2 = NormalizeConditionText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref text2);
				return true;
			}
			if ((ref method) == MethodName.FormatPositionComponent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string text3 = FormatPositionComponent(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref text3);
				return true;
			}
			if ((ref method) == MethodName.NormalizeOptionalRuleText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string text4 = NormalizeOptionalRuleText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref text4);
				return true;
			}
			return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0159: Unknown result type (might be due to invalid IL or missing references)
			//IL_015e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01da: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			//IL_0198: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.LocalizeText && ((NativeVariantPtrArgs)(ref args)).Count == 2)
			{
				string text = LocalizeText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[1]));
				ret = VariantUtils.CreateFrom<string>(ref text);
				return true;
			}
			if ((ref method) == MethodName.GetCompletionPrefix && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string completionPrefix = GetCompletionPrefix(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref completionPrefix);
				return true;
			}
			if ((ref method) == MethodName.IsCompletionCharacter && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag = IsCompletionCharacter(VariantUtils.ConvertTo<char>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag);
				return true;
			}
			if ((ref method) == MethodName.IsCaretInsideString && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				bool flag2 = IsCaretInsideString(VariantUtils.ConvertTo<CodeEdit>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<bool>(ref flag2);
				return true;
			}
			if ((ref method) == MethodName.NormalizeSecondaryInitialStates && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string[] array = NormalizeSecondaryInitialStates(VariantUtils.ConvertTo<string[]>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string[]>(ref array);
				return true;
			}
			if ((ref method) == MethodName.NormalizeConditionText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string text2 = NormalizeConditionText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref text2);
				return true;
			}
			if ((ref method) == MethodName.FormatPositionComponent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string text3 = FormatPositionComponent(VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref text3);
				return true;
			}
			if ((ref method) == MethodName.NormalizeOptionalRuleText && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				string text4 = NormalizeOptionalRuleText(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = VariantUtils.CreateFrom<string>(ref text4);
				return true;
			}
			ret = default(godot_variant);
			return false;
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName.LocalizeText)
			{
				return true;
			}
			if ((ref method) == MethodName._Ready)
			{
				return true;
			}
			if ((ref method) == MethodName._UnhandledInput)
			{
				return true;
			}
			if ((ref method) == MethodName.ApplyViewportLayout)
			{
				return true;
			}
			if ((ref method) == MethodName.LoadDefinitionsFromDiskOrRuntime)
			{
				return true;
			}
			if ((ref method) == MethodName.ApplyLocalization)
			{
				return true;
			}
			if ((ref method) == MethodName.SetTabTitle)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshUiFromState)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshVariantList)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshStateIdList)
			{
				return true;
			}
			if ((ref method) == MethodName.LoadSelectedDefinitionIntoEditors)
			{
				return true;
			}
			if ((ref method) == MethodName.LoadIntentStringsIntoEditor)
			{
				return true;
			}
			if ((ref method) == MethodName.UpdateActionState)
			{
				return true;
			}
			if ((ref method) == MethodName.SetEditorsEnabled)
			{
				return true;
			}
			if ((ref method) == MethodName.OnEditorFieldsChanged)
			{
				return true;
			}
			if ((ref method) == MethodName.OnIntentStringsChanged)
			{
				return true;
			}
			if ((ref method) == MethodName.ConfigureCodeEditor)
			{
				return true;
			}
			if ((ref method) == MethodName.OnCodeEditorTextChanged)
			{
				return true;
			}
			if ((ref method) == MethodName.OnCodeCompletionRequested)
			{
				return true;
			}
			if ((ref method) == MethodName.RegisterEditableInput)
			{
				return true;
			}
			if ((ref method) == MethodName.OnVariantSelected)
			{
				return true;
			}
			if ((ref method) == MethodName.OnAddVariantPressed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnStateIdClicked)
			{
				return true;
			}
			if ((ref method) == MethodName.OnDuplicateVariantPressed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnDeleteVariantPressed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnMoveVariantUpPressed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnMoveVariantDownPressed)
			{
				return true;
			}
			if ((ref method) == MethodName.MoveSelectedVariant)
			{
				return true;
			}
			if ((ref method) == MethodName.OnSavePressed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnReloadPressed)
			{
				return true;
			}
			if ((ref method) == MethodName.OnClosePressed)
			{
				return true;
			}
			if ((ref method) == MethodName.RequestClose)
			{
				return true;
			}
			if ((ref method) == MethodName.OnUnsavedChangesConfirmed)
			{
				return true;
			}
			if ((ref method) == MethodName.ReloadDefinitions)
			{
				return true;
			}
			if ((ref method) == MethodName.CloseEditor)
			{
				return true;
			}
			if ((ref method) == MethodName.RefreshPreview)
			{
				return true;
			}
			if ((ref method) == MethodName.UpdateConditionStatus)
			{
				return true;
			}
			if ((ref method) == MethodName.UpdateUpToDateConditionStatus)
			{
				return true;
			}
			if ((ref method) == MethodName.UpdateHeaderStatus)
			{
				return true;
			}
			if ((ref method) == MethodName.UpdateIntentStringsStatus)
			{
				return true;
			}
			if ((ref method) == MethodName.InsertStateIdIntoActiveInput)
			{
				return true;
			}
			if ((ref method) == MethodName.GetCompletionPrefix)
			{
				return true;
			}
			if ((ref method) == MethodName.IsCompletionCharacter)
			{
				return true;
			}
			if ((ref method) == MethodName.IsCaretInsideString)
			{
				return true;
			}
			if ((ref method) == MethodName.ShowMessage)
			{
				return true;
			}
			if ((ref method) == MethodName.NormalizeSecondaryInitialStates)
			{
				return true;
			}
			if ((ref method) == MethodName.NormalizeConditionText)
			{
				return true;
			}
			if ((ref method) == MethodName.FormatPositionComponent)
			{
				return true;
			}
			if ((ref method) == MethodName.NormalizeOptionalRuleText)
			{
				return true;
			}
			return ((Control)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((ref name) == PropertyName.monsterNameLabel)
			{
				monsterNameLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.monsterModelLabel)
			{
				monsterModelLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.headerStatusLabel)
			{
				headerStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.variantList)
			{
				variantList = VariantUtils.ConvertTo<ItemList>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.stateIdList)
			{
				stateIdList = VariantUtils.ConvertTo<ItemList>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.addVariantButton)
			{
				addVariantButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.duplicateVariantButton)
			{
				duplicateVariantButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.deleteVariantButton)
			{
				deleteVariantButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.moveVariantUpButton)
			{
				moveVariantUpButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.moveVariantDownButton)
			{
				moveVariantDownButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.saveButton)
			{
				saveButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.reloadButton)
			{
				reloadButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.closeButton)
			{
				closeButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.conditionEdit)
			{
				conditionEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.conditionStatusLabel)
			{
				conditionStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.upToDateConditionEdit)
			{
				upToDateConditionEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.upToDateConditionStatusLabel)
			{
				upToDateConditionStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.offsetXEdit)
			{
				offsetXEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.offsetYEdit)
			{
				offsetYEdit = VariantUtils.ConvertTo<LineEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.secondaryInitialStatesEdit)
			{
				secondaryInitialStatesEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.stateMachineEdit)
			{
				stateMachineEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.moveReplacementsEdit)
			{
				moveReplacementsEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.graphPatchEdit)
			{
				graphPatchEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.intentStringsStatusLabel)
			{
				intentStringsStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.intentStringsEdit)
			{
				intentStringsEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.readOnlySummaryEdit)
			{
				readOnlySummaryEdit = VariantUtils.ConvertTo<CodeEdit>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.previewStatusLabel)
			{
				previewStatusLabel = VariantUtils.ConvertTo<Label>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.previewGraph)
			{
				previewGraph = VariantUtils.ConvertTo<NIntentGraph>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.unsavedChangesDialog)
			{
				unsavedChangesDialog = VariantUtils.ConvertTo<ConfirmationDialog>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.messageDialog)
			{
				messageDialog = VariantUtils.ConvertTo<AcceptDialog>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.windowPanel)
			{
				windowPanel = VariantUtils.ConvertTo<PanelContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.editorTabs)
			{
				editorTabs = VariantUtils.ConvertTo<TabContainer>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.monsterDisplayName)
			{
				monsterDisplayName = VariantUtils.ConvertTo<string>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.monsterModelFullName)
			{
				monsterModelFullName = VariantUtils.ConvertTo<string>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.loadedIntentStringsLanguage)
			{
				loadedIntentStringsLanguage = VariantUtils.ConvertTo<string>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.selectedVariantIndex)
			{
				selectedVariantIndex = VariantUtils.ConvertTo<int>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.isReady)
			{
				isReady = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.isRefreshingUi)
			{
				isRefreshingUi = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.hasUnsavedChanges)
			{
				hasUnsavedChanges = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.pendingAction)
			{
				pendingAction = VariantUtils.ConvertTo<PendingAction>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.lastActiveTextInput)
			{
				lastActiveTextInput = VariantUtils.ConvertTo<Control>(ref value);
				return true;
			}
			return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0197: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0217: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0237: Unknown result type (might be due to invalid IL or missing references)
			//IL_023c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0257: Unknown result type (might be due to invalid IL or missing references)
			//IL_025c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0277: Unknown result type (might be due to invalid IL or missing references)
			//IL_027c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0297: Unknown result type (might be due to invalid IL or missing references)
			//IL_029c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0317: Unknown result type (might be due to invalid IL or missing references)
			//IL_031c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0337: Unknown result type (might be due to invalid IL or missing references)
			//IL_033c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_035c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0377: Unknown result type (might be due to invalid IL or missing references)
			//IL_037c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0397: Unknown result type (might be due to invalid IL or missing references)
			//IL_039c: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_03fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0417: Unknown result type (might be due to invalid IL or missing references)
			//IL_041c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0437: Unknown result type (might be due to invalid IL or missing references)
			//IL_043c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0457: Unknown result type (might be due to invalid IL or missing references)
			//IL_045c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0477: Unknown result type (might be due to invalid IL or missing references)
			//IL_047c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0497: Unknown result type (might be due to invalid IL or missing references)
			//IL_049c: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_04f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0517: Unknown result type (might be due to invalid IL or missing references)
			//IL_051c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0537: Unknown result type (might be due to invalid IL or missing references)
			//IL_053c: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.HasUnsavedChanges)
			{
				bool flag = HasUnsavedChanges;
				value = VariantUtils.CreateFrom<bool>(ref flag);
				return true;
			}
			if ((ref name) == PropertyName.monsterNameLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref monsterNameLabel);
				return true;
			}
			if ((ref name) == PropertyName.monsterModelLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref monsterModelLabel);
				return true;
			}
			if ((ref name) == PropertyName.headerStatusLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref headerStatusLabel);
				return true;
			}
			if ((ref name) == PropertyName.variantList)
			{
				value = VariantUtils.CreateFrom<ItemList>(ref variantList);
				return true;
			}
			if ((ref name) == PropertyName.stateIdList)
			{
				value = VariantUtils.CreateFrom<ItemList>(ref stateIdList);
				return true;
			}
			if ((ref name) == PropertyName.addVariantButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref addVariantButton);
				return true;
			}
			if ((ref name) == PropertyName.duplicateVariantButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref duplicateVariantButton);
				return true;
			}
			if ((ref name) == PropertyName.deleteVariantButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref deleteVariantButton);
				return true;
			}
			if ((ref name) == PropertyName.moveVariantUpButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref moveVariantUpButton);
				return true;
			}
			if ((ref name) == PropertyName.moveVariantDownButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref moveVariantDownButton);
				return true;
			}
			if ((ref name) == PropertyName.saveButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref saveButton);
				return true;
			}
			if ((ref name) == PropertyName.reloadButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref reloadButton);
				return true;
			}
			if ((ref name) == PropertyName.closeButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref closeButton);
				return true;
			}
			if ((ref name) == PropertyName.conditionEdit)
			{
				value = VariantUtils.CreateFrom<LineEdit>(ref conditionEdit);
				return true;
			}
			if ((ref name) == PropertyName.conditionStatusLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref conditionStatusLabel);
				return true;
			}
			if ((ref name) == PropertyName.upToDateConditionEdit)
			{
				value = VariantUtils.CreateFrom<LineEdit>(ref upToDateConditionEdit);
				return true;
			}
			if ((ref name) == PropertyName.upToDateConditionStatusLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref upToDateConditionStatusLabel);
				return true;
			}
			if ((ref name) == PropertyName.offsetXEdit)
			{
				value = VariantUtils.CreateFrom<LineEdit>(ref offsetXEdit);
				return true;
			}
			if ((ref name) == PropertyName.offsetYEdit)
			{
				value = VariantUtils.CreateFrom<LineEdit>(ref offsetYEdit);
				return true;
			}
			if ((ref name) == PropertyName.secondaryInitialStatesEdit)
			{
				value = VariantUtils.CreateFrom<CodeEdit>(ref secondaryInitialStatesEdit);
				return true;
			}
			if ((ref name) == PropertyName.stateMachineEdit)
			{
				value = VariantUtils.CreateFrom<CodeEdit>(ref stateMachineEdit);
				return true;
			}
			if ((ref name) == PropertyName.moveReplacementsEdit)
			{
				value = VariantUtils.CreateFrom<CodeEdit>(ref moveReplacementsEdit);
				return true;
			}
			if ((ref name) == PropertyName.graphPatchEdit)
			{
				value = VariantUtils.CreateFrom<CodeEdit>(ref graphPatchEdit);
				return true;
			}
			if ((ref name) == PropertyName.intentStringsStatusLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref intentStringsStatusLabel);
				return true;
			}
			if ((ref name) == PropertyName.intentStringsEdit)
			{
				value = VariantUtils.CreateFrom<CodeEdit>(ref intentStringsEdit);
				return true;
			}
			if ((ref name) == PropertyName.readOnlySummaryEdit)
			{
				value = VariantUtils.CreateFrom<CodeEdit>(ref readOnlySummaryEdit);
				return true;
			}
			if ((ref name) == PropertyName.previewStatusLabel)
			{
				value = VariantUtils.CreateFrom<Label>(ref previewStatusLabel);
				return true;
			}
			if ((ref name) == PropertyName.previewGraph)
			{
				value = VariantUtils.CreateFrom<NIntentGraph>(ref previewGraph);
				return true;
			}
			if ((ref name) == PropertyName.unsavedChangesDialog)
			{
				value = VariantUtils.CreateFrom<ConfirmationDialog>(ref unsavedChangesDialog);
				return true;
			}
			if ((ref name) == PropertyName.messageDialog)
			{
				value = VariantUtils.CreateFrom<AcceptDialog>(ref messageDialog);
				return true;
			}
			if ((ref name) == PropertyName.windowPanel)
			{
				value = VariantUtils.CreateFrom<PanelContainer>(ref windowPanel);
				return true;
			}
			if ((ref name) == PropertyName.editorTabs)
			{
				value = VariantUtils.CreateFrom<TabContainer>(ref editorTabs);
				return true;
			}
			if ((ref name) == PropertyName.monsterDisplayName)
			{
				value = VariantUtils.CreateFrom<string>(ref monsterDisplayName);
				return true;
			}
			if ((ref name) == PropertyName.monsterModelFullName)
			{
				value = VariantUtils.CreateFrom<string>(ref monsterModelFullName);
				return true;
			}
			if ((ref name) == PropertyName.loadedIntentStringsLanguage)
			{
				value = VariantUtils.CreateFrom<string>(ref loadedIntentStringsLanguage);
				return true;
			}
			if ((ref name) == PropertyName.selectedVariantIndex)
			{
				value = VariantUtils.CreateFrom<int>(ref selectedVariantIndex);
				return true;
			}
			if ((ref name) == PropertyName.isReady)
			{
				value = VariantUtils.CreateFrom<bool>(ref isReady);
				return true;
			}
			if ((ref name) == PropertyName.isRefreshingUi)
			{
				value = VariantUtils.CreateFrom<bool>(ref isRefreshingUi);
				return true;
			}
			if ((ref name) == PropertyName.hasUnsavedChanges)
			{
				value = VariantUtils.CreateFrom<bool>(ref hasUnsavedChanges);
				return true;
			}
			if ((ref name) == PropertyName.pendingAction)
			{
				value = VariantUtils.CreateFrom<PendingAction>(ref pendingAction);
				return true;
			}
			if ((ref name) == PropertyName.lastActiveTextInput)
			{
				value = VariantUtils.CreateFrom<Control>(ref lastActiveTextInput);
				return true;
			}
			return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0187: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_020b: Unknown result type (might be due to invalid IL or missing references)
			//IL_022c: Unknown result type (might be due to invalid IL or missing references)
			//IL_024d: Unknown result type (might be due to invalid IL or missing references)
			//IL_026e: Unknown result type (might be due to invalid IL or missing references)
			//IL_028f: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0313: Unknown result type (might be due to invalid IL or missing references)
			//IL_0334: Unknown result type (might be due to invalid IL or missing references)
			//IL_0355: Unknown result type (might be due to invalid IL or missing references)
			//IL_0376: Unknown result type (might be due to invalid IL or missing references)
			//IL_0397: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_03fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_041b: Unknown result type (might be due to invalid IL or missing references)
			//IL_043b: Unknown result type (might be due to invalid IL or missing references)
			//IL_045b: Unknown result type (might be due to invalid IL or missing references)
			//IL_047b: Unknown result type (might be due to invalid IL or missing references)
			//IL_049b: Unknown result type (might be due to invalid IL or missing references)
			//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_04db: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_051b: Unknown result type (might be due to invalid IL or missing references)
			//IL_053c: Unknown result type (might be due to invalid IL or missing references)
			//IL_055c: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, PropertyName.monsterNameLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.monsterModelLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.headerStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.variantList, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.stateIdList, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.addVariantButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.duplicateVariantButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.deleteVariantButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.moveVariantUpButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.moveVariantDownButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.saveButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.reloadButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.closeButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.conditionEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.conditionStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.upToDateConditionEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.upToDateConditionStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.offsetXEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.offsetYEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.secondaryInitialStatesEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.stateMachineEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.moveReplacementsEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.graphPatchEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.intentStringsStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.intentStringsEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.readOnlySummaryEdit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.previewStatusLabel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.previewGraph, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.unsavedChangesDialog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.messageDialog, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.windowPanel, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.editorTabs, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)4, PropertyName.monsterDisplayName, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)4, PropertyName.monsterModelFullName, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)4, PropertyName.loadedIntentStringsLanguage, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)2, PropertyName.selectedVariantIndex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.isReady, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.isRefreshingUi, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.hasUnsavedChanges, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)2, PropertyName.pendingAction, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.lastActiveTextInput, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.HasUnsavedChanges, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0189: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_020d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0223: Unknown result type (might be due to invalid IL or missing references)
			//IL_0239: Unknown result type (might be due to invalid IL or missing references)
			//IL_024f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0265: Unknown result type (might be due to invalid IL or missing references)
			//IL_027b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0291: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0315: Unknown result type (might be due to invalid IL or missing references)
			//IL_032b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0341: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_036d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0383: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName.monsterNameLabel, Variant.From<Label>(ref monsterNameLabel));
			info.AddProperty(PropertyName.monsterModelLabel, Variant.From<Label>(ref monsterModelLabel));
			info.AddProperty(PropertyName.headerStatusLabel, Variant.From<Label>(ref headerStatusLabel));
			info.AddProperty(PropertyName.variantList, Variant.From<ItemList>(ref variantList));
			info.AddProperty(PropertyName.stateIdList, Variant.From<ItemList>(ref stateIdList));
			info.AddProperty(PropertyName.addVariantButton, Variant.From<Button>(ref addVariantButton));
			info.AddProperty(PropertyName.duplicateVariantButton, Variant.From<Button>(ref duplicateVariantButton));
			info.AddProperty(PropertyName.deleteVariantButton, Variant.From<Button>(ref deleteVariantButton));
			info.AddProperty(PropertyName.moveVariantUpButton, Variant.From<Button>(ref moveVariantUpButton));
			info.AddProperty(PropertyName.moveVariantDownButton, Variant.From<Button>(ref moveVariantDownButton));
			info.AddProperty(PropertyName.saveButton, Variant.From<Button>(ref saveButton));
			info.AddProperty(PropertyName.reloadButton, Variant.From<Button>(ref reloadButton));
			info.AddProperty(PropertyName.closeButton, Variant.From<Button>(ref closeButton));
			info.AddProperty(PropertyName.conditionEdit, Variant.From<LineEdit>(ref conditionEdit));
			info.AddProperty(PropertyName.conditionStatusLabel, Variant.From<Label>(ref conditionStatusLabel));
			info.AddProperty(PropertyName.upToDateConditionEdit, Variant.From<LineEdit>(ref upToDateConditionEdit));
			info.AddProperty(PropertyName.upToDateConditionStatusLabel, Variant.From<Label>(ref upToDateConditionStatusLabel));
			info.AddProperty(PropertyName.offsetXEdit, Variant.From<LineEdit>(ref offsetXEdit));
			info.AddProperty(PropertyName.offsetYEdit, Variant.From<LineEdit>(ref offsetYEdit));
			info.AddProperty(PropertyName.secondaryInitialStatesEdit, Variant.From<CodeEdit>(ref secondaryInitialStatesEdit));
			info.AddProperty(PropertyName.stateMachineEdit, Variant.From<CodeEdit>(ref stateMachineEdit));
			info.AddProperty(PropertyName.moveReplacementsEdit, Variant.From<CodeEdit>(ref moveReplacementsEdit));
			info.AddProperty(PropertyName.graphPatchEdit, Variant.From<CodeEdit>(ref graphPatchEdit));
			info.AddProperty(PropertyName.intentStringsStatusLabel, Variant.From<Label>(ref intentStringsStatusLabel));
			info.AddProperty(PropertyName.intentStringsEdit, Variant.From<CodeEdit>(ref intentStringsEdit));
			info.AddProperty(PropertyName.readOnlySummaryEdit, Variant.From<CodeEdit>(ref readOnlySummaryEdit));
			info.AddProperty(PropertyName.previewStatusLabel, Variant.From<Label>(ref previewStatusLabel));
			info.AddProperty(PropertyName.previewGraph, Variant.From<NIntentGraph>(ref previewGraph));
			info.AddProperty(PropertyName.unsavedChangesDialog, Variant.From<ConfirmationDialog>(ref unsavedChangesDialog));
			info.AddProperty(PropertyName.messageDialog, Variant.From<AcceptDialog>(ref messageDialog));
			info.AddProperty(PropertyName.windowPanel, Variant.From<PanelContainer>(ref windowPanel));
			info.AddProperty(PropertyName.editorTabs, Variant.From<TabContainer>(ref editorTabs));
			info.AddProperty(PropertyName.monsterDisplayName, Variant.From<string>(ref monsterDisplayName));
			info.AddProperty(PropertyName.monsterModelFullName, Variant.From<string>(ref monsterModelFullName));
			info.AddProperty(PropertyName.loadedIntentStringsLanguage, Variant.From<string>(ref loadedIntentStringsLanguage));
			info.AddProperty(PropertyName.selectedVariantIndex, Variant.From<int>(ref selectedVariantIndex));
			info.AddProperty(PropertyName.isReady, Variant.From<bool>(ref isReady));
			info.AddProperty(PropertyName.isRefreshingUi, Variant.From<bool>(ref isRefreshingUi));
			info.AddProperty(PropertyName.hasUnsavedChanges, Variant.From<bool>(ref hasUnsavedChanges));
			info.AddProperty(PropertyName.pendingAction, Variant.From<PendingAction>(ref pendingAction));
			info.AddProperty(PropertyName.lastActiveTextInput, Variant.From<Control>(ref lastActiveTextInput));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			((GodotObject)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName.monsterNameLabel, ref val))
			{
				monsterNameLabel = ((Variant)(ref val)).As<Label>();
			}
			Variant val2 = default(Variant);
			if (info.TryGetProperty(PropertyName.monsterModelLabel, ref val2))
			{
				monsterModelLabel = ((Variant)(ref val2)).As<Label>();
			}
			Variant val3 = default(Variant);
			if (info.TryGetProperty(PropertyName.headerStatusLabel, ref val3))
			{
				headerStatusLabel = ((Variant)(ref val3)).As<Label>();
			}
			Variant val4 = default(Variant);
			if (info.TryGetProperty(PropertyName.variantList, ref val4))
			{
				variantList = ((Variant)(ref val4)).As<ItemList>();
			}
			Variant val5 = default(Variant);
			if (info.TryGetProperty(PropertyName.stateIdList, ref val5))
			{
				stateIdList = ((Variant)(ref val5)).As<ItemList>();
			}
			Variant val6 = default(Variant);
			if (info.TryGetProperty(PropertyName.addVariantButton, ref val6))
			{
				addVariantButton = ((Variant)(ref val6)).As<Button>();
			}
			Variant val7 = default(Variant);
			if (info.TryGetProperty(PropertyName.duplicateVariantButton, ref val7))
			{
				duplicateVariantButton = ((Variant)(ref val7)).As<Button>();
			}
			Variant val8 = default(Variant);
			if (info.TryGetProperty(PropertyName.deleteVariantButton, ref val8))
			{
				deleteVariantButton = ((Variant)(ref val8)).As<Button>();
			}
			Variant val9 = default(Variant);
			if (info.TryGetProperty(PropertyName.moveVariantUpButton, ref val9))
			{
				moveVariantUpButton = ((Variant)(ref val9)).As<Button>();
			}
			Variant val10 = default(Variant);
			if (info.TryGetProperty(PropertyName.moveVariantDownButton, ref val10))
			{
				moveVariantDownButton = ((Variant)(ref val10)).As<Button>();
			}
			Variant val11 = default(Variant);
			if (info.TryGetProperty(PropertyName.saveButton, ref val11))
			{
				saveButton = ((Variant)(ref val11)).As<Button>();
			}
			Variant val12 = default(Variant);
			if (info.TryGetProperty(PropertyName.reloadButton, ref val12))
			{
				reloadButton = ((Variant)(ref val12)).As<Button>();
			}
			Variant val13 = default(Variant);
			if (info.TryGetProperty(PropertyName.closeButton, ref val13))
			{
				closeButton = ((Variant)(ref val13)).As<Button>();
			}
			Variant val14 = default(Variant);
			if (info.TryGetProperty(PropertyName.conditionEdit, ref val14))
			{
				conditionEdit = ((Variant)(ref val14)).As<LineEdit>();
			}
			Variant val15 = default(Variant);
			if (info.TryGetProperty(PropertyName.conditionStatusLabel, ref val15))
			{
				conditionStatusLabel = ((Variant)(ref val15)).As<Label>();
			}
			Variant val16 = default(Variant);
			if (info.TryGetProperty(PropertyName.upToDateConditionEdit, ref val16))
			{
				upToDateConditionEdit = ((Variant)(ref val16)).As<LineEdit>();
			}
			Variant val17 = default(Variant);
			if (info.TryGetProperty(PropertyName.upToDateConditionStatusLabel, ref val17))
			{
				upToDateConditionStatusLabel = ((Variant)(ref val17)).As<Label>();
			}
			Variant val18 = default(Variant);
			if (info.TryGetProperty(PropertyName.offsetXEdit, ref val18))
			{
				offsetXEdit = ((Variant)(ref val18)).As<LineEdit>();
			}
			Variant val19 = default(Variant);
			if (info.TryGetProperty(PropertyName.offsetYEdit, ref val19))
			{
				offsetYEdit = ((Variant)(ref val19)).As<LineEdit>();
			}
			Variant val20 = default(Variant);
			if (info.TryGetProperty(PropertyName.secondaryInitialStatesEdit, ref val20))
			{
				secondaryInitialStatesEdit = ((Variant)(ref val20)).As<CodeEdit>();
			}
			Variant val21 = default(Variant);
			if (info.TryGetProperty(PropertyName.stateMachineEdit, ref val21))
			{
				stateMachineEdit = ((Variant)(ref val21)).As<CodeEdit>();
			}
			Variant val22 = default(Variant);
			if (info.TryGetProperty(PropertyName.moveReplacementsEdit, ref val22))
			{
				moveReplacementsEdit = ((Variant)(ref val22)).As<CodeEdit>();
			}
			Variant val23 = default(Variant);
			if (info.TryGetProperty(PropertyName.graphPatchEdit, ref val23))
			{
				graphPatchEdit = ((Variant)(ref val23)).As<CodeEdit>();
			}
			Variant val24 = default(Variant);
			if (info.TryGetProperty(PropertyName.intentStringsStatusLabel, ref val24))
			{
				intentStringsStatusLabel = ((Variant)(ref val24)).As<Label>();
			}
			Variant val25 = default(Variant);
			if (info.TryGetProperty(PropertyName.intentStringsEdit, ref val25))
			{
				intentStringsEdit = ((Variant)(ref val25)).As<CodeEdit>();
			}
			Variant val26 = default(Variant);
			if (info.TryGetProperty(PropertyName.readOnlySummaryEdit, ref val26))
			{
				readOnlySummaryEdit = ((Variant)(ref val26)).As<CodeEdit>();
			}
			Variant val27 = default(Variant);
			if (info.TryGetProperty(PropertyName.previewStatusLabel, ref val27))
			{
				previewStatusLabel = ((Variant)(ref val27)).As<Label>();
			}
			Variant val28 = default(Variant);
			if (info.TryGetProperty(PropertyName.previewGraph, ref val28))
			{
				previewGraph = ((Variant)(ref val28)).As<NIntentGraph>();
			}
			Variant val29 = default(Variant);
			if (info.TryGetProperty(PropertyName.unsavedChangesDialog, ref val29))
			{
				unsavedChangesDialog = ((Variant)(ref val29)).As<ConfirmationDialog>();
			}
			Variant val30 = default(Variant);
			if (info.TryGetProperty(PropertyName.messageDialog, ref val30))
			{
				messageDialog = ((Variant)(ref val30)).As<AcceptDialog>();
			}
			Variant val31 = default(Variant);
			if (info.TryGetProperty(PropertyName.windowPanel, ref val31))
			{
				windowPanel = ((Variant)(ref val31)).As<PanelContainer>();
			}
			Variant val32 = default(Variant);
			if (info.TryGetProperty(PropertyName.editorTabs, ref val32))
			{
				editorTabs = ((Variant)(ref val32)).As<TabContainer>();
			}
			Variant val33 = default(Variant);
			if (info.TryGetProperty(PropertyName.monsterDisplayName, ref val33))
			{
				monsterDisplayName = ((Variant)(ref val33)).As<string>();
			}
			Variant val34 = default(Variant);
			if (info.TryGetProperty(PropertyName.monsterModelFullName, ref val34))
			{
				monsterModelFullName = ((Variant)(ref val34)).As<string>();
			}
			Variant val35 = default(Variant);
			if (info.TryGetProperty(PropertyName.loadedIntentStringsLanguage, ref val35))
			{
				loadedIntentStringsLanguage = ((Variant)(ref val35)).As<string>();
			}
			Variant val36 = default(Variant);
			if (info.TryGetProperty(PropertyName.selectedVariantIndex, ref val36))
			{
				selectedVariantIndex = ((Variant)(ref val36)).As<int>();
			}
			Variant val37 = default(Variant);
			if (info.TryGetProperty(PropertyName.isReady, ref val37))
			{
				isReady = ((Variant)(ref val37)).As<bool>();
			}
			Variant val38 = default(Variant);
			if (info.TryGetProperty(PropertyName.isRefreshingUi, ref val38))
			{
				isRefreshingUi = ((Variant)(ref val38)).As<bool>();
			}
			Variant val39 = default(Variant);
			if (info.TryGetProperty(PropertyName.hasUnsavedChanges, ref val39))
			{
				hasUnsavedChanges = ((Variant)(ref val39)).As<bool>();
			}
			Variant val40 = default(Variant);
			if (info.TryGetProperty(PropertyName.pendingAction, ref val40))
			{
				pendingAction = ((Variant)(ref val40)).As<PendingAction>();
			}
			Variant val41 = default(Variant);
			if (info.TryGetProperty(PropertyName.lastActiveTextInput, ref val41))
			{
				lastActiveTextInput = ((Variant)(ref val41)).As<Control>();
			}
		}
	}
	[ScriptPath("res://intentgraph2/src/Scenes/NIntentGraphPanel.cs")]
	public class NIntentGraphPanel : MarginContainer
	{
		public class MethodName : MethodName
		{
			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

			public static readonly StringName _GuiInput = StringName.op_Implicit("_GuiInput");

			public static readonly StringName _Input = StringName.op_Implicit("_Input");

			public static readonly StringName OnPinButtonToggled = StringName.op_Implicit("OnPinButtonToggled");

			public static readonly StringName OnCloseButtonPressed = StringName.op_Implicit("OnCloseButtonPressed");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName NCreature = StringName.op_Implicit("NCreature");

			public static readonly StringName Pinned = StringName.op_Implicit("Pinned");

			public static readonly StringName dragging = StringName.op_Implicit("dragging");

			public static readonly StringName dragOffset = StringName.op_Implicit("dragOffset");

			public static readonly StringName pinButton = StringName.op_Implicit("pinButton");

			public static readonly StringName closeButton = StringName.op_Implicit("closeButton");
		}

		public class SignalName : SignalName
		{
		}

		private bool dragging;

		private Vector2 dragOffset = Vector2.Zero;

		private Button? pinButton;

		private Button? closeButton;

		public NCreature? NCreature { get; set; }

		public bool Pinned
		{
			get
			{
				Button? obj = pinButton;
				if (obj == null)
				{
					return false;
				}
				return ((BaseButton)obj).ButtonPressed;
			}
		}

		public override void _Ready()
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Expected O, but got Unknown
			if (IntentGraphMod.Config.PinableIntentGraph)
			{
				((CanvasItem)((Node)this).GetNode<BoxContainer>(NodePath.op_Implicit("%ButtonContainer"))).Show();
				((Control)this).MouseFilter = (MouseFilterEnum)1;
			}
			pinButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%PinButton"));
			closeButton = ((Node)this).GetNode<Button>(NodePath.op_Implicit("%CloseButton"));
			((BaseButton)pinButton).Toggled += new ToggledEventHandler(OnPinButtonToggled);
			((BaseButton)closeButton).Pressed += OnCloseButtonPressed;
		}

		public override void _GuiInput(InputEvent evt)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Invalid comparison between Unknown and I8
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			InputEventMouseButton val = (InputEventMouseButton)(object)((evt is InputEventMouseButton) ? evt : null);
			if (val != null && (long)val.ButtonIndex == 1)
			{
				if (val.Pressed)
				{
					((CanvasItem)this).MoveToFront();
					dragging = true;
					dragOffset = ((CanvasItem)this).GetGlobalMousePosition() - ((Control)this).GlobalPosition;
				}
				else
				{
					dragging = false;
				}
			}
			if (evt is InputEventMouseMotion && dragging)
			{
				((Control)this).GlobalPosition = ((CanvasItem)this).GetGlobalMousePosition() - dragOffset;
			}
		}

		public override void _Input(InputEvent evt)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Invalid comparison between Unknown and I8
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			InputEventKey val = (InputEventKey)(object)((evt is InputEventKey) ? evt : null);
			if (val != null && ((InputEvent)val).IsPressed() && IntentGraphMod.Config.ToggleIntentGraphKey == val.Keycode)
			{
				IntentGraphHost.ToggleIntentGraphVisibility();
				((Node)this).GetViewport().SetInputAsHandled();
			}
			InputEventMouseButton val2 = (InputEventMouseButton)(object)((evt is InputEventMouseButton) ? evt : null);
			if (val2 != null && (long)val2.ButtonIndex == 1 && val2.Pressed && NCreature != null && !Pinned)
			{
				Vector2 position = ((InputEventMouse)(InputEventMouseButton)((CanvasItem)this).MakeInputLocal(evt)).Position;
				Rect2 val3 = default(Rect2);
				((Rect2)(ref val3))..ctor(Vector2.Zero, ((Control)this).Size);
				if (!((Rect2)(ref val3)).HasPoint(position))
				{
					IntentGraphHost.Remove(NCreature);
				}
			}
		}

		private void OnPinButtonToggled(bool toggledOn)
		{
			if (pinButton != null)
			{
				if (toggledOn)
				{
					pinButton.Icon = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/unpin.png", (string)null, (CacheMode)1);
				}
				else
				{
					pinButton.Icon = ResourceLoader.Load<Texture2D>("res://intentgraph2/images/ui/pin.png", (string)null, (CacheMode)1);
				}
			}
		}

		private void OnCloseButtonPressed()
		{
			if (NCreature != null)
			{
				IntentGraphHost.Remove(NCreature);
			}
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Expected O, but got Unknown
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Expected O, but got Unknown
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0163: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(5)
			{
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName._GuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("evt"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("evt"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnPinButtonToggled, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)1, StringName.op_Implicit("toggledOn"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName.OnCloseButtonPressed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((Node)this)._Ready();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._GuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				((Control)this)._GuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnPinButtonToggled && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				OnPinButtonToggled(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnCloseButtonPressed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				OnCloseButtonPressed();
				ret = default(godot_variant);
				return true;
			}
			return ((MarginContainer)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName._Ready)
			{
				return true;
			}
			if ((ref method) == MethodName._GuiInput)
			{
				return true;
			}
			if ((ref method) == MethodName._Input)
			{
				return true;
			}
			if ((ref method) == MethodName.OnPinButtonToggled)
			{
				return true;
			}
			if ((ref method) == MethodName.OnCloseButtonPressed)
			{
				return true;
			}
			return ((MarginContainer)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.NCreature)
			{
				NCreature = VariantUtils.ConvertTo<NCreature>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.dragging)
			{
				dragging = VariantUtils.ConvertTo<bool>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.dragOffset)
			{
				dragOffset = VariantUtils.ConvertTo<Vector2>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.pinButton)
			{
				pinButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			if ((ref name) == PropertyName.closeButton)
			{
				closeButton = VariantUtils.ConvertTo<Button>(ref value);
				return true;
			}
			return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName.NCreature)
			{
				NCreature nCreature = NCreature;
				value = VariantUtils.CreateFrom<NCreature>(ref nCreature);
				return true;
			}
			if ((ref name) == PropertyName.Pinned)
			{
				bool pinned = Pinned;
				value = VariantUtils.CreateFrom<bool>(ref pinned);
				return true;
			}
			if ((ref name) == PropertyName.dragging)
			{
				value = VariantUtils.CreateFrom<bool>(ref dragging);
				return true;
			}
			if ((ref name) == PropertyName.dragOffset)
			{
				value = VariantUtils.CreateFrom<Vector2>(ref dragOffset);
				return true;
			}
			if ((ref name) == PropertyName.pinButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref pinButton);
				return true;
			}
			if ((ref name) == PropertyName.closeButton)
			{
				value = VariantUtils.CreateFrom<Button>(ref closeButton);
				return true;
			}
			return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, PropertyName.dragging, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)5, PropertyName.dragOffset, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.pinButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.closeButton, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)24, PropertyName.NCreature, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
				new PropertyInfo((Type)1, PropertyName.Pinned, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).SaveGodotObjectData(info);
			StringName nCreature = PropertyName.NCreature;
			NCreature nCreature2 = NCreature;
			info.AddProperty(nCreature, Variant.From<NCreature>(ref nCreature2));
			info.AddProperty(PropertyName.dragging, Variant.From<bool>(ref dragging));
			info.AddProperty(PropertyName.dragOffset, Variant.From<Vector2>(ref dragOffset));
			info.AddProperty(PropertyName.pinButton, Variant.From<Button>(ref pinButton));
			info.AddProperty(PropertyName.closeButton, Variant.From<Button>(ref closeButton));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			((GodotObject)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName.NCreature, ref val))
			{
				NCreature = ((Variant)(ref val)).As<NCreature>();
			}
			Variant val2 = default(Variant);
			if (info.TryGetProperty(PropertyName.dragging, ref val2))
			{
				dragging = ((Variant)(ref val2)).As<bool>();
			}
			Variant val3 = default(Variant);
			if (info.TryGetProperty(PropertyName.dragOffset, ref val3))
			{
				dragOffset = ((Variant)(ref val3)).As<Vector2>();
			}
			Variant val4 = default(Variant);
			if (info.TryGetProperty(PropertyName.pinButton, ref val4))
			{
				pinButton = ((Variant)(ref val4)).As<Button>();
			}
			Variant val5 = default(Variant);
			if (info.TryGetProperty(PropertyName.closeButton, ref val5))
			{
				closeButton = ((Variant)(ref val5)).As<Button>();
			}
		}
	}
}
namespace IntentGraph2.Patches
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	public class DevConsoleConstructorPatch
	{
		public static void Postfix(DevConsole __instance)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			new Traverse((object)__instance).Method("RegisterCommand", new Type[1] { typeof(AbstractConsoleCmd) }, (object[])null).GetValue(new object[1]
			{
				new ReloadIntentsConsoleCmd()
			});
			new Traverse((object)__instance).Method("RegisterCommand", new Type[1] { typeof(AbstractConsoleCmd) }, (object[])null).GetValue(new object[1]
			{
				new EditIntentConsoleCmd()
			});
			IgLogger.Info("Registered reloadintents to DevConsole.");
			IgLogger.Info("Registered editintent to DevConsole.");
		}
	}
	[HarmonyPatch(typeof(LocManager), "SetLanguage")]
	public class LocManagerSetLanguagePatch
	{
		public static void Postfix(LocManager __instance, string language)
		{
			IntentGraphMod.LoadIntentStrings(language);
		}
	}
	[HarmonyPatch(typeof(CombatManager), "AfterCreatureAdded")]
	public class MonsterSetupPatch
	{
		public static void Postfix(CombatManager __instance, Creature creature)
		{
			if (creature.IsMonster)
			{
				IntentGraphGenerator.GenerateAndCacheGraphForCreature(creature);
			}
		}
	}
	[HarmonyPatch(typeof(LocManager), "Initialize")]
	public class PostModInitializePatch
	{
		public static void Prefix()
		{
			IntentGraphMod.PostInitializeMod();
		}
	}
	public class ShowIntentGraphPatches
	{
		[HarmonyPatch(typeof(NCreature), "OnFocus")]
		public static class OnFocusPatch
		{
			public static void Postfix(NCreature __instance)
			{
				IntentGraphHost.Create(__instance);
			}
		}

		[HarmonyPatch(typeof(NCreature), "OnUnfocus")]
		public static class OnUnfocusPatch
		{
			public static void Prefix(NCreature __instance)
			{
				if (!IntentGraphMod.Config.PinableIntentGraph)
				{
					IntentGraphHost.Remove(__instance);
				}
			}
		}

		[HarmonyPatch(typeof(NCreature), "_ExitTree")]
		public static class ExitTreePatch
		{
			public static void Prefix(NCreature __instance)
			{
				IntentGraphHost.Remove(__instance);
			}
		}
	}
	public class StateLogPatches
	{
		[HarmonyPatch(typeof(MonsterMoveStateMachine), "SetCurrentState")]
		public static class SetCurrentStatePatch
		{
			public static void Prefix(MonsterMoveStateMachine __instance, MonsterState state)
			{
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				//IL_003c: Invalid comparison between Unknown and I4
				if (!state.ShouldAppearInLogs)
				{
					return;
				}
				MonsterState val = state;
				MoveState val2 = (MoveState)(object)((state is MoveState) ? state : null);
				if (val2 != null && val2.Intents.Count == 1 && (int)val2.Intents[0].IntentType == 10)
				{
					StackFrame stackFrame = new StackTrace().GetFrames().Where(delegate(StackFrame t)
					{
						Type type = t.GetMethod()?.DeclaringType;
						return (object)type != null && type.IsAssignableTo(typeof(IAsyncStateMachine)) && (object)type != null && type.DeclaringType?.IsAssignableTo(typeof(AbstractModel)) == true;
					}).FirstOrDefault();
					int num;
					if (stackFrame != null)
					{
						MethodBase? method = stackFrame.GetMethod();
						num = (((object)method != null && method.DeclaringType?.DeclaringType?.IsAssignableTo(typeof(CardModel)) == true) ? 1 : 0);
					}
					else
					{
						num = 0;
					}
					bool flag = false;
					if (num == 0)
					{
						List<MonsterState> list = __instance.States.Values.Where(delegate(MonsterState s)
						{
							//IL_0024: Unknown result type (might be due to invalid IL or missing references)
							//IL_002b: Invalid comparison between Unknown and I4
							MoveState val3 = (MoveState)(object)((s is MoveState) ? s : null);
							return val3 != null && val3.Intents.Count == 1 && (int)val3.Intents[0].IntentType == 10;
						}).ToList();
						if (list.Count == 1)
						{
							val = list[0];
							flag = true;
						}
					}
					if (!flag)
					{
						val = (MonsterState)(object)NormalStun;
					}
				}
				List<MonsterState> orCreateValue = FullStateLog.GetOrCreateValue(__instance);
				if (orCreateValue.Count >= 2)
				{
					if ((object)orCreateValue[orCreateValue.Count - 1] == NormalStun)
					{
						if (orCreateValue[orCreateValue.Count - 2] == val)
						{
							orCreateValue.RemoveAt(orCreateValue.Count - 1);
							return;
						}
					}
				}
				if (orCreateValue.Count >= 1)
				{
					if ((object)orCreateValue[orCreateValue.Count - 1] == NormalStun)
					{
						orCreateValue[orCreateValue.Count - 1] = val;
						return;
					}
				}
				orCreateValue.Add(val);
			}
		}

		[HarmonyPatch(/*Could not decode attribute arguments.*/)]
		public static class StateMachineConstructorPatch
		{
			public static void Postfix(MonsterMoveStateMachine __instance, IEnumerable<MonsterState> states, MonsterState initialState)
			{
				List<MonsterState> list = new List<MonsterState>();
				FullStateLog.Add(__instance, list);
				if (initialState.ShouldAppearInLogs)
				{
					list.Add(initialState);
				}
			}
		}

		public static readonly ConditionalWeakTable<MonsterMoveStateMachine, List<MonsterState>> FullStateLog = new ConditionalWeakTable<MonsterMoveStateMachine, List<MonsterState>>();

		private static readonly MoveState NormalStun = new MoveState("NormalStun", (Func<IReadOnlyList<Creature>, Task>)((IReadOnlyList<Creature> c) => Task.CompletedTask), (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new StunIntent() });
	}
}
namespace IntentGraph2.Models
{
	public class Graph
	{
		public float Width { get; set; } = 1f;

		public float Height { get; set; } = 1f;

		public List<Icon> Icons { get; set; } = new List<Icon>();

		public List<IconGroup> IconGroups { get; set; } = new List<IconGroup>();

		public List<Label> Labels { get; set; } = new List<Label>();

		public List<Arrow> Arrows { get; set; } = new List<Arrow>();

		public List<Move> Moves { get; set; } = new List<Move>();

		[JsonIgnore]
		public string? Warning { get; set; }

		[JsonIgnore]
		public IRule? Condition { get; set; }

		public bool Expand { get; set; }
	}
	public record Icon(float X = 0f, float Y = 0f, IntentType IntentType = (IntentType)7, int? Value = null, int Times = 1, string ValueText = "", string TimesText = "", string? RelativeTo = null) : IRelativeToPosition
	{
		[CompilerGenerated]
		public void Deconstruct(out float X, out float Y, out IntentType IntentType, out int? Value, out int Times, out string ValueText, out string TimesText, out string? RelativeTo)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Expected I4, but got Unknown
			X = this.X;
			Y = this.Y;
			IntentType = (IntentType)(int)this.IntentType;
			Value = this.Value;
			Times = this.Times;
			ValueText = this.ValueText;
			TimesText = this.TimesText;
			RelativeTo = this.RelativeTo;
		}
	}
	public record IconGroup(float X = 0f, float Y = 0f, float Width = 1f, float Height = 1f, string? RelativeTo = null) : IRelativeToPosition;
	public record Label(float X = 0f, float Y = 0f, string Text = "", string Align = "left", int FontSize = 18, string? RelativeTo = null) : IRelativeToPosition;
	public record Arrow(float[] Path, string? RelativeTo = null);
	public record Move(string Id, string[]? Ids = null, float X = 0f, float Y = 0f, Icon[]? Icons = null, int?[]? PossiblePreviousMoveNodeIndices = null, string? RelativeTo = null) : IRelativeToPosition;
	public interface IRelativeToPosition
	{
		float X { get; }

		float Y { get; }

		string? RelativeTo { get; }
	}
	public class IntentDefinitionList : List<IntentDefinition>
	{
		public (IntentDefinition?, IRule?) FindFirstMatchCondition(MonsterModel monster)
		{
			foreach (IntentDefinition item in Enumerable.Reverse(this))
			{
				try
				{
					IRule rule = IRule.Parse(item.Condition, new IntentGraph2.Utils.Rule.RuleContext(monster));
					if (rule != null && rule.GetBool())
					{
						return (item, rule);
					}
				}
				catch (Exception ex)
				{
					IgLogger.Warn($"Error parsing condition '{item.Condition}' for monster '{((AbstractModel)monster).Id}': {ex.Message}");
				}
			}
			return (null, null);
		}
	}
	public class IntentDefinition
	{
		public string Condition { get; set; } = "true";

		public string? UpToDateCondition { get; set; }

		public SecondaryInitialState[]? SecondaryInitialStates { get; set; }

		public Graph? Graph { get; set; }

		public Graph? GraphPatch { get; set; }

		public StateMachineNode[]? StateMachine { get; set; }

		public Dictionary<string, MoveReplacement>? MoveReplacements { get; set; }

		public Position Offset { get; set; }
	}
	public class StateMachineNode
	{
		public string Name { get; set; } = string.Empty;

		public string? MoveName { get; set; }

		public string[]? AlternativeMoveNames { get; set; }

		public bool IsInitialState { get; set; }

		public int InitialStatePriority { get; set; }

		public StateMachinNodeChildren[]? Children { get; set; }

		public string? FollowUpState { get; set; }

		public bool HorizontalLayout { get; set; }

		public int PlaceholderIntentCount { get; set; }

		public bool NotSimpleLoopStart { get; set; }

		public Position Offset { get; set; }
	}
	public record StateMachinNodeChildren(string Label = "", StateMachineNode? Node = null);
	[JsonConverter(typeof(MoveReplacementJsonConverter))]
	public record MoveReplacement(IntentOverride[]? IntentOverrides, ArrowOverride? ArrowOverride);
	public record IntentOverride(string? ValueText, string? TimesText);
	public record ArrowOverride(float?[] Path);
	[JsonConverter(typeof(SecondaryInitialStateJsonConverter))]
	public record SecondaryInitialState(string Id, Position Offset = default(Position));
	public record struct Position(float X = 0f, float Y = 0f);
}
namespace IntentGraph2.DevConsole
{
	public class EditIntentConsoleCmd : AbstractConsoleCmd
	{
		public override string CmdName => "editintent";

		public override string Args => "<monster model full name>";

		public override string Description => "Open the intent graph editor for the given monster model in the current combat";

		public override bool IsNetworked => false;

		public override CmdResult Process(Player? issuingPlayer, string[] args)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			string monsterModelFullName = string.Join(" ", args).Trim();
			if (string.IsNullOrWhiteSpace(monsterModelFullName))
			{
				return new CmdResult(false, "Monster model full name is required.");
			}
			CombatState val = CombatManager.Instance.DebugOnlyGetState();
			if (val == null || val.Encounter == null)
			{
				return new CmdResult(false, "You must be in combat to edit an intent graph.");
			}
			Creature val2 = ((IEnumerable<Creature>)val.Enemies).FirstOrDefault((Func<Creature, bool>)delegate(Creature enemy)
			{
				MonsterModel monster = enemy.Monster;
				return monster != null && ((object)monster).GetType().FullName?.Equals(monsterModelFullName, StringComparison.OrdinalIgnoreCase) == true;
			});
			if (((val2 != null) ? val2.Monster : null) == null)
			{
				string[] array = (from name in (from enemy in val.Enemies
						select ((object)enemy.Monster)?.GetType().FullName into name
						where !string.IsNullOrWhiteSpace(name)
						select name).Distinct<string>(StringComparer.OrdinalIgnoreCase)
					orderby name
					select name).ToArray();
				string text = ((array.Length == 0) ? ("Monster model '" + monsterModelFullName + "' is not active in this combat.") : ("Monster model '" + monsterModelFullName + "' is not active in this combat. Active models: " + string.Join(", ", array)));
				return new CmdResult(false, text);
			}
			if (!IntentGraphEditorHost.TryOpenEditor(val2.Monster, val2.Name, out string message))
			{
				return new CmdResult(false, message);
			}
			return new CmdResult(true, message);
		}

		public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
		{
			if (args.Length <= 1)
			{
				return ((AbstractConsoleCmd)this).CompleteArgument((IEnumerable<string>)GetAvailableMonsterModelNames(), Array.Empty<string>(), args.FirstOrDefault() ?? string.Empty, (CompletionType)2, (Func<string, string, bool>)null);
			}
			return ((AbstractConsoleCmd)this).GetArgumentCompletions(player, args);
		}

		private static string[] GetAvailableMonsterModelNames()
		{
			CombatState val = CombatManager.Instance.DebugOnlyGetState();
			if (((val != null) ? val.Encounter : null) == null)
			{
				return Array.Empty<string>();
			}
			return (from enemy in val.Enemies
				select ((object)enemy.Monster)?.GetType().FullName into name
				where !string.IsNullOrWhiteSpace(name)
				select name).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string name) => name, StringComparer.OrdinalIgnoreCase).ToArray();
		}
	}
	public class ReloadIntentsConsoleCmd : AbstractConsoleCmd
	{
		public override string CmdName => "reloadintents";

		public override string Args => string.Empty;

		public override string Description => "Reload intent graph for developing";

		public override bool IsNetworked => false;

		public override CmdResult Process(Player? issuingPlayer, string[] args)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			IntentGraphMod.ReloadIntentDefinitionsAndGraphs();
			return new CmdResult(true, "Intent graph reloaded");
		}
	}
}
namespace IntentGraph2.Crossovers
{
	public interface IBaseLibHelper
	{
		IntentGraphModConfig Config { get; }

		void RegisterConfig();

		void SaveConfig();
	}
	public interface IRitsuLibHelper
	{
		IntentGraphModConfig Config { get; }

		void RegisterConfig();

		void SaveConfig();
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	int ICollection.Count => _items.Length;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Length;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Length;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlyArray(T[] items)
	{
		_items = items;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_items).GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_items).CopyTo(array, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return ((IList)_items).Contains(value);
	}

	int IList.IndexOf(object? value)
	{
		return ((IList)_items).IndexOf(value);
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)_items).GetEnumerator();
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return ((ICollection<T>)_items).Contains(item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		((ICollection<T>)_items).CopyTo(array, arrayIndex);
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		return ((IList<T>)_items).IndexOf(item);
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlyList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	int ICollection.Count => _items.Count;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => _items.Count;

	T IReadOnlyList<T>.this[int index] => _items[index];

	int ICollection<T>.Count => _items.Count;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			return _items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlyList(List<T> items)
	{
		_items = items;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable)_items).GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		((ICollection)_items).CopyTo(array, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return ((IList)_items).Contains(value);
	}

	int IList.IndexOf(object? value)
	{
		return ((IList)_items).IndexOf(value);
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return ((IEnumerable<T>)_items).GetEnumerator();
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return _items.Contains(item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		_items.CopyTo(array, arrayIndex);
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		return _items.IndexOf(item);
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		object IEnumerator.Current => _item;

		T IEnumerator<T>.Current => _item;

		public Enumerator(T item)
		{
			_item = item;
		}

		bool IEnumerator.MoveNext()
		{
			if (!_moveNextCalled)
			{
				return _moveNextCalled = true;
			}
			return false;
		}

		void IEnumerator.Reset()
		{
			_moveNextCalled = false;
		}

		void IDisposable.Dispose()
		{
		}
	}

	int ICollection.Count => 1;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => 1;

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
	}

	int ICollection<T>.Count => 1;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlySingleElementList(T item)
	{
		_item = item;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(_item, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return EqualityComparer<T>.Default.Equals(_item, (T)value);
	}

	int IList.IndexOf(object? value)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, (T)value))
		{
			return -1;
		}
		return 0;
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(_item, item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		array[arrayIndex] = _item;
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, item))
		{
			return -1;
		}
		return 0;
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '9.1.0.7988')
