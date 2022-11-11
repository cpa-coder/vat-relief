using System;
using DebitExpress.StringBuilders;

namespace DebitExpress.VatRelief.Models;

public struct Purchases
{
    public DateTime EndOfMonth { get; set; }
    public string Tin { get; set; }

    public string RegName { get; set; }

    public string LastName { get; set; }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string FullName => new NameBuilder()
        .LastName(LastName)
        .WithFirstName(FirstName)
        .WithMiddleName(MiddleName)
        .ToString() ?? string.Empty;
    public string Street { get; set; }

    public string City { get; set; }

    public decimal Exempt { get; set; }

    public decimal ZeroRated { get; set; }

    public decimal Service { get; set; }

    public decimal CapitalGoods { get; set; }

    public decimal OtherGoods { get; set; }

    public decimal InputTax { get; set; }

    public decimal NonCreditable { get; set; }
}