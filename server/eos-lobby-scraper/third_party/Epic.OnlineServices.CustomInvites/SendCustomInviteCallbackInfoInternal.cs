using System;

namespace Epic.OnlineServices.CustomInvites;

internal struct SendCustomInviteCallbackInfoInternal : ICallbackInfoInternal, IGettable<SendCustomInviteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_TargetUserIds;

	private uint m_TargetUserIdsCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out SendCustomInviteCallbackInfo other)
	{
		other = default(SendCustomInviteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_TargetUserIds, out ProductUserId[] to3, m_TargetUserIdsCount);
		other.TargetUserIds = to3;
	}
}
