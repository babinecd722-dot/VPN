namespace Epic.OnlineServices.P2P;

internal static class OnIncomingPacketQueueFullCallbackInternalImplementation
{
	private static OnIncomingPacketQueueFullCallbackInternal s_Delegate;

	public static OnIncomingPacketQueueFullCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnIncomingPacketQueueFullCallbackInternal))]
	public static void EntryPoint(ref OnIncomingPacketQueueFullInfoInternal data)
	{
		if (Helper.TryGetCallback<OnIncomingPacketQueueFullInfoInternal, OnIncomingPacketQueueFullCallback, OnIncomingPacketQueueFullInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
