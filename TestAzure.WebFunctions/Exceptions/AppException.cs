using Microsoft.AspNetCore.Http;

namespace TestAzure.WebFunctions.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(IEnumerable<string> errors)
        : base(string.Join("\r\n", errors))
    {
    }

    protected AppException(string error)
        : base(error)
    {
    }

    protected AppException()
    {
    }

    public virtual int HttpStatusCode => StatusCodes.Status500InternalServerError;
}