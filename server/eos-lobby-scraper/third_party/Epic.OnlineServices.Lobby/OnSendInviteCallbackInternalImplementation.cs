namespace Epic.OnlineServices.Lobby;

internal static class OnSendInviteCallbackInternalImplementation
{
	private static OnSendInviteCallbackInternal s_Delegate;

	public static OnSendInviteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendInviteCallbackInternal))]
	public static void EntryPoint(ref SendInviteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SendInviteCallbackInfoInternal, OnSendInviteCallback, SendInviteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
