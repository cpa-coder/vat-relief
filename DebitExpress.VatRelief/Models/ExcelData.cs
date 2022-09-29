using System.Collections.Generic;

namespace DebitExpress.VatRelief.Models;

public sealed class ExcelData
{
    public ExcelData(Info info, List<Sales> sales, List<Purchases> purchases)
    {
        Info = info;
        Sales = sales;
        Purchases = purchases;
    }

    public Info Info { get; }
    public List<Sales> Sales { get; }
    public List<Purchases> Purchases { get; }
}