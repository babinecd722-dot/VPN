namespace Epic.OnlineServices.UserInfo;

internal static class OnQueryUserInfoByDisplayNameCallbackInternalImplementation
{
	private static OnQueryUserInfoByDisplayNameCallbackInternal s_Delegate;

	public static OnQueryUserInfoByDisplayNameCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryUserInfoByDisplayNameCallbackInternal))]
	public static void EntryPoint(ref QueryUserInfoByDisplayNameCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryUserInfoByDisplayNameCallbackInfoInternal, OnQueryUserInfoByDisplayNameCallback, QueryUserInfoByDisplayNameCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
