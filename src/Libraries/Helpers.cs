﻿using System;
using System.IO;
using System.Text.Json;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace WebTool.Lib;

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

    /// <summary>
    /// 尝试从 <see cref="JsonElement"/> 取值，成功时执行特定动作。
    /// <br/>
    /// 若明确指定了值类型但与取得结果不符合，将抛出 <see cref="InvalidDataException"/>
    /// </summary>
    /// <param name="element">来源</param>
    /// <param name="key">键名</param>
    /// <param name="action">成功取值后的特定动作</param>
    /// <param name="kind">(可选) 要比对的值类型</param>
    /// <returns>一个布尔值，表示操作是否成功</returns>
    /// <exception cref="InvalidDataException"></exception>
    public static bool TryGetValue(this JsonElement element, string key, Action<JsonElement> action, JsonValueKind kind = JsonValueKind.Undefined)
    {
        if (!element.TryGetProperty(key, out var result)) return false;
        if (kind != JsonValueKind.Undefined && result.ValueKind != kind) throw new InvalidDataException();
        action(result);
        return true;
    }

    public static bool TryGetItemValue(this JsonElement element, out object value)
    {
        value = null;
        switch (element.ValueKind)
        {
            case JsonValueKind.True:
            case JsonValueKind.False:
                value = element.GetBoolean();
                return true;
            case JsonValueKind.Number:
                value = element.GetDouble();
                return true;
            case JsonValueKind.String:
                value = element.GetString();
                return true;
        }
        return false;
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

public static class UtilHelper
{
    /// <summary>
    /// 由布尔值 <paramref name="value"/> 设置控件的可见性
    /// </summary>
    /// <returns>修改后的 <see cref="Visibility"/>，可用于连续设置多个控件</returns>
    public static Visibility SetVisibility(this UIElement uiElement, bool value)
    {
        return uiElement.Visibility = value switch {
            true => Visibility.Visible,
            false => Visibility.Collapsed
        };
    }

    /// <summary>
    /// 添加新键值，或修改已有键值
    /// </summary>
    public static void AddOrSetValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (!dictionary.TryAdd(key, value)) dictionary[key] = value;
    }

    public static void MergeChildDictionary<TValue>(this Dictionary<string, Dictionary<string, TValue>> parent, string key, Dictionary<string, TValue> child)
    {
        if (!parent.TryAdd(key, child))
        {
            var existing = parent[key];
            foreach (var kvp in child) existing.AddOrSetValue(kvp.Key, kvp.Value);
        }
    }
}
