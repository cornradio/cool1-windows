using System;
using System.Text;

namespace Cool1Windows.Services
{
    public static class PinyinService
    {
        /// <summary>
        /// 获取汉字字符串的首字母拼音
        /// </summary>
        public static string GetInitials(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (char c in text)
            {
                sb.Append(GetFirstLetter(c));
            }
            return sb.ToString();
        }

        private static char GetFirstLetter(char c)
        {
            // 如果不是汉字，直接返回原字符
            if (c < 0x4e00 || c > 0x9fa5)
            {
                return char.ToLower(c);
            }

            try
            {
                // 使用 GB2312 编码进行拼音首字母提取（仅限简体中文）
                byte[] array = Encoding.GetEncoding("GB2312").GetBytes(c.ToString());
                if (array.Length < 2) return c;

                int i = array[0] * 256 + array[1];

                if (i < 0xB0A1) return c;
                if (i < 0xB0C5) return 'a';
                if (i < 0xB2C1) return 'b';
                if (i < 0xB4EE) return 'c';
                if (i < 0xB6EA) return 'd';
                if (i < 0xB7A2) return 'e';
                if (i < 0xB8C1) return 'f';
                if (i < 0xB9FE) return 'g';
                if (i < 0xBBF7) return 'h';
                if (i < 0xBFA6) return 'j';
                if (i < 0xC0AC) return 'k';
                if (i < 0xC2E8) return 'l';
                if (i < 0xC4C3) return 'm';
                if (i < 0xC5B6) return 'n';
                if (i < 0xC5BE) return 'o';
                if (i < 0xC6DA) return 'p';
                if (i < 0xC8BB) return 'q';
                if (i < 0xC8F6) return 'r';
                if (i < 0xCBFA) return 's';
                if (i < 0xCDDA) return 't';
                if (i < 0xCEF4) return 'w';
                if (i < 0xD1B9) return 'x';
                if (i < 0xD4D1) return 'y';
                if (i < 0xD7FA) return 'z';
            }
            catch
            {
                return c;
            }

            return c;
        }

        /// <summary>
        /// 检查源字符串是否包含搜索关键词（支持原名匹配和首字母匹配）
        /// </summary>
        public static bool Match(string source, string query)
        {
            if (string.IsNullOrEmpty(query)) return true;
            if (string.IsNullOrEmpty(source)) return false;

            // 1. 原文包含匹配
            if (source.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;

            // 2. 首字母包含匹配
            string initials = GetInitials(source);
            if (initials.Contains(query, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}
