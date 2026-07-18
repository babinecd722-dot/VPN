namespace Epic.OnlineServices.RTCAudio;

public struct AudioBuffer
{
	public short[] Frames { get; set; }

	public uint SampleRate { get; set; }

	public uint Channels { get; set; }
}
