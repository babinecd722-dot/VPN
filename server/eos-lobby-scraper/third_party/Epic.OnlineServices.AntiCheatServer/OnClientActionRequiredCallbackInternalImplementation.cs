using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

internal static class OnClientActionRequiredCallbackInternalImplementation
{
	private static OnClientActionRequiredCallbackInternal s_Delegate;

	public static OnClientActionRequiredCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnClientActionRequiredCallbackInternal))]
	public static void EntryPoint(ref OnClientActionRequiredCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnClientActionRequiredCallbackInfoInternal, OnClientActionRequiredCallback, OnClientActionRequiredCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
