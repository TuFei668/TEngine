using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class StringUtil 
{
    private static MD5 md5_caculate = null;
    private static StringBuilder builder = null;
    public static int getWordCount(string str)
    {
        if (str == null)
            return 0;
        return str.Length;
    }

    public static string getSubStr(string str, int nFrom, int nLen)
    {
        if (str == null)
            return "";
        int nTotal = str.Length;
        if (nFrom > nTotal - 1)
        {
            return "";
        }
        if (nFrom + nLen > nTotal)
        {
            nLen = nTotal - nFrom;
        }
        return str.Substring(nFrom, nLen);
    }

    /// <summary>
    ///获取指定字符串的HashCode 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static int get_hash_code(string str)
    {
        Debug.Log("str = " + str);
        if (string.IsNullOrEmpty(str))
        {
            return -1;
        }
        return str.GetHashCode();
    }

    public static string get_md5(string str)
    {
        if (md5_caculate == null) {
            md5_caculate = MD5.Create();
        }
        if(builder == null)
        {
            builder = new StringBuilder();
        }
        builder.Clear();

        byte[] data = md5_caculate.ComputeHash(Encoding.UTF8.GetBytes(str));

        for (int i = 0; i < data.Length; i++)
        {
            builder.Append(data[i].ToString("x2"));
        }
        return builder.ToString();
    }
}
