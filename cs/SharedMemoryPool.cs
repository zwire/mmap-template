using System.IO.MemoryMappedFiles;

public sealed class SharedMemoryPool : IDisposable
{

    // ----- fields ------ //

    private readonly MemoryMappedFile _mm;
    private readonly MemoryMappedViewStream _stream;
    private readonly int _bufCapacity;
    private readonly int _poolSize;
    private readonly byte[] _buf;


    // ------ constructors ------ //

    public SharedMemoryPool(string filePath, int bufCapacity, int poolSize = 1)
    {
        _bufCapacity = bufCapacity;
        _poolSize = poolSize;
        _buf = new byte[_bufCapacity];
        var totalSize = _poolSize * (_bufCapacity + 6);
        using var fs = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        _mm = MemoryMappedFile.CreateFromFile(
            fs, null, totalSize, MemoryMappedFileAccess.ReadWrite,
            HandleInheritability.Inheritable, true
        );
        _stream = _mm.CreateViewStream();
    }


    // ------ public methods ------ //

    public void Dispose()
    {
        _stream.Dispose();
        _mm.Dispose();
    }

    public void Flush()
    {
        _stream.Seek(0, SeekOrigin.Begin);
        _stream.Write(new byte[_poolSize * 6].AsSpan());
    }

    public bool TryRead(out ReadOnlySpan<byte> buf)
    {
        buf = default;
        for (var t = 1; t <= _poolSize; t++) 
        {
            for (int i = 0; i < _poolSize; i++)
            {
                _stream.Seek(i, SeekOrigin.Begin);
                if (_stream.ReadByte() == t)
                {
                    var locker = _poolSize + i;
                    _stream.Seek(locker, SeekOrigin.Begin);
                    if (_stream.ReadByte() is not 2) break;
                    _stream.Seek(locker, SeekOrigin.Begin);
                    _stream.WriteByte(1);
                    _stream.Seek(2 * _poolSize + i * 4, SeekOrigin.Begin);
                    var intBuf = new byte[4];
                    _stream.Read(intBuf, 0, intBuf.Length);
                    var size = BitConverter.ToInt32(intBuf, 0);
                    _stream.Seek(6 * _poolSize + i * _bufCapacity, SeekOrigin.Begin);
                    var count = _stream.Read(_buf, 0, size);
                    buf = _buf.AsSpan(0, count);
                    _stream.Seek(locker, SeekOrigin.Begin);
                    _stream.WriteByte(3);
                    return count == size;
                }
            }
        }
        return false;
    }

    public bool TryWrite(ReadOnlySpan<byte> buf)
    {
        for (int i = 0; i < _poolSize; i++)
        {
            _stream.Seek(i, SeekOrigin.Begin);
            var order = _stream.ReadByte();
            if (order == _poolSize || order == 0)
            {
                var locker = _poolSize + i;
                _stream.Seek(locker, SeekOrigin.Begin);
                if (_stream.ReadByte() is 1) break;
                _stream.Seek(locker, SeekOrigin.Begin);
                _stream.WriteByte(1);
                _stream.Seek(2 * _poolSize + i * 4, SeekOrigin.Begin);
                _stream.Write(BitConverter.GetBytes(buf.Length));
                _stream.Seek(6 * _poolSize + i * _bufCapacity, SeekOrigin.Begin);
                _stream.Write(buf);
                _stream.Seek(locker, SeekOrigin.Begin);
                _stream.WriteByte(2);
                _stream.Seek(0, SeekOrigin.Begin);
                for (int j = 0; j < _poolSize; j++)
                {
                    var o = _stream.ReadByte();
                    if (o > 0)
                    {
                        _stream.Seek(j, SeekOrigin.Begin);
                        _stream.WriteByte((byte)(o + 1));
                    }
                }
                _stream.Seek(i, SeekOrigin.Begin);
                _stream.WriteByte(1);
                return true;
            }
        }
        return false;
    }

}