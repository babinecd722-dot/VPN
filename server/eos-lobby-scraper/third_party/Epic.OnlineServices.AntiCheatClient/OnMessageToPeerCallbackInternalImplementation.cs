using Epic.OnlineServices.AntiCheatCommon;

namespace Epic.OnlineServices.AntiCheatClient;

internal static class OnMessageToPeerCallbackInternalImplementation
{
	private static OnMessageToPeerCallbackInternal s_Delegate;

	public static OnMessageToPeerCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnMessageToPeerCallbackInternal))]
	public static void EntryPoint(ref OnMessageToClientCallbackInfoInternal data)
	{
		if (Helper.TryGetCallback<OnMessageToClientCallbackInfoInternal, OnMessageToPeerCallback, OnMessageToClientCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
