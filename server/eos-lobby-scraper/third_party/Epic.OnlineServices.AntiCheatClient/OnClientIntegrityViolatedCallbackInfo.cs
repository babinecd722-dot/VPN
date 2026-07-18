namespace Epic.OnlineServices.AntiCheatClient;

public struct OnClientIntegrityViolatedCallbackInfo : ICallbackInfo
{
	public object ClientData { get; set; }

	public AntiCheatClientViolationType ViolationType { get; set; }

	public Utf8String ViolationMessage { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return null;
	}
}
