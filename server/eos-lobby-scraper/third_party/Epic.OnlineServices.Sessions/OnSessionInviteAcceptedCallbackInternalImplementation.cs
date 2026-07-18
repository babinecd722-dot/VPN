namespace Epic.OnlineServices.Sessions;

internal static class OnSessionInviteAcceptedCallbackInternalImplementation
{
	private static OnSessionInviteAcceptedCallbackInternal s_Delegate;

	public static OnSessionInviteAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSessionInviteAcceptedCallbackInternal))]
	public static void EntryPoint(ref SessionInviteAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<SessionInviteAcceptedCallbackInfoInternal, OnSessionInviteAcceptedCallback, SessionInviteAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
