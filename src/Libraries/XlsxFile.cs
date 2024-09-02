using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace WebTool.Lib.IO;

public class XlsxFile : IDisposable
{
    private readonly XLWorkbook _workbook;
    private readonly List<string> _headerList;
    private readonly FileStream _fileStream;
    private IXLWorksheet _worksheet;
    private IXLWorksheet _properties;
    private bool _disposed = false;
    private int _currentRow;

    public string Path { get; private set; }

    private XlsxFile(FileStream fileStream, IXLWorksheet worksheet, List<string> headerList, int currentRow)
    {
        _fileStream = fileStream;
        _workbook = worksheet.Workbook;
        _worksheet = worksheet;
        _headerList = headerList;
        _currentRow = currentRow;
    }

    private static void SetWorksheetStyle(IXLWorksheet worksheet)
    {
        // 冻结表头行
        worksheet.SheetView.FreezeRows(1);

        // 设置行高、列宽、单元格样式
        // 此处专门为 orderkeystone.com 导出的数据定制，后续版本迭代可能更变
        worksheet.RowHeight = 24;
        worksheet.Style.Font.FontSize = 12;
        worksheet.Style.Font.FontName = "Arial";
        worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Column(1).Width = 16;
        worksheet.Column(2).Width = 16;
        worksheet.Column(3).Width = 16;
        worksheet.Column(4).Width = 64;
        worksheet.Column(5).Width = 64;
    }

    /// <summary>
    /// 创建一个新的 XlsxFile 实例
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="worksheetName">工作表名称</param>
    /// <returns>一个 <see cref="XlsxFile"/> 实例</returns>
    public static XlsxFile Create(string path, string worksheetName = "Sheet1")
    {
        // 新建文件
        var workbook = new XLWorkbook();
        workbook.Worksheets.Add(worksheetName);
        workbook.SaveAs(path);
        workbook.Dispose();

        // 打开文件流并锁定文件
        var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        workbook = new XLWorkbook(fileStream);

        // 初始化
        var worksheet = workbook.Worksheet(1);
        var headerList = new List<string>();
        int currentRow = 2; // 从第二行开始

        SetWorksheetStyle(worksheet);

        return new XlsxFile(fileStream, worksheet, headerList, currentRow) { Path = path };
    }

    /// <summary>
    /// 打开已存在的文件并恢复 XlsxFile 实例，若不存在指定工作表则创建一个新的
    /// </summary>
    /// <param name="path">文件路径</param>
    /// <param name="worksheetName">工作表名称</param>
    /// <returns>一个 <see cref="XlsxFile"/> 实例</returns>
    public static XlsxFile Open(string path, string worksheetName)
    {
        // 打开文件流并锁定文件
        var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        var workbook = new XLWorkbook(fileStream);

        // 尝试获取指定工作表，如果不存在则创建一个新的
        if (!workbook.TryGetWorksheet(worksheetName, out var worksheet))
        {
            worksheet = workbook.Worksheets.Add(worksheetName);
        }

        SetWorksheetStyle(worksheet);

        // 从文件首行加载表头
        var headerList = worksheet.Row(1).CellsUsed().Select(cell => cell.GetString()).ToList();
        int currentRow = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 2;

        return new XlsxFile(fileStream, worksheet, headerList, currentRow) { Path = path };
    }

    /// <summary>
    /// 依据指定键搜索特定行
    /// </summary>
    /// <param name="worksheet">作用中的工作表</param>
    /// <param name="key">待搜索的键</param>
    /// <param name="where">可选，键所在的列</param>
    /// <returns><see cref="IXLRow"/> 找到的特定行或新的空行</returns>
    private static IXLRow SearchSpecificRow(IXLWorksheet worksheet, string key, int where = 1)
    {
        int row = 1;

        while (!string.IsNullOrEmpty(worksheet.Cell(row, where).GetString()))
        {
            if (worksheet.Cell(row, 1).GetString().Equals(key, StringComparison.OrdinalIgnoreCase))
                return worksheet.Row(row);
            row++;
        }

        // 如果键未找到，传回新的空行
        return worksheet.Row(row);
    }

