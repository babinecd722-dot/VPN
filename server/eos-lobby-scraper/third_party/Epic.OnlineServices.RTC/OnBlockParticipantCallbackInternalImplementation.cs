namespace Epic.OnlineServices.RTC;

internal static class OnBlockParticipantCallbackInternalImplementation
{
	private static OnBlockParticipantCallbackInternal s_Delegate;

	public static OnBlockParticipantCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnBlockParticipantCallbackInternal))]
	public static void EntryPoint(ref BlockParticipantCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<BlockParticipantCallbackInfoInternal, OnBlockParticipantCallback, BlockParticipantCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
