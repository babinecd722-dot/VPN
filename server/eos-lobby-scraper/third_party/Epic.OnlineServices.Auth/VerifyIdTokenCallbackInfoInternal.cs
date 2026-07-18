using System;

namespace Epic.OnlineServices.Auth;

internal struct VerifyIdTokenCallbackInfoInternal : ICallbackInfoInternal, IGettable<VerifyIdTokenCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_ApplicationId;

	private IntPtr m_ClientId;

	private IntPtr m_ProductId;

	private IntPtr m_SandboxId;

	private IntPtr m_DeploymentId;

	private IntPtr m_DisplayName;

	private int m_IsExternalAccountInfoPresent;

	private ExternalAccountType m_ExternalAccountIdType;

	private IntPtr m_ExternalAccountId;

	private IntPtr m_ExternalAccountDisplayName;

	private IntPtr m_Platform;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out VerifyIdTokenCallbackInfo other)
	{
		other = default(VerifyIdTokenCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_ApplicationId, out Utf8String to2);
		other.ApplicationId = to2;
		Helper.Get(m_ClientId, out Utf8String to3);
		other.ClientId = to3;
		Helper.Get(m_ProductId, out Utf8String to4);
		other.ProductId = to4;
		Helper.Get(m_SandboxId, out Utf8String to5);
		other.SandboxId = to5;
		Helper.Get(m_DeploymentId, out Utf8String to6);
		other.DeploymentId = to6;
		Helper.Get(m_DisplayName, out Utf8String to7);
		other.DisplayName = to7;
		Helper.Get(m_IsExternalAccountInfoPresent, out bool to8);
		other.IsExternalAccountInfoPresent = to8;
		other.ExternalAccountIdType = m_ExternalAccountIdType;
		Helper.Get(m_ExternalAccountId, out Utf8String to9);
		other.ExternalAccountId = to9;
		Helper.Get(m_ExternalAccountDisplayName, out Utf8String to10);
		other.ExternalAccountDisplayName = to10;
		Helper.Get(m_Platform, out Utf8String to11);
		other.Platform = to11;
	}
}
