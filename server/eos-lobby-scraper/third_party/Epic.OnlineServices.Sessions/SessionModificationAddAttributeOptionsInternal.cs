using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionModificationAddAttributeOptionsInternal : ISettable<SessionModificationAddAttributeOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_SessionAttribute;

	private SessionAttributeAdvertisementType m_AdvertisementType;

	public void Set(ref SessionModificationAddAttributeOptions other)
	{
		Dispose();
		m_ApiVersion = 2;
		Helper.Set<AttributeData, AttributeDataInternal>(other.SessionAttribute, ref m_SessionAttribute);
		m_AdvertisementType = other.AdvertisementType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_SessionAttribute);
	}
}
