using System;

namespace Epic.OnlineServices.Presence;

internal struct InfoInternal : IGettable<Info>
{
	private int m_ApiVersion;

	private Status m_Status;

	private IntPtr m_UserId;

	private IntPtr m_ProductId;

	private IntPtr m_ProductVersion;

	private IntPtr m_Platform;

	private IntPtr m_RichText;

	private int m_RecordsCount;

	private IntPtr m_Records;

	private IntPtr m_ProductName;

	private IntPtr m_IntegratedPlatform;

	public void Get(out Info other)
	{
		other = default(Info);
		other.Status = m_Status;
		Helper.Get(m_UserId, out EpicAccountId to);
		other.UserId = to;
		Helper.Get(m_ProductId, out Utf8String to2);
		other.ProductId = to2;
		Helper.Get(m_ProductVersion, out Utf8String to3);
		other.ProductVersion = to3;
		Helper.Get(m_Platform, out Utf8String to4);
		other.Platform = to4;
		Helper.Get(m_RichText, out Utf8String to5);
		other.RichText = to5;
		Helper.Get<DataRecordInternal, DataRecord>(m_Records, out var to6, m_RecordsCount, isArrayItemAllocated: false);
		other.Records = to6;
		Helper.Get(m_ProductName, out Utf8String to7);
		other.ProductName = to7;
		Helper.Get(m_IntegratedPlatform, out Utf8String to8);
		other.IntegratedPlatform = to8;
	}
}
