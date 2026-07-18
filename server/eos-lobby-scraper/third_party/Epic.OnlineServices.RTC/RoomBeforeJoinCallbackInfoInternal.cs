using System;

namespace Epic.OnlineServices.RTC;

internal struct RoomBeforeJoinCallbackInfoInternal : ICallbackInfoInternal, IGettable<RoomBeforeJoinCallbackInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RoomBeforeJoinCallbackInfo other)
	{
		other = default(RoomBeforeJoinCallbackInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
	}
}
