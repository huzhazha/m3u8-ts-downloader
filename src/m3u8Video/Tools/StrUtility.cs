using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3u8Video.Tools
{
    public class StrUtility
    {
        /// <summary>
        /// 去掉文件名中的无效字符,如 \ / : * ? " < > | 
        /// </summary>
        /// <param name="fileName">待处理的文件名</param>
        /// <returns>处理后的文件名</returns>
        public static string ReplaceBadCharOfFileName(string fileName)
        {
            string str = fileName;
            str = str.Replace("\\", string.Empty);
            str = str.Replace("/", string.Empty);
            str = str.Replace(":", string.Empty);
            str = str.Replace("*", string.Empty);
            str = str.Replace("?", string.Empty);
            str = str.Replace("\"", string.Empty);
            str = str.Replace("<", string.Empty);
            str = str.Replace(">", string.Empty);
            str = str.Replace("|", string.Empty);
            str = str.Replace(" ", string.Empty);    //前面的替换会产生空格,最后将其一并替换掉
            return str;
        }
    }
}
