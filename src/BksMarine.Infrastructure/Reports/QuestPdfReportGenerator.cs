using BksMarine.Core.Domain.Operations;
using BksMarine.Core.Domain.Ports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BksMarine.Infrastructure.Reports;

public sealed class QuestPdfReportGenerator : IReportGenerator
{
    private readonly string _uploadsDirectory;

    public QuestPdfReportGenerator(string uploadsDirectory) => _uploadsDirectory = uploadsDirectory;

    public Task<byte[]> GenerateAsync(OperationReportData data, CancellationToken ct = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Relatório de Operações").FontSize(18).Bold();
                    col.Item().Text(BuildFilterDescription(data)).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                page.Content().Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.4f); // data
                            c.RelativeColumn(1);     // tipo
                            c.RelativeColumn(1.6f); // navio
                            c.RelativeColumn(1.2f); // porto
                            c.RelativeColumn(1.2f); // berço
                            c.RelativeColumn(1.2f); // responsável
                            c.RelativeColumn(1.4f); // status
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Data");
                            header.Cell().Element(HeaderCell).Text("Tipo");
                            header.Cell().Element(HeaderCell).Text("Navio");
                            header.Cell().Element(HeaderCell).Text("Porto");
                            header.Cell().Element(HeaderCell).Text("Berço");
                            header.Cell().Element(HeaderCell).Text("Responsável");
                            header.Cell().Element(HeaderCell).Text("Transmissão");
                        });

                        foreach (var row in data.Rows)
                        {
                            table.Cell().Text(row.OccurredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                            table.Cell().Text(row.Type.ToString());
                            table.Cell().Text(row.ShipName);
                            table.Cell().Text(row.PortName);
                            table.Cell().Text(row.BerthName);
                            table.Cell().Text(string.IsNullOrWhiteSpace(row.ResponsibleName) ? "—" : row.ResponsibleName);
                            table.Cell().Text(row.TransmissionStatus == TransmissionStatus.Transmitted ? "Transmitida" : "Não transmitida");
                        }
                    });

                    foreach (var row in data.Rows.Where(r => r.Photos.Count > 0))
                    {
                        col.Item().PaddingTop(12).Text($"{row.ShipName} — fotos ({row.OccurredAt:dd/MM/yyyy})").FontSize(11).SemiBold();
                        col.Item().Row(r =>
                        {
                            foreach (var photo in row.Photos.Take(6))
                            {
                                if (TryReadPhoto(photo, out var bytes))
                                    r.AutoItem().Width(120).Image(bytes);
                                else
                                    r.AutoItem().Width(120).Text("(foto indisponível)").FontColor(Colors.Grey.Medium);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("BKS-Marine · gerado em ");
                    t.Span(DateTime.UtcNow.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                    t.Span(" · página ");
                    t.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(pdf.GeneratePdf());
    }

    private string BuildFilterDescription(OperationReportData data)
    {
        var parts = new List<string>();
        if (data.From is not null) parts.Add($"de {data.From:dd/MM/yyyy}");
        if (data.To is not null) parts.Add($"até {data.To:dd/MM/yyyy}");
        if (data.Type is not null) parts.Add(data.Type == OperationType.Docking ? "atracação" : "desatracação");
        if (data.PortId is not null) parts.Add($"porto {data.PortId}");
        if (data.ResponsibleUserId is not null) parts.Add($"responsável {data.ResponsibleUserId}");
        parts.Add($"{data.Rows.Count} operação(ões)");
        return string.Join(" · ", parts);
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(x => x.SemiBold());

    private bool TryReadPhoto(string photoUrl, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        var path = Path.Combine(_uploadsDirectory, Path.GetFileName(photoUrl));
        if (!File.Exists(path))
            return false;
        bytes = File.ReadAllBytes(path);
        return true;
    }
}
