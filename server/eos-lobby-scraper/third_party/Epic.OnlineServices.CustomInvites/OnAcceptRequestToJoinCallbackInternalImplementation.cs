namespace Epic.OnlineServices.CustomInvites;

internal static class OnAcceptRequestToJoinCallbackInternalImplementation
{
	private static OnAcceptRequestToJoinCallbackInternal s_Delegate;

	public static OnAcceptRequestToJoinCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAcceptRequestToJoinCallbackInternal))]
	public static void EntryPoint(ref AcceptRequestToJoinCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<AcceptRequestToJoinCallbackInfoInternal, OnAcceptRequestToJoinCallback, AcceptRequestToJoinCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
