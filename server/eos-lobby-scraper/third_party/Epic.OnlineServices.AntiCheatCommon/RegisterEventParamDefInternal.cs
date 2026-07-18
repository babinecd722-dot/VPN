using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct RegisterEventParamDefInternal : ISettable<RegisterEventParamDef>, IDisposable
{
	private IntPtr m_ParamName;

	private AntiCheatCommonEventParamType m_ParamType;

	public void Set(ref RegisterEventParamDef other)
	{
		Dispose();
		Helper.Set(other.ParamName, ref m_ParamName);
		m_ParamType = other.ParamType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ParamName);
	}
}
