using System;

namespace Epic.OnlineServices.Connect;

internal struct VerifyIdTokenCallbackInfoInternal : ICallbackInfoInternal, IGettable<VerifyIdTokenCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_ProductUserId;

	private int m_IsAccountInfoPresent;

	private ExternalAccountType m_AccountIdType;

	private IntPtr m_AccountId;

	private IntPtr m_Platform;

	private IntPtr m_DeviceType;

	private IntPtr m_ClientId;

	private IntPtr m_ProductId;

	private IntPtr m_SandboxId;

	private IntPtr m_DeploymentId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out VerifyIdTokenCallbackInfo other)
	{
		other = default(VerifyIdTokenCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_ProductUserId, out ProductUserId to2);
		other.ProductUserId = to2;
		Helper.Get(m_IsAccountInfoPresent, out bool to3);
		other.IsAccountInfoPresent = to3;
		other.AccountIdType = m_AccountIdType;
		Helper.Get(m_AccountId, out Utf8String to4);
		other.AccountId = to4;
		Helper.Get(m_Platform, out Utf8String to5);
		other.Platform = to5;
		Helper.Get(m_DeviceType, out Utf8String to6);
		other.DeviceType = to6;
		Helper.Get(m_ClientId, out Utf8String to7);
		other.ClientId = to7;
		Helper.Get(m_ProductId, out Utf8String to8);
		other.ProductId = to8;
		Helper.Get(m_SandboxId, out Utf8String to9);
		other.SandboxId = to9;
		Helper.Get(m_DeploymentId, out Utf8String to10);
		other.DeploymentId = to10;
	}
}
