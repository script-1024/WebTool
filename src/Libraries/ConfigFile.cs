using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace WebTool.Lib;

public class ConfigFile
{
    #region Metadata
    public string Content { get; private set; }
    public JsonElement Root { get; private set; }
    #endregion

    #region Content
    public string Id { get; private set; }
    public uint Version { get; private set; } = 1;
    public string DefaultUri { get; private set; } = null;
    public string[] Scripts { get; private set; }
    //public ConfigControl[] Controls { get; private set; }
    #endregion

    public static ConfigFile Read(string path)
    {
        try
        {
            var file = new ConfigFile() { Content = File.ReadAllText(path) };

            using var document = JsonDocument.Parse(file.Content);
            file.Root = document.RootElement.Clone();

            file.Id = file.Root.GetProperty("id").GetString();
            file.Version = file.Root.GetProperty("version").GetUInt32();

            file.Root.TryGetValue("default_uri", (elem) => file.DefaultUri = elem.GetString(), JsonValueKind.String);
            
            file.Root.TryGetValue("scripts", (elem) =>
            {
                int i = 0, len = elem.GetArrayLength();
                var arr = elem.EnumerateArray();
                file.Scripts = new string[len];
                foreach (var item in arr) file.Scripts[i++] = item.GetString();
            }, JsonValueKind.Array);

            file.Root.TryGetValue("data", (elem) =>
            {
                var dataProperties = elem.EnumerateObject();
                foreach (var obj in dataProperties)
                {
                    if (obj.Value.ValueKind == JsonValueKind.Object)
                    {
                        var objProperties = obj.Value.Deserialize<Dictionary<string, JsonElement>>();
                        AppConfig.AdditionalData.MergeChildDictionary(obj.Name, objProperties);
                    }
                }
            }, JsonValueKind.Object);

            /*
            file.Root.TryGetValue("controls", (e) =>
            {
                int i = 0, len = e.GetArrayLength();
                var arr = e.EnumerateArray();
                file.Controls = new ConfigControl[len];
                foreach (var item in arr) file.Controls[i++] = ParseControl(item);
            }, JsonValueKind.Array);
            */

            return file;
        }
        catch (Exception)
        {
            // 忽略无效或格式错误的文件
            return null;
        }
    }

    /*
    public static ConfigControl ParseControl(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new InvalidDataException();
        var control = new ConfigControl();

        var type = element.GetProperty("type").GetString();
        control.Name = element.GetProperty("name").GetString();

        switch (type)
        {
            case "label":
                control.Type = ConfigControl.ControlType.Label;
                element.TryGetValue("text", (e) => control.Text = e.GetString());
                break;
            default:
                throw new InvalidDataException();
        }

        return control;
    }
    */
}

public static class AppConfig
{
    #region Properties
    public static string DefaultUri { get; internal set; } = null;
    public static List<string> LoadedId { get; private set; } = [];
    public static List<string> UsedScripts { get; private set; } = [];
    public static List<string> LoadedFiles { get; private set; } = [];
    public static List<string> BlockedFiles { get; private set; } = [];
    public static Dictionary<string, Dictionary<string, JsonElement>> AdditionalData { get; private set; } = [];
    #endregion

    public delegate void ConfigReloadedEvent();
    public static event ConfigReloadedEvent Reloaded;

    public static void ClearAll()
    {
        DefaultUri = null;
        AdditionalData.Clear();
        UsedScripts.Clear();
        LoadedFiles.Clear();
        LoadedId.Clear();
    }

    public static void Reload()
    {
        DefaultUri = null;
        AdditionalData.Clear();
        UsedScripts.Clear();
        LoadedId.Clear();

        static void RemoveInvalidFile(string filename)
        {
            LoadedFiles.Remove(filename);
            BlockedFiles.Remove(filename);
        }

        static void RegisterNewConfigFile(ConfigFile configFile)
        {
            LoadedId.Add(configFile.Id);
            DefaultUri ??= configFile.DefaultUri;
            UsedScripts.AddRange(configFile.Scripts.Where(str => !UsedScripts.Contains(str)));
        }

        // 先尝试读取已加载过的文件
        foreach (var file in LoadedFiles)
        {
            if (File.Exists(file))
            {
                if (BlockedFiles.Contains(file)) continue;

                var configFile = ConfigFile.Read(file);
                if (configFile is null)
                {
                    RemoveInvalidFile(file);
                    continue;
                }

                RegisterNewConfigFile(configFile);
            }
            else RemoveInvalidFile(file);
        }

        // 再加载新的配置文件
        var files = Directory.EnumerateFiles("Config");
        foreach (var filename in files)
        {
            if (LoadedFiles.Contains(filename)) continue;

            var configFile = ConfigFile.Read(filename);
            if (configFile is null) continue;

            // 使用相同 ID 的文件将被忽略
            if (LoadedId.Contains(configFile.Id)) continue;

            LoadedFiles.Add(filename);
            RegisterNewConfigFile(configFile);
        }

        // 通知订阅者
        Reloaded?.Invoke();
    }
}

/*
public class ConfigControl
{
    public enum ControlType { Label, Button, Panel, InputBox, CheckBox }

    #region "common"
    public ControlType Type { get; set; }
    public string Name { get; set; }
    public Thickness Margin { get; set; }
    public bool Visible { get; set; }
    public bool Enable { get; set; }
    public int FontSize { get; set; }
    #endregion

    #region "label"
    public string Text { get; set; }
    #endregion

    #region "button"
    public ButtonAction Action { get; set; }
    #endregion

    #region "panel"

    #endregion

    #region "inputbox"
    public string Header { get; set; }
    public string PlaceHolder { get; set; }
    public string Content { get; set; }
    public ConfigControl ActionButton { get; set; }
    #endregion

    #region "checkbox"
    public bool IsChecked { get; set; }
    #endregion
}
*/

/*
public struct ButtonAction
{
    public string Type { get; set; }
    public string Function { get; set; }
    public string[] Args { get; set; }
    public string Script { get; set; }
}
*/
