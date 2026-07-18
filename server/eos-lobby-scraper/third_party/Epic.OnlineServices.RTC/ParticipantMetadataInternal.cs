using System;

namespace Epic.OnlineServices.RTC;

internal struct ParticipantMetadataInternal : IGettable<ParticipantMetadata>
{
	private int m_ApiVersion;

	private IntPtr m_Key;

	private IntPtr m_Value;

	public void Get(out ParticipantMetadata other)
	{
		other = default(ParticipantMetadata);
		Helper.Get(m_Key, out Utf8String to);
		other.Key = to;
		Helper.Get(m_Value, out Utf8String to2);
		other.Value = to2;
	}
}
