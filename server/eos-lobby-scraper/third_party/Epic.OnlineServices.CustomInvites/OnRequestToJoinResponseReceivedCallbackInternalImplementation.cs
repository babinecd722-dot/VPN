namespace Epic.OnlineServices.CustomInvites;

internal static class OnRequestToJoinResponseReceivedCallbackInternalImplementation
{
	private static OnRequestToJoinResponseReceivedCallbackInternal s_Delegate;

	public static OnRequestToJoinResponseReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRequestToJoinResponseReceivedCallbackInternal))]
	public static void EntryPoint(ref RequestToJoinResponseReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<RequestToJoinResponseReceivedCallbackInfoInternal, OnRequestToJoinResponseReceivedCallback, RequestToJoinResponseReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
