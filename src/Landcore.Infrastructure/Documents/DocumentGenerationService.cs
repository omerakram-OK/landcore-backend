using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Landcore.Infrastructure.Documents;

public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IGeneratedDocumentRepository _documentRepository;
    private readonly IAuditLogger _auditLogger;

    public DocumentGenerationService(
        IPlotRepository plotRepository,
        IClientRepository clientRepository,
        IBookingRepository bookingRepository,
        IGeneratedDocumentRepository documentRepository,
        IAuditLogger auditLogger)
    {
        _plotRepository = plotRepository;
        _clientRepository = clientRepository;
        _bookingRepository = bookingRepository;
        _documentRepository = documentRepository;
        _auditLogger = auditLogger;
    }

    public async Task<GeneratedDocumentResponseDto> GenerateAsync(string adminId, GenerateDocumentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var documentType = ParseEnum<DocumentType>(request.DocumentType, "DocumentType");
        var plotId = ParseObjectId(request.PlotId, "PlotId");

        var plot = await _plotRepository.GetByIdAsync(adminObjectId, plotId, cancellationToken);
        if (plot is null)
        {
            throw new NotFoundAppException($"Plot '{request.PlotId}' was not found.");
        }

        var booking = await _bookingRepository.GetMostRecentByPlotIdAsync(adminObjectId, plot.Id, cancellationToken);
        var clientId = booking?.ClientId ?? plot.OwnerClientIds.FirstOrDefault();
        if (clientId == ObjectId.Empty)
        {
            throw new ValidationAppException(
                "This Plot has no associated Client (no Booking and no OwnerClientIds) to generate a document for.",
                new Dictionary<string, string[]> { ["PlotId"] = ["This Plot has no associated Client to generate a document for."] });
        }

        var client = await _clientRepository.GetByIdAsync(adminObjectId, clientId, cancellationToken);
        if (client is null)
        {
            throw new NotFoundAppException($"Client '{clientId}' was not found.");
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var pdfBytes = RenderPdf(documentType, plot, client, booking, now);

        var document = new GeneratedDocument
        {
            AdminId = adminObjectId,
            PlotId = plot.Id,
            ClientId = client.Id,
            BookingId = booking?.Id,
            DocumentType = documentType,
            FileContent = pdfBytes,
            GeneratedAt = now,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _documentRepository.CreateAsync(document, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "DocumentGenerated", "GeneratedDocument", document.Id.ToString(), adminId, new
        {
            PlotId = plot.Id.ToString(),
            ClientId = client.Id.ToString(),
            BookingId = booking?.Id.ToString(),
            DocumentType = documentType.ToString(),
        });

        return MapToDto(document);
    }

    public async Task<IReadOnlyList<GeneratedDocumentResponseDto>> GetHistoryByPlotIdAsync(string adminId, string plotId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(plotId, "plotId");
        var documents = await _documentRepository.GetByPlotIdAsync(adminObjectId, id, cancellationToken);
        return documents.Select(MapToDto).ToList();
    }

    public async Task<GeneratedDocumentFileDto> DownloadAsync(string adminId, string documentId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(documentId, "documentId");
        var document = await _documentRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (document is null)
        {
            throw new NotFoundAppException($"GeneratedDocument '{documentId}' was not found.");
        }

        var fileName = $"{document.DocumentType}-{document.PlotId}-{document.Id}.pdf";
        return new GeneratedDocumentFileDto(document.FileContent, fileName, document.DocumentType.ToString());
    }

    private static byte[] RenderPdf(DocumentType documentType, Plot plot, Client client, Booking? booking, DateTime generatedAt)
    {
        var title = documentType switch
        {
            DocumentType.AllotmentLetter => "ALLOTMENT LETTER",
            DocumentType.TransferLetter => "TRANSFER LETTER",
            DocumentType.NOC => "NO OBJECTION CERTIFICATE",
            DocumentType.PossessionLetter => "POSSESSION LETTER",
            _ => documentType.ToString(),
        };

        var body = documentType switch
        {
            DocumentType.AllotmentLetter =>
                $"This is to certify that Plot No. {plot.PlotNumber} ({plot.Category}, {plot.Size} {plot.SizeUnit}) " +
                $"has been allotted to {client.FullName} (CNIC: {client.CNIC}) at a base price of PKR {(decimal)plot.BasePrice:N0}" +
                (booking is not null
                    ? $", against a Booking dated {booking.CreatedAt:dd-MMM-yyyy} with a token amount of PKR {(decimal)booking.TokenAmount:N0}."
                    : "."),
            DocumentType.TransferLetter =>
                $"This is to certify that ownership of Plot No. {plot.PlotNumber} ({plot.Category}, {plot.Size} {plot.SizeUnit}) " +
                $"stands transferred to {client.FullName} (CNIC: {client.CNIC}), effective {generatedAt:dd-MMM-yyyy}.",
            DocumentType.NOC =>
                $"This is to certify that there is no objection from this office regarding Plot No. {plot.PlotNumber} " +
                $"({plot.Category}, {plot.Size} {plot.SizeUnit}), registered in the name of {client.FullName} (CNIC: {client.CNIC}), " +
                "for the purpose of transfer, mortgage, or any other lawful dealing.",
            DocumentType.PossessionLetter =>
                $"This is to certify that possession of Plot No. {plot.PlotNumber} ({plot.Category}, {plot.Size} {plot.SizeUnit}) " +
                $"has been handed over to {client.FullName} (CNIC: {client.CNIC}) on {generatedAt:dd-MMM-yyyy}. " +
                $"Current possession status: {plot.PossessionStatus}.",
            _ => $"Document for Plot No. {plot.PlotNumber}, Client {client.FullName}.",
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(11));

                page.Header().Column(column =>
                {
                    column.Item().AlignCenter().Text("LANDCORE").Bold().FontSize(20);
                    column.Item().AlignCenter().Text("Property Management").FontSize(10);
                    column.Item().PaddingTop(10).LineHorizontal(1);
                });

                page.Content().PaddingVertical(20).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().AlignCenter().Text(title).Bold().FontSize(16);
                    column.Item().Text($"Date: {generatedAt:dd-MMM-yyyy}");
                    column.Item().Text($"Plot No.: {plot.PlotNumber}");
                    column.Item().Text($"Client: {client.FullName}");
                    column.Item().PaddingTop(10).Text(body).FontSize(12).LineHeight(1.4f);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("This is a system-generated document — ");
                    text.Span($"generated {generatedAt:dd-MMM-yyyy HH:mm} UTC.").FontSize(9);
                });
            });
        }).GeneratePdf();
    }

    private static ObjectId ParseObjectId(string value, string fieldName)
    {
        if (!ObjectId.TryParse(value, out var id))
        {
            throw new ValidationAppException(
                $"'{fieldName}' is not a valid identifier.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid identifier."] });
        }

        return id;
    }

    private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw new ValidationAppException(
                $"'{fieldName}' must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid {typeof(TEnum).Name}."] });
        }

        return parsed;
    }

    private static GeneratedDocumentResponseDto MapToDto(GeneratedDocument document) => new(
        document.Id.ToString(),
        document.AdminId.ToString(),
        document.PlotId.ToString(),
        document.ClientId.ToString(),
        document.BookingId?.ToString(),
        document.DocumentType.ToString(),
        document.GeneratedAt,
        document.CreatedBy.ToString(),
        document.CreatedAt);
}
