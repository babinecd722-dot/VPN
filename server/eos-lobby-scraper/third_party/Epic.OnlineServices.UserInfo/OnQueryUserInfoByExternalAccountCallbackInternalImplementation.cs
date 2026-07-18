namespace Epic.OnlineServices.UserInfo;

internal static class OnQueryUserInfoByExternalAccountCallbackInternalImplementation
{
	private static OnQueryUserInfoByExternalAccountCallbackInternal s_Delegate;

	public static OnQueryUserInfoByExternalAccountCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryUserInfoByExternalAccountCallbackInternal))]
	public static void EntryPoint(ref QueryUserInfoByExternalAccountCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryUserInfoByExternalAccountCallbackInfoInternal, OnQueryUserInfoByExternalAccountCallback, QueryUserInfoByExternalAccountCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
