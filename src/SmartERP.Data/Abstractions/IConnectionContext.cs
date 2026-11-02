namespace SmartERP.Data.Abstractions;

public interface IConnectionContext
{
    string? ClientIdentifier { get; set; }
}