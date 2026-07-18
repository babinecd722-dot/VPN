using System;

namespace Epic.OnlineServices.Ecom;

internal struct CatalogReleaseInternal : IGettable<CatalogRelease>
{
	private int m_ApiVersion;

	private uint m_CompatibleAppIdCount;

	private IntPtr m_CompatibleAppIds;

	private uint m_CompatiblePlatformCount;

	private IntPtr m_CompatiblePlatforms;

	private IntPtr m_ReleaseNote;

	public void Get(out CatalogRelease other)
	{
		other = default(CatalogRelease);
		Helper.Get(m_CompatibleAppIds, out var to, m_CompatibleAppIdCount, isArrayItemAllocated: true);
		other.CompatibleAppIds = to;
		Helper.Get(m_CompatiblePlatforms, out var to2, m_CompatiblePlatformCount, isArrayItemAllocated: true);
		other.CompatiblePlatforms = to2;
		Helper.Get(m_ReleaseNote, out Utf8String to3);
		other.ReleaseNote = to3;
	}
}
