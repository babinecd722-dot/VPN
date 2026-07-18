namespace Epic.OnlineServices.Friends;

internal static class OnAcceptInviteCallbackInternalImplementation
{
	private static OnAcceptInviteCallbackInternal s_Delegate;

	public static OnAcceptInviteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAcceptInviteCallbackInternal))]
	public static void EntryPoint(ref AcceptInviteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<AcceptInviteCallbackInfoInternal, OnAcceptInviteCallback, AcceptInviteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
