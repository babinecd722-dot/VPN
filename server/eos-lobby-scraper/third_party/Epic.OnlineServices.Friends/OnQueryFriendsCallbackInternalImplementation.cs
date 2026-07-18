namespace Epic.OnlineServices.Friends;

internal static class OnQueryFriendsCallbackInternalImplementation
{
	private static OnQueryFriendsCallbackInternal s_Delegate;

	public static OnQueryFriendsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryFriendsCallbackInternal))]
	public static void EntryPoint(ref QueryFriendsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryFriendsCallbackInfoInternal, OnQueryFriendsCallback, QueryFriendsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
