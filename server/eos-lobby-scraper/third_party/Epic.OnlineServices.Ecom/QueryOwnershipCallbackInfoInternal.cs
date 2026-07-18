using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryOwnershipCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryOwnershipCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_ItemOwnership;

	private uint m_ItemOwnershipCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryOwnershipCallbackInfo other)
	{
		other = default(QueryOwnershipCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get<ItemOwnershipInternal, ItemOwnership>(m_ItemOwnership, out var to3, m_ItemOwnershipCount, isArrayItemAllocated: false);
		other.ItemOwnership = to3;
	}
}
