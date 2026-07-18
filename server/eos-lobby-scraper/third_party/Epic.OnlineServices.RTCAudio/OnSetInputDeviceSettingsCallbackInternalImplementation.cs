namespace Epic.OnlineServices.RTCAudio;

internal static class OnSetInputDeviceSettingsCallbackInternalImplementation
{
	private static OnSetInputDeviceSettingsCallbackInternal s_Delegate;

	public static OnSetInputDeviceSettingsCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnSetInputDeviceSettingsCallbackInternal))]
	public static void EntryPoint(ref OnSetInputDeviceSettingsCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnSetInputDeviceSettingsCallbackInfoInternal, OnSetInputDeviceSettingsCallback, OnSetInputDeviceSettingsCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
