namespace Epic.OnlineServices.CustomInvites;

internal static class OnRequestToJoinReceivedCallbackInternalImplementation
{
	private static OnRequestToJoinReceivedCallbackInternal s_Delegate;

	public static OnRequestToJoinReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRequestToJoinReceivedCallbackInternal))]
	public static void EntryPoint(ref RequestToJoinReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<RequestToJoinReceivedCallbackInfoInternal, OnRequestToJoinReceivedCallback, RequestToJoinReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
