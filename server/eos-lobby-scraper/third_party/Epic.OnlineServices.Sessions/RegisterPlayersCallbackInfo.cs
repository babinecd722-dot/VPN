namespace Epic.OnlineServices.Sessions;

public struct RegisterPlayersCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public ProductUserId[] RegisteredPlayers { get; set; }

	public ProductUserId[] SanctionedPlayers { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
