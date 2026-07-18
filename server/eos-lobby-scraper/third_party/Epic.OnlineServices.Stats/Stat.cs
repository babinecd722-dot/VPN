using System;

namespace Epic.OnlineServices.Stats;

public struct Stat
{
	public Utf8String Name { get; set; }

	public DateTimeOffset? StartTime { get; set; }

	public DateTimeOffset? EndTime { get; set; }

	public int Value { get; set; }
}
