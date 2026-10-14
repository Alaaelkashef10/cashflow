namespace CashFlow.Api.Contracts;

public sealed record CategorizeRequest(string Description);

public sealed record CategorizeResponse(string Category, double Confidence, string Source);