namespace Epic.OnlineServices.UI;

internal static class OnDisplaySettingsUpdatedCallbackInternalImplementation
{
	private static OnDisplaySettingsUpdatedCallbackInternal s_Delegate;

	public static OnDisplaySettingsUpdatedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnDisplaySettingsUpdatedCallbackInternal))]
	public static void EntryPoint(ref OnDisplaySettingsUpdatedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnDisplaySettingsUpdatedCallbackInfoInternal, OnDisplaySettingsUpdatedCallback, OnDisplaySettingsUpdatedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
