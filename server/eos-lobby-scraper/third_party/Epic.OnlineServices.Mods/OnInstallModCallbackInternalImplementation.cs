namespace Epic.OnlineServices.Mods;

internal static class OnInstallModCallbackInternalImplementation
{
	private static OnInstallModCallbackInternal s_Delegate;

	public static OnInstallModCallbackInternal Delegate
	{
		get
		{
			if (s_Delegate == null)
			{
				s_Delegate = EntryPoint;
			}
			return s_Delegate;
		}
	}

	[MonoPInvokeCallback(typeof(OnInstallModCallbackInternal))]
	public static void EntryPoint(ref InstallModCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<InstallModCallbackInfoInternal, OnInstallModCallback, InstallModCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
