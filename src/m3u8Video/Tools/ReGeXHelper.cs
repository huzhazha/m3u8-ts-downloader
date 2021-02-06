using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace m3u8Video.Tools
{
    public class ReGeXHelper
    {
        //string pattern = @"^[#]{0,1}创建表格[ ]{0,3}(.*?)[ ]{1,3}(.*?)$";
        //^[#]{0,1}(.*?)[ ]{1,3}
        public static Match GetRes(string input, string pattern)  //返回值 访问XX.groups[0--count-1].value  ,groups[0]是原来的
        {
            var temp = Regex.Matches(input, pattern);
            if (temp.Count == 1)
            {
                return temp[0];   //
            }
            else if (temp.Count == 0)
            {
                return null;
            }

            throw new Exception("多个匹配");
        }

        public static List<Match> GetResList(string input, string pattern)  //返回值 访问XX.groups[0--count-1].value  ,groups[0]是原来的
        {
            List<Match> list = new List<Match>();
            var temp = Regex.Matches(input, pattern);
            if (false)//temp.Count == 1
            {
                throw new Exception("1个匹配");
            }
            else
            {
                for (int i = 0; i < temp.Count; i++)
                {
                    list.Add(temp[i]);
                }
            }
            return list;


        }
    }
}
