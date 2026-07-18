namespace Epic.OnlineServices.Auth;

internal static class OnVerifyUserAuthCallbackInternalImplementation
{
	private static OnVerifyUserAuthCallbackInternal s_Delegate;

	public static OnVerifyUserAuthCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnVerifyUserAuthCallbackInternal))]
	public static void EntryPoint(ref VerifyUserAuthCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<VerifyUserAuthCallbackInfoInternal, OnVerifyUserAuthCallback, VerifyUserAuthCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
