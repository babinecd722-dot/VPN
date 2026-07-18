using System;

namespace Epic.OnlineServices.Logging;

internal struct LogMessageInternal : IGettable<LogMessage>
{
	private IntPtr m_Category;

	private IntPtr m_Message;

	private LogLevel m_Level;

	public void Get(out LogMessage other)
	{
		other = default(LogMessage);
		Helper.Get(m_Category, out Utf8String to);
		other.Category = to;
		Helper.Get(m_Message, out Utf8String to2);
		other.Message = to2;
		other.Level = m_Level;
	}
}
