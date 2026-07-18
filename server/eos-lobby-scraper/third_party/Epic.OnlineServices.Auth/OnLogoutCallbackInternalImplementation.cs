namespace Epic.OnlineServices.Auth;

internal static class OnLogoutCallbackInternalImplementation
{
	private static OnLogoutCallbackInternal s_Delegate;

	public static OnLogoutCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLogoutCallbackInternal))]
	public static void EntryPoint(ref LogoutCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LogoutCallbackInfoInternal, OnLogoutCallback, LogoutCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
