using CashFlow.Core.Entities;

namespace CashFlow.Core.Projection;

public interface IProjectionService
{
    ProjectionResult Calculate(Scenario scenario, DateOnly? horizonEnd = null);
}