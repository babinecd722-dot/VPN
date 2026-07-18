namespace Epic.OnlineServices.Auth;

internal static class OnQueryIdTokenCallbackInternalImplementation
{
	private static OnQueryIdTokenCallbackInternal s_Delegate;

	public static OnQueryIdTokenCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryIdTokenCallbackInternal))]
	public static void EntryPoint(ref QueryIdTokenCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryIdTokenCallbackInfoInternal, OnQueryIdTokenCallback, QueryIdTokenCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
