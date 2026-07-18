namespace Epic.OnlineServices.Lobby;

internal static class OnLeaveRTCRoomCallbackInternalImplementation
{
	private static OnLeaveRTCRoomCallbackInternal s_Delegate;

	public static OnLeaveRTCRoomCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnLeaveRTCRoomCallbackInternal))]
	public static void EntryPoint(ref LeaveRTCRoomCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<LeaveRTCRoomCallbackInfoInternal, OnLeaveRTCRoomCallback, LeaveRTCRoomCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
