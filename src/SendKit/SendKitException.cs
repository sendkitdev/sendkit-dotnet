namespace SendKit;

public class SendKitException : Exception
{
    public string Name { get; }
    public int? StatusCode { get; }

    public SendKitException(string message, string name, int? statusCode = null)
        : base(message)
    {
        Name = name;
        StatusCode = statusCode;
    }
}
