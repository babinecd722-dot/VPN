namespace Epic.OnlineServices.P2P;

internal static class OnQueryNATTypeCompleteCallbackInternalImplementation
{
	private static OnQueryNATTypeCompleteCallbackInternal s_Delegate;

	public static OnQueryNATTypeCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryNATTypeCompleteCallbackInternal))]
	public static void EntryPoint(ref OnQueryNATTypeCompleteInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnQueryNATTypeCompleteInfoInternal, OnQueryNATTypeCompleteCallback, OnQueryNATTypeCompleteInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
