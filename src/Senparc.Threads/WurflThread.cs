namespace Senparc.Threads
{
    ///// <summary>
    ///// 异步初始化的线程
    ///// </summary>
    //public class WurflThread
    //{
    //    public WurflThread()
    //    {
    //    }

    //    public void Run()
    //    {
    //        try
    //        {
    //            lock (WurflUtility.RegisterLock)
    //            {
    //                Thread.Sleep(10 * 1000);//停止10秒，先等第一个请求的页面通过

    //                WurflUtility.RegisterState = "begin";
    //                var startTime = DateTime.Now;
    //                Senparc.Utility.WurflUtility.RegisterWurfl();
    //                var endTime = DateTime.Now;
    //                Senparc.Utility.WurflUtility.WurflStartupTime = (endTime - startTime).TotalSeconds.ToString("##.##");
    //                WurflUtility.RegisterState = "finish";
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            WurflUtility.RegisterState = "failed";
    //            LogUtility.WebLogger.Error(ex.Message, ex);
    //        }
    //    }
    //}
}
