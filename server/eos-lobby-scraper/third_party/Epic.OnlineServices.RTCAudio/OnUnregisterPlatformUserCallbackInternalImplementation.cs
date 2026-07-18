namespace Epic.OnlineServices.RTCAudio;

internal static class OnUnregisterPlatformUserCallbackInternalImplementation
{
	private static OnUnregisterPlatformUserCallbackInternal s_Delegate;

	public static OnUnregisterPlatformUserCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUnregisterPlatformUserCallbackInternal))]
	public static void EntryPoint(ref OnUnregisterPlatformUserCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnUnregisterPlatformUserCallbackInfoInternal, OnUnregisterPlatformUserCallback, OnUnregisterPlatformUserCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
