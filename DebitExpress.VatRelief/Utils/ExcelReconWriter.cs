using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DebitExpress.VatRelief.Models;

namespace DebitExpress.VatRelief.Utils;

public class ExcelReconWriter
{
    public Result WriteReconciliationReport(ExcelData data, string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            var fileName = $"{Extensions.QuarterRangeString(data.Info.Month, data.Info.Year)}.xlsx";
            var fullPath = Path.Combine(path, fileName);

            var workbook = new XLWorkbook();
            var salesSheet = workbook.Worksheets.Add("SALES");
            var purchasesSheet = workbook.Worksheets.Add("PURCHASES");

            var info = data.Info;
            var sales = data.Sales;
            var purchases = data.Purchases;

            var startingMonth = info.Month;
            var year = info.Year;
            var firstMonth = Extensions.GetEndOfMonth(year, startingMonth);
            var secondMonth = Extensions.GetEndOfMonth(year, startingMonth + 1);
            var thirdMonth = Extensions.GetEndOfMonth(year, startingMonth + 2);

            var firstMonthSales = sales.Where(x => x.EndOfMonth == firstMonth)
                .OrderBy(i => i.Tin)
                .ThenBy(i => i.RegName)
                .ThenBy(i => i.LastName)
                .ToList();
            var secondMonthSales = sales.Where(x => x.EndOfMonth == secondMonth)
                .OrderBy(i => i.Tin)
                .ThenBy(i => i.RegName)
                .ThenBy(i => i.LastName)
                .ToList();
            var thirdMonthSales = sales.Where(x => x.EndOfMonth == thirdMonth)
                .OrderBy(i => i.Tin)
                .ThenBy(i => i.RegName)
                .ThenBy(i => i.LastName)
                .ToList();

            var firstMonthPurchases = purchases.Where(x => x.EndOfMonth == firstMonth)
                .OrderBy(i => i.RegName)
                .ThenBy(i => i.LastName)
                .ToList();
            var secondMonthPurchases = purchases.Where(x => x.EndOfMonth == secondMonth)
                .OrderBy(i => i.RegName)
                .ThenBy(i => i.LastName)
                .ToList();
            var thirdMonthPurchases = purchases.Where(x => x.EndOfMonth == thirdMonth)
                .OrderBy(i => i.RegName)
                .ThenBy(i => i.LastName)
                .ToList();

            var extractedSales = new List<Sales>();
            extractedSales.AddRange(firstMonthSales);
            extractedSales.AddRange(secondMonthSales);
            extractedSales.AddRange(thirdMonthSales);
            WriteSalesData(salesSheet, data.Info, extractedSales);

            var extractedPurchases = new List<Purchases>();
            extractedPurchases.AddRange(firstMonthPurchases);
            extractedPurchases.AddRange(secondMonthPurchases);
            extractedPurchases.AddRange(thirdMonthPurchases);
            WritePurchasesData(purchasesSheet, data.Info, extractedPurchases);

            workbook.SaveAs(fullPath);

            extractedSales.ForceClear();
            extractedPurchases.ForceClear();

            return new Result();
        }
        catch (Exception e)
        {
            return new Result(new Exception("Error writing reconciliation excel file", e));
        }
    }

    private void WriteSalesData(IXLWorksheet sheet, Info info, List<Sales> sales)
    {
        sheet.Style.Font.FontSize = 10;
        sheet.Cells("A1:K14").Style.Font.Bold = true;
        sheet.Column("A").Width = 10;
        sheet.Column("B").Width = 13;
        sheet.Column("C").Width = 35;
        sheet.Column("D").Width = 35;
        sheet.Column("E").Width = 40;
        sheet.Columns("F:K").Width = 14;

        sheet.Cell("A1").Value = "SALES TRANSACTION";
        sheet.Cell("A2").Value = "RECONCILIATION OF LISTING FOR ENFORCEMENT";
        sheet.Cell("A6").Value = $"TIN : {info.Tin.Strip()}";

        var name = $"{info.LastName}, {info.FirstName} {info.MiddleName}";
        sheet.Cell("A7").Value = $"OWNER'S NAME: {name}";

        sheet.Cell("A8").Value = $"OWNER'S TRADE NAME : {info.TradeName}";

        var address = string.IsNullOrEmpty(info.City) ? info.Street : $"{info.Street} {info.City}";
        sheet.Cell("A9").Value = $"OWNER'S ADDRESS : {address}";

        sheet.Cell("A11").Value = "TAXABLE";
        sheet.Cell("B11").Value = "TAXPAYER";
        sheet.Cell("C11").Value = "REGISTERED NAME";
        sheet.Cell("D11").Value = "NAME OF CUSTOMER";
        sheet.Cell("E11").Value = "CUSTOMER'S ADDRESS";
        sheet.Cell("F11").Value = "AMOUNT OF";
        sheet.Cell("G11").Value = "AMOUNT OF";
        sheet.Cell("H11").Value = "AMOUNT OF";
        sheet.Cell("I11").Value = "AMOUNT OF";
        sheet.Cell("J11").Value = "AMOUNT OF";
        sheet.Cell("K11").Value = "AMOUNT OF";

        sheet.Cell("A12").Value = "MONTH";
        sheet.Cell("B12").Value = "IDENTIFICATION";
        sheet.Cell("D12").Value = "(Last Name, First Name, Middle Name)";
        sheet.Cell("F12").Value = "GROSS SALES";
        sheet.Cell("G12").Value = "EXEMPT SALES";
        sheet.Cell("H12").Value = "ZERO-RATED SALES";
        sheet.Cell("I12").Value = "TAXABLE SALES";
        sheet.Cell("J12").Value = "OUTPUT TAX";
        sheet.Cell("K12").Value = "GROSS TAXABLE SALES";

        sheet.Cell("B13").Value = "NUMBER";

        sheet.Cell("A14").Value = "(1)";
        sheet.Cell("B14").Value = "(2)";
        sheet.Cell("C14").Value = "(3)";
        sheet.Cell("D14").Value = "(4)";
        sheet.Cell("E14").Value = "(5)";
        sheet.Cell("F14").Value = "(6)";
        sheet.Cell("G14").Value = "(7)";
        sheet.Cell("H14").Value = "(8)";
        sheet.Cell("I14").Value = "(9)";
        sheet.Cell("J14").Value = "(10)";
        sheet.Cell("K14").Value = "(11)";

        var row = 15;
        foreach (var item in sales)
        {
            sheet.Cell($"A{row}").Value = $"{item.EndOfMonth:MM/dd/yyyy}";
            sheet.Cell($"B{row}").Value = string.IsNullOrEmpty(item.Tin) ? "   -   -   " : $"{item.Tin[..11]}";
            sheet.Cell($"C{row}").Value = item.RegName;
            sheet.Cell($"D{row}").Value = item.FullName;
            sheet.Cell($"E{row}").Value = $"{item.Street} {item.City}";
            sheet.Cell($"F{row}").Value = item.Exempt + item.ZeroRated + item.NetTaxable;
            sheet.Cell($"G{row}").Value = item.Exempt;
            sheet.Cell($"H{row}").Value = item.ZeroRated;
            sheet.Cell($"I{row}").Value = item.NetTaxable;
            sheet.Cell($"J{row}").Value = item.Vat;
            sheet.Cell($"K{row}").Value = item.NetTaxable + item.Vat;

            sheet.Cells($"F{row}:K{row}").Style.NumberFormat.Format = "###,###,###,##0.00";

            row++;
        }

        sheet.Cell($"A{row + 2}").Value = "Grand Total :";
        sheet.Cell($"F{row + 2}").FormulaA1 = $"=SUM(F15:F{row - 1})";
        sheet.Cell($"G{row + 2}").FormulaA1 = $"=SUM(G15:G{row - 1})";
        sheet.Cell($"H{row + 2}").FormulaA1 = $"=SUM(H15:H{row - 1})";
        sheet.Cell($"I{row + 2}").FormulaA1 = $"=SUM(I15:I{row - 1})";
        sheet.Cell($"J{row + 2}").FormulaA1 = $"=SUM(J15:J{row - 1})";
        sheet.Cell($"K{row + 2}").FormulaA1 = $"=SUM(K15:K{row - 1})";

        sheet.Cells($"F{row + 2}:K{row + 2}").Style.Font.Bold = true;
        sheet.Cells($"F{row + 2}:K{row + 2}").Style.NumberFormat.Format = "###,###,###,##0.00";

        sheet.Cell($"A{row + 4}").Value = "END OF REPORT";
    }

    private void WritePurchasesData(IXLWorksheet sheet, Info info, List<Purchases> purchases)
    {
        sheet.Style.Font.FontSize = 10;
        sheet.Cells("A1:N14").Style.Font.Bold = true;

        sheet.Column("A").Width = 10;
        sheet.Column("B").Width = 13;
        sheet.Column("C").Width = 35;
        sheet.Column("D").Width = 35;
        sheet.Column("E").Width = 40;
        sheet.Columns("F:N").Width = 14;

        sheet.Cell("A1").Value = "PURCHASE TRANSACTION";
        sheet.Cell("A2").Value = "RECONCILIATION OF LISTING FOR ENFORCEMENT";
        sheet.Cell("A6").Value = $"TIN : {info.Tin.Strip()}";

        var name = $"{info.LastName}, {info.FirstName} {info.MiddleName}";
        sheet.Cell("A7").Value = $"OWNER'S NAME: {name}";

        sheet.Cell("A8").Value = $"OWNER'S TRADE NAME : {info.TradeName}";

        var address = string.IsNullOrEmpty(info.City) ? info.Street : $"{info.Street} {info.City}";
        sheet.Cell("A9").Value = $"OWNER'S ADDRESS : {address}";

        sheet.Cell("A11").Value = "TAXABLE";
        sheet.Cell("B11").Value = "TAXPAYER";
        sheet.Cell("C11").Value = "REGISTERED NAME";
        sheet.Cell("D11").Value = "NAME OF SUPPLIER";
        sheet.Cell("E11").Value = "SUPPLIER'S ADDRESS";
        sheet.Cell("F11").Value = "AMOUNT OF";
        sheet.Cell("G11").Value = "AMOUNT OF";
        sheet.Cell("H11").Value = "AMOUNT OF";
        sheet.Cell("I11").Value = "AMOUNT OF";
        sheet.Cell("J11").Value = "AMOUNT OF";
        sheet.Cell("K11").Value = "AMOUNT OF";
        sheet.Cell("L11").Value = "AMOUNT OF";
        sheet.Cell("M11").Value = "AMOUNT OF";
        sheet.Cell("N11").Value = "AMOUNT OF";

        sheet.Cell("A12").Value = "MONTH";
        sheet.Cell("B12").Value = "IDENTIFICATION";
        sheet.Cell("D12").Value = "(Last Name, First Name, Middle Name)";
        sheet.Cell("F12").Value = "GROSS PURCHASES";
        sheet.Cell("G12").Value = "EXEMPT PURCHASES";
        sheet.Cell("H12").Value = "ZERO-RATED PURCHASES";
        sheet.Cell("I12").Value = "TAXABLE PURCHASES";
        sheet.Cell("J12").Value = "PURCHASE OF SERVICES";
        sheet.Cell("K12").Value = "PURCHASE OF CAPITAL GOODS";
        sheet.Cell("L12").Value = "PURCHASE OF GOODS OTHER THAN CAPITAL GOODS";
        sheet.Cell("M12").Value = "INPUT TAX";
        sheet.Cell("N12").Value = "GROSS TAXABLE PURCHASE";

        sheet.Cell("B13").Value = "NUMBER";

        sheet.Cell("A14").Value = "(1)";
        sheet.Cell("B14").Value = "(2)";
        sheet.Cell("C14").Value = "(3)";
        sheet.Cell("D14").Value = "(4)";
        sheet.Cell("E14").Value = "(5)";
        sheet.Cell("F14").Value = "(6)";
        sheet.Cell("G14").Value = "(7)";
        sheet.Cell("H14").Value = "(8)";
        sheet.Cell("I14").Value = "(9)";
        sheet.Cell("J14").Value = "(10)";
        sheet.Cell("K14").Value = "(11)";
        sheet.Cell("L14").Value = "(12)";
        sheet.Cell("M14").Value = "(13)";
        sheet.Cell("N14").Value = "(14)";

        var row = 15;
        foreach (var item in purchases)
        {
            sheet.Cell($"A{row}").Value = $"{item.EndOfMonth:MM/dd/yyyy}";
            sheet.Cell($"B{row}").Value = $"{item.Tin[..11]}";
            sheet.Cell($"C{row}").Value = item.RegName;
            sheet.Cell($"D{row}").Value = item.FullName;
            sheet.Cell($"E{row}").Value = $"{item.Street} {item.City}";
            sheet.Cell($"F{row}").Value = item.Exempt + item.ZeroRated + item.Service + item.CapitalGoods + item.OtherGoods;
            sheet.Cell($"G{row}").Value = item.Exempt;
            sheet.Cell($"H{row}").Value = item.ZeroRated;
            sheet.Cell($"I{row}").Value = item.Service + item.CapitalGoods + item.OtherGoods;
            sheet.Cell($"J{row}").Value = item.Service;
            sheet.Cell($"K{row}").Value = item.CapitalGoods;
            sheet.Cell($"L{row}").Value = item.OtherGoods;
            sheet.Cell($"M{row}").Value = item.InputTax;
            sheet.Cell($"N{row}").Value = item.Service + item.CapitalGoods + item.OtherGoods + item.InputTax;

            sheet.Cells($"F{row}:N{row}").Style.NumberFormat.Format = "###,###,###,##0.00";

            row++;
        }

        sheet.Cell($"A{row + 2}").Value = "Grand Total :";
        sheet.Cell($"F{row + 2}").FormulaA1 = $"=SUM(F15:F{row - 1})";
        sheet.Cell($"G{row + 2}").FormulaA1 = $"=SUM(G15:G{row - 1})";
        sheet.Cell($"H{row + 2}").FormulaA1 = $"=SUM(H15:H{row - 1})";
        sheet.Cell($"I{row + 2}").FormulaA1 = $"=SUM(I15:I{row - 1})";
        sheet.Cell($"J{row + 2}").FormulaA1 = $"=SUM(J15:J{row - 1})";
        sheet.Cell($"K{row + 2}").FormulaA1 = $"=SUM(K15:K{row - 1})";
        sheet.Cell($"L{row + 2}").FormulaA1 = $"=SUM(L15:L{row - 1})";
        sheet.Cell($"M{row + 2}").FormulaA1 = $"=SUM(M15:M{row - 1})";
        sheet.Cell($"N{row + 2}").FormulaA1 = $"=SUM(N15:N{row - 1})";

        sheet.Cells($"F{row + 2}:N{row + 2}").Style.Font.Bold = true;
        sheet.Cells($"F{row + 2}:N{row + 2}").Style.NumberFormat.Format = "###,###,###,##0.00";

        sheet.Cell($"A{row + 4}").Value = "END OF REPORT";
    }
}