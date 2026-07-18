namespace Epic.OnlineServices.Logging;

internal static class LogMessageFuncInternalImplementation
{
	private static LogMessageFuncInternal s_Delegate;

	public static LogMessageFuncInternal Delegate
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

	[MonoPInvokeCallback(typeof(LogMessageFuncInternal))]
	public static void EntryPoint(ref LogMessageInternal message)
	{
		if (Helper.TryGetStaticCallback<LogMessageFunc>("Logging.LogMessageFunc", out var callback))
		{
			Helper.Get<LogMessageInternal, LogMessage>(ref message, out LogMessage to);
			callback(ref to);
		}
	}
}
