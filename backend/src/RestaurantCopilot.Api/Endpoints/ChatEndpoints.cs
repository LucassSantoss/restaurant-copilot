using RestaurantCopilot.Api.Http;
using RestaurantCopilot.Application.Abstractions.Messaging;
using RestaurantCopilot.Application.Chat.AskQuestion;

namespace RestaurantCopilot.Api.Endpoints;

internal static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/chat", AskAsync)
            .WithTags("Chat")
            .WithSummary("Answers a natural-language question about the restaurant using the AI agent.")
            .WithDescription("The agent queries sales, reviews and the semantic index through tools, and returns the answer with the sources and tool calls that grounded it.")
            .Produces<ChatResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }

    private static async Task<IResult> AskAsync(
        ChatRequest request,
        ICommandHandler<AskQuestionCommand, ChatResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new AskQuestionCommand(request.Question), cancellationToken);
        return result.ToHttpResult();
    }

    internal sealed record ChatRequest(string Question);
}
