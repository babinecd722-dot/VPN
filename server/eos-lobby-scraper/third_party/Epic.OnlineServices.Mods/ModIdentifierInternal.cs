using System;

namespace Epic.OnlineServices.Mods;

internal struct ModIdentifierInternal : IGettable<ModIdentifier>, ISettable<ModIdentifier>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_NamespaceId;

	private IntPtr m_ItemId;

	private IntPtr m_ArtifactId;

	private IntPtr m_Title;

	private IntPtr m_Version;

	public void Get(out ModIdentifier other)
	{
		other = default(ModIdentifier);
		Helper.Get(m_NamespaceId, out Utf8String to);
		other.NamespaceId = to;
		Helper.Get(m_ItemId, out Utf8String to2);
		other.ItemId = to2;
		Helper.Get(m_ArtifactId, out Utf8String to3);
		other.ArtifactId = to3;
		Helper.Get(m_Title, out Utf8String to4);
		other.Title = to4;
		Helper.Get(m_Version, out Utf8String to5);
		other.Version = to5;
	}

	public void Set(ref ModIdentifier other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.NamespaceId, ref m_NamespaceId);
		Helper.Set(other.ItemId, ref m_ItemId);
		Helper.Set(other.ArtifactId, ref m_ArtifactId);
		Helper.Set(other.Title, ref m_Title);
		Helper.Set(other.Version, ref m_Version);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_NamespaceId);
		Helper.Dispose(ref m_ItemId);
		Helper.Dispose(ref m_ArtifactId);
		Helper.Dispose(ref m_Title);
		Helper.Dispose(ref m_Version);
	}
}
