namespace Epic.OnlineServices.Sessions;

internal static class OnSessionInviteRejectedCallbackInternalImplementation
{
	private static OnSessionInviteRejectedCallbackInternal s_Delegate;

	public static OnSessionInviteRejectedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSessionInviteRejectedCallbackInternal))]
	public static void EntryPoint(ref SessionInviteRejectedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<SessionInviteRejectedCallbackInfoInternal, OnSessionInviteRejectedCallback, SessionInviteRejectedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
