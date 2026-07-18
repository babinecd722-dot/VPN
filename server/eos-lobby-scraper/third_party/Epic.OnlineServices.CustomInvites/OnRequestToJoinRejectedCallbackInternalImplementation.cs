namespace Epic.OnlineServices.CustomInvites;

internal static class OnRequestToJoinRejectedCallbackInternalImplementation
{
	private static OnRequestToJoinRejectedCallbackInternal s_Delegate;

	public static OnRequestToJoinRejectedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRequestToJoinRejectedCallbackInternal))]
	public static void EntryPoint(ref OnRequestToJoinRejectedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnRequestToJoinRejectedCallbackInfoInternal, OnRequestToJoinRejectedCallback, OnRequestToJoinRejectedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
