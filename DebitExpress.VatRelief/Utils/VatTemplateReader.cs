using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DebitExpress.VatRelief.Models;

namespace DebitExpress.VatRelief.Utils;

public class VatTemplateReader
{
    private readonly Dictionary<string, int> _months;

    public VatTemplateReader()
    {
        _months = new Dictionary<string, int>
        {
            { "January", 1 },
            { "February", 2 },
            { "March", 3 },
            { "April", 4 },
            { "May", 5 },
            { "June", 6 },
            { "July", 7 },
            { "August", 8 },
            { "September", 9 },
            { "October", 10 },
            { "November", 11 },
            { "December", 12 }
        };
    }

    public Task<Result<ExcelData>> ReadAsync(string path)
    {
        return Task.Run(() => ReadInternal(path));
    }

    private Result<ExcelData> ReadInternal(string path)
    {
        try
        {
            var workbook = new XLWorkbook(path);
            var info = GetInfo(workbook);
            var sales = GetSalesData(workbook);
            var purchases = GetPurchaseData(workbook, info);

            return new Result<ExcelData>(new ExcelData(info, sales, purchases));
        }
        catch (Exception e)
        {
            return new Result<ExcelData>(e);
        }
    }

    private Info GetInfo(IXLWorkbook workbook)
    {
        var infoSheet = workbook.Worksheet("INFO");
        if (infoSheet == null) throw new ArgumentException("INFO sheet not found");

        var tin = infoSheet.Cell("B2").Value.ToString() ??
                  throw new ArgumentException("Taxpayer TIN is required in INFO sheet cell B2");
        var regName = infoSheet.Cell("B3").Value.ToString() ?? string.Empty;
        var lastName = infoSheet.Cell("B4").Value.ToString() ?? string.Empty;
        var firstName = infoSheet.Cell("B5").Value.ToString() ?? string.Empty;
        var middleName = infoSheet.Cell("B6").Value.ToString() ?? string.Empty;
        var tradeName = infoSheet.Cell("B7").Value.ToString() ?? string.Empty;
        var street = infoSheet.Cell("B8").Value.ToString() ?? string.Empty;
        var city = infoSheet.Cell("B9").Value.ToString() ?? string.Empty;
        var rdo = infoSheet.Cell("B10").Value.ToString() ?? throw new ArgumentException("RDO is required in INFO sheet cell B10");

        var periodEndStr = infoSheet.Cell("B11").Value.ToString();
        if (string.IsNullOrEmpty(periodEndStr)) throw new ArgumentException("Period end is required in INFO sheet cell B11");
        var periodEndGet = _months.TryGetValue(periodEndStr, out var periodEnd);
        if (!periodEndGet) throw new ArgumentException("Invalid [Period End] value in INFO sheet cell B11");

        var nonIndividual = infoSheet.Cell("B12").Value.ToString() ?? string.Empty;

        var monthStr = infoSheet.Cell("B15").Value.ToString();
        if (string.IsNullOrEmpty(monthStr)) throw new ArgumentException("Starting month is required in INFO sheet cell B16");
        var monthGet = _months.TryGetValue(monthStr, out var month);
        if (!monthGet) throw new ArgumentException("Invalid [Starting Month] value in INFO sheet cell B16");

        var yearStr = infoSheet.Cell("B16").Value.ToString();
        if (string.IsNullOrEmpty(yearStr)) throw new ArgumentException("Year is required in INFO sheet cell B17");
        var yearGet = int.TryParse(yearStr, out var year);
        if (!yearGet) throw new ArgumentException("Invalid [Year] value in INFO sheet cell B17");

        return new Info(tin.Strip(),
            month,
            year,
            regName.CleanUp(),
            lastName.CleanUp(),
            firstName.CleanUp(),
            middleName.CleanUp(),
            tradeName.CleanUp(),
            street.CleanUp(),
            city.CleanUp(),
            rdo.CleanUp(),
            periodEnd,
            nonIndividual.IsTrue());
    }

    private static List<Sales> GetSalesData(IXLWorkbook workbook)
    {
        var salesSheet = workbook.Worksheet("SALES");
        if (salesSheet == null) throw new ArgumentException("SALES sheet not found");

        var sales = new List<Sales>();
        var rowCount = salesSheet.LastRowUsed().RowNumber();
        for (var i = 2; i <= rowCount; i++)
        {
            var row = salesSheet.Row(i);
            sales.Add(ParseSalesLineItem(row, i));
        }

        return sales;
    }

