using HighPrecisionTimer;
using System.Diagnostics;

namespace Sample
{
    internal class Program
    {
        static int s_i = 0;

        static void Main(string[] args)
        {
            s_i = 0;
            Sample();
            Console.WriteLine(s_i);
            Console.WriteLine();
            s_i = 0;
            Sample2();
            Console.WriteLine(s_i);
        }

        static void Sample()
        {
            HighPrecisionTimer.HighPrecisionTimer highPrecisionTimer = new HighPrecisionTimer.HighPrecisionTimer(0, 10000, 1, Tick);
            highPrecisionTimer.Open();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency < 10000) ;
            highPrecisionTimer.Dispose();
        }
        static void Sample2()
        {
            HighPrecisionTimer.MultimediaTimer multimediaTimer = new HighPrecisionTimer.MultimediaTimer(Tick);
            multimediaTimer.Interval = 1;
            multimediaTimer.Open();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency < 10000) ;
            multimediaTimer.Dispose();
        }

        static void Tick(object sender, double JumpPeriod, long interval)
        {
            //Console.WriteLine(JumpPeriod + " " + interval);
            if(Math.Abs(JumpPeriod - 1) > 0.1)
            {
                s_i++;
            }
        }
    }
}