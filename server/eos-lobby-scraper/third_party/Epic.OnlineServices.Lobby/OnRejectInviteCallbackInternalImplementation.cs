namespace Epic.OnlineServices.Lobby;

internal static class OnRejectInviteCallbackInternalImplementation
{
	private static OnRejectInviteCallbackInternal s_Delegate;

	public static OnRejectInviteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRejectInviteCallbackInternal))]
	public static void EntryPoint(ref RejectInviteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<RejectInviteCallbackInfoInternal, OnRejectInviteCallback, RejectInviteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
