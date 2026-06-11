using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Attachment;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace cgrbussinesslogic.Services;

public class ComplaintAttachmentService : IComplaintAttachmentService
{
    private const int MaxFilesPerComplaint = 3;
    private const long MaxImageSizeBytes = 3 * 1024 * 1024;   // 3 MB
    private const long MaxPdfSizeBytes = 5 * 1024 * 1024;   // 5 MB
    private const long MaxTotalSizeBytes = 15 * 1024 * 1024;  // 15 MB

    private static readonly string[] AllowedMimeTypes =
        { "image/png", "image/jpg", "image/jpeg", "application/pdf" };

    private readonly IComplaintAttachmentRepository _attachmentRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly string _uploadBasePath;

    public ComplaintAttachmentService(
        IComplaintAttachmentRepository attachmentRepository,
        IComplaintRepository complaintRepository,
        IConfiguration configuration,
        ICurrentUserService currentUserService)
    {
        _attachmentRepository = attachmentRepository;
        _complaintRepository = complaintRepository;
        _currentUserService = currentUserService;
        _uploadBasePath = configuration["FileStorage:ComplaintAttachmentsPath"] ?? throw new InvalidOperationException(
                "Complaint attachment path not configured.");
    }

    public async Task<IEnumerable<ComplaintAttachmentDto>> GetByComplaintIdAsync(int complaintId)
    {
        var attachments = await _attachmentRepository.GetByComplaintIdAsync(complaintId);
        return attachments.Select(MapToDto);
    }
    public async Task<(List<ComplaintAttachment> Attachments, List<string> CreatedFiles)> SaveAttachmentsAsync(
        int complaintId, List<IFormFile> files)
    {
        var attachments = new List<ComplaintAttachment>();
        var createdFiles = new List<string>();

        if (files == null || files.Count == 0)
        {
            return (attachments, createdFiles);
        }

        if (files.Count > MaxFilesPerComplaint)
        {
            throw new BusinessRuleException($"Maximum {MaxFilesPerComplaint} files allowed.");
        }

        long totalSize = files.Sum(f => f.Length);

        if (totalSize > MaxTotalSizeBytes)
        {
            throw new BusinessRuleException(
                "Total attachment size exceeds 15 MB.");
        }

        var complaintDirectory =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                _uploadBasePath,
                complaintId.ToString());

        // Directory.CreateDirectory(complaintDirectory);
        bool folderCreated = false;

        foreach (var file in files)
        {
            if (!AllowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                throw new ValidationException(
                    "Only PNG, JPG, JPEG and PDF files are allowed.");
            }

            bool isPdf =
                file.ContentType.Equals(
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase);

            long maxSize =
                isPdf
                    ? MaxPdfSizeBytes
                    : MaxImageSizeBytes;

            if (file.Length > maxSize)
            {
                throw new ValidationException(
                    "Attachment exceeds allowed size.");
            }
            if (!folderCreated)
            {
                Directory.CreateDirectory(complaintDirectory);
                folderCreated = true;
            }
            string uniqueFileName =
                $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            string fullPath =
                Path.Combine(
                    complaintDirectory,
                    uniqueFileName);

            await using var stream = File.Create(fullPath);

            await file.CopyToAsync(stream);

            createdFiles.Add(fullPath);

            var attachment =
                new ComplaintAttachment
                {
                    ComplaintId = complaintId,
                    OriginalFileName = file.FileName,
                    FilePath = Path.Combine(
                        _uploadBasePath,
                        complaintId.ToString(),
                        uniqueFileName),
                    MimeType = file.ContentType,
                    FileSizeBytes = file.Length,
                    CreatedAt = DateTime.UtcNow,
                    UploadedBy = _currentUserService.EmployeeId
                };

            var created =
                await _attachmentRepository.Create(attachment);

            attachments.Add(created);
        }

        return (attachments, createdFiles);
    }

    public async Task DeleteFilesAsync(IEnumerable<string> filePaths)
    {
        var complaintFolder = string.Empty;

        await Task.Run(() =>
        {
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (string.IsNullOrEmpty(complaintFolder))
                {
                    complaintFolder = Path.GetDirectoryName(filePath) ?? string.Empty;
                }
            }

            if (!string.IsNullOrEmpty(complaintFolder) &&
                Directory.Exists(complaintFolder) &&
                !Directory.EnumerateFileSystemEntries(complaintFolder).Any())
            {
                Directory.Delete(complaintFolder);
            }
        });
    }

    public async Task DeleteComplaintFolderAsync(int complaintId)
    {
        var complaintDirectory =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                _uploadBasePath,
                complaintId.ToString());

        await Task.Run(() =>
        {
            if (Directory.Exists(complaintDirectory))
            {
                Directory.Delete(complaintDirectory, true);
            }
        });
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