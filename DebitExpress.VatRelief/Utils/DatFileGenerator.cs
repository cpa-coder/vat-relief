using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DebitExpress.VatRelief.Models;

namespace DebitExpress.VatRelief.Utils;

public class DatFileGenerator
{
    private readonly Encoding _encoding;

    public DatFileGenerator()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding = CodePagesEncodingProvider.Instance.GetEncoding(1252) ?? throw new Exception("Encoding not found");
    }

    public async Task<Result> GenerateAsync(ExcelData data, string path)
    {
        var info = data.Info;
        var sales = data.Sales;
        var purchases = data.Purchases;

        var startingMonth = info.Month;
        var year = info.Year;
        var firstMonth = Extensions.GetEndOfMonth(year, startingMonth);
        var secondMonth = Extensions.GetEndOfMonth(year, startingMonth + 1);
        var thirdMonth = Extensions.GetEndOfMonth(year, startingMonth + 2);

        var firstMonthSales = sales.Where(x => x.EndOfMonth == firstMonth).ToList();
        var secondMonthSales = sales.Where(x => x.EndOfMonth == secondMonth).ToList();
        var thirdMonthSales = sales.Where(x => x.EndOfMonth == thirdMonth).ToList();

        var firstMonthPurchases = purchases.Where(x => x.EndOfMonth == firstMonth).ToList();
        var secondMonthPurchases = purchases.Where(x => x.EndOfMonth == secondMonth).ToList();
        var thirdMonthPurchases = purchases.Where(x => x.EndOfMonth == thirdMonth).ToList();

        if (firstMonthSales.Count == 0 && secondMonthSales.Count == 0 && thirdMonthSales.Count == 0 &&
            firstMonthPurchases.Count == 0 && secondMonthPurchases.Count == 0 && thirdMonthPurchases.Count == 0)
            return new Result(new Exception("No data available for generating the files"));

        var generator = new DatFileGenerator();
        if (firstMonthSales.Count > 0) await generator.GenerateSalesAsync(info, firstMonth, firstMonthSales, path);
        if (secondMonthSales.Count > 0) await generator.GenerateSalesAsync(info, secondMonth, secondMonthSales, path);
        if (thirdMonthSales.Count > 0) await generator.GenerateSalesAsync(info, thirdMonth, thirdMonthSales, path);

        if (firstMonthPurchases.Count > 0) await generator.GeneratePurchasesAsync(info, firstMonth, firstMonthPurchases, path);
        if (secondMonthPurchases.Count > 0) await generator.GeneratePurchasesAsync(info, secondMonth, secondMonthPurchases, path);
        if (thirdMonthPurchases.Count > 0) await generator.GeneratePurchasesAsync(info, thirdMonth, thirdMonthPurchases, path);

        return new Result();
    }

    private async Task GenerateSalesAsync(Info info, DateTime month, IReadOnlyCollection<Sales> items, string path)
    {
        var datFileFolder = Path.Combine(path, "DAT FILES");
        Directory.CreateDirectory(datFileFolder);

        var fileName = $"{info.Tin}S{month:MM}{month:yyyy}.DAT";
        var fullPath = Path.Combine(datFileFolder, fileName);

        await using var file = new StreamWriter(File.Open(fullPath, FileMode.Create), _encoding);

        await WriteSalesHeader(file, info, month, items);
        var orderedList = ReorderSalesItems(items);
        await WriteSalesDataAsync(info, month, orderedList, file);
    }

    private static async Task WriteSalesHeader(TextWriter file, Info info, DateTime month, IReadOnlyCollection<Sales> items)
    {
        var registerName = info.NonIndividual ? info.RegName : string.Empty;
        var lastName = info.NonIndividual ? string.Empty : info.LastName;
        var firstName = info.NonIndividual ? string.Empty : info.FirstName;
        var middleName = info.NonIndividual ? string.Empty : info.MiddleName;

        var header = $"H,S,\"{info.Tin}\"," +
                     $"\"{registerName}\",\"{lastName}\"," +
                     $"\"{firstName}\",\"{middleName}\"," +
                     $"\"{info.TradeName}\"," +
                     $"\"{info.Street}\"," +
                     $"\"{info.City}\"," +
                     $"{items.Sum(i => i.Exempt).Round()}," +
                     $"{items.Sum(i => i.ZeroRated).Round()}," +
                     $"{items.Sum(i => i.NetTaxable).Round()}," +
                     $"{items.Sum(i => i.Vat).Round()}," +
                     $"{info.Rdo}," +
                     $"{month:MM/dd/yyyy}," +
                     $"{info.PeriodEnd}";

        await file.WriteLineAsync(header);
    }

    private static IEnumerable<Sales> ReorderSalesItems(IReadOnlyCollection<Sales> items)
    {
        var withTin = items
            .Where(i => !string.IsNullOrEmpty(i.Tin))
            .GroupBy(i => i.Tin)
            .Select(i => new Sales
            {
                Tin = i.First().Tin,
                RegName = i.First().RegName,
                LastName = i.First().LastName,
                FirstName = i.First().FirstName,
                MiddleName = i.First().MiddleName,
                Street = i.First().Street,
                City = i.First().City,
                Exempt = i.Sum(x => x.Exempt),
                ZeroRated = i.Sum(x => x.ZeroRated),
                NetTaxable = i.Sum(x => x.NetTaxable),
                Vat = i.Sum(x => x.Vat)
            }).ToList();
        var emptyTin = items
            .Where(i => string.IsNullOrEmpty(i.Tin) && !string.IsNullOrWhiteSpace(i.RegName))
            .GroupBy(i => i.RegName)
            .Select(i => new Sales
            {
                Tin = i.First().Tin,
                RegName = i.First().RegName,
                LastName = i.First().LastName,
                FirstName = i.First().FirstName,
                MiddleName = i.First().MiddleName,
                Street = i.First().Street,
                City = i.First().City,
                Exempt = i.Sum(x => x.Exempt),
                ZeroRated = i.Sum(x => x.ZeroRated),
                NetTaxable = i.Sum(x => x.NetTaxable),
                Vat = i.Sum(x => x.Vat)
            }).ToList();
        var emptyReg = items
            .Where(i => string.IsNullOrEmpty(i.Tin) &&
                        string.IsNullOrEmpty(i.RegName) &&
                        !string.IsNullOrEmpty(i.FullName))
            .GroupBy(i => i.FullName)
            .Select(i => new Sales
            {
                Tin = i.First().Tin,
                RegName = i.First().RegName,
                LastName = i.First().LastName,
                FirstName = i.First().FirstName,
                MiddleName = i.First().MiddleName,
                Street = i.First().Street,
                City = i.First().City,
                Exempt = i.Sum(x => x.Exempt),
                ZeroRated = i.Sum(x => x.ZeroRated),
                NetTaxable = i.Sum(x => x.NetTaxable),
                Vat = i.Sum(x => x.Vat)
            }).ToList();

        withTin.AddRange(emptyTin);
        withTin.AddRange(emptyReg);

        var orderedList = withTin.OrderBy(i => i.Tin)
            .ThenBy(i => i.RegName)
            .ThenBy(i => i.LastName);
        return orderedList;
    }

    private static async Task WriteSalesDataAsync(Info info, DateTime month,
        IEnumerable<Sales> items, TextWriter file)
    {
        foreach (var item in items)
        {
            var line = $"D,S,\"{item.Tin.Strip()}\"," +
                       $"\"{item.RegName}\"," +
                       $"\"{item.LastName}\"," +
                       $"\"{item.FirstName}\"," +
                       $"\"{item.MiddleName}\"," +
                       $"\"{item.Street}\"," +
                       $"\"{item.City}\"," +
                       $"{item.Exempt.ToValue()}," +
                       $"{item.ZeroRated.ToValue()}," +
                       $"{item.NetTaxable.ToValue()}," +
                       $"{item.Vat.ToValue()}," +
                       $"{info.Tin}," +
                       $"{month:MM/dd/yyyy}";

            await file.WriteLineAsync(line);
        }
    }

    private async Task GeneratePurchasesAsync(Info company, DateTime month, List<Purchases> items, string path)
    {
        var datFileFolder = Path.Combine(path, "DAT FILES");
        Directory.CreateDirectory(datFileFolder);

        var fileName = $"{company.Tin.Strip()}P{month:MM}{month:yyyy}.DAT";

        var fullPath = Path.Combine(datFileFolder, fileName);

        await using var file = new StreamWriter(File.Open(fullPath, FileMode.Create), _encoding);

        await WritePurchasesHeader(file, company, month, items);
        var orderedList = ReorderPurchaseItems(items);
        await WritePurchasesDataAsync(file, company, month, orderedList);
    }

    private async Task WritePurchasesHeader(TextWriter file, Info info, DateTime month, IReadOnlyCollection<Purchases> items)
    {
        var registerName = info.NonIndividual ? info.RegName : string.Empty;
        var lastName = info.NonIndividual ? string.Empty : info.LastName;
        var firstName = info.NonIndividual ? string.Empty : info.FirstName;
        var middleName = info.NonIndividual ? string.Empty : info.MiddleName;

        var header = $"H,P,\"{info.Tin}\"," +
                     $"\"{registerName}\",\"{lastName}\"," +
                     $"\"{firstName}\",\"{middleName}\"," +
                     $"\"{info.TradeName}\"," +
                     $"\"{info.Street}\"," +
                     $"\"{info.City}\"," +
                     $"{items.Sum(i => i.Exempt).Round()}," +
                     $"{items.Sum(i => i.ZeroRated).Round()}," +
                     $"{items.Sum(i => i.Service).Round()}," +
                     $"{items.Sum(i => i.CapitalGoods).Round()}," +
                     $"{items.Sum(i => i.OtherGoods).Round()}," +
                     $"{items.Sum(i => i.InputTax).Round()}," +
                     $"{items.Sum(i => i.InputTax - i.NonCreditable).Round()}," +
                     $"{items.Sum(i => i.NonCreditable).Round()}," +
                     $"{info.Rdo}," +
                     $"{month:MM/dd/yyyy}," +
                     $"{info.PeriodEnd}";

        await file.WriteLineAsync(header);
    }

    private IEnumerable<Purchases> ReorderPurchaseItems(IReadOnlyCollection<Purchases> items)
    {
        var orderedItems = items
            .GroupBy(i => i.Tin)
            .Select(i => new Purchases
            {
                Tin = i.First().Tin,
                RegName = i.First().RegName,
                LastName = i.First().LastName,
                FirstName = i.First().FirstName,
                MiddleName = i.First().MiddleName,
                Street = i.First().Street,
                City = i.First().City,
                Exempt = i.Sum(x => x.Exempt),
                ZeroRated = i.Sum(x => x.ZeroRated),
                Service = i.Sum(x => x.Service),
                CapitalGoods = i.Sum(x => x.CapitalGoods),
                OtherGoods = i.Sum(x => x.OtherGoods),
                InputTax = i.Sum(x => x.InputTax),
                NonCreditable = i.Sum(x => x.NonCreditable)
            }).ToList();

        return orderedItems.OrderBy(i => i.RegName).ThenBy(i => i.LastName);
    }

    private async Task WritePurchasesDataAsync(TextWriter file, Info info, DateTime month, IEnumerable<Purchases> items)
    {
        foreach (var item in items)
        {
            var line = $"D,P,{item.Tin.Strip()}," +
                       $"\"{item.RegName}\"," +
                       $"\"{item.LastName}\"," +
                       $"\"{item.FirstName}\"," +
                       $"\"{item.MiddleName}\"," +
                       $"\"{item.Street}\"," +
                       $"\"{item.City}\"," +
                       $"{item.Exempt.ToValue()}," +
                       $"{item.ZeroRated.ToValue()}," +
                       $"{item.Service.ToValue()}," +
                       $"{item.CapitalGoods.ToValue()}," +
                       $"{item.OtherGoods.ToValue()}," +
                       $"{item.InputTax.ToValue()}," +
                       $"{info.Tin.Strip()}," +
                       $"{month:MM/dd/yyyy}";

            await file.WriteLineAsync(line);
        }
    }
}