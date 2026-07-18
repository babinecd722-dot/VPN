namespace Epic.OnlineServices.RTCAudio;

internal static class OnAudioBeforeRenderCallbackInternalImplementation
{
	private static OnAudioBeforeRenderCallbackInternal s_Delegate;

	public static OnAudioBeforeRenderCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnAudioBeforeRenderCallbackInternal))]
	public static void EntryPoint(ref AudioBeforeRenderCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<AudioBeforeRenderCallbackInfoInternal, OnAudioBeforeRenderCallback, AudioBeforeRenderCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
