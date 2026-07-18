namespace Epic.OnlineServices.CustomInvites;

internal static class OnRejectRequestToJoinCallbackInternalImplementation
{
	private static OnRejectRequestToJoinCallbackInternal s_Delegate;

	public static OnRejectRequestToJoinCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRejectRequestToJoinCallbackInternal))]
	public static void EntryPoint(ref RejectRequestToJoinCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<RejectRequestToJoinCallbackInfoInternal, OnRejectRequestToJoinCallback, RejectRequestToJoinCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
