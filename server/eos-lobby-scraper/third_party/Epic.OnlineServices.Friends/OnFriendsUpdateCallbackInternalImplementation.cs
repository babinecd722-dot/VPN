namespace Epic.OnlineServices.Friends;

internal static class OnFriendsUpdateCallbackInternalImplementation
{
	private static OnFriendsUpdateCallbackInternal s_Delegate;

	public static OnFriendsUpdateCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnFriendsUpdateCallbackInternal))]
	public static void EntryPoint(ref OnFriendsUpdateInfoInternal data)
	{
		if (Helper.TryGetCallback<OnFriendsUpdateInfoInternal, OnFriendsUpdateCallback, OnFriendsUpdateInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
