namespace Epic.OnlineServices.Platform;

internal struct DesktopCrossplayStatusInfoInternal : IGettable<DesktopCrossplayStatusInfo>
{
	private DesktopCrossplayStatus m_Status;

	private int m_ServiceInitResult;

	public void Get(out DesktopCrossplayStatusInfo other)
	{
		other = default(DesktopCrossplayStatusInfo);
		other.Status = m_Status;
		other.ServiceInitResult = m_ServiceInitResult;
	}
}
