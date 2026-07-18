using System;

namespace Epic.OnlineServices.RTCAdmin;

internal struct QueryJoinRoomTokenCompleteCallbackInfoInternal : ICallbackInfoInternal, IGettable<QueryJoinRoomTokenCompleteCallbackInfo>
{
	private Result m_ResultCode;

	private IntPtr m_ClientData;

	private IntPtr m_RoomName;

	private IntPtr m_ClientBaseUrl;

	private uint m_QueryId;

	private uint m_TokenCount;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out QueryJoinRoomTokenCompleteCallbackInfo other)
	{
		other = default(QueryJoinRoomTokenCompleteCallbackInfo);
		other.ResultCode = m_ResultCode;
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_RoomName, out Utf8String to2);
		other.RoomName = to2;
		Helper.Get(m_ClientBaseUrl, out Utf8String to3);
		other.ClientBaseUrl = to3;
		other.QueryId = m_QueryId;
		other.TokenCount = m_TokenCount;
	}
}
