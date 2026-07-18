namespace Epic.OnlineServices.RTCAudio;

internal static class OnRegisterPlatformUserCallbackInternalImplementation
{
	private static OnRegisterPlatformUserCallbackInternal s_Delegate;

	public static OnRegisterPlatformUserCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRegisterPlatformUserCallbackInternal))]
	public static void EntryPoint(ref OnRegisterPlatformUserCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnRegisterPlatformUserCallbackInfoInternal, OnRegisterPlatformUserCallback, OnRegisterPlatformUserCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
