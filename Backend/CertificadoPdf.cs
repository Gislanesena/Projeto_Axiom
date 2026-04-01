using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AxiomCode;

/// <summary>Gera PDF do certificado com paleta alinhada ao site (fundo escuro, texto claro).</summary>
public static class CertificadoPdf
{
    public static byte[] Gerar(string nomeParticipante, string tituloEvento, DateOnly dataEvento)
    {
        var nome = string.IsNullOrWhiteSpace(nomeParticipante) ? "Participante" : nomeParticipante.Trim();
        var titulo = string.IsNullOrWhiteSpace(tituloEvento) ? "Evento" : tituloEvento.Trim();
        var dataTxt = dataEvento.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(36);
                // Tom próximo ao gradiente da home (#050538 → #000000)
                page.PageColor("#03031a");
                page.DefaultTextStyle(x => x.FontColor("#f2f1f5").FontSize(14));

                page.Content()
                    .AlignMiddle()
                    .Padding(28)
                    .Column(outer =>
                    {
                        outer.Item()
                            .Background("#040227")
                            .Border(1.5f)
                            .BorderColor("#e2e3ed")
                            .CornerRadius(14)
                            .Padding(40)
                            .Column(col =>
                            {
                                col.Spacing(14);
                                col.Item().AlignCenter()
                                    .Text("Certificado de participação")
                                    .FontSize(30)
                                    .Bold()
                                    .FontColor("#FFFFFF");
                                col.Item().AlignCenter()
                                    .Text("AxiomCode")
                                    .FontSize(13)
                                    .Italic()
                                    .FontColor("#c8c8e8");
                                col.Item().Height(8);
                                col.Item().AlignCenter().Text(t =>
                                {
                                    t.DefaultTextStyle(x => x.FontSize(16).FontColor("#f2f1f5"));
                                    t.Span("Certificamos que ");
                                    t.Span(nome).Bold().FontSize(18).FontColor("#FFFFFF");
                                    t.Span(" concluiu a participação no evento");
                                });
                                col.Item().AlignCenter()
                                    .Text(titulo)
                                    .FontSize(22)
                                    .Bold()
                                    .FontColor("#FFFFFF");
                                col.Item().AlignCenter()
                                    .Text($"Data do evento: {dataTxt}")
                                    .FontSize(15)
                                    .FontColor("#e2e3ed");
                                col.Item().Height(24);
                                col.Item().AlignCenter()
                                    .Text("— Comunidade AxiomCode —")
                                    .FontSize(11)
                                    .FontColor("#9393c4");
                            });
                    });
            });
        });

        return document.GeneratePdf();
    }
}
