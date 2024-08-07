using System;
using System.Text.Json;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace HttpCrawler.Lib;

public static class JsonHelper
{
    /// <summary>
    /// 检查给定字符串能否被解析为 JSON 物件
    /// </summary>
    public static bool IsJsonStringValid(string json)
    {
        try { JsonDocument.Parse(json); return true; }
        catch (JsonException) { return false; }
    }
}

public static class ColorHelper
{
    /// <summary>
    /// 扩展方法，用于取得 SolidColorBrush
    /// </summary>
    public static SolidColorBrush GetBrush(this Color color) => new(color);

    /// <summary>
    /// 将十六进制色彩字符串转换为颜色
    /// </summary>
    /// <param name="hex">
    /// 以 '#' 开头的字符串，接受以下格式
    /// <list type="bullet">
    /// <item>#AARRGGBB</item><item>#RRGGBB</item>
    /// <item>#ARGB</item><item>#RGB</item><item>#XX (灰度)</item>
    /// </list>
    /// </param>
    public static Color FromStringHex(string hex)
    {
        if (!hex.StartsWith('#')) return Colors.White;
        byte a = 0xFF, r = 0xFF, g = 0xFF, b = 0xFF;

        int i = 1;
        switch (hex.Length)
        {
            case 9: /* #AARRGGBB */
                a = Convert.ToByte(hex[i..(i+=2)], 16);
                goto case 7; // Compiler Error CS0163
            case 7: /* #RRGGBB */
                r = Convert.ToByte(hex[i..(i+=2)], 16);
                g = Convert.ToByte(hex[i..(i+=2)], 16);
                b = Convert.ToByte(hex[i..(i+=2)], 16);
                break;
            case 5: /* #ARGB */
                a = Convert.ToByte(hex[i..(i+=1)], 16);
                a = (byte)(a << 4 + a);
                goto case 4; // Compiler Error CS0163
            case 4: /* #RGB */
                r = Convert.ToByte(hex[i..(i+=1)], 16);
                r = (byte)(r << 4 + r);
                g = Convert.ToByte(hex[i..(i+=1)], 16);
                g = (byte)(g << 4 + g);
                b = Convert.ToByte(hex[i..(i+=1)], 16);
                b = (byte)(b << 4 + b);
                break;
            case 3: /* #XX */
                r = g = b = Convert.ToByte(hex[i..(i+=2)], 16);
                break;
        }

        return new() { A = a, R = r, G = g, B = b };
    }
}
