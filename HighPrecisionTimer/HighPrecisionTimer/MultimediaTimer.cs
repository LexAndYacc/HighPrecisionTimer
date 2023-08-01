using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HighPrecisionTimer
{
    /// <summary>
    /// 基于win32api的多媒体计时器
    /// 最小分辨率通常为1ms
    /// </summary>
    public class MultimediaTimer
    {
        /// <summary>
        /// 计时器类型
        /// </summary>
        private const EventType _eventType = EventType.TIME_PERIODIC;
        /// <summary>
        /// 计时器回调
        /// </summary>
        private readonly TimerCallback _callback;
        /// <summary>
        /// 用于计算触发间隔的计时器
        /// </summary>
        private Stopwatch stopwatch = new Stopwatch();
        /// <summary>
        /// 是否销毁计时器
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// 计时器id
        /// </summary>
        private volatile uint _timerId;
        /// <summary>
        /// 回调函数定义
        /// </summary>
        private OnTickHandle? _tick;

        private uint _interval;
        /// <summary>
        /// 计时器间隔
        /// </summary>
        public uint Interval
        {
            get => _interval;
            set
            {
                var tc = GetTimerCaps();
                if (value < tc.wPeriodMin || value > tc.wPeriodMax)
                    throw new ArgumentOutOfRangeException($"{value} 超出范围。\n必须在{tc.wPeriodMin}和{tc.wPeriodMax} 之间（ms）");
                _interval = value;
            }
        }
        private static TimerCaps GetTimerCaps()
        {
            var caps = new TimerCaps();
            TimeGetDevCaps(ref caps, Marshal.SizeOf(caps));
            return caps;
        }
        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning => _timerId != 0;

        /// <summary>
        /// 计时器构造函数
        /// </summary>
        /// <param name="delay">首次启动计时器延时时间</param>
        /// <param name="period">计时器触发的周期</param>
        /// <param name="cpuIndex">指定计时器线程独占的CPU核心索引，必须>0，不允许为计时器分配0#CPU</param>
        /// <param name="tick">计时器触发时的回调函数</param>
        public MultimediaTimer(OnTickHandle onTickHandle)
        {
            _callback = TimerCallbackMethod;
            Interval = 10;
            _tick = onTickHandle;
        }
        ~MultimediaTimer()
        {
            Dispose(false);
        }
        /// <summary>
        /// 开启计时器
        /// </summary>
        public void Open()
        {
            if (IsRunning)
                return;

            _disposed = false;
            uint dwUser = 0;
            _timerId = TimeSetEvent((uint)Interval, 1, _callback, ref dwUser, _eventType);
            if (_timerId == 0)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Win32Exception(error);
            }
            stopwatch.Start();
        }
        /// <summary>
        /// 销毁当前计时器所占用的资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        /// <summary>
        /// 销毁当前计时器所占用的资源
        /// </summary>
        /// <param name="disposing">是否完全释放资源</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            if (IsRunning)
            {
                TimeKillEvent(_timerId);
                _timerId = 0;
            }
            if (disposing)
            {
                _tick = null;
                GC.SuppressFinalize(this);
            }
        }
        /// <summary>
        /// 获取计时范围
        /// </summary>
        /// <param name="caps"></param>
        /// <returns></returns>
        public bool GetTimerCaps(out TimerCaps caps)
        {
            caps = GetTimerCaps();
            return true;
        }


        /// <summary>
        /// 设置计时器分辨率
        /// </summary>
        /// <param name="RequestResolution"></param>
        /// <param name="Set"></param>
        /// <param name="ActualResolution"></param>
        /// <returns></returns>
        [DllImport("ntdll.dll", EntryPoint = "NtSetTimerResolution")]
        private static extern NtSetTimerResolutionResult NtSetTimerResolution(uint requestResolution, bool set, ref uint actualResolution);
        /// <summary>
        /// 设置计时器分辨率结果
        /// </summary>
        private enum NtSetTimerResolutionResult : uint
        {
            /// <summary>
            /// 设置成功
            /// </summary>
            STATUS_SUCCESS = 0,
            /// <summary>
            /// 未设置
            /// </summary>
            STATUS_TIMER_RESOLUTION_NOT_SET = 0xC0000245
        }

        /// <summary>
        /// 查询计时器设备以确定其分辨率
        /// </summary>
        /// <param name="ptc">指向 TimerCaps 的指针 提供有关计时器设备分辨率的信息</param>
        /// <param name="cbtc">TimerCaps</param>
        /// <returns></returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeGetDevCaps")]
        private static extern int TimeGetDevCaps(ref TimerCaps ptc, int cbtc);
        /// <summary>
        /// 有关计时器分辨率的信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct TimerCaps
        {
            /// <summary>
            /// 支持的最小分辨率（毫秒）
            /// </summary>
            public readonly int wPeriodMin;
            /// <summary>
            /// 支持的最大分辨率（毫秒）
            /// </summary>
            public readonly int wPeriodMax;
        }

        /// <summary>
        /// 设置一个指定参数的计时器
        /// </summary>
        /// <param name="uDelay"></param>
        /// <param name="uResolution"></param>
        /// <param name="lpTimeProc"></param>
        /// <param name="dwUser"></param>
        /// <param name="fuEvent"></param>
        /// <returns></returns>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeSetEvent")]
        private static extern uint TimeSetEvent(uint uDelay, uint uResolution, TimerCallback lpTimeProc, ref uint dwUser, EventType fuEvent);
        /// <summary>
        /// 计时器回调
        /// </summary>
        /// <param name="uTimerID">计时器id</param>
        /// <param name="uMsg">保留</param>
        /// <param name="dwUser">设置计时器时用户提供的值</param>
        /// <param name="dw1">保留</param>
        /// <param name="dw2">保留</param>
        private void TimerCallbackMethod(uint uTimerID, uint uMsg, ref uint dwUser, uint dw1, uint dw2)
        {
            var ticks = stopwatch.ElapsedTicks;
            stopwatch.Restart();
            _tick?.Invoke(this, ticks * 1000.0 / Stopwatch.Frequency / Interval, ticks);
        }


        /// <summary>
        /// 计时器事件类型
        /// </summary>
        private enum EventType : uint
        {
            /// <summary>
            /// 在uDelay毫秒后发生一次事件
            /// </summary>
            TIME_ONESHOT,
            /// <summary>
            /// 事件每uDelay毫秒发生一次
            /// </summary>
            TIME_PERIODIC
        }
        /// <summary>
        /// 取消一个指定的计时器
        /// </summary>
        /// <param name="uTimerId">设置计时器时回调返回的计时器id</param>
        [DllImport("winmm.dll", SetLastError = true, EntryPoint = "timeKillEvent")]
        private static extern void TimeKillEvent(uint uTimerId);


        /// <summary>
        /// 计时器回调
        /// </summary>
        /// <param name="uTimerID">计时器id</param>
        /// <param name="uMsg">保留</param>
        /// <param name="dwUser">设置计时器时用户指定的值</param>
        /// <param name="dw1">保留</param>
        /// <param name="dw2">保留</param>
        private delegate void TimerCallback(uint uTimerID, uint uMsg, ref uint dwUser, uint dw1, uint dw2);
    }
}
