using System;

namespace Epic.OnlineServices.ProgressionSnapshot;

internal struct DeleteSnapshotCallbackInfoInternal : ICallbackInfoInternal, IGettable<DeleteSnapshotCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_LocalUserId;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out DeleteSnapshotCallbackInfo other)
	{
		other = default(DeleteSnapshotCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_LocalUserId, out ProductUserId to);
		other.LocalUserId = to;
		Helper.Get(m_ClientData, out object to2);
		other.ClientData = to2;
	}
}
