using System;

namespace Epic.OnlineServices.IntegratedPlatform;

internal struct UserPreLogoutCallbackInfoInternal : ICallbackInfoInternal, IGettable<UserPreLogoutCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_PlatformType;

	private IntPtr m_LocalPlatformUserId;

	private IntPtr m_AccountId;

	private IntPtr m_ProductUserId;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out UserPreLogoutCallbackInfo other)
	{
		other = default(UserPreLogoutCallbackInfo);
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
	}
}
