namespace Epic.OnlineServices.P2P;

internal static class OnPeerConnectionEstablishedCallbackInternalImplementation
{
	private static OnPeerConnectionEstablishedCallbackInternal s_Delegate;

	public static OnPeerConnectionEstablishedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPeerConnectionEstablishedCallbackInternal))]
	public static void EntryPoint(ref OnPeerConnectionEstablishedInfoInternal data)
	{
		if (Helper.TryGetCallback<OnPeerConnectionEstablishedInfoInternal, OnPeerConnectionEstablishedCallback, OnPeerConnectionEstablishedInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
