using System;

namespace Epic.OnlineServices.Mods;

public sealed class ModsInterface : Handle
{
	public const int COPYMODINFO_API_LATEST = 1;

	public const int ENUMERATEMODS_API_LATEST = 1;

	public const int INSTALLMOD_API_LATEST = 1;

	public const int MODINFO_API_LATEST = 1;

	public const int MOD_IDENTIFIER_API_LATEST = 1;

	public const int UNINSTALLMOD_API_LATEST = 1;

	public const int UPDATEMOD_API_LATEST = 1;

	public ModsInterface()
	{
	}

	public ModsInterface(IntPtr innerHandle)
		: base(innerHandle)
	{
	}

	public Result CopyModInfo(ref CopyModInfoOptions options, out ModInfo? outEnumeratedMods)
	{
		CopyModInfoOptionsInternal options2 = default(CopyModInfoOptionsInternal);
		options2.Set(ref options);
		IntPtr outEnumeratedMods2 = IntPtr.Zero;
		Result result = Bindings.EOS_Mods_CopyModInfo(base.InnerHandle, ref options2, out outEnumeratedMods2);
		Helper.Dispose(ref options2);
		Helper.Get<ModInfoInternal, ModInfo>(outEnumeratedMods2, out outEnumeratedMods);
		if (outEnumeratedMods2 != IntPtr.Zero)
		{
			Bindings.EOS_Mods_ModInfo_Release(outEnumeratedMods2);
		}
		return result;
	}

	public void EnumerateMods(ref EnumerateModsOptions options, object clientData, OnEnumerateModsCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		EnumerateModsOptionsInternal options2 = default(EnumerateModsOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Mods_EnumerateMods(base.InnerHandle, ref options2, clientDataPointer, OnEnumerateModsCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void InstallMod(ref InstallModOptions options, object clientData, OnInstallModCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		InstallModOptionsInternal options2 = default(InstallModOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Mods_InstallMod(base.InnerHandle, ref options2, clientDataPointer, OnInstallModCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UninstallMod(ref UninstallModOptions options, object clientData, OnUninstallModCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UninstallModOptionsInternal options2 = default(UninstallModOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Mods_UninstallMod(base.InnerHandle, ref options2, clientDataPointer, OnUninstallModCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}

	public void UpdateMod(ref UpdateModOptions options, object clientData, OnUpdateModCallback completionDelegate)
	{
		if (completionDelegate == null)
		{
			throw new ArgumentNullException("completionDelegate");
		}
		UpdateModOptionsInternal options2 = default(UpdateModOptionsInternal);
		options2.Set(ref options);
		IntPtr clientDataPointer = IntPtr.Zero;
		Helper.AddCallback(out clientDataPointer, clientData, completionDelegate);
		Bindings.EOS_Mods_UpdateMod(base.InnerHandle, ref options2, clientDataPointer, OnUpdateModCallbackInternalImplementation.Delegate);
		Helper.Dispose(ref options2);
	}
}
