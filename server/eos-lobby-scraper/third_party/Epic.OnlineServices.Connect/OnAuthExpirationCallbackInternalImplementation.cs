namespace Epic.OnlineServices.Connect;

internal static class OnAuthExpirationCallbackInternalImplementation
{
	private static OnAuthExpirationCallbackInternal s_Delegate;

	public static OnAuthExpirationCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAuthExpirationCallbackInternal))]
	public static void EntryPoint(ref AuthExpirationCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<AuthExpirationCallbackInfoInternal, OnAuthExpirationCallback, AuthExpirationCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
