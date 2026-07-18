namespace Epic.OnlineServices.CustomInvites;

internal static class OnCustomInviteAcceptedCallbackInternalImplementation
{
	private static OnCustomInviteAcceptedCallbackInternal s_Delegate;

	public static OnCustomInviteAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnCustomInviteAcceptedCallbackInternal))]
	public static void EntryPoint(ref OnCustomInviteAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnCustomInviteAcceptedCallbackInfoInternal, OnCustomInviteAcceptedCallback, OnCustomInviteAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
