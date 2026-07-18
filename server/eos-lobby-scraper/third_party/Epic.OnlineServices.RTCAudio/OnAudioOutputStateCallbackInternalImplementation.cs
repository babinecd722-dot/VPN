namespace Epic.OnlineServices.RTCAudio;

internal static class OnAudioOutputStateCallbackInternalImplementation
{
	private static OnAudioOutputStateCallbackInternal s_Delegate;

	public static OnAudioOutputStateCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAudioOutputStateCallbackInternal))]
	public static void EntryPoint(ref AudioOutputStateCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<AudioOutputStateCallbackInfoInternal, OnAudioOutputStateCallback, AudioOutputStateCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
