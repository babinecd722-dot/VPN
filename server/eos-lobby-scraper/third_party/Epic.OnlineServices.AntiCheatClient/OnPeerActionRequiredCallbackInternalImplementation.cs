using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatClient;

internal static class OnPeerActionRequiredCallbackInternalImplementation
{
	private static OnPeerActionRequiredCallbackInternal s_Delegate;

	public static OnPeerActionRequiredCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnPeerActionRequiredCallbackInternal))]
	public static void EntryPoint(ref OnClientActionRequiredCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnClientActionRequiredCallbackInfoInternal, OnPeerActionRequiredCallback, OnClientActionRequiredCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
