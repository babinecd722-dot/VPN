namespace Epic.OnlineServices.CustomInvites;

internal static class OnSendCustomNativeInviteRequestedCallbackInternalImplementation
{
	private static OnSendCustomNativeInviteRequestedCallbackInternal s_Delegate;

	public static OnSendCustomNativeInviteRequestedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendCustomNativeInviteRequestedCallbackInternal))]
	public static void EntryPoint(ref SendCustomNativeInviteRequestedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<SendCustomNativeInviteRequestedCallbackInfoInternal, OnSendCustomNativeInviteRequestedCallback, SendCustomNativeInviteRequestedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
