namespace DebitExpress.VatRelief.Models;

public readonly struct Info
{
    public Info(string tin,
        int quarter,
        int month,
        int year, 
        string regName = "",
        string lastName = "",
        string firstName = "",
        string middleName = "",
        string tradeName = "",
        string street = "",
        string city = "",
        string rdo = "",
        int periodEnd = 12,
        bool nonIndividual = false)
    {
        Tin = tin;
        Month = month;
        Year = year;
        Quarter = quarter;
        RegName = regName;
        LastName = lastName;
        FirstName = firstName;
        MiddleName = middleName;
        TradeName = tradeName;
        Street = street;
        City = city;
        Rdo = rdo;
        PeriodEnd = periodEnd;
        NonIndividual = nonIndividual;
    }

    public string Tin { get; }
    public int Quarter { get; }
    public int Month { get; }
    public int Year { get; }
    public string RegName { get; }
    public string LastName { get; }
    public string FirstName { get; }
    public string MiddleName { get; }
    public string TradeName { get; }
    public string Street { get; }
    public string City { get; }
    public string Rdo { get; }
    public int PeriodEnd { get; }
    public bool NonIndividual { get; }
}