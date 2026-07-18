using System;

namespace Epic.OnlineServices.Mods;

internal struct UninstallModOptionsInternal : ISettable<UninstallModOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Mod;

	public void Set(ref UninstallModOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set<ModIdentifier, ModIdentifierInternal>(other.Mod, ref m_Mod);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Mod);
	}
}
