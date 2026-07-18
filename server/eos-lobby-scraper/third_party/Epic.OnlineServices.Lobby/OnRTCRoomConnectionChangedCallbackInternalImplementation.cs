namespace Epic.OnlineServices.Lobby;

internal static class OnRTCRoomConnectionChangedCallbackInternalImplementation
{
	private static OnRTCRoomConnectionChangedCallbackInternal s_Delegate;

	public static OnRTCRoomConnectionChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRTCRoomConnectionChangedCallbackInternal))]
	public static void EntryPoint(ref RTCRoomConnectionChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<RTCRoomConnectionChangedCallbackInfoInternal, OnRTCRoomConnectionChangedCallback, RTCRoomConnectionChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
