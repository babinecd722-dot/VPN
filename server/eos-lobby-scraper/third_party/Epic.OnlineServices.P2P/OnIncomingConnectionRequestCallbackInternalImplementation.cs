namespace Epic.OnlineServices.P2P;

internal static class OnIncomingConnectionRequestCallbackInternalImplementation
{
	private static OnIncomingConnectionRequestCallbackInternal s_Delegate;

	public static OnIncomingConnectionRequestCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnIncomingConnectionRequestCallbackInternal))]
	public static void EntryPoint(ref OnIncomingConnectionRequestInfoInternal data)
	{
		if (Helper.TryGetCallback<OnIncomingConnectionRequestInfoInternal, OnIncomingConnectionRequestCallback, OnIncomingConnectionRequestInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
