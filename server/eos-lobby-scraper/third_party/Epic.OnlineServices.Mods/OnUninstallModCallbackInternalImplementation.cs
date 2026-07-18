namespace Epic.OnlineServices.Mods;

internal static class OnUninstallModCallbackInternalImplementation
{
	private static OnUninstallModCallbackInternal s_Delegate;

	public static OnUninstallModCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUninstallModCallbackInternal))]
	public static void EntryPoint(ref UninstallModCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UninstallModCallbackInfoInternal, OnUninstallModCallback, UninstallModCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
