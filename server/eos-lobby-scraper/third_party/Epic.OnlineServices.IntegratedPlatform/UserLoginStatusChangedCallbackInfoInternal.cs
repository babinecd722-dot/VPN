using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct UserLoginStatusChangedCallbackInfoInternal : ICallbackInfoInternal, IGettable<UserLoginStatusChangedCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_PlatformType;

	private IntPtr m_LocalPlatformUserId;

	private IntPtr m_AccountId;

	private IntPtr m_ProductUserId;

	private LoginStatus m_PreviousLoginStatus;

	private LoginStatus m_CurrentLoginStatus;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UserLoginStatusChangedCallbackInfo other)
	{
		other = default(UserLoginStatusChangedCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_PlatformType, out Utf8String to2);
		other.PlatformType = to2;
		Helper.Get(m_LocalPlatformUserId, out Utf8String to3);
		other.LocalPlatformUserId = to3;
		Helper.Get(m_AccountId, out EpicAccountId to4);
		other.AccountId = to4;
		Helper.Get(m_ProductUserId, out ProductUserId to5);
		other.ProductUserId = to5;
		other.PreviousLoginStatus = m_PreviousLoginStatus;
		other.CurrentLoginStatus = m_CurrentLoginStatus;
	}
}
