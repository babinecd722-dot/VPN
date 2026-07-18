namespace Epic.OnlineServices.Mods;

public struct UninstallModCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public EpicAccountId LocalUserId { get; set; }

	public object ClientData { get; set; }

	public ModIdentifier? Mod { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
