namespace Epic.OnlineServices.Auth;

internal static class OnVerifyIdTokenCallbackInternalImplementation
{
	private static OnVerifyIdTokenCallbackInternal s_Delegate;

	public static OnVerifyIdTokenCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnVerifyIdTokenCallbackInternal))]
	public static void EntryPoint(ref VerifyIdTokenCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<VerifyIdTokenCallbackInfoInternal, OnVerifyIdTokenCallback, VerifyIdTokenCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
