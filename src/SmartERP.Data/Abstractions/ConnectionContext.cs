namespace SmartERP.Data.Abstractions;

public class ConnectionContext : IConnectionContext
{
    private static readonly AsyncLocal<string?> _value = new();
    public string? ClientIdentifier { get => _value.Value; set => _value.Value = value; }
}