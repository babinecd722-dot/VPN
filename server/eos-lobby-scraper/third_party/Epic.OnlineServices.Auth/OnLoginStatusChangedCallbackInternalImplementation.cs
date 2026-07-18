namespace Epic.OnlineServices.Auth;

internal static class OnLoginStatusChangedCallbackInternalImplementation
{
	private static OnLoginStatusChangedCallbackInternal s_Delegate;

	public static OnLoginStatusChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLoginStatusChangedCallbackInternal))]
	public static void EntryPoint(ref LoginStatusChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<LoginStatusChangedCallbackInfoInternal, OnLoginStatusChangedCallback, LoginStatusChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