    private void InitializePropertiesWorksheet()
    {
        _properties =
            _workbook.Worksheets.FirstOrDefault(ws => ws.Name == "properties") ?? 
            _workbook.Worksheets.Add("properties");

        _properties.RowHeight = 24;
        _properties.Style.Font.FontSize = 12;
        _properties.Style.Font.FontName = "Arial";
        _properties.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        var keyColumn = _properties.Column(1);
        keyColumn.Width = 18;
        keyColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        var valueColumn = _properties.Column(2);
        valueColumn.Width = 8;
        valueColumn.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    /// <summary>
    /// 设置或更新属性工作表
    /// </summary>
    public void SetProperty(string key, object value)
    {
        if (_properties is null) InitializePropertiesWorksheet();
        var row = SearchSpecificRow(_properties, key);
        row.Cell(1).Value = key;
        if (value is null) row.Cell(2).Clear();  // 如果值为 null，清空单元格
        else row.Cell(2).SetValue(XLCellValue.FromObject(value));  // 使用 SetValue 保留数据类型
    }

    /// <summary>
    /// 从属性工作表取得值
    /// </summary>
    /// <param name="key">属性的键名</param>
    /// <param name="fallback">类型转换失败的回退值</param>
    public T GetProperty<T>(string key, T fallback = default)
    {
        if (_properties is null) InitializePropertiesWorksheet();
        var value = SearchSpecificRow(_properties, key).Cell(2).Value;

        object obj = value.Type switch
        {
            XLDataType.Blank => null,
            XLDataType.Boolean => value.GetBoolean(),
            XLDataType.Number => value.GetNumber(),
            XLDataType.Text => value.GetText(),
            XLDataType.Error => value.GetError(),
            XLDataType.DateTime => value.GetDateTime(),
            XLDataType.TimeSpan => value.GetTimeSpan(),
            _ => throw new InvalidCastException(),
        };

        try
        {
            return (T)Convert.ChangeType(obj, typeof(T));
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Unable to convert value '{value}' to specific type '{typeof(T)}' for key '{key}': {e.Message}");
            return fallback;
        }
    }

    /// <summary>
    /// 从字典列表添加数据，每个列表元素的所有键值都会分别对应到表头和单元格
    /// </summary>
    public void AppendData(List<Dictionary<string, JsonElement>> dataList)
    {
        if (_disposed) return;
        foreach (var data in dataList) AppendData(data);
    }

    /// <summary>
    /// 从字典添加数据，所有键值都会分别对应到表头和单元格
    /// </summary>
    public void AppendData(Dictionary<string, JsonElement> data)
    {
        if (_disposed) return;

        foreach (var key in data.Keys)
        {
            // 如果表头中不存在该键，添加到表头
            if (!_headerList.Contains(key))
            {
                _headerList.Add(key);

                var cell = _worksheet.Cell(1, _headerList.Count);

                cell.Value = key;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#B7DEE8");
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Font.FontSize = 14;
            }
        }

        // 根据当前表头顺序写入数据
        for (int i = 0; i < _headerList.Count; i++)
        {
            var key = _headerList[i];
            var cell = _worksheet.Cell(_currentRow, i + 1);

            cell.Value = (data.TryGetValue(key, out JsonElement value) && value.TryGetItemValue(out object val))
                ? XLCellValue.FromObject(val) : string.Empty;
        }

        // 数据写入后，当前行下移一行
        _currentRow++;
    }

    /// <summary>
    /// 取得单元格
    /// </summary>
    /// <param name="row">单元格所在的行</param>
    /// <param name="column">单元格所在的列</param>
    /// <returns><see cref="IXLCell"/></returns>
    public IXLCell this[int row, int column] { get => _disposed ? null : _worksheet.Cell(row, column); }

    /// <summary>
    /// 设置行高
    /// </summary>
    public void SetRowHeight(int row, double height)
    {
        if (!_disposed) _worksheet.Row(row).Height = height;
    }

    /// <summary>
    /// 设置列宽
    /// </summary>
    public void SetColumnWidth(int column, double width)
    {
        if (!_disposed) _worksheet.Column(column).Width = width;
    }

    /// <summary>
    /// 改变当前作用中的工作表
    /// </summary>
    /// <param name="worksheetName">工作表名称</param>
    public void ChangeWorksheet(string worksheetName)
    {
        if (!_workbook.TryGetWorksheet(worksheetName, out var worksheet))
        {
            worksheet = _workbook.Worksheets.Add(worksheetName);
        }
        
        _worksheet = worksheet;
    }

    /// <summary>
    /// 保存并关闭文件
    /// </summary>
    public void SaveAndClose()
    {
        if (_disposed) return;
        _workbook.Save();
        Close();
    }

    /// <summary>
    /// 保存文件
    /// </summary>
    public void Save()
    {
        if (_disposed) return;
        _workbook.Save();
        // 不释放资源，这样可以继续使用
    }

    /// <summary>
    /// 关闭文件释放资源
    /// </summary>
    public void Close()
    {
        if (_disposed) return;
        _workbook.Dispose(); // 释放资源
        _fileStream.Close(); // 释放文件流
        _disposed = true;
    }

    // IDisposable 实现，以便在 using 语句中使用
    public void Dispose()
    {
        if (_disposed) return;
        SaveAndClose();
        GC.SuppressFinalize(this);
    }

    ~XlsxFile()
    {
        if (!_disposed) Dispose();
    }
}
