namespace Epic.OnlineServices.Logging;

public sealed class LoggingInterface
{
	public static Result SetCallback(LogMessageFunc callback)
	{
		Helper.AddStaticCallback("Logging.LogMessageFunc", callback);
		return Bindings.EOS_Logging_SetCallback(LogMessageFuncInternalImplementation.Delegate);
	}

	public static Result SetLogLevel(LogCategory logCategory, LogLevel logLevel)
	{
		return Bindings.EOS_Logging_SetLogLevel(logCategory, logLevel);
	}
}