    private static Sales ParseSalesLineItem(IXLRow row, int i)
    {
        if (row.IsEmpty()) throw new ArgumentException($"Empty row found in SALES sheet at row {i}");

        var eomStr = row.Cell("A").Value.ToString();
        if (string.IsNullOrEmpty(eomStr)) throw new ArgumentException($"MONTH_END field is required in SALES sheet cell A{i}");
        var eomParsed = DateTime.TryParse(eomStr, out var eom);

        if (!eomParsed) throw new ArgumentException($"Invalid [MONTH_END] value in SALES sheet cell A{i}");

        var monthEnd = eom.Day;
        var expectedMonthEnd = DateTime.DaysInMonth(eom.Year, eom.Month);
        if (monthEnd != expectedMonthEnd)
            throw new ArgumentException($"Invalid [MONTH_END] value in SALES sheet cell A{i}");
        
        var tin = row.Cell("B").Value.ToString() ?? string.Empty;
        if (!string.IsNullOrEmpty(tin) && !tin.IsValidTin())
            throw new ArgumentException($"Invalid [TIN] value in SALES sheet cell B{i}");

        var regName = row.Cell("C").Value.ToString() ?? string.Empty;
        var lastName = row.Cell("D").Value.ToString() ?? string.Empty;
        var firstName = row.Cell("E").Value.ToString() ?? string.Empty;
        var middleName = row.Cell("F").Value.ToString() ?? string.Empty;
        var street = row.Cell("G").Value.ToString() ?? string.Empty;
        var city = row.Cell("H").Value.ToString() ?? string.Empty;

        var exemptStr = row.Cell("I").Value.ToString() ?? "0";
        var exemptParsed = decimal.TryParse(exemptStr, out var exempt);
        if (!exemptParsed) throw new ArgumentException($"Invalid [EXEMPT] value in SALES sheet cell I{i}");

        var zeroRatedStr = row.Cell("J").Value.ToString() ?? "0";
        var zeroRatedParsed = decimal.TryParse(zeroRatedStr, out var zeroRated);
        if (!zeroRatedParsed) throw new ArgumentException($"Invalid [ZERO_RATED] value in SALES sheet cell J{i}");

        var netTaxableStr = row.Cell("K").Value.ToString() ?? "0";
        var netTaxableParsed = decimal.TryParse(netTaxableStr, out var netTaxable);
        if (!netTaxableParsed) throw new ArgumentException($"Invalid [NET_TAXABLE] value in SALES sheet cell K{i}");

        var vatStr = row.Cell("L").Value.ToString() ?? "0";
        var vatParsed = decimal.TryParse(vatStr, out var vat);
        if (!vatParsed) throw new ArgumentException($"Invalid [VAT] value in SALES sheet cell L{i}");

        return new Sales
        {
            EndOfMonth = eom,
            Tin = tin,
            RegName = regName.CleanUp(),
            LastName = lastName.CleanUp(),
            FirstName = firstName.CleanUp(),
            MiddleName = middleName.CleanUp(),
            Street = street.CleanUp(),
            City = city.CleanUp(),
            Exempt = exempt,
            ZeroRated = zeroRated,
            NetTaxable = netTaxable,
            Vat = vat,
        };
    }

    private static List<Purchases> GetPurchaseData(IXLWorkbook workbook, Info info)
    {
        var purchaseSheet = workbook.Worksheet("PURCHASES");
        if (purchaseSheet == null) throw new ArgumentException("PURCHASES sheet not found");

        var purchases = new List<Purchases>();
        var rowCount = purchaseSheet.LastRowUsed().RowNumber();
        for (var i = 2; i <= rowCount; i++)
        {
            var row = purchaseSheet.Row(i);
            var purchase = ParsePurchasesLineItem(row, i);

            if (purchase.Tin.Strip() == info.Tin)
                throw new ArgumentException($"TIN in PURCHASES sheet cell B{i} cannot be the same as the TIN of the taxpayer");

            purchases.Add(purchase);
        }

        return purchases;
    }

