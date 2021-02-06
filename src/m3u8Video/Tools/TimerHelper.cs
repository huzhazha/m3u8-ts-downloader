using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace m3u8Video.Tools
{
    class TimerHelper
    {
        //定义全局变量
        public int currentCount = 0;
        //定义Timer类
        System.Timers.Timer timer;
        public Action Action;

        /// <summary>
        /// 初始化Timer控件
        /// </summary>
        public Timer InitTimer(int interval, bool AutoReset)
        {
            //设置定时间隔(毫秒为单位)
            timer = new System.Timers.Timer(interval);
            //设置执行一次（false）还是一直执行(true)
            timer.AutoReset = AutoReset;
            //设置是否执行System.Timers.Timer.Elapsed事件
            timer.Enabled = true;
            //绑定Elapsed事件
            timer.Elapsed += new System.Timers.ElapsedEventHandler(TimerUp);
            return timer;
        }

        /// <summary>
        /// Timer类执行定时到点事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Action.Invoke();


            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                // MessageBox.Show("执行定时到点事件失败:" + ex.Message);
            }
        }

    }
}
