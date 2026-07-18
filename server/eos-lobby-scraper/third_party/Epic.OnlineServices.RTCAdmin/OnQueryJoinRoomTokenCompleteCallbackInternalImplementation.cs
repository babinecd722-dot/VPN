namespace Epic.OnlineServices.RTCAdmin;

internal static class OnQueryJoinRoomTokenCompleteCallbackInternalImplementation
{
	private static OnQueryJoinRoomTokenCompleteCallbackInternal s_Delegate;

	public static OnQueryJoinRoomTokenCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnQueryJoinRoomTokenCompleteCallbackInternal))]
	public static void EntryPoint(ref QueryJoinRoomTokenCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<QueryJoinRoomTokenCompleteCallbackInfoInternal, OnQueryJoinRoomTokenCompleteCallback, QueryJoinRoomTokenCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
