namespace Epic.OnlineServices.IntegratedPlatform;

internal static class OnUserPreLogoutCallbackInternalImplementation
{
	private static OnUserPreLogoutCallbackInternal s_Delegate;

	public static OnUserPreLogoutCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnUserPreLogoutCallbackInternal))]
	public static IntegratedPlatformPreLogoutAction EntryPoint(ref UserPreLogoutCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<UserPreLogoutCallbackInfoInternal, OnUserPreLogoutCallback, UserPreLogoutCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			return callback(ref callbackInfo);
		}
		return IntegratedPlatformPreLogoutAction.ProcessLogoutImmediately;
	}
}
