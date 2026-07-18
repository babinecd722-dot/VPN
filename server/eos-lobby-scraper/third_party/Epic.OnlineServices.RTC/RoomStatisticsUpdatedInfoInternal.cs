using System;

namespace Epic.OnlineServices.RTC;

internal struct RoomStatisticsUpdatedInfoInternal : ICallbackInfoInternal, IGettable<RoomStatisticsUpdatedInfo>
{
	private IntPtr m_ClientData;

	private IntPtr m_LocalUserId;

	private IntPtr m_RoomName;

	private IntPtr m_Statistic;

	public IntPtr ClientDataPointer => m_ClientData;

	public void Get(out RoomStatisticsUpdatedInfo other)
	{
		other = default(RoomStatisticsUpdatedInfo);
		Helper.Get(m_ClientData, out object to);
		other.ClientData = to;
		Helper.Get(m_LocalUserId, out ProductUserId to2);
		other.LocalUserId = to2;
		Helper.Get(m_RoomName, out Utf8String to3);
		other.RoomName = to3;
		Helper.Get(m_Statistic, out Utf8String to4);
		other.Statistic = to4;
	}
}
