namespace Epic.OnlineServices.UserInfo;

internal static class OnQueryUserInfoCallbackInternalImplementation
{
	private static OnQueryUserInfoCallbackInternal s_Delegate;

	public static OnQueryUserInfoCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryUserInfoCallbackInternal))]
	public static void EntryPoint(ref QueryUserInfoCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryUserInfoCallbackInfoInternal, OnQueryUserInfoCallback, QueryUserInfoCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
