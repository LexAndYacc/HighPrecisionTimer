using System;
using System.Collections.Generic;
using System.Text;

namespace HighPrecisionTimer
{
    /// <summary>
    /// 计时器事件的委托定义
    /// </summary>
    /// <param name="sender">事件的发起者，即计时器对象</param>
    /// <param name="jumpPeriod">上次调用和本次调用跳跃的周期数</param>
    /// <param name="interval">上次调用和本次调用之间的间隔时间</param>
    public delegate void OnTickHandle(object sender, double jumpPeriod, long interval);
}
