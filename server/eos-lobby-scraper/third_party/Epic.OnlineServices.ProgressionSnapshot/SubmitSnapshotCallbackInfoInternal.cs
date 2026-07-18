using System;

namespace Epic.OnlineServices.ProgressionSnapshot;

internal struct SubmitSnapshotCallbackInfoInternal : ICallbackInfoInternal, IGettable<SubmitSnapshotCallbackInfo>
{
	private Result m_ResultCode;

	private uint m_SnapshotId;

	private IntPtr m_ClientData;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SubmitSnapshotCallbackInfo other)
	{
		other = default(SubmitSnapshotCallbackInfo);
		other.ResultCode = m_ResultCode;
		other.SnapshotId = m_SnapshotId;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
	}
}
