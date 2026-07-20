namespace Landcore.Application.DTOs;

public sealed record GeneratedDocumentFileDto(
    byte[] FileContent,
    string FileName,
    string DocumentType);
