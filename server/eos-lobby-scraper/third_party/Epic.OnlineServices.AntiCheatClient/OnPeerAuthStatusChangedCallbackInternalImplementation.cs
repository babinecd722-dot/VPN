using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatClient;

internal static class OnPeerAuthStatusChangedCallbackInternalImplementation
{
	private static OnPeerAuthStatusChangedCallbackInternal s_Delegate;

	public static OnPeerAuthStatusChangedCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPeerAuthStatusChangedCallbackInternal))]
	public static void EntryPoint(ref OnClientAuthStatusChangedCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnClientAuthStatusChangedCallbackInfoInternal, OnPeerAuthStatusChangedCallback, OnClientAuthStatusChangedCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
