using System.Diagnostics;

namespace RollTheDice.Utils
{
    public static class Watch
    {
        public static Stopwatch Start()
        {
            // Start stopwatch
            Stopwatch stopwatch = new();
            stopwatch.Start();
            return stopwatch;
        }

        public static void Stop(Stopwatch stopwatch)
        {
            // Stop stopwatch
            stopwatch.Stop();
            Console.WriteLine($"==================== TASK TOOK {stopwatch.Elapsed.TotalMilliseconds}ms");
        }
    }
}