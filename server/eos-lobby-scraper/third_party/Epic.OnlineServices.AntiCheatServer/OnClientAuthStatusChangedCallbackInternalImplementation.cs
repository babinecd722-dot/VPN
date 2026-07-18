using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

internal static class OnClientAuthStatusChangedCallbackInternalImplementation
{
	private static OnClientAuthStatusChangedCallbackInternal s_Delegate;

	public static OnClientAuthStatusChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnClientAuthStatusChangedCallbackInternal))]
	public static void EntryPoint(ref OnClientAuthStatusChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnClientAuthStatusChangedCallbackInfoInternal, OnClientAuthStatusChangedCallback, OnClientAuthStatusChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
