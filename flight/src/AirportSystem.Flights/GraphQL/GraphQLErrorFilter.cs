namespace AirportSystem.Flights.GraphQL;

public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        if (error.Exception is null) return error;

        _logger.LogError(error.Exception, "GraphQL error: {Message}", error.Exception.Message);

        return error.Exception switch
        {
            KeyNotFoundException e =>
                error.WithMessage(e.Message).WithCode("NOT_FOUND").RemoveExtensions(),

            InvalidOperationException e =>
                error.WithMessage(e.Message).WithCode("BAD_REQUEST").RemoveExtensions(),

            UnauthorizedAccessException e =>
                error.WithMessage(e.Message).WithCode("UNAUTHORIZED").RemoveExtensions(),

            ArgumentException e =>
                error.WithMessage(e.Message).WithCode("INVALID_ARGUMENT").RemoveExtensions(),

            _ => error
                .WithMessage("An unexpected error occurred.")
                .WithCode("INTERNAL_ERROR")
                .RemoveExtensions()
        };
    }
}
