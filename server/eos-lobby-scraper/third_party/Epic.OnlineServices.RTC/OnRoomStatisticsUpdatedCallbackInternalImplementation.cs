namespace Epic.OnlineServices.RTC;

internal static class OnRoomStatisticsUpdatedCallbackInternalImplementation
{
	private static OnRoomStatisticsUpdatedCallbackInternal s_Delegate;

	public static OnRoomStatisticsUpdatedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnRoomStatisticsUpdatedCallbackInternal))]
	public static void EntryPoint(ref RoomStatisticsUpdatedInfoInternal data)
	{
		if (Helper.TryGetCallback<RoomStatisticsUpdatedInfoInternal, OnRoomStatisticsUpdatedCallback, RoomStatisticsUpdatedInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
