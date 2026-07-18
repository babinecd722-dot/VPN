namespace Epic.OnlineServices.CustomInvites;

internal static class OnSendRequestToJoinCallbackInternalImplementation
{
	private static OnSendRequestToJoinCallbackInternal s_Delegate;

	public static OnSendRequestToJoinCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSendRequestToJoinCallbackInternal))]
	public static void EntryPoint(ref SendRequestToJoinCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SendRequestToJoinCallbackInfoInternal, OnSendRequestToJoinCallback, SendRequestToJoinCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
