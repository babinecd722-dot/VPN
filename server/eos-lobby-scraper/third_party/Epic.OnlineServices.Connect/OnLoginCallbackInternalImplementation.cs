namespace Epic.OnlineServices.Connect;

internal static class OnLoginCallbackInternalImplementation
{
	private static OnLoginCallbackInternal s_Delegate;

	public static OnLoginCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLoginCallbackInternal))]
	public static void EntryPoint(ref LoginCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LoginCallbackInfoInternal, OnLoginCallback, LoginCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
