namespace Epic.OnlineServices.Sessions;

internal static class OnSessionInviteReceivedCallbackInternalImplementation
{
	private static OnSessionInviteReceivedCallbackInternal s_Delegate;

	public static OnSessionInviteReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSessionInviteReceivedCallbackInternal))]
	public static void EntryPoint(ref SessionInviteReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<SessionInviteReceivedCallbackInfoInternal, OnSessionInviteReceivedCallback, SessionInviteReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
