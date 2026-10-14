using CashFlow.Core.Categorization;

namespace CashFlow.Tests;

public class KeywordCategorizerTests
{
    private readonly KeywordCategorizer _sut = new();

    [Theory]
    [InlineData("Netflix monthly", "Subscriptions")]
    [InlineData("NETFLIX", "Subscriptions")]
    [InlineData("Salary", "Salary/Client Payment")]
    [InlineData("Car loan payment", "Loans/Installments")]
    [InlineData("Rental income", "Other Income")]
    [InlineData("Gasoline", "Transport")]
    [InlineData("Petrol", "Transport")]
    [InlineData("Electrician", "Other Expense")]
    // Whole-word rules: these look like keywords but must not match.
    [InlineData("Business lunch", "Other Expense")]
    [InlineData("Emirates flight", "Transport")]
    [InlineData("Pet food", "Other Expense")]
    [InlineData("Training course", "Uncategorized")]
    public void Suggests_the_expected_category(string description, string expected)
    {
        Assert.Equal(expected, _sut.Suggest(description).Category);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Empty_description_is_uncategorized_with_zero_confidence(string? description)
    {
        var result = _sut.Suggest(description);

        Assert.Equal("Uncategorized", result.Category);
        Assert.Equal(0, result.Confidence);
    }

    [Fact]
    public async Task Async_method_returns_the_same_result()
    {
        var result = await _sut.SuggestAsync("Spotify");

        Assert.Equal("Subscriptions", result.Category);
        Assert.Equal(KeywordCategorizer.SourceName, result.Source);
    }
}