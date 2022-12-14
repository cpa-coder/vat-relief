using System;
using DebitExpress.StringBuilders;

namespace DebitExpress.VatRelief.Models;

public struct Sales
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
    public decimal NetTaxable { get; set; }
    public decimal Vat { get; set; }
}