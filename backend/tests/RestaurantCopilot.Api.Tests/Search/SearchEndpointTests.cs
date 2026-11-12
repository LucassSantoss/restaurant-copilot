using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RestaurantCopilot.Api.Tests.Infrastructure;
using RestaurantCopilot.Application.Abstractions.Ai;
using RestaurantCopilot.Domain.Common;

namespace RestaurantCopilot.Api.Tests.Search;

[Collection(ApiTestCollectionDefinition.Name)]
public sealed class SearchEndpointTests(ApiFactory factory)
{
    [Fact]
    public async Task ReindexThenSearch_FindsRelevantReviews()
    {
        await using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IEmbeddingService, KeywordEmbeddingService>()));
        using var client = app.CreateClient();

        var reindex = await client.PostAsync("/api/ingestion/reindex", null, TestContext.Current.CancellationToken);
        var counts = await reindex.Content.ReadFromJsonAsync<ReindexBody>(TestContext.Current.CancellationToken);
        var results = await client.GetFromJsonAsync<List<SearchResultBody>>(
            "/api/search?query=molho vazou da embalagem&source=Review&limit=3", TestContext.Current.CancellationToken);

        reindex.StatusCode.ShouldBe(HttpStatusCode.OK);
        counts.ShouldNotBeNull();
        counts.MenuItems.ShouldBe(24);
        counts.Reviews.ShouldBeGreaterThanOrEqualTo(242);
        results.ShouldNotBeNull();
        results.Count.ShouldBe(3);
        results.ShouldAllBe(result => result.Source == "Review"
            && (result.Content.Contains("embalagem", StringComparison.OrdinalIgnoreCase) || result.Content.Contains("vaz", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task NewReview_BecomesSearchableWithoutReindex()
    {
        var llm = Substitute.For<ILlmClient>();
        llm.GenerateAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<LlmResponse>(LlmErrors.RateLimited));
        await using var app = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ReviewProcessing:Enabled", "true");
            builder.UseSetting("ReviewProcessing:ClassifyReviews", "false");
            builder.UseSetting("ReviewProcessing:PollInterval", "00:00:00.200");
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton<IEmbeddingService, KeywordEmbeddingService>();
                services.AddSingleton(llm);
            });
        });
        using var client = app.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        const string comment = "Garçom distraído esqueceu nosso guardanapo xadrez";

        var created = await client.PostAsJsonAsync("/api/reviews", new { orderNumber = 1004, rating = 2, comment }, cancellationToken);
        created.StatusCode.ShouldBe(HttpStatusCode.Created);

        List<SearchResultBody>? results = null;
        for (var attempt = 0; attempt < 60 && !(results?.Any(result => result.Content.Contains(comment)) ?? false); attempt++)
        {
            await Task.Delay(500, cancellationToken);
            results = await client.GetFromJsonAsync<List<SearchResultBody>>(
                "/api/search?query=garçom distraído guardanapo xadrez&source=Review&limit=3", cancellationToken);
        }

        results.ShouldNotBeNull();
        results.ShouldContain(result => result.Content.Contains(comment));
    }

    [Fact]
    public async Task Search_WithoutLlmKey_ReturnsServiceUnavailable()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/search?query=embalagem", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Search_WithInvalidLimit_ReturnsValidationProblem()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/search?query=embalagem&limit=50", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private sealed record ReindexBody(int MenuItems, int Reviews);

    private sealed record SearchResultBody(string Source, Guid SourceId, string Content, double Similarity);
}
