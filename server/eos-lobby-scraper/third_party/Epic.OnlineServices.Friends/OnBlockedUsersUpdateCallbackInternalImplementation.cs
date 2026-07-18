namespace Epic.OnlineServices.Friends;

internal static class OnBlockedUsersUpdateCallbackInternalImplementation
{
	private static OnBlockedUsersUpdateCallbackInternal s_Delegate;

	public static OnBlockedUsersUpdateCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnBlockedUsersUpdateCallbackInternal))]
	public static void EntryPoint(ref OnBlockedUsersUpdateInfoInternal data)
	{
		if (Helper.TryGetCallback<OnBlockedUsersUpdateInfoInternal, OnBlockedUsersUpdateCallback, OnBlockedUsersUpdateInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
