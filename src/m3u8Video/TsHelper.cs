using m3u8Video.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace m3u8Video
{
    /// <summary>
    /// 解密用的
    /// </summary>
    class AesConf
    {
        public string method;
        public string uri;
        public string iv;
        public byte[] key;

    }
    /// <summary>
    /// 用于生成M3U8的 配置
    /// </summary>
    class OutFile
    {
        public List<string> PathTime = new List<string>();
        public List<string> Times = new List<string>();
        public string version;
        public string targetduration;
        public string sequence;

        public bool saveAsEncrpy;  //是否用自己的密钥加密
        public byte[] tsAESKey;   //自己加密密钥
        public string tsAESURIName = "key";


        public string Template = "#EXTM3U\n" +
                                "#EXT-X-VERSION:{VERSION}\n" +
                                "#EXT-X-TARGETDURATION:{TARGETDURATION}\n" +
                                "#EXT-X-MEDIA-SEQUENCE:{SEQUENCE}\n" +
                                "#EXT-X-KEY:METHOD=AES-128,URI=\"{KEYURI}\"\n" +
                                "{CONTENT}" +
                                "#EXT-X-ENDLIST";
    }


    public class TsHelper
    {
        #region 私有变量
        //解密配置
        private AesConf aesConf = new AesConf();
        private string M3U8Url = default;
        private string M3U8Response = default;
        //ts文件
        private List<string> TsTimes = new List<string>();
        private List<string> TsPaths = new List<string>();
        private List<string> TsBadDownloadUrl = new List<string>();  //失败重下载列表
        //按照github中 利用index 构造iv偏移量
        private int index;
        //存储地址
        private string BasePath;
        private string FolderName;
        //输出配置
        private OutFile outFileConfig = new OutFile();

        //其他设置
        private bool UseMutiDownload;  //多线程下载
        private bool IsComplete = true;



        private readonly object locker = new object();
        #endregion


        #region 公开函数
        public TsHelper(string m3u8Url, string videoBasePath, string folderName, bool useMutiDownload = true)
        {
            M3U8Url = m3u8Url;
            BasePath = videoBasePath;
            FolderName = StrUtility.ReplaceBadCharOfFileName(folderName);
            UseMutiDownload = useMutiDownload;

        }


        /// <summary>
        /// 起始函数 下载并保存对应M3U8对应的文件
        /// </summary>
        public bool ReuqestAndSave(bool saveAsEncrpy = false)
        {
            outFileConfig.saveAsEncrpy = saveAsEncrpy;

            Logger.LogDebug("请求" + M3U8Url);
            RequestM3U8();

            if (String.IsNullOrWhiteSpace(M3U8Response)) { Logger.LogDebug("m3u8Response无"); return false; }
            Logger.LogDebug("解析" + M3U8Url);
            AnalysisM3U8();
            return IsComplete;
        }

        #endregion

        #region 私有函数
        /// <summary>
        /// 请求m3u8地址
        /// </summary>
        private void RequestM3U8()
        {
            M3U8Response = RequestM3U8(this.M3U8Url);
        }

        /// <summary>
        /// 解析m3u8 并保存成自己的
        /// </summary>
        private void AnalysisM3U8()
        {
            //判断是否  多码率适配流 并重新请求
            IsMutiple();

            //如果需要自己重新加密的 那么需要生成对应的key
            if (outFileConfig.saveAsEncrpy) GenerateAESKey();

            Logger.LogDebug("分解出所有的ts文件" + M3U8Response);
            //分解出所有的ts文件
            SpliteM3U8(M3U8Response);

            //检测视频 AES 加密
            if (M3U8Response.IndexOf("#EXT-X-KEY") != -1)
            {
                //.*METHOD=([^,]+) #EXT-X-KEY:METHOD=AES-128,
                var method = ReGeXHelper.GetRes(M3U8Response, @".*METHOD=([^,]+)");
                aesConf.method = method != null ? method.Groups[1].Value : "";

                //.*URI="([^"]+)  URI="key.key" 
                var uri = ReGeXHelper.GetRes(M3U8Response, @".*URI=""([^""]+)");
                aesConf.uri = uri != null ? uri.Groups[1].Value : "";

                //.*IV=([^,]+) 
                var iv = ReGeXHelper.GetRes(M3U8Response, @".*IV=([^,]+)");
                aesConf.iv = iv != null ? iv.Groups[1].Value : "";

                aesConf.uri = ApplyURL(aesConf.uri, M3U8Url);

                Logger.LogDebug("解密" + aesConf.uri);
                GetAES();
            }
            else
            {
                DownloadTS();
            }
            Logger.LogDebug("保存新的m3u8");
            OutSaveEnd();

        }


        /// <summary>
        /// 判断是否是多码率
        /// </summary>
        /// <returns></returns>
        private bool IsMutiple()
        {
            //#EXT-X-STREAM-INF.*?\n(.*)
            var inf = ReGeXHelper.GetResList(M3U8Response, @"#EXT-X-STREAM-INF.*?\n(.*)");
            if (inf.Count != 0)
            {
                M3U8Url = ApplyURL(inf[0].Groups[1].Value, M3U8Url);
                RequestM3U8();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 有加密 才会进入的函数
        /// </summary>
        private void GetAES()
        {
            string url = aesConf.uri;
            //请求获取key
            string key = new WebApiInvoker().InvokeWebAPI(url);
            aesConf.key = Encoding.UTF8.GetBytes(key);

            DownloadTS();


        }

        /// <summary>
        /// 生成 ASE的密钥 16位
        /// </summary>
        private void GenerateAESKey()
        {
            string randomStr =RandomStringBuilder.Create(16).ToLower();
            outFileConfig.tsAESKey = Encoding.UTF8.GetBytes(randomStr);

        }

        private void DownloadExitSafe(List<Action> actions, int seconds = 60)
        {
            List<Task> tasks = new List<Task>();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;  //统一取消标志
            try
            {
                for (int i = 0; i < actions.Count; i++)
                {
                    Logger.LogDebug("i:i=" + i);
                    int num = i;
                    try
                    {
                        Task task = Task.Run(
                            actions[num]
                        , token);
                        tasks.Add(task);
                        //task.Wait(TimeSpan.FromMilliseconds(10000));
                        // task.Wait((seconds));
                    }
                    catch (Exception e)
                    {
                        //IsComplete = false;
                        Console.WriteLine(e.Message);
                    }

                }
                //Task.WaitAny(tasks.ToArray());
                //tasks[tasks.Count-1].Wait((seconds));  //超过180s就自动退出  只要完成就不会阻塞
                //Console.WriteLine("取消");
                if (actions.Count == 0) { IsComplete = false; Logger.LogError("检查M3U8的Response！"); return; }
                TimerHelper timerHelper = new TimerHelper();
                System.Timers.Timer timer = timerHelper.InitTimer(1000 * seconds, false);
                timerHelper.Action = () => { source.Cancel(); };
                timer.Start();

                Task.WaitAll(tasks.ToArray());  //因为cancel会产生异常
            }
            catch (AggregateException ex)
            {
                IsComplete = false;
                Console.WriteLine(ex.Message);
                foreach (var e in ex.InnerExceptions)
                {
                    Logger.LogDebug(String.Format("\nhi,我是OperationCanceledException：{0}\n", e.Message));
                    //Console.WriteLine("\nhi,我是OperationCanceledException：{0}\n", e.Message);
                }
            }
            for (int j = 0; j < tasks.Count; j++)
            {
                Logger.LogDebug(String.Format("task{0}是不是被取消了？ {1}", j, tasks[j].IsCanceled));
                //Console.WriteLine("task{0}是不是被取消了？ {1}", j, tasks[j].IsCanceled);
                if (tasks[j].IsCanceled)
                {
                    TsBadDownloadUrl.Add(TsPaths[j]);
                }
            }
        }

        private void DownloadTS()
        {
            if (UseMutiDownload)
            {
                MutiDownloadTS();
                //为失败的地址重新下 目前一次机会
                if (TsBadDownloadUrl.Count != 0)
                {
                    Logger.LogDebug("重新下载 失败的文件");
                    TsPaths = TsBadDownloadUrl;
                    MutiDownloadTS();
                }

            }
            else
            {
                SingleDownloadTS();
                //为失败的地址重新下 目前一次机会
                if (TsBadDownloadUrl.Count != 0)
                {
                    Logger.LogDebug("重新下载 失败的文件");
                    TsPaths = TsBadDownloadUrl;
                    SingleDownloadTS();
                }

            }


        }

        /// <summary>
        /// 多线程下载
        /// </summary>
        private void MutiDownloadTS()
        {
            index = 0;
            //List<Task> tasks = new List<Task>();
            List<Action> actions = new List<Action>();
            TsPaths.ForEach(tsPath =>
            {

                int curIndex = index++;
                actions.Add(() =>
                {
                    try
                    {
                        string tsUrl = ApplyURL(tsPath, M3U8Url);
                        Logger.LogDebug($"请求file:{curIndex}");
                        byte[] file = new WebApiInvoker().RequestFile(tsUrl);
                        Logger.LogDebug($"请求file:{curIndex}结束");
                        //var res = ByteHelper.WriteByteToFile(file, @"C:/temp/Test2/v1.ts");
                        file = DealTS(file, curIndex);  //解密 如果有就解密  没有就原样返回
                        if (file == default) { return; }
                        string dicTsPath = GetTSDic();
                        string tsName = $"__{curIndex.ToString().PadLeft(5, '0')}";
                        if (outFileConfig.saveAsEncrpy)  //需要加密保存
                        {
                            file = TsEncrypt(file, curIndex);
                        }

                        ByteHelper.WriteByteToFile(file, GetTsPath(tsName));
                        OutConfigSave(tsName, curIndex, GetTsPath(tsName));
                    }
                    catch (Exception ex)
                    {
                        TsBadDownloadUrl.Add(tsPath);
                        //IsComplete = false;
                        Logger.LogError("下载失败：" + tsPath + "::" + ex.Message);
                    }
                });




            });
            Logger.LogDebug("开启下载");
            //给定下载的延迟 不然有时候waitall并不退出 时间是timer * action的个数
            DownloadExitSafe(actions, 5 * actions.Count);
            Logger.LogDebug("下载结束");
            //Task.WaitAll(tasks.ToArray());


        }


        /// <summary>
        /// 单线程下载
        /// </summary>
        private void SingleDownloadTS()
        {

            Logger.LogDebug("开启下载");
            TsPaths.ForEach(tsPath =>
            {

                int curIndex = index++;

                try
                {
                    string tsUrl = ApplyURL(tsPath, M3U8Url);
                    Logger.LogDebug("请求file");
                    byte[] file = new WebApiInvoker().RequestFile(tsUrl);
                    Logger.LogDebug("请求file结束");
                    //var res = ByteHelper.WriteByteToFile(file, @"C:/temp/Test2/v1.ts");
                    file = DealTS(file, curIndex);  //解密 如果有就解密  没有就原样返回
                    if (file == default) { return; }
                    if (outFileConfig.saveAsEncrpy)  //需要加密保存
                    {
                        file = TsEncrypt(file, curIndex);
                    }

                    string dicTsPath = GetTSDic();
                    string tsName = $"__{curIndex.ToString().PadLeft(5, '0')}";
                    ByteHelper.WriteByteToFile(file, GetTsPath(tsName));
                    OutConfigSave(tsName, curIndex, GetTsPath(tsName));
                }
                catch (Exception ex)
                {
                    TsBadDownloadUrl.Add(tsPath);
                    //IsComplete = false;
                    Logger.LogError(ex.Message);
                }


            });


            Logger.LogDebug("下载结束");
            //Task.WaitAll(tasks.ToArray());


        }


        /// <summary>
        /// 对Ts文件加密
        /// </summary>
        /// <param name="file"></param>
        /// <param name="curIndex"></param>
        /// <returns></returns>
        private byte[] TsEncrypt(byte[] file, int curIndex)
        {
            byte[] arrayIv = default;
            arrayIv = ListToBytes(new ArrayList() {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, curIndex/255, curIndex%255
            });
            byte[] data = default;
            if (file != null && file.Length != 0)
            {
                data = AES128.AESEncrypt(file, arrayIv, outFileConfig.tsAESKey);
            }
            else
            {
                Logger.LogDebug("file为空");
            }

            return data;
        }


        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="file"></param>
        /// <param name="curIndex"></param>
        /// <returns></returns>
        private byte[] DealTS(byte[] file, int curIndex)
        {
            //偏移量
            byte[] arrayIv = default;
            if (string.IsNullOrEmpty(aesConf.iv))
            {
                arrayIv = ListToBytes(new ArrayList() {
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, curIndex/255, curIndex%255
                });
            }
            else
            {
                arrayIv = Encoding.UTF8.GetBytes(aesConf.iv);
            }
            byte[] data = default;
            if (file != null && file.Length != 0)
            {
                data = !string.IsNullOrEmpty(aesConf.uri) ? AES128.AESDecrypt(file, arrayIv, aesConf.key) : file;
            }
            else
            {
                //IsComplete = false;
                Logger.LogDebug("file为空");
            }
            //解密 AES

            return data;
        }

        private byte[] ListToBytes(ArrayList al)
        {
            byte[] id_list = al.Cast<object>().Select(t => Convert.ToByte(t)).ToArray();
            return id_list;
        }

        private string ApplyURL(string targetURL, string baseURL)
        {

            if (targetURL.IndexOf("http") != -1)
            {
                return targetURL;
            }
            else if (targetURL[0] == '/')
            {
                var domain = baseURL.Split('/');
                return domain[0] + "//" + domain[2] + targetURL;
            }
            else
            {
                var domain = baseURL.Split('/');
                var domainList = domain.ToList();
                domainList.Remove(domain[domain.Length - 1]);
                domain = domainList.ToArray();
                return String.Join("/", domain) + "/" + targetURL;
            }
        }


        private void SpliteM3U8(string m3u8Response)
        {
            var matches = ReGeXHelper.GetResList(m3u8Response, @"#EXTINF:(.*)?,\n(.*?\.ts)");
            //获取所有的ts片段信息
            matches.ForEach(m =>
            {
                TsTimes.Add(m?.Groups[1].Value);
                TsPaths.Add(m?.Groups[2].Value);
            });
        }

        /// <summary>
        /// 请求M3U8
        /// </summary>
        /// <param name="url">互联网上 M3U8的地址</param>
        /// <returns></returns>
        private string RequestM3U8(string url)
        {
            WebApiInvoker webApiInvoker = new WebApiInvoker();
            return webApiInvoker.InvokeWebAPI(url);

        }


        #region 获取保存地址

        /// <summary>
        /// 全路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetM3U8Path(string name = "__main__")
        {
            return GetTSDic() + @"\" + name + ".m3u8";
        }

        /// <summary>
        /// 全路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetTsPath(string name)
        {
            return GetTSDic() + @"\" + name + ".ts";
        }

        /// <summary>
        /// 全路径
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetAESKeyPath(string name)
        {
            return GetTSDic() + @"\" + name + ".key";
        }

        /// <summary>
        /// 全路径
        /// </summary>
        /// <returns></returns>
        private string GetTSDic()
        {
            string dicPath = $@"{BasePath}\{FolderName}\{(outFileConfig.saveAsEncrpy ? "en" : "")}ts";
            if (!Directory.Exists(dicPath))
            {
                Directory.CreateDirectory(dicPath);
            }
            return dicPath;
        }

        #endregion

        #region 输出m3u8
        /// <summary>
        /// 每个Ts 的时间和相对路径
        /// </summary>
        /// <param name="tsName"></param>
        /// <param name="curIndex"></param>
        /// <param name="fullPath"></param>
        private void OutConfigSave(string tsName, int curIndex, string fullPath)
        {
            //outFileConfig.PathTime.Add(new PathTimeNode() { 
            //    path = tsName,
            //    time = TsTimes[curIndex]
            //} );;  //相对 m3u的路径
            lock (locker)
            {
                outFileConfig.PathTime.Add(tsName);
                outFileConfig.Times.Add(TsTimes[curIndex]);  //时间
            }


            Logger.LogDebug($"index:{curIndex},:tsName:{tsName},tsTime:{TsTimes[curIndex]}");
            if (outFileConfig.PathTime.Count != outFileConfig.Times.Count)
            {
                Logger.LogDebug("此处不一致,将输出对比");
                Logger.LogDebug(String.Join("-", outFileConfig.PathTime));
                Logger.LogDebug(String.Join("-", outFileConfig.Times));
                Logger.LogDebug("对比结束");
            }

        }

        /// <summary>
        /// 最后写入属于自己的M3U8
        /// </summary>
        private void OutSaveEnd()
        {
            string m3u8 = M3U8Response;
            //判断是否请求成功  利用//#EXTM3U  每个 M3U 文件必须将该标签放置在第一行
            var m3u8Top = ReGeXHelper.GetRes(m3u8, @"#EXTM3U");
            if (m3u8Top == null)
            {
                Logger.LogError("请求失败（最后写入属于自己的M3U8）：" + M3U8Response);
                IsComplete = false;
                return;
            }
            Logger.LogDebug($"m3u8Top::{m3u8Top}");

            //版本
            var version = ReGeXHelper.GetRes(m3u8, @"#EXT-X-VERSION:(.*)");
            outFileConfig.version = version != null ? version.Groups[1].Value : "0";
            Logger.LogDebug($"version::{outFileConfig.version}");

            if (outFileConfig.Times.Count == 0)
            {
                Logger.LogDebug("请求完成后，并没有下载到ts文件，自动退出");
                IsComplete = false;
                return;
            }

            //#EXT-X-TARGETDURATION 最大时间 int
            var targetduration = ReGeXHelper.GetRes(m3u8, @"#EXT-X-TARGETDURATION:(.*)");
            outFileConfig.targetduration = targetduration != null ? targetduration.Groups[1].Value : (((int)Math.Ceiling(outFileConfig.Times.Max(m => Convert.ToDouble(m)))).ToString());
            Logger.LogDebug($"targetduration::{outFileConfig.targetduration}");

            //#EXT-X-MEDIA-SEQUENCE
            var sequence = ReGeXHelper.GetRes(m3u8, @"#EXT-X-SEQUENCE:(.*)");
            outFileConfig.sequence = sequence != null ? sequence.Groups[1].Value : "0";
            Logger.LogDebug($"sequence::{outFileConfig.sequence}");

            outFileConfig.Template = outFileConfig.Template.Replace("{VERSION}", outFileConfig.version);
            outFileConfig.Template = outFileConfig.Template.Replace("{TARGETDURATION}", outFileConfig.targetduration);
            outFileConfig.Template = outFileConfig.Template.Replace("{SEQUENCE}", outFileConfig.sequence);

            //是否加密的处理
            OutSaveAESEncrypt();

            Logger.LogDebug($"开始content");

            string allContent = "";
            var paths = outFileConfig.PathTime.ToArray();
            var times = outFileConfig.Times.ToArray();
            Logger.LogDebug($"开始sort::{paths.Length},{times.Length}");
            Array.Sort(paths, times);
            Logger.LogDebug($"sortj结束::{paths.Length}");
            for (int i = 0; i < paths.Length; i++)
            {
                var time = times[i];
                var path = paths[i];
                string content = $"#EXTINF:{time},\n" +
                                          $"{path}.ts\n";
                allContent += content;
                Logger.LogDebug($"content::{content}");

            }

            //{CONTENT}
            outFileConfig.Template = outFileConfig.Template.Replace("{CONTENT}", allContent);
            Logger.LogDebug($"Template::{outFileConfig.Template}");

            using (FileStream file = new FileStream($"{GetM3U8Path()}", FileMode.Create, FileAccess.ReadWrite))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(outFileConfig.Template);
                file.Write(bytes, 0, bytes.Length);
            }

        }

        private void OutSaveAESEncrypt()
        {
            if (outFileConfig.saveAsEncrpy)
            {
                outFileConfig.Template = outFileConfig.Template.Replace("{KEYURI}", outFileConfig.tsAESURIName + ".key");
                //保存密码文件
                string key = Encoding.UTF8.GetString(outFileConfig.tsAESKey);
                Logger.LogDebug($"写入file加密key::{key}");
                using (FileStream file = new FileStream($"{GetAESKeyPath(outFileConfig.tsAESURIName)}", FileMode.Create, FileAccess.ReadWrite))
                {

                    byte[] bytes = Encoding.UTF8.GetBytes(key);
                    file.Write(bytes, 0, bytes.Length);
                }

            }
            else
            {  //如果没有就清楚这一段话
                outFileConfig.Template = outFileConfig.Template.Replace("#EXT-X-KEY:METHOD=AES-128,URI=\"{KEYURI}\"\n", "");
            }
        }

        #endregion

        #endregion
    }
}
