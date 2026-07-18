namespace Epic.OnlineServices.RTCAdmin;

internal static class OnSetParticipantHardMuteCompleteCallbackInternalImplementation
{
	private static OnSetParticipantHardMuteCompleteCallbackInternal s_Delegate;

	public static OnSetParticipantHardMuteCompleteCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSetParticipantHardMuteCompleteCallbackInternal))]
	public static void EntryPoint(ref SetParticipantHardMuteCompleteCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<SetParticipantHardMuteCompleteCallbackInfoInternal, OnSetParticipantHardMuteCompleteCallback, SetParticipantHardMuteCompleteCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
