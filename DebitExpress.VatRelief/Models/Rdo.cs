namespace DebitExpress.VatRelief.Models;

public readonly struct Rdo
{
    public Rdo(string number, string name)
    {
        Number = number;
        Name = name;
    }

    public string Number { get; }
    public string Name { get; }
}