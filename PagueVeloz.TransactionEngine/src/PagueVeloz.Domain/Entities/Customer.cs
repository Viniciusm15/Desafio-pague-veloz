namespace PagueVeloz.Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Document { get; private set; }

    private Customer(string name, string document)
    {
        Id = Guid.NewGuid();
        Name = name;
        Document = document;
    }

    public static Customer Create(string name, string document)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(document))
            throw new ArgumentException("Customer document is required.");

        var digits = new string(document.Where(char.IsDigit).ToArray());
        if (digits.Length != 11 && digits.Length != 14)
            throw new ArgumentException("Customer document must be a valid CPF or CNPJ.");

        return new Customer(name, digits);
    }
}
