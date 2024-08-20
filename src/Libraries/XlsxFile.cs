using System;
using System.Linq;
using System.Collections.Generic;
using ClosedXML.Excel;
using System.Text.Json;

namespace WebTool.Lib.IO;

public class XlsxFile : IDisposable
{
    private readonly string _filePath;
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _worksheet;
    private List<string> _headerList;
    private int _currentRow;

    private bool disposed = false;

    public XlsxFile(string filePath, string sheetName = "Sheet1")
    {
        _filePath = filePath;
        _workbook = new XLWorkbook();
        _worksheet = _workbook.Worksheets.Add(sheetName);
        _headerList = new List<string>();
        _currentRow = 2; // 从第二行开始

        // 冻结表头行
        _worksheet.SheetView.FreezeRows(1);

        // 设置行高、列宽、单元格样式
        // 此处专门为 orderkeystone.com 导出的数据定制，后续版本迭代可能更变
        _worksheet.RowHeight = 24;
        _worksheet.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        _worksheet.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        SetColumnWidth(1, 16);
        SetColumnWidth(2, 16);
        SetColumnWidth(3, 16);
        SetColumnWidth(4, 56);
        SetColumnWidth(5, 56);
    }

    /// <summary>
    /// 添加数据
    /// </summary>
    public void AppendData(List<Dictionary<string, JsonElement>> dataList)
    {
        foreach (var data in dataList)
        {
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
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Font.FontName = "Arial";
                    cell.Style.Font.FontSize = 14;
                }
            }

            // 根据当前表头顺序写入数据
            for (int i = 0; i < _headerList.Count; i++)
            {
                var key = _headerList[i];
                var cell = _worksheet.Cell(_currentRow, i + 1);

                cell.Value = (
                    !data.TryGetValue(key, out JsonElement value) ||
                    !value.TryGetItemValue(out object val)) ?
                    string.Empty : XLCellValue.FromObject(val);

                cell.Style.Font.FontName = "Arial";
                cell.Style.Font.FontSize = 12;
            }

            // 数据写入后，当前行下移一行
            _currentRow++;
        }
    }

    public IXLCell this[string worksheetName, int row, int colum]
    {
        get => _workbook.TryGetWorksheet(worksheetName, out var workbook) ? workbook.Cell(row, colum) : null;
    }

    /// <summary>
    /// 设置行高
    /// </summary>
    public void SetRowHeight(int row, double height) => _worksheet.Row(row).Height = height;

    /// <summary>
    /// 设置列宽
    /// </summary>
    public void SetColumnWidth(int column, double width) => _worksheet.Column(column).Width = width;

    /// <summary>
    /// 保存并关闭文件
    /// </summary>
    public void SaveAndClose()
    {
        if (disposed) return;
        _workbook.SaveAs(_filePath);
        _workbook.Dispose(); // 释放资源
        disposed = true;
    }

    // IDisposable 实现，以便在 using 语句中使用
    public void Dispose()
    {
        if (disposed) return;
        SaveAndClose();
        GC.SuppressFinalize(this);
    }

    ~XlsxFile()
    {
        if (!disposed) Dispose();
    }
}
