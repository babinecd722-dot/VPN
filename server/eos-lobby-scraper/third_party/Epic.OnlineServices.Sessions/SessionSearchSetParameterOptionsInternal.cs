using System;

namespace Epic.OnlineServices.Sessions;

internal struct SessionSearchSetParameterOptionsInternal : ISettable<SessionSearchSetParameterOptions>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Parameter;

	private ComparisonOp m_ComparisonOp;

	public void Set(ref SessionSearchSetParameterOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<AttributeData, AttributeDataInternal>(other.Parameter, ref m_Parameter);
		m_ComparisonOp = other.ComparisonOp;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Parameter);
	}
}
