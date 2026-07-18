namespace Epic.OnlineServices.Connect;

public struct VerifyIdTokenCallbackInfo : ICallbackInfo
{
	public Result ResultCode { get; set; }

	public object ClientData { get; set; }

	public ProductUserId ProductUserId { get; set; }

	public bool IsAccountInfoPresent { get; set; }

	public ExternalAccountType AccountIdType { get; set; }

	public Utf8String AccountId { get; set; }

	public Utf8String Platform { get; set; }

	public Utf8String DeviceType { get; set; }

	public Utf8String ClientId { get; set; }

	public Utf8String ProductId { get; set; }

	public Utf8String SandboxId { get; set; }

	public Utf8String DeploymentId { get; set; }

	public object GetClientData()
	{
		return ClientData;
	}

	public Result? GetResultCode()
	{
		return ResultCode;
	}
}
