using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace m3u8Video.Tools
{
    public class AES128
    {
        /// AES加密
        /// </summary>
        /// <param name="inputdata">输入的数据</param>
        /// <param name="iv">向量128位</param>
        /// <param name="strKey">加密密钥</param>
        /// <returns></returns>
        public static byte[] AESEncrypt(byte[] inputdata, byte[] iv, byte[] key)
        {
            //分组加密算法  
            SymmetricAlgorithm des = Rijndael.Create();
            byte[] inputByteArray = inputdata;//得到需要加密的字节数组      
            //设置密钥及密钥向量
            des.Key = key; //Encoding.UTF8.GetBytes(strKey.Substring(0, 32));
            des.IV = iv;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    byte[] cipherBytes = ms.ToArray();//得到加密后的字节数组  
                    cs.Close();
                    ms.Close();
                    return cipherBytes;
                }
            }
        }


        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="inputdata">输入的数据</param>
        /// <param name="iv">向量128</param>
        /// <param name="strKey">key</param>
        /// <returns></returns>
        public static byte[] AESDecrypt(byte[] inputdata, byte[] iv, byte[] key)
        {
            SymmetricAlgorithm des = Rijndael.Create();
            des.Key = key;// Encoding.UTF8.GetBytes(strKey.Substring(0, 32));
            des.IV = iv;
            byte[] decryptBytes = new byte[inputdata.Length];
            using (MemoryStream ms = new MemoryStream(inputdata))
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.Read(decryptBytes, 0, decryptBytes.Length);
                    cs.Close();
                    ms.Close();
                }
            }
            return decryptBytes;
        }
    }
}
