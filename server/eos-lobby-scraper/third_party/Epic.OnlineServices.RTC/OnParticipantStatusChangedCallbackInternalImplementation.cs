namespace Epic.OnlineServices.RTC;

internal static class OnParticipantStatusChangedCallbackInternalImplementation
{
	private static OnParticipantStatusChangedCallbackInternal s_Delegate;

	public static OnParticipantStatusChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnParticipantStatusChangedCallbackInternal))]
	public static void EntryPoint(ref ParticipantStatusChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<ParticipantStatusChangedCallbackInfoInternal, OnParticipantStatusChangedCallback, ParticipantStatusChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
