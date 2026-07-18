using System;

namespace Epic.OnlineServices.RTC;

internal struct JoinRoomCallbackInfoInternal : ICallbackInfoInternal, IGettable<JoinRoomCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private uint m_RoomOptionsCount;

	private IntPtr m_RoomOptions;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out JoinRoomCallbackInfo other)
	{
		other = default(JoinRoomCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get<OptionInternal, Option>(m_RoomOptions, out var to4, m_RoomOptionsCount, isArrayItemAllocated: false);
		other.RoomOptions = to4;
	}
}
