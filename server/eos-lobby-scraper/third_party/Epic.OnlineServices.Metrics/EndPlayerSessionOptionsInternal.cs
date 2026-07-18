using System;

namespace Epic.OnlineServices.Metrics;

internal struct EndPlayerSessionOptionsInternal : ISettable<EndPlayerSessionOptions>, IDisposable
{
	private int m_ApiVersion;

	private MetricsAccountIdType m_AccountIdType;

	private EndPlayerSessionOptionsAccountIdInternal m_AccountId;

	public void Set(ref EndPlayerSessionOptions other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set<EndPlayerSessionOptionsAccountId, EndPlayerSessionOptionsAccountIdInternal>(other.AccountId, ref m_AccountId);
		m_AccountIdType = other.AccountId.AccountIdType;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_AccountId);
	}
}
