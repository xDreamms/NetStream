using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace NetStream
{
    public static class StringExtensions
    {
        static readonly Encoding UTF8 = Encoding.UTF8;

        public static string UrlEncodeQueryUTF8(this string str)
            => HttpUtility.UrlEncode(str, UTF8).Replace("+", "%20");

        public static string UrlEncodeUTF8(this string str)
            => HttpUtility.UrlEncode(str, UTF8);

        public static string UrlDecodeUTF8(this string str)
            => HttpUtility.UrlDecode(str, Encoding.UTF8);
        public static bool HasSpecialChar(this string input)
        {
            string specialChar = @"\|!#$%&/=?»«@£§€{};'<>, ";
            foreach (var item in specialChar)
            {
                if (input.Contains(item)) return true;
            }

            return false;
        }
        public static string MakeStringWithoutSpecialChar(this string input)
        {
            string result = input;
            string specialChar = @"\|!#$%&/=?»«@£§€{};'<>, ";
            foreach (var item in specialChar)
            {
                if (result.Contains(item))
                {
                    result = result.Replace(item,'.');
                }
            }

            return result;
        }
    }
}
