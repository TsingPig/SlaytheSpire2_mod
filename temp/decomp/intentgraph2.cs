using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v9.0", FrameworkDisplayName = ".NET 9.0")]
[assembly: AssemblyCompany("intentgraph2")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0+bd0515c7734d50d6bc22fb8510efec153af8cf9e")]
[assembly: AssemblyProduct("intentgraph2")]
[assembly: AssemblyTitle("intentgraph2")]
[assembly: AssemblyVersion("1.0.0.0")]
[module: RefSafetyRules(11)]
namespace IntentGraph2.Initializer;

[ModInitializer("Initialize")]
public class Initializer
{
	private static nint _holder;

	public static void Initialize()
	{
		LogInfo("IntentGraph entry initialize");
		Libgcc();
		LoadDll("Antlr4.Runtime.Standard");
		LoadDll("intentgraph2core")?.GetType("IntentGraph2.IntentGraphMod")?.GetMethod("InitializeMod", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
		LogInfo("IntentGraph entry initialize done.");
	}

	public static Assembly? LoadDll(string dllName)
	{
		try
		{
			Assembly assembly = typeof(Initializer).Assembly;
			AssemblyLoadContext loadContext = AssemblyLoadContext.GetLoadContext(assembly);
			if (loadContext != null)
			{
				string assemblyPath = Path.Join(Path.GetDirectoryName(assembly.Location), dllName + ".dll");
				return loadContext.LoadFromAssemblyPath(assemblyPath);
			}
			LogInfo("Failed to get assembly load context for IntentGraphMod.");
		}
		catch (Exception value)
		{
			LogInfo($"Failed to load dll {dllName}: {value}");
		}
		return null;
	}

	private static void LogInfo(string message)
	{
		Log.Info("[IntentGraph] " + message, 2);
	}

	[DllImport("libdl.so.2")]
	private static extern nint dlopen(string filename, int flags);

	[DllImport("libdl.so.2")]
	private static extern nint dlerror();

	private static void Libgcc()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			LogInfo("Running on Linux, manually dlopen libgcc for Harmony");
			_holder = dlopen("libgcc_s.so.1", 258);
			if (_holder == IntPtr.Zero)
			{
				LogInfo("Or Nor: " + Marshal.PtrToStringAnsi(dlerror()));
			}
		}
	}
}
You are not using the latest version of the tool, please update.
Latest version is '10.1.1.8388' (yours is '9.1.0.7988')
