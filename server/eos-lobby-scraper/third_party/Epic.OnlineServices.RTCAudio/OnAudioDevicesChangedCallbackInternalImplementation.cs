namespace Epic.OnlineServices.RTCAudio;

internal static class OnAudioDevicesChangedCallbackInternalImplementation
{
	private static OnAudioDevicesChangedCallbackInternal s_Delegate;

	public static OnAudioDevicesChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAudioDevicesChangedCallbackInternal))]
	public static void EntryPoint(ref AudioDevicesChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<AudioDevicesChangedCallbackInfoInternal, OnAudioDevicesChangedCallback, AudioDevicesChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
