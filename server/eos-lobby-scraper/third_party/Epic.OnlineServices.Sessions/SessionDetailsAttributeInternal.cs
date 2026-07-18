using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionDetailsAttributeInternal : IGettable<SessionDetailsAttribute>
{
	private int m_ApiVersion;

	private IntPtr m_Data;

	private SessionAttributeAdvertisementType m_AdvertisementType;

	public void Get(out SessionDetailsAttribute other)
	{
		other = default(SessionDetailsAttribute);
		Helper.Get<AttributeDataInternal, AttributeData>(m_Data, out AttributeData? to);
		other.Data = to;
		other.AdvertisementType = m_AdvertisementType;
	}
}
