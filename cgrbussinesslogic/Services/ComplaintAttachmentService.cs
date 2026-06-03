using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Attachment;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace cgrbussinesslogic.Services;

public class ComplaintAttachmentService : IComplaintAttachmentService
{
    private const int MaxFilesPerComplaint = 3;
    private const long MaxImageSizeBytes = 3 * 1024 * 1024;   // 3 MB
    private const long MaxPdfSizeBytes   = 5 * 1024 * 1024;   // 5 MB
    private const long MaxTotalSizeBytes = 15 * 1024 * 1024;  // 15 MB

    private static readonly string[] AllowedMimeTypes =
        { "image/png", "image/jpg", "image/jpeg", "application/pdf" };

    private readonly IComplaintAttachmentRepository _attachmentRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly string _uploadBasePath;

    public ComplaintAttachmentService(
        IComplaintAttachmentRepository attachmentRepository,
        IComplaintRepository complaintRepository,
        IWebHostEnvironment env)
    {
        _attachmentRepository = attachmentRepository;
        _complaintRepository = complaintRepository;
        _uploadBasePath = Path.Combine(env.ContentRootPath, "Uploads", "Complaints");
    }

    public async Task<IEnumerable<ComplaintAttachmentDto>> GetByComplaintIdAsync(int complaintId)
    {
        var attachments = await _attachmentRepository.GetByComplaintIdAsync(complaintId);
        return attachments.Select(MapToDto);
    }

    public async Task<ComplaintAttachmentDto> UploadAsync(int complaintId, IFormFile file, int employeeId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        // Validate mime type
        if (!AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            throw new ValidationException("Only PNG, JPG, JPEG, and PDF files are allowed.");

        // Validate individual file size
        bool isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
        long maxSize = isPdf ? MaxPdfSizeBytes : MaxImageSizeBytes;
        if (file.Length > maxSize)
            throw new ValidationException($"File size exceeds the {(isPdf ? "5 MB (PDF)" : "3 MB (image)")} limit.");

        // Check existing attachments
        var existing = (await _attachmentRepository.GetByComplaintIdAsync(complaintId)).ToList();

        if (existing.Count >= MaxFilesPerComplaint)
            throw new BusinessRuleException($"Maximum {MaxFilesPerComplaint} files allowed per complaint.");

        long currentTotal = existing.Sum(a => a.FileSizeBytes);
        if (currentTotal + file.Length > MaxTotalSizeBytes)
            throw new BusinessRuleException("Total attachment size for this complaint would exceed 15 MB.");

        // Save file to disk
        var directory = Path.Combine(_uploadBasePath, complaintId.ToString());
        Directory.CreateDirectory(directory);

        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(directory, uniqueFileName);

        await using (var stream = File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new ComplaintAttachment
        {
            ComplaintId = complaintId,
            OriginalFileName = file.FileName,
            FilePath = Path.Combine("Uploads", "Complaints", complaintId.ToString(), uniqueFileName),
            MimeType = file.ContentType,
            FileSizeBytes = file.Length,
            UploadedBy = employeeId,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _attachmentRepository.Create(attachment);
        return MapToDto(created);
    }

    public async Task DeleteAsync(int attachmentId, int currentEmployeeId, string role)
    {
        var attachment = await _attachmentRepository.Get(attachmentId);
        var complaint = await _complaintRepository.GetDetailByIdAsync(attachment.ComplaintId)
            ?? throw new NotFoundException($"Complaint {attachment.ComplaintId}");

        bool isOwner = attachment.UploadedBy == currentEmployeeId;
        bool isComplainant = complaint.RaisedByEmployeeId == currentEmployeeId;
        bool isAdmin = role == "ADMIN";

        if (!isOwner && !isComplainant && !isAdmin)
            throw new ForbiddenException("You are not authorized to delete this attachment.");

        // Delete physical file
        if (File.Exists(attachment.FilePath))
            File.Delete(attachment.FilePath);

        await _attachmentRepository.Delete(attachmentId);
    }

    private static ComplaintAttachmentDto MapToDto(ComplaintAttachment a) => new()
    {
        AttachmentId = a.AttachmentId,
        ComplaintId = a.ComplaintId,
        OriginalFileName = a.OriginalFileName,
        MimeType = a.MimeType,
        FileSizeBytes = a.FileSizeBytes,
        FilePath = a.FilePath,
        UploadedBy = a.UploadedBy,
        UploadedByName = a.UploadedByNavigation?.EmployeeName ?? string.Empty,
        CreatedAt = a.CreatedAt
    };
}
