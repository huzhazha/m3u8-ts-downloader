using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace m3u8Video.Tools
{
    public class WebApiInvoker : IDisposable
    {
        HttpClientHandler myHandler = new HttpClientHandler();
        HttpClient myClient;


        public WebApiInvoker()
        {
            //重定向响应
            myHandler.AllowAutoRedirect = true;
            //使用Cookies
            myHandler.UseCookies = true;

            myClient = new HttpClient(myHandler);  //GetUserAgent  //Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.116 Safari/537.36
            myClient.DefaultRequestHeaders.Add("User-Agent", $"{GetUserAgent()}");//加头
            myClient.Timeout = TimeSpan.FromSeconds(30);

        }




        public string InvokeWebAPI(string uri)
        {
            var task = myClient.GetAsync(uri);
            try
            {
                task.Result.EnsureSuccessStatusCode();

                HttpResponseMessage response = task.Result;
                var result = response.Content.ReadAsStringAsync();
                return result.Result;
            }
            catch (Exception ex)
            {
                return InvokeWeb(uri);
            }

        }


        private string InvokeWeb(string uri)
        {
            try
            {
                var client = new RestClient(uri);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                client.UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 13_2_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.3 Mobile/15E148 Safari/604.1";
                IRestResponse response = client.Execute(request);
                return (response.Content);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }



        }

        /// <summary>
        /// Post
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="KeyValueDict"></param>
        /// <returns></returns>
        public string PostDataToWebAPI(string uri, Dictionary<string, string> KeyValueDict)
        {

            var content = new FormUrlEncodedContent(KeyValueDict);
            try
            {
                var task = myClient.PostAsync(uri, content);
                task.Result.EnsureSuccessStatusCode();
                HttpResponseMessage response = task.Result;
                var result = response.Content.ReadAsStringAsync();
                return result.Result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        ///<summary>
        /// 下载文件
        /// </summary>
        /// <param name="URL">下载文件地址</param>
        /// <param name="Filename">下载后另存为（全路径）</param>
        public bool DownloadFile(string URL, string filename)
        {
            HttpWebRequest Myrq = default;
            HttpWebResponse myrp = default;
            Stream st = default;
            Stream so = default;
            try
            {
                Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                Myrq.Timeout = 10000 * 20;
                Myrq.UserAgent = GetUserAgent();
                //if (!Myrq.HaveResponse) {
                //    return false;
                //}
                //Myrq.Method = "POST";
                //Myrq.
                myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                if (myrp == null)
                {
                    return false;
                }
                st = myrp.GetResponseStream();
                so = new System.IO.FileStream(filename, System.IO.FileMode.Create);
                byte[] by = new byte[1024];
                int osize = st.Read(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    so.Write(by, 0, osize);
                    osize = st.Read(by, 0, (int)by.Length);
                }
                so.Close();
                st.Close();
                myrp.Close();
                Myrq.Abort();
                return true;
            }
            catch (System.Exception e)
            {
                //Console.WriteLine(e.Message);
                Logger.LogError($"{e.Message},Url={URL}");
                if (so != null)
                {
                    so.Close();
                }
                if (st != null)
                {
                    st.Close();
                }
                if (myrp != null)
                {
                    myrp.Close();
                }
                if (Myrq != null)
                {
                    Myrq.Abort();
                }
                //Logger.LogDebug(e.Message);
                return false;

            }
            finally
            {


            }
        }


        ///<summary>
        /// 下载文件
        /// </summary>
        /// <param name="URL">下载文件地址</param>
        /// <param name="Filename">下载后另存为（全路径）</param>
        public byte[] RequestFile(string URL)
        {
            HttpWebRequest Myrq = default;
            HttpWebResponse myrp = default;
            Stream st = default;

            try
            {
                Myrq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(URL);
                Myrq.Timeout = 1000 * 80;
                Myrq.UserAgent = GetUserAgent();
                myrp = (System.Net.HttpWebResponse)Myrq.GetResponse();
                if (myrp == null)
                {
                    return default;
                }
                st = myrp.GetResponseStream();
                var memoryStream = StreamToMemoryStream(st);

                byte[] bytes = memoryStream.ToArray();

                memoryStream.Close();
                //so.Close();
                st.Close();
                myrp.Close();
                Myrq.Abort();
                return bytes;
            }
            catch (System.Exception e)
            {
                Logger.LogError(e.Message);
                //Logger.LogDebug(e.Message);

                if (st != null)
                {
                    st.Close();
                }
                if (myrp != null)
                {
                    myrp.Close();
                }
                if (Myrq != null)
                {
                    Myrq.Abort();
                }
                //Logger.LogDebug(e.Message);
                return default;

            }

        }

        /// <summary>
        /// stream流 转MemoryStream 主要是网络中的stream.length会报错
        /// </summary>
        /// <param name="instream"></param>
        /// <returns></returns>
        MemoryStream StreamToMemoryStream(Stream instream)
        {
            MemoryStream outstream = new MemoryStream();
            const int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int count = 0;
            while ((count = instream.Read(buffer, 0, bufferLen)) > 0)
            {
                outstream.Write(buffer, 0, count);
            }
            return outstream;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }


        public static string GetUserAgent()
        {

            var user_agent_list = new List<string>() {
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/22.0.1207.1 Safari/537.1",
                "Mozilla/5.0 (X11; CrOS i686 2268.111.0) AppleWebKit/536.11 (KHTML, like Gecko) Chrome/20.0.1132.57 Safari/536.11",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1092.0 Safari/536.6",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.6 (KHTML, like Gecko) Chrome/20.0.1090.0 Safari/536.6",
                "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/19.77.34.5 Safari/537.1",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.9 Safari/536.5",
                "Mozilla/5.0 (Windows NT 6.0) AppleWebKit/536.5 (KHTML, like Gecko) Chrome/19.0.1084.36 Safari/536.5",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_8_0) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1063.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1062.0 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.1 Safari/536.3",
                "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/536.3 (KHTML, like Gecko) Chrome/19.0.1061.0 Safari/536.3",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24",
                "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/535.24 (KHTML, like Gecko) Chrome/19.0.1055.1 Safari/535.24"
            };

            int randNum = new Random().Next(0, user_agent_list.Count);

            return user_agent_list[randNum];
        }
    }
}