    // ReSharper disable once CognitiveComplexity
    private static Purchases ParsePurchasesLineItem(IXLRow row, int i)
    {
        if (row.IsEmpty()) throw new ArgumentException($"Empty row found in PURCHASES sheet at row {i}");

        var eomStr = row.Cell("A").Value.ToString();
        if (string.IsNullOrEmpty(eomStr)) throw new ArgumentException($"MONTH_END field is required in PURCHASES sheet cell A{i}");
        var eomParsed = DateTime.TryParse(eomStr, out var eom);

        if (!eomParsed) throw new ArgumentException($"Invalid [MONTH_END] value in PURCHASES sheet cell A{i}");

        var monthEnd = eom.Day;
        var expectedMonthEnd = DateTime.DaysInMonth(eom.Year, eom.Month);
        if (monthEnd != expectedMonthEnd)
            throw new ArgumentException($"Invalid [MONTH_END] value in PURCHASES sheet cell A{i}");

        var tin = row.Cell("B").Value.ToString() ?? string.Empty;
        if (!tin.IsValidTin()) throw new ArgumentException($"Invalid [TIN] value in PURCHASES sheet cell B{i}");

        var regName = row.Cell("C").Value.ToString() ?? string.Empty;
        var lastName = row.Cell("D").Value.ToString() ?? string.Empty;
        var firstName = row.Cell("E").Value.ToString() ?? string.Empty;
        var middleName = row.Cell("F").Value.ToString() ?? string.Empty;
        var street = row.Cell("G").Value.ToString() ?? string.Empty;
        var city = row.Cell("H").Value.ToString() ?? string.Empty;

        var exemptStr = row.Cell("I").Value.ToString() ?? "0";
        var exemptParsed = decimal.TryParse(exemptStr, out var exempt);
        if (!exemptParsed) throw new ArgumentException($"Invalid [EXEMPT] value in PURCHASES sheet cell I{i}");

        var zeroRatedStr = row.Cell("J").Value.ToString() ?? "0";
        var zeroRatedParsed = decimal.TryParse(zeroRatedStr, out var zeroRated);
        if (!zeroRatedParsed) throw new ArgumentException($"Invalid [ZERO_RATED] value in PURCHASES sheet cell J{i}");

        var serviceStr = row.Cell("K").Value.ToString() ?? "0";
        var serviceParsed = decimal.TryParse(serviceStr, out var service);
        if (!serviceParsed) throw new ArgumentException($"Invalid [SERVICE] value in PURCHASES sheet cell K{i}");

        var capitalGoodsStr = row.Cell("L").Value.ToString() ?? "0";
        var capitalGoodsParsed = decimal.TryParse(capitalGoodsStr, out var capitalGoods);
        if (!capitalGoodsParsed) throw new ArgumentException($"Invalid [CAPITAL_GOODS] value in PURCHASES sheet cell L{i}");

        var otherGoodsStr = row.Cell("M").Value.ToString() ?? "0";
        var otherGoodsParsed = decimal.TryParse(otherGoodsStr, out var otherGoods);
        if (!otherGoodsParsed) throw new ArgumentException($"Invalid [OTHER_GOODS] value in PURCHASES sheet cell M{i}");

        var inputTaxStr = row.Cell("N").Value.ToString() ?? "0";
        var inputTaxParsed = decimal.TryParse(inputTaxStr, out var inputTax);
        if (!inputTaxParsed) throw new ArgumentException($"Invalid [INPUT_TAX] value in PURCHASES sheet cell N{i}");

        var nonCreditableStr = row.Cell("O").Value.ToString() ?? "0";
        var nonCreditableParsed = decimal.TryParse(nonCreditableStr, out var nonCreditable);
        if (!nonCreditableParsed) throw new ArgumentException($"Invalid [NON_CREDITABLE] value in PURCHASES sheet cell O{i}");

        return new Purchases
        {
            EndOfMonth = eom,
            Tin = tin,
            RegName = regName.CleanUp(),
            LastName = lastName.CleanUp(),
            FirstName = firstName.CleanUp(),
            MiddleName = middleName.CleanUp(),
            Street = street.CleanUp(),
            City = city.CleanUp(),
            Exempt = exempt,
            ZeroRated = zeroRated,
            Service = service,
            CapitalGoods = capitalGoods,
            OtherGoods = otherGoods,
            InputTax = inputTax,
            NonCreditable = nonCreditable,
        };
    }
}