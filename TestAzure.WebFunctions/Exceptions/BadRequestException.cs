using Microsoft.AspNetCore.Http;

namespace TestAzure.WebFunctions.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(IEnumerable<string> errors) : base(errors)
    {
    }

    public BadRequestException(string error) : base(error)
    {
    }

    public override int HttpStatusCode => StatusCodes.Status400BadRequest;
}