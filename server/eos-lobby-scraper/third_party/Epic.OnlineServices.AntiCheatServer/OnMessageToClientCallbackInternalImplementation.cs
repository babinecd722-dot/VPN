using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatServer;

internal static class OnMessageToClientCallbackInternalImplementation
{
	private static OnMessageToClientCallbackInternal s_Delegate;

	public static OnMessageToClientCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnMessageToClientCallbackInternal))]
	public static void EntryPoint(ref OnMessageToClientCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnMessageToClientCallbackInfoInternal, OnMessageToClientCallback, OnMessageToClientCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
