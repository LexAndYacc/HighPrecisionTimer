using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace HighPrecisionTimer
{
    /// <summary>
    /// 基于系统性能计数的高性能计时器
    /// 需要完全占用一个逻辑处理器
    /// </summary>
    public class HighPrecisionTimer : IDisposable
    {
        /// <summary>
        /// 是否销毁计时器
        /// </summary>
        private bool _disposed = false;
        /// <summary>
        /// 是否正在运行计时器
        /// </summary>
        private bool _runingTimer = false;
        /// <summary>
        /// 首次启动延时
        /// </summary>
        private uint _delay = 0;
        /// <summary>
        /// 计时器周期
        /// </summary>
        private long _period = 10;
        /// <summary>
        /// 计时器运行时独占的CPU核心索引序号
        /// </summary>
        private byte _cpuIndex = 0;
        /// <summary>
        /// 回调函数定义
        /// </summary>
        private OnTickHandle _tick;
        /// <summary>
        /// 性能计数缓存
        /// </summary>
        private long _q1, _q2;

        private long _freq = 0;
        /// <summary>
        /// 系统性能计数频率（每秒）
        /// </summary>
        public long Frequency { get { return _freq; } }


        /// <summary>
        /// 根据CPU的索引序号获取CPU的标识序号
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        private ulong GetCpuID(int idx)
        {
            ulong cpuid = 0;
            if (idx < 0 || idx >= System.Environment.ProcessorCount)
            {
                idx = 0;
            }
            cpuid |= 1UL << idx;
            return cpuid;
        }
        /// <summary>
        /// 计时器构造函数
        /// </summary>
        /// <param name="delay">首次启动计时器延时时间</param>
        /// <param name="period">计时器触发的周期</param>
        /// <param name="cpuIndex">指定计时器线程独占的CPU核心索引，必须>0，不允许为计时器分配0#CPU</param>
        /// <param name="tick">计时器触发时的回调函数</param>
        public HighPrecisionTimer(uint delay, uint period, byte cpuIndex, OnTickHandle tick)
        {
            _tick = tick;
            _delay = delay;
            _period = period;
            _cpuIndex = cpuIndex;
            _threadRumTimer = new System.Threading.Thread(new System.Threading.ThreadStart(RunTimer));
            long freq = 0;
            QueryPerformanceFrequency(out freq);
            if (freq > 0)
            {
                _freq = freq;
            }
            else
            {
                throw new Exception("初始化计时器失败");
            }
            if (_cpuIndex == 0)
            {
                throw new Exception("计时器不允许被分配到0#CPU");
            }
            if (_cpuIndex >= System.Environment.ProcessorCount)
            {
                throw new Exception("为计时器分配了超出索引的CPU");
            }
        }


        private System.Threading.Thread _threadRumTimer;
        /// <summary>
        /// 开启计时器
        /// </summary>
        public void Open()
        {
            if (_tick != null)
            {
                QueryPerformanceCounter(out _q1);
                _threadRumTimer.Start();
            }
        }


        /// <summary>
        /// 运行计时器
        /// </summary>
        private void RunTimer()
        {
            UIntPtr up = UIntPtr.Zero;
            if (_cpuIndex != 0)
                up = SetThreadAffinityMask(GetCurrentThread(), new UIntPtr(GetCpuID(_cpuIndex)));
            if (up == UIntPtr.Zero)
            {
                throw new Exception("为计时器分配CPU核心时失败");
            }
            QueryPerformanceCounter(out _q2);
            if (_delay > 0)
            {
                while (_q2 < _q1 + _delay)
                {
                    QueryPerformanceCounter(out _q2);
                }
            }
            QueryPerformanceCounter(out _q1);
            QueryPerformanceCounter(out _q2);
            while (!_disposed)
            {
                _runingTimer = true;
                QueryPerformanceCounter(out _q2);
                if (_q2 > _q1 + _period)
                {
                    if (!_disposed)
                        _tick?.Invoke(this, (_q2 - _q1) / 1.0 / _period, _q2 - _q1);
                    _q1 = _q2;
                }
                _runingTimer = false;
            }
        }
        /// <summary>
        /// 销毁当前计时器所占用的资源
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            while (_runingTimer)
            {
                Thread.Sleep(1);
            }
            if (_threadRumTimer != null)
                _threadRumTimer.Interrupt();
        }



        /// <summary>
        /// 获取当前系统性能计数
        /// </summary>
        /// <param name="lpPerformanceCount"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);
        /// <summary>
        /// 获取当前系统性能频率
        /// </summary>
        /// <param name="lpFrequency"></param>
        /// <returns></returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);
        /// <summary>
        /// 指定某一特定线程运行在指定的CPU核心
        /// </summary>
        /// <param name="hThread"></param>
        /// <param name="dwThreadAffinityMask"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static extern UIntPtr SetThreadAffinityMask(IntPtr hThread, UIntPtr dwThreadAffinityMask);
        /// <summary>
        /// 获取当前线程的Handler
        /// </summary>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThread();
    }
}
