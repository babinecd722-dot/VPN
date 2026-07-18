using System;

namespace Epic.OnlineServices.Ecom;

internal struct QueryOwnershipBySandboxIdsCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryOwnershipBySandboxIdsCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_SandboxIdItemOwnerships;

	private uint m_SandboxIdItemOwnershipsCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryOwnershipBySandboxIdsCallbackInfo other)
	{
		other = default(QueryOwnershipBySandboxIdsCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out EpicAccountId to2);
		other.LocalUserId = to2;
		Helper.Get<SandboxIdItemOwnershipInternal, SandboxIdItemOwnership>(m_SandboxIdItemOwnerships, out var to3, m_SandboxIdItemOwnershipsCount, isArrayItemAllocated: false);
		other.SandboxIdItemOwnerships = to3;
	}
}
