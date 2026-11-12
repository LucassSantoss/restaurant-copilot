using RestaurantCopilot.Evaluation.Grading;

namespace RestaurantCopilot.Evaluation.Tests;

public sealed class DeterministicGraderTests
{
    [Theory]
    [InlineData("Você faturou R$ 55.284,70 em outubro.", 55284.70)]
    [InlineData("O faturamento foi de R$ 55,284.70.", 55284.70)]
    [InlineData("Total: 55284.7 reais", 55284.70)]
    [InlineData("A nota média foi 3,22 estrelas.", 3.22)]
    [InlineData("Foram 422 pedidos concluídos.", 422)]
    public void Extract_ReadsBrazilianAndInternationalFormats(string text, decimal expected)
    {
        NumberExtractor.Extract(text).ShouldContain(expected);
    }

    [Fact]
    public void GradeNumeric_RequiresEveryExpectedValue()
    {
        const string answer = "Na última semana: R$ 13.143,60. Na anterior: R$ 12.889,90 (alta de 2,0%).";

        DeterministicGrader.GradeNumeric(answer, [13143.60m, 12889.90m], tolerance: null).Passed.ShouldBeTrue();
        DeterministicGrader.GradeNumeric(answer, [13143.60m, 11971.00m], tolerance: null).Passed.ShouldBeFalse();
    }

    [Fact]
    public void GradeNumeric_AcceptsRoundingWithinTolerance()
    {
        DeterministicGrader.GradeNumeric("Cerca de R$ 55.285", [55284.70m], tolerance: null).Passed.ShouldBeTrue();
        DeterministicGrader.GradeNumeric("Nota média de 3,2", [3.22m], tolerance: 0.01m).Passed.ShouldBeFalse();
    }

    [Theory]
    [InlineData("O dia mais fraco é a TERCA-feira.", true)]
    [InlineData("O dia mais fraco é a terça-feira.", true)]
    [InlineData("O dia mais fraco é a segunda-feira.", false)]
    public void GradeKeywords_IgnoresCaseAndAccents(string answer, bool passed)
    {
        DeterministicGrader.GradeKeywords(answer, ["terça"]).Passed.ShouldBe(passed);
    }

    [Fact]
    public void GradeKeywords_AcceptsAlternatives()
    {
        DeterministicGrader.GradeKeywords("A maior parte veio da entrega.", ["delivery|entrega"]).Passed.ShouldBeTrue();
    }
}
