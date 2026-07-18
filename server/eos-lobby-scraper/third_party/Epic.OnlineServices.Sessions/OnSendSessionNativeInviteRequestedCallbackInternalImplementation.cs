namespace Epic.OnlineServices.Sessions;

internal static class OnSendSessionNativeInviteRequestedCallbackInternalImplementation
{
	private static OnSendSessionNativeInviteRequestedCallbackInternal s_Delegate;

	public static OnSendSessionNativeInviteRequestedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendSessionNativeInviteRequestedCallbackInternal))]
	public static void EntryPoint(ref SendSessionNativeInviteRequestedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<SendSessionNativeInviteRequestedCallbackInfoInternal, OnSendSessionNativeInviteRequestedCallback, SendSessionNativeInviteRequestedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
