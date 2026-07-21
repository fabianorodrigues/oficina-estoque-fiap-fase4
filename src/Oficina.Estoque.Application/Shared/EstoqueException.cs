namespace Oficina.Estoque.Application.Shared;

public sealed class EstoqueException : Exception
{
    public EstoqueException(string code, string message, int statusCode)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public string Code { get; }
    public int StatusCode { get; }

    public static EstoqueException NotFound(string message) => new("not_found", message, 404);
    public static EstoqueException Conflict(string message) => new("conflict", message, 409);
    public static EstoqueException Validation(string message) => new("validation_error", message, 400);
}
