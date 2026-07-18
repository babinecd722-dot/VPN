using System;

namespace Epic.OnlineServices.AntiCheatCommon;

internal struct LogEventParamPairInternal : ISettable<LogEventParamPair>, IDisposable
{
	private AntiCheatCommonEventParamType m_ParamValueType;

	private LogEventParamPairParamValueInternal m_ParamValue;

	public void Set(ref LogEventParamPair other)
	{
		Dispose();
		Helper.Set<LogEventParamPairParamValue, LogEventParamPairParamValueInternal>(other.ParamValue, ref m_ParamValue);
		m_ParamValueType = other.ParamValue.ParamValueType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_ParamValue);
	}
}
