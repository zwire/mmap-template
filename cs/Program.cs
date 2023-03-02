var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../py/shared.pool");
var bufCapacity = 1920 * 1080 * 3;
using var memory = new SharedMemoryPool(path, bufCapacity, 2);
memory.Flush();

Console.WriteLine("Press 'Q' key to exit...");

while (!(Console.KeyAvailable && Console.ReadKey().Key is ConsoleKey.Q))
{
    // If you want to write OpenCvSharp's Mat, do the following.
    // unsafe { memory.TryWrite(new Span<byte>(mat.DataPointer, mat.Width * mat.Height * 3)); }
    if (memory.TryRead(out var buf))
    {
        Console.WriteLine($"Received: {buf.Length} bytes.");
        // If you want to copy to OpenCvSharp's Mat, do the following.
        // unsafe { buf.CopyTo(new Span<byte>(mat.DataPointer, buf.Length)); }
        // Cv2.ImShow(" ", mat);
        // Cv2.WaitKey(1);
    }
    else Thread.Sleep(10);
}

Console.WriteLine("Completed.");