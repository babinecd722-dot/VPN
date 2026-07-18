using System;

namespace Epic.OnlineServices.Mods;

internal struct InstallModOptionsInternal : ISettable<InstallModOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_LocalUserId;

	private IntPtr m_Mod;

	private int m_RemoveAfterExit;

	public void Set(ref InstallModOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set((Handle)other.LocalUserId, ref m_LocalUserId);
		Helper.Set<ModIdentifier, ModIdentifierInternal>(other.Mod, ref m_Mod);
		Helper.Set(other.RemoveAfterExit, ref m_RemoveAfterExit);
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_LocalUserId);
		Helper.Dispose(ref m_Mod);
	}
}
