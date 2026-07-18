namespace Epic.OnlineServices.IntegratedPlatform;

internal static class OnUserLoginStatusChangedCallbackInternalImplementation
{
	private static OnUserLoginStatusChangedCallbackInternal s_Delegate;

	public static OnUserLoginStatusChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUserLoginStatusChangedCallbackInternal))]
	public static void EntryPoint(ref UserLoginStatusChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<UserLoginStatusChangedCallbackInfoInternal, OnUserLoginStatusChangedCallback, UserLoginStatusChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
