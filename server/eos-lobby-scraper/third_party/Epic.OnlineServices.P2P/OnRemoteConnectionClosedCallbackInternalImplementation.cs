namespace Epic.OnlineServices.P2P;

internal static class OnRemoteConnectionClosedCallbackInternalImplementation
{
	private static OnRemoteConnectionClosedCallbackInternal s_Delegate;

	public static OnRemoteConnectionClosedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRemoteConnectionClosedCallbackInternal))]
	public static void EntryPoint(ref OnRemoteConnectionClosedInfoInternal data)
	{
		if (Helper.TryGetCallback<OnRemoteConnectionClosedInfoInternal, OnRemoteConnectionClosedCallback, OnRemoteConnectionClosedInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
