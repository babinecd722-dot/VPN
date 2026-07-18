using System;

namespace Epic.OnlineServices.Mods;

internal struct ModInfoInternal : IGettable<ModInfo>
{
	private int m_ApiVersion;

	private int m_ModsCount;

	private IntPtr m_Mods;

	private ModEnumerationType m_Type;

	public void Get(out ModInfo other)
	{
		other = default(ModInfo);
		Helper.Get<ModIdentifierInternal, ModIdentifier>(m_Mods, out var to, m_ModsCount, isArrayItemAllocated: false);
		other.Mods = to;
		other.Type = m_Type;
	}
}
