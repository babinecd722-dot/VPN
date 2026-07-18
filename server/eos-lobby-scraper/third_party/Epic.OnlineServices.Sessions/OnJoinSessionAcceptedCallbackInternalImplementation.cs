namespace Epic.OnlineServices.Sessions;

internal static class OnJoinSessionAcceptedCallbackInternalImplementation
{
	private static OnJoinSessionAcceptedCallbackInternal s_Delegate;

	public static OnJoinSessionAcceptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnJoinSessionAcceptedCallbackInternal))]
	public static void EntryPoint(ref JoinSessionAcceptedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<JoinSessionAcceptedCallbackInfoInternal, OnJoinSessionAcceptedCallback, JoinSessionAcceptedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
