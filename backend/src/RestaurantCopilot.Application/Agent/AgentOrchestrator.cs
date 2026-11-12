using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Application.Abstractions.Persistence;
using RestaurantCopilot.Application.Agent.Tools;
using RestaurantCopilot.Application.Common;
using RestaurantCopilot.Domain.Common;
using RestaurantCopilot.Domain.Restaurants;

namespace RestaurantCopilot.Application.Agent;

/// <summary>
/// Runs the function-calling loop: the model either answers or asks for tools; the application runs
/// the tools and feeds the results back, until the model answers. The loop is bounded by
/// <see cref="AgentOptions.MaxIterations"/> and <see cref="AgentOptions.Timeout"/>, and a wrong tool
/// name or invalid arguments are reported back to the model instead of failing the request.
/// </summary>
internal sealed class AgentOrchestrator(
    ILlmClient llmClient,
    IEnumerable<IAgentTool> tools,
    IRestaurantRepository restaurants,
    IMenuItemRepository menuItems,
    ISalesRepository sales,
    TimeProvider timeProvider,
    IOptions<AgentOptions> options,
    ILogger<AgentOrchestrator> logger)
{
    private readonly AgentOptions _options = options.Value;
    private readonly Dictionary<string, IAgentTool> _tools = tools.ToDictionary(tool => tool.Name);

    public async Task<Result<AgentAnswer>> AnswerAsync(string question, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.Timeout);

        try
        {
            return await RunAsync(question, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Agent timed out after {Timeout}", _options.Timeout);
            return AgentErrors.Timeout;
        }
    }

    private async Task<Result<AgentAnswer>> RunAsync(string question, CancellationToken cancellationToken)
    {
        var restaurant = await restaurants.GetAsync(cancellationToken);
        if (restaurant is null)
        {
            return ApplicationErrors.RestaurantNotConfigured;
        }

        var context = new AgentContext(restaurant, ReportingPeriod.Today(restaurant, timeProvider));
        var conversation = new Conversation(await BuildSystemInstructionAsync(context, cancellationToken), question);
        var declarations = _tools.Values
            .Select(tool => new LlmToolDeclaration(tool.Name, tool.Description, tool.ParametersSchema))
            .ToList();

        for (var iteration = 1; iteration <= _options.MaxIterations; iteration++)
        {
            var response = await conversation.SendAsync(llmClient, declarations, cancellationToken);
            if (response.IsFailure)
            {
                return response.Error;
            }

            if (!response.Value.HasToolCalls)
            {
                return conversation.Finish(response.Value);
            }

            foreach (var call in response.Value.ToolCalls)
            {
                await ExecuteToolAsync(call, context, conversation, cancellationToken);
            }
        }

        logger.LogWarning("Agent reached {MaxIterations} iterations; forcing a final answer", _options.MaxIterations);
        conversation.Add(new UserMessage(AgentPrompt.FinalAnswerInstruction));
        var final = await conversation.SendAsync(llmClient, tools: [], cancellationToken);
        return final.IsFailure ? final.Error : conversation.Finish(final.Value);
    }

    private async Task ExecuteToolAsync(LlmToolCall call, AgentContext context, Conversation conversation, CancellationToken cancellationToken)
    {
        var result = _tools.TryGetValue(call.Name, out var tool)
            ? await tool.ExecuteAsync(call.Arguments, context, cancellationToken)
            : AgentToolErrors.UnknownTool(call.Name, _tools.Keys);

        if (result.IsSuccess)
        {
            logger.LogInformation("Agent tool {Tool} succeeded", call.Name);
            conversation.RecordToolSuccess(call, JsonSerializer.SerializeToElement(result.Value.Data, AgentJson.SerializerOptions), result.Value.Sources);
        }
        else
        {
            logger.LogInformation("Agent tool {Tool} failed: {Error}", call.Name, result.Error.Message);
            conversation.RecordToolFailure(call, result.Error.Message);
        }
    }

    private async Task<string> BuildSystemInstructionAsync(AgentContext context, CancellationToken cancellationToken)
    {
        var menu = await menuItems.ListAsync(category: null, availableOnly: false, cancellationToken);
        var range = await sales.GetOrderDateRangeAsync(cancellationToken);

        return AgentPrompt.SystemInstruction(
            context,
            menu.Select(item => item.Name).ToList(),
            range is { } dates ? (LocalDate(dates.First, context.Restaurant), LocalDate(dates.Last, context.Restaurant)) : null);
    }

    private static DateOnly LocalDate(DateTimeOffset instant, Restaurant restaurant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(restaurant.TimeZoneId)).DateTime);

    /// <summary>The messages exchanged so far, plus what the final answer will report (sources, steps, tokens).</summary>
    private sealed class Conversation(string systemInstruction, string question)
    {
        private readonly List<LlmMessage> _messages = [new UserMessage(question)];
        private readonly List<AgentSource> _sources = [];
        private readonly List<AgentStep> _steps = [];
        private int _inputTokens;
        private int _outputTokens;

        public void Add(LlmMessage message) => _messages.Add(message);

        public async Task<Result<LlmResponse>> SendAsync(
            ILlmClient llmClient,
            IReadOnlyList<LlmToolDeclaration> tools,
            CancellationToken cancellationToken)
        {
            var request = new LlmRequest(systemInstruction, _messages.ToList()) { Tools = tools, Temperature = 0.2 };
            var response = await llmClient.GenerateAsync(request, cancellationToken);
            if (response.IsSuccess)
            {
                _inputTokens += response.Value.Usage.InputTokens;
                _outputTokens += response.Value.Usage.OutputTokens;
                if (response.Value.HasToolCalls)
                {
                    _messages.Add(new AssistantMessage(response.Value.Text, response.Value.ToolCalls));
                }
            }

            return response;
        }

        public void RecordToolSuccess(LlmToolCall call, JsonElement data, IReadOnlyList<AgentSource> sources)
        {
            _messages.Add(new ToolResultMessage(call.Name, data, call.CallId));
            _steps.Add(new AgentStep(call.Name, call.Arguments, Succeeded: true, Error: null));
            _sources.AddRange(sources.Where(source => !_sources.Contains(source)));
        }

        public void RecordToolFailure(LlmToolCall call, string error)
        {
            _messages.Add(new ToolResultMessage(call.Name, JsonSerializer.SerializeToElement(new { error }), call.CallId));
            _steps.Add(new AgentStep(call.Name, call.Arguments, Succeeded: false, error));
        }

        public Result<AgentAnswer> Finish(LlmResponse response) =>
            string.IsNullOrWhiteSpace(response.Text)
                ? AgentErrors.EmptyAnswer
                : new AgentAnswer(response.Text.Trim(), _sources.ToList(), _steps.ToList(), new LlmUsage(_inputTokens, _outputTokens));
    }
}
