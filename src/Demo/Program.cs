using m3u8Video;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            //多线程下载 这测试的uri文件较大 100M左右 建议替换成自己的uri
            string uri = "http://1252093142.vod2.myqcloud.com/4704461fvodcq1252093142/f865d8a05285890787810776469/playlist.f3.m3u8";
            TsHelper tsHelper = new TsHelper($"{uri}", @"C:\temp\存放所有视频的base文件夹", "我的测试视频1文件夹");
            tsHelper.ReuqestAndSave();


            //Console.WriteLine("单线程开始");
            ////单线程下载：
            //TsHelper tsHelperSingle = new TsHelper($"{uri}", @"C:\temp\存放所有视频的base文件夹", "我的测试视频1文件夹",false);
            //tsHelperSingle.ReuqestAndSave();


            ////下载后再次用随机生成密钥加密
            //TsHelper tsHelperEn = new TsHelper($"{uri}", @"C:\temp\存放所有视频的base文件夹", "我的测试视频1文件夹");
            //tsHelperEn.ReuqestAndSave(true);
        }
    }
}
