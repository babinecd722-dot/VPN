namespace Epic.OnlineServices.UI;

internal static class OnShowFriendsCallbackInternalImplementation
{
	private static OnShowFriendsCallbackInternal s_Delegate;

	public static OnShowFriendsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnShowFriendsCallbackInternal))]
	public static void EntryPoint(ref ShowFriendsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<ShowFriendsCallbackInfoInternal, OnShowFriendsCallback, ShowFriendsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
