using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.UI.Xaml;

namespace WebTool.Lib;

public class ConfigFile
{
    #region Metadata
    public string Content { get; private set; }
    public JsonElement Root { get; private set; }
    #endregion

    #region Content
    public string Id { get; set; }
    public uint Version { get; set; }
    public string DefaultUri { get; set; }
    public string[] Scripts { get; set; }
    public ConfigControl[] Controls { get; set; }
    #endregion

    public static ConfigFile Read(string path)
    {
        var file = new ConfigFile() { Content = File.ReadAllText(path) };

        try
        {
            using var document = JsonDocument.Parse(file.Content);
            file.Root = document.RootElement.Clone();

            file.Id = file.Root.GetProperty("id").GetString();
            file.Version = file.Root.GetProperty("version").GetUInt32();

            file.Root.TryGetValue("default_uri", (e) => file.DefaultUri = e.GetString());
            
            file.Root.TryGetValue("scripts", (e) =>
            {
                int i = 0, len = e.GetArrayLength();
                var arr = e.EnumerateArray();
                file.Scripts = new string[len];
                foreach (var item in arr) file.Scripts[i++] = item.GetString();
            }, JsonValueKind.Array);

            file.Root.TryGetValue("controls", (e) =>
            {
                int i = 0, len = e.GetArrayLength();
                var arr = e.EnumerateArray();
                file.Controls = new ConfigControl[len];
                foreach (var item in arr) file.Controls[i++] = ParseControl(item);
            }, JsonValueKind.Array);
        }
        catch (Exception)
        {
            return null;
        }

        return file;
    }

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
}

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

public struct ButtonAction
{
    public string Type { get; set; }
    public string Function { get; set; }
    public string[] Args { get; set; }
    public string Script { get; set; }
}