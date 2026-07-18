namespace Epic.OnlineServices.RTCAudio;

internal static class OnAudioBeforeSendCallbackInternalImplementation
{
	private static OnAudioBeforeSendCallbackInternal s_Delegate;

	public static OnAudioBeforeSendCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAudioBeforeSendCallbackInternal))]
	public static void EntryPoint(ref AudioBeforeSendCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<AudioBeforeSendCallbackInfoInternal, OnAudioBeforeSendCallback, AudioBeforeSendCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
