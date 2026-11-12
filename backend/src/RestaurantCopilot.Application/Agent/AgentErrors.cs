using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Application.Agent;

public static class AgentErrors
{
    public static readonly Error Timeout = Error.Unavailable(
        "Agent.Timeout", "The agent took too long to answer. Try a more specific question.");

    public static readonly Error EmptyAnswer = Error.Failure(
        "Agent.EmptyAnswer", "The model finished without producing an answer.");
}
