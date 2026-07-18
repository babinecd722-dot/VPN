namespace Epic.OnlineServices.P2P;

internal static class OnPeerConnectionInterruptedCallbackInternalImplementation
{
	private static OnPeerConnectionInterruptedCallbackInternal s_Delegate;

	public static OnPeerConnectionInterruptedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPeerConnectionInterruptedCallbackInternal))]
	public static void EntryPoint(ref OnPeerConnectionInterruptedInfoInternal data)
	{
		if (Helper.TryGetCallback<OnPeerConnectionInterruptedInfoInternal, OnPeerConnectionInterruptedCallback, OnPeerConnectionInterruptedInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
