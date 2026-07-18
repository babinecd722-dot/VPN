namespace Epic.OnlineServices.RTCAudio;

internal static class OnSetOutputDeviceSettingsCallbackInternalImplementation
{
	private static OnSetOutputDeviceSettingsCallbackInternal s_Delegate;

	public static OnSetOutputDeviceSettingsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSetOutputDeviceSettingsCallbackInternal))]
	public static void EntryPoint(ref OnSetOutputDeviceSettingsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnSetOutputDeviceSettingsCallbackInfoInternal, OnSetOutputDeviceSettingsCallback, OnSetOutputDeviceSettingsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
