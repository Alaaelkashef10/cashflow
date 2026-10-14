using CashFlow.Core.Categorization;
using CashFlow.Core.Entities;

using Xunit.Abstractions;

namespace CashFlow.Tests;

public class CategorizerEvaluationTests(ITestOutputHelper output)
{
    private sealed record Row(string Split, string Description, string Category);

    private static string FindDatasetPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "ai", "data", "descriptions.csv");
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        throw new FileNotFoundException("Could not find ai/data/descriptions.csv above the test output folder.");
    }

    private static List<Row> LoadRows() =>
        File.ReadLines(FindDatasetPath())
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(','))
            .Select(p => new Row(p[0].Trim(), p[1].Trim(), p[2].Trim()))
            .ToList();

    private static List<(Row Row, string Predicted)> Predict(IEnumerable<Row> rows)
    {
        var categorizer = new KeywordCategorizer();
        return rows.Select(r => (r, categorizer.Suggest(r.Description).Category)).ToList();
    }

    private static double Accuracy(List<(Row Row, string Predicted)> results) =>
        results.Count(r => r.Row.Category == r.Predicted) / (double)results.Count;

    [Fact]
    public void Dataset_is_well_formed()
    {
        var rows = LoadRows();

        Assert.True(rows.Count >= 100, "The dataset should have at least 100 rows.");
        Assert.All(rows, r => Assert.Contains(r.Category, Categories.All));
        Assert.All(rows, r => Assert.True(r.Split is "train" or "test", $"Bad split: {r.Split}"));

        foreach (var category in Categories.All)
        {
            Assert.Contains(rows, r => r.Split == "train" && r.Category == category);
            Assert.Contains(rows, r => r.Split == "test" && r.Category == category);
        }
    }

    [Fact]
    public void Keyword_baseline_meets_the_accuracy_target_on_the_test_split()
    {
        var rows = LoadRows();
        var train = Predict(rows.Where(r => r.Split == "train"));
        var test = Predict(rows.Where(r => r.Split == "test"));

        Assert.All(train.Concat(test), r => Assert.Contains(r.Predicted, Categories.All));

        output.WriteLine("KEYWORD BASELINE EVALUATION");
        output.WriteLine($"Train accuracy: {Accuracy(train):P1} ({train.Count} rows)");
        output.WriteLine($"Test accuracy:  {Accuracy(test):P1} ({test.Count} rows)");
        output.WriteLine("");
        output.WriteLine("Test split, correct / total per category:");

        foreach (var group in test.GroupBy(r => r.Row.Category).OrderBy(g => g.Key))
        {
            var correct = group.Count(r => r.Predicted == r.Row.Category);
            output.WriteLine($"  {group.Key,-24} {correct} / {group.Count()}");
        }

        output.WriteLine("");
        output.WriteLine("Test errors (expected -> predicted):");
        foreach (var error in test.Where(r => r.Predicted != r.Row.Category))
            output.WriteLine($"  \"{error.Row.Description}\": {error.Row.Category} -> {error.Predicted}");

        // NFR-09: at least 80% accuracy on the held-out test set.
        Assert.True(Accuracy(test) >= 0.80, $"Test accuracy was {Accuracy(test):P1}.");
    }
}