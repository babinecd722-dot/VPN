using System;

namespace Epic.OnlineServices.PlayerDataStorage;

internal struct QueryFileListCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryFileListCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private uint m_FileCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryFileListCallbackInfo other)
	{
		other = default(QueryFileListCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		other.FileCount = m_FileCount;
	}
}
