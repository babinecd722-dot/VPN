using System;

namespace Epic.OnlineServices.RTC;

internal struct LeaveRoomCallbackInfoInternal : ICallbackInfoInternal, IGettable<LeaveRoomCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out LeaveRoomCallbackInfo other)
	{
		other = default(LeaveRoomCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
	}
}
