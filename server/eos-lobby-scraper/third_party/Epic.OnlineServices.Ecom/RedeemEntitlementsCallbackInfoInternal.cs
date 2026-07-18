using System;

namespace Epic.OnlineServices.Ecom;

internal struct RedeemEntitlementsCallbackInfoInternal : ICallbackInfoInternal, IGettable<RedeemEntitlementsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private uint m_RedeemedEntitlementIdsCount;

	private uint m_PreviouslyRedeemedEntitlementIdsCount;

	private uint m_InvalidEntitlementIdsCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RedeemEntitlementsCallbackInfo other)
	{
		other = default(RedeemEntitlementsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		other.RedeemedEntitlementIdsCount = m_RedeemedEntitlementIdsCount;
		other.PreviouslyRedeemedEntitlementIdsCount = m_PreviouslyRedeemedEntitlementIdsCount;
		other.InvalidEntitlementIdsCount = m_InvalidEntitlementIdsCount;
	}
}
