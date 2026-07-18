using System;

namespace Epic.OnlineServices.Lobby;

public struct LocalRTCOptions
{
	public uint Flags { get; set; }

	public bool UseManualAudioInput { get; set; }

	public bool UseManualAudioOutput { get; set; }

	public bool LocalAudioDeviceInputStartsMuted { get; set; }

	public IntPtr Reserved { get; set; }
}
