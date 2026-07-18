namespace Epic.OnlineServices.RTCData;

internal static class OnDataReceivedCallbackInternalImplementation
{
	private static OnDataReceivedCallbackInternal s_Delegate;

	public static OnDataReceivedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDataReceivedCallbackInternal))]
	public static void EntryPoint(ref DataReceivedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<DataReceivedCallbackInfoInternal, OnDataReceivedCallback, DataReceivedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
