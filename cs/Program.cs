using OpenCvSharp;

var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../../py/shared.pool");
var bufCapacity = 1920 * 1080 * 3;
using var memory = new SharedMemoryPool(path, bufCapacity, 2);
using var mat = new Mat(1080, 1920, MatType.CV_8UC3);

Console.WriteLine("Press 'Q' key to exit...");

while (!(Console.KeyAvailable && Console.ReadKey().Key is ConsoleKey.Q))
{
    if (memory.TryRead(out var buf))
    {
        Console.WriteLine($"Received: {buf.Length} bytes.");
        unsafe { buf.CopyTo(new Span<byte>(mat.DataPointer, buf.Length)); }
        Cv2.ImShow("FRAME", mat);
        Cv2.WaitKey(1);
    }
    else Thread.Sleep(10);
}

Console.WriteLine("Completed.");