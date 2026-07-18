namespace Epic.OnlineServices.Mods;

internal static class OnEnumerateModsCallbackInternalImplementation
{
	private static OnEnumerateModsCallbackInternal s_Delegate;

	public static OnEnumerateModsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnEnumerateModsCallbackInternal))]
	public static void EntryPoint(ref EnumerateModsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<EnumerateModsCallbackInfoInternal, OnEnumerateModsCallback, EnumerateModsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
