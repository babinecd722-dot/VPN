namespace Epic.OnlineServices.RTCData;

internal static class OnParticipantUpdatedCallbackInternalImplementation
{
	private static OnParticipantUpdatedCallbackInternal s_Delegate;

	public static OnParticipantUpdatedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnParticipantUpdatedCallbackInternal))]
	public static void EntryPoint(ref ParticipantUpdatedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<ParticipantUpdatedCallbackInfoInternal, OnParticipantUpdatedCallback, ParticipantUpdatedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
