namespace Epic.OnlineServices.RTCAudio;

internal static class OnAudioInputStateCallbackInternalImplementation
{
	private static OnAudioInputStateCallbackInternal s_Delegate;

	public static OnAudioInputStateCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAudioInputStateCallbackInternal))]
	public static void EntryPoint(ref AudioInputStateCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<AudioInputStateCallbackInfoInternal, OnAudioInputStateCallback, AudioInputStateCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
