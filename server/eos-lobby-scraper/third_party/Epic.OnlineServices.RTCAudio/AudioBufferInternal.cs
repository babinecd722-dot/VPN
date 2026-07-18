using System;

namespace Epic.OnlineServices.RTCAudio;

internal struct AudioBufferInternal : IGettable<AudioBuffer>, ISettable<AudioBuffer>, IDisposable
{
	private int m_ApiVersion;

	private IntPtr m_Frames;

	private uint m_FramesCount;

	private uint m_SampleRate;

	private uint m_Channels;

	public void Get(out AudioBuffer other)
	{
		other = default(AudioBuffer);
		Helper.Get(m_Frames, out short[] to, m_FramesCount, isArrayItemAllocated: false);
		other.Frames = to;
		other.SampleRate = m_SampleRate;
		other.Channels = m_Channels;
	}

	public void Set(ref AudioBuffer other)
	{
		Dispose();
		m_ApiVersion = 1;
		Helper.Set(other.Frames, ref m_Frames, out m_FramesCount, isArrayItemAllocated: false);
		m_SampleRate = other.SampleRate;
		m_Channels = other.Channels;
	}

	public void Dispose()
	{
		Helper.Dispose(ref m_Frames);
	}
}
