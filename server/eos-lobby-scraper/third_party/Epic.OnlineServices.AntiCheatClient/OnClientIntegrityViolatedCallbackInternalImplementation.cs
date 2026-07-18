namespace Epic.OnlineServices.AntiCheatClient;

internal static class OnClientIntegrityViolatedCallbackInternalImplementation
{
	private static OnClientIntegrityViolatedCallbackInternal s_Delegate;

	public static OnClientIntegrityViolatedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnClientIntegrityViolatedCallbackInternal))]
	public static void EntryPoint(ref OnClientIntegrityViolatedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnClientIntegrityViolatedCallbackInfoInternal, OnClientIntegrityViolatedCallback, OnClientIntegrityViolatedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
