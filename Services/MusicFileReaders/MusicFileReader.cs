using NAudio.Wave;
using CSCore.Codecs.FLAC;

namespace AudioAnalyser.MusicFileReader;
public interface IMusicFileStream : IDisposable
{
    int NumChannels { get; }
    int SampleRate { get; }

    long LengthBytes { get; }
    TimeSpan TotalTime { get; }
    string Filename { get; set; }
    float[]? ReadNextSampleFrame();

    // Summary:
    //      Reads in a block of data from the music stream.
    //
    // Parameters:
    //   blockSize:
    //     Number of samples to be read.
    //   buffer:
    //      2D array that stores the read data.
    //
    // Returns:
    //     Number of samples read. If n-channel audio is present, total samples will be n*return value.
    //
    // Exceptions:
    //   T:System.ArgumentOutOfRangeException:
    //     The length of value is less than 2.
    List<float[]> ReadBlock(int blockLength);
    void PlayAudio();
    void PlayAudioAndDisplayBeat(int bpm);
    void Seek(long offset, SeekOrigin origin);
}
public class WaveFileStream : IMusicFileStream
{
    public int NumChannels { get => _reader.WaveFormat.Channels; }
    public int SampleRate { get => _reader.WaveFormat.SampleRate; }

    public long LengthBytes { get => _reader.Length; }
    private string _filename = String.Empty;
    public string Filename
    {
        get { return _filename; }
        set
        {
            _filename = value;
            _reader = new(value);
        }
    }

    public WaveFileReader _reader { get; private set; }

    public TimeSpan TotalTime { get => _reader.TotalTime; }

    public WaveFileStream(string filename)
    {
        _filename = filename;
        _reader = new(filename);
    }

    public float[] ReadNextSampleFrame()
    {
        return _reader.ReadNextSampleFrame();
    }

    public List<float[]> ReadBlock(int blockSize)
    {
        var sampleBlock = new List<float[]>();
        int bytesToRead = _reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8) * blockSize;
        byte[] raw = new byte[bytesToRead];
        int bytesRead = _reader.Read(raw, 0, bytesToRead);
        if (bytesRead == 0) return sampleBlock; // end of file
        if (bytesRead % 2 != 0) throw new InvalidDataException("Unexpected end of file");
        var samplesRead = bytesRead / (_reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8));
        int offset = 0;
        for (int sample = 0; sample < samplesRead; sample++)
        {
            var buffer = new float[2];
            for (int channel = 0; channel < _reader.WaveFormat.Channels; channel++)
            {
                if (_reader.WaveFormat.BitsPerSample == 16)
                {

                    buffer[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                    offset += 2;
                }
                else if (_reader.WaveFormat.BitsPerSample == 24)
                {
                    buffer[channel] = (((sbyte)raw[offset + 2] << 16) | (raw[offset + 1] << 8) | raw[offset]) / 8388608f;
                    offset += 3;
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            sampleBlock.Add(buffer);
        }

        return sampleBlock;
    }

    public void PlayAudio()
    {
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(_reader);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }

        }
    }

    public void PlayAudioAndDisplayBeat(int bpm)
    {
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(_reader);
            outputDevice.Play();
            int index = 0;
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                if (index % 2 == 0) { Console.WriteLine("XXXXX"); } else { Console.WriteLine("OOOOO"); }
                index++;
                Thread.Sleep((int)Math.Floor(60d / bpm * 1000));
            }

        }
    }

    public void Seek(long offset, SeekOrigin origin)
    {
        _reader.Seek(offset, origin);
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}

public class FlacFileStream : IMusicFileStream
{
    public int NumChannels { get => _reader.WaveFormat.Channels; }
    public int SampleRate { get => _reader.WaveFormat.SampleRate; }

    public long LengthBytes { get => _reader.Length; }
    private string _filename = String.Empty;
    public string Filename
    {
        get { return _filename; }
        set
        {
            _filename = value;
            _reader = new(value);
        }
    }

    private byte[] _sampleBuffer = new byte[2];
    public FlacFile _reader { get; private set; }

    public TimeSpan TotalTime { get => new TimeSpan(0, 0, 0, 0, (int)_reader.WaveFormat.BytesToMilliseconds(_reader.Length)); }

    public FlacFileStream(string filename)
    {
        _filename = filename;
        _reader = new(filename);
    }

    public float[]? ReadNextSampleFrame()
    {
        var sampleFrame = new float[_reader.WaveFormat.Channels];
        int bytesToRead = _reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8);
        byte[] raw = new byte[bytesToRead];
        int bytesRead = _reader.Read(raw, 0, bytesToRead);
        if (bytesRead == 0) return null; // end of file
        if (bytesRead < bytesToRead) throw new InvalidDataException("Unexpected end of file");
        int offset = 0;
        for (int channel = 0; channel < _reader.WaveFormat.Channels; channel++)
        {
            if (_reader.WaveFormat.BitsPerSample == 16)
            {
                sampleFrame[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                offset += 2;
            }
            else
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
        }
        return sampleFrame;
    }

