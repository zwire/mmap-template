var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\..\\..\\py\\shared.pool");
var bufCapacity = 1920 * 1080 * 3;
using var memory = new SharedMemoryPool(path, bufCapacity, 5);

Console.WriteLine("Press 'Q' key to exit...");

while (!(Console.KeyAvailable && Console.ReadKey().Key is ConsoleKey.Q))
{
    if (memory.TryRead(out var buf))
    {
        Console.WriteLine($"Received: {buf.Length} bytes.");
        // Matに貼り付けるなら，unsafeで括ってこんな感じにします
        // buf.CopyTo(new Span<byte>(mat.DataPointer, size));
    }
    else Thread.Sleep(10);
}

Console.WriteLine("Completed.");