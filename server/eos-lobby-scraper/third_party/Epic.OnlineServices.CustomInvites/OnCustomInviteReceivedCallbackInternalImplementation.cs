namespace Epic.OnlineServices.CustomInvites;

internal static class OnCustomInviteReceivedCallbackInternalImplementation
{
	private static OnCustomInviteReceivedCallbackInternal s_Delegate;

	public static OnCustomInviteReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCustomInviteReceivedCallbackInternal))]
	public static void EntryPoint(ref OnCustomInviteReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnCustomInviteReceivedCallbackInfoInternal, OnCustomInviteReceivedCallback, OnCustomInviteReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
