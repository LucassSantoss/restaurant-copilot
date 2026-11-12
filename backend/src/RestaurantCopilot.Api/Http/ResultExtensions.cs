using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Api.Http;

internal static class ResultExtensions
{
    /// <summary>Maps a successful result to 200 OK and a business error to an RFC 9457 problem response.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();

    public static IResult ToProblem(this Error error) =>
        TypedResults.Problem(
            statusCode: error.Type switch
            {
                ErrorType.Validation => StatusCodes.Status400BadRequest,
                ErrorType.NotFound => StatusCodes.Status404NotFound,
                ErrorType.Conflict => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status500InternalServerError,
            },
            title: error.Code,
            detail: error.Message);
}
