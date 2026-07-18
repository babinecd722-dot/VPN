namespace Epic.OnlineServices.UI;

internal static class OnHideFriendsCallbackInternalImplementation
{
	private static OnHideFriendsCallbackInternal s_Delegate;

	public static OnHideFriendsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnHideFriendsCallbackInternal))]
	public static void EntryPoint(ref HideFriendsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<HideFriendsCallbackInfoInternal, OnHideFriendsCallback, HideFriendsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
