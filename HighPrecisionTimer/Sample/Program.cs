using HighPrecisionTimer;
using System.Diagnostics;

namespace Sample
{
    internal class Program
    {
        static int s_i = 0;
        static double s_d = 0;

        static void Main(string[] args)
        {
            Console.WriteLine("不同计时器在时间间隔为1ms时，1s内触发事件误差超过10%的数量及总误差周期数：\n");
            Sample();
            Sample2();
            Console.WriteLine();
        }

        static void Sample()
        {
            s_i = 0;
            s_d = 0;
            HighPrecisionTimer.HighPrecisionTimer highPrecisionTimer = new HighPrecisionTimer.HighPrecisionTimer(0, 10000, 1, Tick);
            highPrecisionTimer.Open();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency < 1000) ;
            highPrecisionTimer.Dispose();
            Console.WriteLine($"基于系统性能计数的高性能计时器：    {s_i}   {s_d:F3}");
        }
        static void Sample2()
        {
            s_i = 0;
            s_d = 0;
            HighPrecisionTimer.MultimediaTimer multimediaTimer = new HighPrecisionTimer.MultimediaTimer(Tick);
            multimediaTimer.Interval = 1;
            multimediaTimer.Open();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.ElapsedTicks * 1000 / Stopwatch.Frequency < 1000) ;
            multimediaTimer.Dispose();
            Console.WriteLine($"基于win32api的多媒体计时器：      {s_i}   {s_d:F3}");
        }

        static void Tick(object sender, double JumpPeriod, long interval)
        {
            //Console.WriteLine(JumpPeriod + " " + interval);
            if (Math.Abs(JumpPeriod - 1) > 0.1)
            {
                s_i++;
            }
            s_d += Math.Abs(JumpPeriod - 1);
        }
    }
}