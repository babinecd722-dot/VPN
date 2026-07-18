namespace Epic.OnlineServices.UI;

internal static class OnShowReportPlayerCallbackInternalImplementation
{
	private static OnShowReportPlayerCallbackInternal s_Delegate;

	public static OnShowReportPlayerCallbackInternal Delegate
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

	[MonoPInvokeCallback(typeof(OnShowReportPlayerCallbackInternal))]
	public static void EntryPoint(ref OnShowReportPlayerCallbackInfoInternal data)
	{
		if (Helper.TryGetAndRemoveCallback<OnShowReportPlayerCallbackInfoInternal, OnShowReportPlayerCallback, OnShowReportPlayerCallbackInfo>(ref data, out var callback, out var callbackInfo))
		{
			callback(ref callbackInfo);
		}
	}
}
