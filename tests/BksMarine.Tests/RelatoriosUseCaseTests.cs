using BksMarine.Application.Reports;
using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using Xunit;

namespace BksMarine.Tests;

public sealed class RelatoriosUseCaseTests
{
    private static readonly Guid PortId = Guid.NewGuid();

    [Fact]
    public async Task Generates_pdf_without_filters()
    {
        var repo = new FakeOperationRepository(OneRow());
        var generator = new FakeReportGenerator();
        var useCase = new GenerateOperationReport(repo, generator);

        var result = await useCase.ExecuteAsync(null, null, null, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, result.Value!.Content);
        Assert.EndsWith(".pdf", result.Value.FileName);
        Assert.Single(generator.LastData!.Rows);
    }

    [Fact]
    public async Task Forwards_filters_to_repository()
    {
        var repo = new FakeOperationRepository(OneRow());
        var generator = new FakeReportGenerator();
        var useCase = new GenerateOperationReport(repo, generator);
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        await useCase.ExecuteAsync(OperationType.Docking, from, to, PortId);

        Assert.Equal(OperationType.Docking, repo.LastType);
        Assert.Equal(from, repo.LastFrom);
        Assert.Equal(to, repo.LastTo);
        Assert.Equal(PortId, repo.LastPortId);
    }

    [Fact]
    public async Task Invalid_period_fails()
    {
        var useCase = new GenerateOperationReport(new FakeOperationRepository(), new FakeReportGenerator());
        var result = await useCase.ExecuteAsync(null, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), null);
        Assert.True(result.IsFailure);
        Assert.Equal("validation.period", result.Error!.Code);
    }

    [Fact]
    public async Task Empty_result_still_generates()
    {
        var useCase = new GenerateOperationReport(new FakeOperationRepository(), new FakeReportGenerator());
        var result = await useCase.ExecuteAsync(null, null, null, null);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Content);
    }

    [Fact]
    public async Task QuestPdf_generates_valid_pdf()
    {
        var generator = new Infrastructure.Reports.QuestPdfReportGenerator(Path.GetTempPath());
        var data = new OperationReportData(null, null, null, null, new List<OperationReportRow> { OneRow() });

        var pdf = await generator.GenerateAsync(data);

        Assert.True(pdf.Length > 0);
        Assert.Equal(0x25, pdf[0]); // '%' magic
        Assert.Equal(0x50, pdf[1]);
        Assert.Equal(0x44, pdf[2]);
        Assert.Equal(0x46, pdf[3]);
    }

    private static OperationReportRow OneRow() =>
        new(Guid.NewGuid(), DateTime.UtcNow, OperationType.Docking, "Pomone", "Santos", "B1",
            TransmissionStatus.NotTransmitted, new List<string>());

    private sealed class FakeReportGenerator : IReportGenerator
    {
        public OperationReportData? LastData { get; private set; }

        public Task<byte[]> GenerateAsync(OperationReportData data, CancellationToken ct = default)
        {
            LastData = data;
            var pdf = data.Rows.Count > 0 ? new byte[] { 0x25, 0x50, 0x44, 0x46 } : Array.Empty<byte>();
            return Task.FromResult(pdf);
        }
    }

    private sealed class FakeOperationRepository : IOperationRepository
    {
        private readonly List<OperationReportRow> _rows;
        private readonly List<Operation> _operations = new();

        public OperationType? LastType { get; private set; }
        public DateTime? LastFrom { get; private set; }
        public DateTime? LastTo { get; private set; }
        public Guid? LastPortId { get; private set; }

        public FakeOperationRepository(params OperationReportRow[] rows) => _rows = rows.ToList();

        public Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_operations.FirstOrDefault(o => o.Id == id));

        public Task<List<Operation>> ListAsync(OperationType? type, DateTime? from, DateTime? to, CancellationToken ct = default) =>
            Task.FromResult(_operations);

        public Task<List<OperationReportRow>> ListReportAsync(OperationType? type, DateTime? from, DateTime? to, Guid? portId, CancellationToken ct = default)
        {
            LastType = type;
            LastFrom = from;
            LastTo = to;
            LastPortId = portId;
            return Task.FromResult(_rows);
        }

        public Task AddAsync(Operation operation, CancellationToken ct = default) => Task.CompletedTask;
        public Task UpdateAsync(Operation operation, CancellationToken ct = default) => Task.CompletedTask;
    }
}