    public List<float[]> ReadBlock(int blockSize)
    {
        var sampleBlock = new List<float[]>();
        int bytesToRead = _reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8) * blockSize;
        byte[] raw = new byte[bytesToRead];
        int bytesRead = _reader.Read(raw, 0, bytesToRead);
        if (bytesRead == 0) return sampleBlock; // end of file
        if (bytesRead % 2 != 0) throw new InvalidDataException("Unexpected end of file");
        var samplesRead = bytesRead / (_reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8));
        int offset = 0;
        for (int sample = 0; sample < samplesRead; sample++)
        {
            var buffer = new float[2];
            for (int channel = 0; channel < _reader.WaveFormat.Channels; channel++)
            {
                if (_reader.WaveFormat.BitsPerSample == 16)
                {

                    buffer[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                    offset += 2;
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            sampleBlock.Add(buffer);
        }

        return sampleBlock;
    }

    public void PlayAudio()
    {
        using (var outputDevice = new CSCore.SoundOut.WaveOut())
        {
            outputDevice.Initialize((CSCore.IWaveSource)_reader);
            outputDevice.Play();
            while (outputDevice.PlaybackState == CSCore.SoundOut.PlaybackState.Playing)
            {
                Thread.Sleep(10000);
            }
        }
    }
    public void PlayAudioAndDisplayBeat(int bpm)
    {
        using (var outputDevice = new CSCore.SoundOut.WaveOut())
        {
            outputDevice.Initialize((CSCore.IWaveSource)_reader);
            outputDevice.Play();
            int index = 0;
            while (outputDevice.PlaybackState == CSCore.SoundOut.PlaybackState.Playing)
            {
                if (index % 2 == 0) { Console.WriteLine("XXXXX"); } else { Console.WriteLine("OOOOO"); }
                index++;
                Thread.Sleep((int)Math.Floor(60d / bpm * 1000));
            }
        }
    }
    public void Seek(long offset, SeekOrigin origin)
    {
        if (_reader.CanSeek)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _reader.Position = offset;
                    break;
                case SeekOrigin.Current:
                    _reader.Position += offset;
                    break;
                case SeekOrigin.End:
                    _reader.Position = _reader.Length + offset;
                    break;
                default:
                    throw new FlacException("Unable to seek current reader", FlacLayer.OutSideOfFrame);
            }
        }

    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}

public class MP3FileStream : IMusicFileStream
{
    public int NumChannels { get => _reader.WaveFormat.Channels; }
    public int SampleRate { get => _reader.WaveFormat.SampleRate; }
    public long LengthBytes { get => _reader.Length; }
    public TimeSpan TotalTime { get => _reader.TotalTime; }
    private string _filename = String.Empty;
    public string Filename
    {
        get { return _filename; }
        set
        {
            _filename = value;
            _reader = new(value);
        }
    }

    private byte[] _sampleBuffer = new byte[2];
    public Mp3FileReader _reader { get; private set; }
    public MP3FileStream(string filename)
    {
        _filename = filename;
        _reader = new(filename);
    }

    public float[]? ReadNextSampleFrame()
    {
        var sampleFrame = new float[_reader.WaveFormat.Channels];
        int bytesToRead = _reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8);
        byte[] raw = new byte[bytesToRead];
        int bytesRead = _reader.Read(raw, 0, bytesToRead);
        if (bytesRead == 0) return null; // end of file
        if (bytesRead < bytesToRead) throw new InvalidDataException("Unexpected end of file");
        int offset = 0;
        for (int channel = 0; channel < _reader.WaveFormat.Channels; channel++)
        {
            if (_reader.WaveFormat.BitsPerSample == 16)
            {
                sampleFrame[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                offset += 2;
            }
            else
            {
                throw new InvalidOperationException("Unsupported bit depth");
            }
        }
        return sampleFrame;
    }

    public List<float[]> ReadBlock(int blockSize)
    {
        var sampleBlock = new List<float[]>();
        int bytesToRead = _reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8) * blockSize;
        byte[] raw = new byte[bytesToRead];
        int bytesRead = _reader.Read(raw, 0, bytesToRead);
        if (bytesRead == 0) return sampleBlock; // end of file
        if (bytesRead % 2 != 0) throw new InvalidDataException("Unexpected end of file");
        var samplesRead = bytesRead / (_reader.WaveFormat.Channels * (_reader.WaveFormat.BitsPerSample / 8));
        int offset = 0;
        for (int sample = 0; sample < samplesRead; sample++)
        {
            var buffer = new float[2];
            for (int channel = 0; channel < _reader.WaveFormat.Channels; channel++)
            {
                if (_reader.WaveFormat.BitsPerSample == 16)
                {

                    buffer[channel] = BitConverter.ToInt16(raw, offset) / 32768f;
                    offset += 2;
                }
                else
                {
                    throw new InvalidOperationException("Unsupported bit depth");
                }
            }
            sampleBlock.Add(buffer);
        }

        return sampleBlock;
    }

    public void PlayAudio()
    {
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(_reader);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(1000);
            }
        }
    }
    public void PlayAudioAndDisplayBeat(int bpm)
    {
        using (var outputDevice = new WaveOutEvent())
        {
            outputDevice.Init(_reader);
            outputDevice.Play();
            int index = 0;
            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                if (index % 2 == 0) { Console.WriteLine("XXXXX"); } else { Console.WriteLine("OOOOO"); }
                index++;
                Thread.Sleep((int)Math.Floor(60d / bpm * 1000));
            }
        }
    }
    public void Seek(long offset, SeekOrigin origin)
    {
        _reader.Seek(offset, origin);
    }

    public void Dispose()
    {
        _reader.Dispose();
    }
}