namespace PagueVeloz.IntegrationTests.Builders;

public class CustomerRequestBuilder
{
    private string _name = "John Doe";
    private string _document = "52998224725";

    public CustomerRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CustomerRequestBuilder WithDocument(string document)
    {
        _document = document;
        return this;
    }

    public object Build()
    {
        return new
        {
            name = _name,
            document = _document
        };
    }
}
