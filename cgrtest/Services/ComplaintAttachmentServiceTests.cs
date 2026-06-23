using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cgrbussinesslogic.Interfaces;
using cgrbussinesslogic.Services;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace cgrtest.Services;

[TestFixture]
public class ComplaintAttachmentServiceTests
{
    private Mock<IComplaintAttachmentRepository> _mockAttachmentRepo = null!;
    private Mock<IComplaintRepository> _mockComplaintRepo = null!;
    private Mock<ICurrentUserService> _mockUserService = null!;
    private Mock<IConfiguration> _mockConfig = null!;
    private ComplaintAttachmentService _serviceSut = null!;

    private const string TestUploadDir = "TestUploads_ComplaintAttachmentService";

    [SetUp]
    public void SetUp()
    {
        _mockAttachmentRepo = new Mock<IComplaintAttachmentRepository>();
        _mockComplaintRepo = new Mock<IComplaintRepository>();
        _mockUserService = new Mock<ICurrentUserService>();
        _mockConfig = new Mock<IConfiguration>();

        // Set up the default path configuration
        _mockConfig.Setup(c => c["FileStorage:ComplaintAttachmentsPath"]).Returns(TestUploadDir);

        _serviceSut = new ComplaintAttachmentService(
            _mockAttachmentRepo.Object,
            _mockComplaintRepo.Object,
            _mockConfig.Object,
            _mockUserService.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        var testUploadPath = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir);
        if (Directory.Exists(testUploadPath))
        {
            try
            {
                Directory.Delete(testUploadPath, true);
            }
            catch
            {
                // Ignore errors during clean up to prevent test failure reporting issues
            }
        }
    }

    #region Constructor Tests

    [Test]
    public void Constructor_ConfigMissingPath_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockConfigMissing = new Mock<IConfiguration>();
        mockConfigMissing.Setup(c => c["FileStorage:ComplaintAttachmentsPath"]).Returns((string?)null);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new ComplaintAttachmentService(
                _mockAttachmentRepo.Object,
                _mockComplaintRepo.Object,
                mockConfigMissing.Object,
                _mockUserService.Object
            ));

        Assert.That(ex!.Message, Is.EqualTo("Complaint attachment path not configured."));
    }

    [Test]
    public void Constructor_ValidConfig_Succeeds()
    {
        // Act & Assert
        Assert.That(_serviceSut, Is.Not.Null);
    }

    #endregion

    #region GetByComplaintIdAsync Tests

    [Test]
    public async Task GetByComplaintIdAsync_WhenUploadedByNavigationIsNull_ReturnsMappedDtoWithEmptyName()
    {
        // Arrange
        var complaintId = 1;
        var attachments = new List<ComplaintAttachment>
        {
            new()
            {
                AttachmentId = 10,
                ComplaintId = complaintId,
                OriginalFileName = "file1.png",
                MimeType = "image/png",
                FileSizeBytes = 1024,
                FilePath = "uploads/1/file1.png",
                UploadedBy = 5,
                CreatedAt = DateTime.UtcNow,
                UploadedByNavigation = null!
            }
        };

        _mockAttachmentRepo
            .Setup(r => r.GetByComplaintIdAsync(complaintId))
            .ReturnsAsync(attachments);

        // Act
        var result = (await _serviceSut.GetByComplaintIdAsync(complaintId)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var dto = result[0];
        Assert.That(dto.AttachmentId, Is.EqualTo(10));
        Assert.That(dto.ComplaintId, Is.EqualTo(complaintId));
        Assert.That(dto.OriginalFileName, Is.EqualTo("file1.png"));
        Assert.That(dto.MimeType, Is.EqualTo("image/png"));
        Assert.That(dto.FileSizeBytes, Is.EqualTo(1024));
        Assert.That(dto.FilePath, Is.EqualTo("uploads/1/file1.png"));
        Assert.That(dto.UploadedBy, Is.EqualTo(5));
        Assert.That(dto.UploadedByName, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task GetByComplaintIdAsync_WhenUploadedByNavigationIsNotNull_ReturnsMappedDtoWithName()
    {
        // Arrange
        var complaintId = 1;
        var attachments = new List<ComplaintAttachment>
        {
            new()
            {
                AttachmentId = 10,
                ComplaintId = complaintId,
                OriginalFileName = "file1.png",
                MimeType = "image/png",
                FileSizeBytes = 1024,
                FilePath = "uploads/1/file1.png",
                UploadedBy = 5,
                CreatedAt = DateTime.UtcNow,
                UploadedByNavigation = new Employee
                {
                    EmployeeId = 5,
                    EmployeeName = "Alice Smith",
                    Email = "alice@company.com",
                    MobileNumber = "123",
                    PasswordHash = "hash"
                }
            }
        };

        _mockAttachmentRepo
            .Setup(r => r.GetByComplaintIdAsync(complaintId))
            .ReturnsAsync(attachments);

        // Act
        var result = (await _serviceSut.GetByComplaintIdAsync(complaintId)).ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var dto = result[0];
        Assert.That(dto.UploadedByName, Is.EqualTo("Alice Smith"));
    }

    #endregion

    #region SaveAttachmentsAsync Tests

    [Test]
    public async Task SaveAttachmentsAsync_FilesListNull_ReturnsEmptyResults()
    {
        // Act
#pragma warning disable CS8625
        var (attachments, createdFiles) = await _serviceSut.SaveAttachmentsAsync(1, null);
#pragma warning restore CS8625

        // Assert
        Assert.That(attachments, Is.Empty);
        Assert.That(createdFiles, Is.Empty);
    }

    [Test]
    public async Task SaveAttachmentsAsync_FilesListEmpty_ReturnsEmptyResults()
    {
        // Act
        var (attachments, createdFiles) = await _serviceSut.SaveAttachmentsAsync(1, new List<IFormFile>());

        // Assert
        Assert.That(attachments, Is.Empty);
        Assert.That(createdFiles, Is.Empty);
    }

    [Test]
    public void SaveAttachmentsAsync_ExceedsMaxFilesLimit_ThrowsBusinessRuleException()
    {
        // Arrange
        var files = new List<IFormFile>();
        for (int i = 0; i < 4; i++)
        {
            var mockFile = new Mock<IFormFile>();
            files.Add(mockFile.Object);
        }

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.SaveAttachmentsAsync(1, files));

        Assert.That(ex!.Message, Is.EqualTo("Maximum 3 files allowed."));
    }

    [Test]
    public void SaveAttachmentsAsync_ExceedsMaxTotalSizeLimit_ThrowsBusinessRuleException()
    {
        // Arrange
        var files = new List<IFormFile>();

        // 3 files, total size = 15 MB + 1 byte
        var size1 = 5 * 1024 * 1024;
        var size2 = 5 * 1024 * 1024;
        var size3 = 5 * 1024 * 1024 + 1;

        var mockFile1 = new Mock<IFormFile>();
        mockFile1.Setup(f => f.Length).Returns(size1);
        files.Add(mockFile1.Object);

        var mockFile2 = new Mock<IFormFile>();
        mockFile2.Setup(f => f.Length).Returns(size2);
        files.Add(mockFile2.Object);

        var mockFile3 = new Mock<IFormFile>();
        mockFile3.Setup(f => f.Length).Returns(size3);
        files.Add(mockFile3.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<BusinessRuleException>(() =>
            _serviceSut.SaveAttachmentsAsync(1, files));

        Assert.That(ex!.Message, Is.EqualTo("Total attachment size exceeds 15 MB."));
    }

    [Test]
    public void SaveAttachmentsAsync_InvalidMimeType_ThrowsValidationException()
    {
        // Arrange
        var files = new List<IFormFile>();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("text/plain");
        files.Add(mockFile.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.SaveAttachmentsAsync(1, files));

        Assert.That(ex!.Message, Is.EqualTo("Only PNG, JPG, JPEG and PDF files are allowed."));
    }

    [Test]
    public void SaveAttachmentsAsync_PdfExceedsMaxSize_ThrowsValidationException()
    {
        // Arrange
        var files = new List<IFormFile>();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(5 * 1024 * 1024 + 1); // 5MB + 1 byte
        mockFile.Setup(f => f.ContentType).Returns("application/pdf");
        files.Add(mockFile.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.SaveAttachmentsAsync(1, files));

        Assert.That(ex!.Message, Is.EqualTo("Attachment exceeds allowed size of 5 MB."));
    }

    [Test]
    public void SaveAttachmentsAsync_ImageExceedsMaxSize_ThrowsValidationException()
    {
        // Arrange
        var files = new List<IFormFile>();
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024 + 1); // 3MB + 1 byte
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        files.Add(mockFile.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<ValidationException>(() =>
            _serviceSut.SaveAttachmentsAsync(1, files));

        Assert.That(ex!.Message, Is.EqualTo("Attachment exceeds allowed size of 3 MB."));
    }

    [Test]
    public async Task SaveAttachmentsAsync_ValidFiles_SuccessfullyCreatesDirectoriesAndFiles()
    {
        // Arrange
        var complaintId = 42;
        var employeeId = 9;
        _mockUserService.Setup(u => u.EmployeeId).Returns(employeeId);

        var mockFile1 = new Mock<IFormFile>();
        mockFile1.Setup(f => f.Length).Returns(2 * 1024 * 1024); // 2 MB PDF
        mockFile1.Setup(f => f.ContentType).Returns("application/pdf");
        mockFile1.Setup(f => f.FileName).Returns("my_document.pdf");
        mockFile1.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var mockFile2 = new Mock<IFormFile>();
        mockFile2.Setup(f => f.Length).Returns(1 * 1024 * 1024); // 1 MB PNG (uppercase mime check too)
        mockFile2.Setup(f => f.ContentType).Returns("IMAGE/PNG");
        mockFile2.Setup(f => f.FileName).Returns("PHOTO.PNG");
        mockFile2.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var files = new List<IFormFile> { mockFile1.Object, mockFile2.Object };

        var savedAttachments = new List<ComplaintAttachment>();
        _mockAttachmentRepo
            .Setup(r => r.Create(It.IsAny<ComplaintAttachment>()))
            .ReturnsAsync((ComplaintAttachment a) =>
            {
                a.AttachmentId = savedAttachments.Count + 100;
                savedAttachments.Add(a);
                return a;
            });

        // Act
        var (attachments, createdFiles) = await _serviceSut.SaveAttachmentsAsync(complaintId, files);

        // Assert
        Assert.That(attachments, Has.Count.EqualTo(2));
        Assert.That(createdFiles, Has.Count.EqualTo(2));

        // Verify folder creation
        var expectedFolder = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir, complaintId.ToString());
        Assert.That(Directory.Exists(expectedFolder), Is.True);

        // Verify repository creates called
        _mockAttachmentRepo.Verify(r => r.Create(It.IsAny<ComplaintAttachment>()), Times.Exactly(2));

        // Verify parameters of first created attachment
        var att1 = attachments[0];
        Assert.That(att1.ComplaintId, Is.EqualTo(complaintId));
        Assert.That(att1.OriginalFileName, Is.EqualTo("my_document.pdf"));
        Assert.That(att1.MimeType, Is.EqualTo("application/pdf"));
        Assert.That(att1.FileSizeBytes, Is.EqualTo(2 * 1024 * 1024));
        Assert.That(att1.UploadedBy, Is.EqualTo(employeeId));
        Assert.That(att1.FilePath, Does.StartWith(Path.Combine(TestUploadDir, complaintId.ToString())));

        // Verify physical files created
        foreach (var createdFile in createdFiles)
        {
            Assert.That(File.Exists(createdFile), Is.True);
        }
    }

    #endregion

    #region DeleteFilesAsync Tests

    [Test]
    public async Task DeleteFilesAsync_FilesExist_DeletesFilesAndRemovesDirectoryIfEmpty()
    {
        // Arrange
        var complaintId = 99;
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir, complaintId.ToString());
        Directory.CreateDirectory(folderPath);

        var filePath1 = Path.Combine(folderPath, "temp1.txt");
        var filePath2 = Path.Combine(folderPath, "temp2.txt");

        await File.WriteAllTextAsync(filePath1, "content1");
        await File.WriteAllTextAsync(filePath2, "content2");

        Assert.That(File.Exists(filePath1), Is.True);
        Assert.That(File.Exists(filePath2), Is.True);

        // Act
        await _serviceSut.DeleteFilesAsync(new[] { filePath1, filePath2 });

        // Assert
        Assert.That(File.Exists(filePath1), Is.False);
        Assert.That(File.Exists(filePath2), Is.False);
        // Folder should be deleted since it's empty now
        Assert.That(Directory.Exists(folderPath), Is.False);
    }

    [Test]
    public async Task DeleteFilesAsync_FileDoesNotExist_SkipDeletionWithoutError()
    {
        // Arrange
        var nonExistentFile = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir, "999", "ghost.txt");

        // Act & Assert
        Assert.DoesNotThrowAsync(() => _serviceSut.DeleteFilesAsync(new[] { nonExistentFile }));
    }

    [Test]
    public async Task DeleteFilesAsync_DirectoryNotEmpty_DeletesOnlySpecifiedFilesAndKeepsDirectory()
    {
        // Arrange
        var complaintId = 123;
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir, complaintId.ToString());
        Directory.CreateDirectory(folderPath);

        var filePathToDelete = Path.Combine(folderPath, "delete_me.txt");
        var filePathToKeep = Path.Combine(folderPath, "keep_me.txt");

        await File.WriteAllTextAsync(filePathToDelete, "delete");
        await File.WriteAllTextAsync(filePathToKeep, "keep");

        // Act
        await _serviceSut.DeleteFilesAsync(new[] { filePathToDelete });

        // Assert
        Assert.That(File.Exists(filePathToDelete), Is.False);
        Assert.That(File.Exists(filePathToKeep), Is.True);
        // Folder should still exist
        Assert.That(Directory.Exists(folderPath), Is.True);
    }

    [Test]
    public async Task DeleteFilesAsync_EmptyPathList_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(() => _serviceSut.DeleteFilesAsync(new List<string>()));
    }

    [Test]
    public async Task DeleteFilesAsync_PathWithoutDirectoryName_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(() => _serviceSut.DeleteFilesAsync(new[] { "just_filename.txt" }));
    }

    #endregion

    #region DeleteComplaintFolderAsync Tests

    [Test]
    public async Task DeleteComplaintFolderAsync_FolderExists_DeletesFolderRecursively()
    {
        // Arrange
        var complaintId = 55;
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir, complaintId.ToString());
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, "document.pdf");
        await File.WriteAllTextAsync(filePath, "some data");

        Assert.That(Directory.Exists(folderPath), Is.True);

        // Act
        await _serviceSut.DeleteComplaintFolderAsync(complaintId);

        // Assert
        Assert.That(Directory.Exists(folderPath), Is.False);
    }

    [Test]
    public async Task DeleteComplaintFolderAsync_FolderDoesNotExist_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrowAsync(() => _serviceSut.DeleteComplaintFolderAsync(8888));
    }

    #endregion

    #region GetAttachmentFileByPathAsync Tests

    [Test]
    public void GetAttachmentFileByPathAsync_AttachmentNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var filePath = "somepath.pdf";
        _mockAttachmentRepo.Setup(r => r.GetByFilePathAsync(filePath))
            .ReturnsAsync((ComplaintAttachment?)null);

        // Act & Assert
        Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.GetAttachmentFileByPathAsync(filePath));
    }

    [Test]
    public async Task GetAttachmentFileByPathAsync_ComplaintNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var filePath = "somepath.pdf";
        var attachment = new ComplaintAttachment
        {
            AttachmentId = 123,
            ComplaintId = 10,
            FilePath = filePath
        };
        _mockAttachmentRepo.Setup(r => r.GetByFilePathAsync(filePath)).ReturnsAsync(attachment);
        _mockComplaintRepo.Setup(r => r.GetDetailByIdAsync(10)).ReturnsAsync((Complaint?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.GetAttachmentFileByPathAsync(filePath));
        Assert.That(ex!.Message, Is.EqualTo("Complaint with key 10 not found"));
    }

    [Test]
    public async Task GetAttachmentFileByPathAsync_UserNotAuthorized_ThrowsForbiddenException()
    {
        // Arrange
        var filePath = "somepath.pdf";
        var attachment = new ComplaintAttachment
        {
            AttachmentId = 123,
            ComplaintId = 10,
            FilePath = filePath
        };
        var complaint = new Complaint
        {
            ComplaintId = 10,
            RaisedByEmployeeId = 99, // Raised by another employee
            CurrentHandlerEmployeeId = 88, // Handled by someone else
            Category = new Category { DepartmentId = 2 } // Department 2
        };

        _mockAttachmentRepo.Setup(r => r.GetByFilePathAsync(filePath)).ReturnsAsync(attachment);
        _mockComplaintRepo.Setup(r => r.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        // Current user is employee 5 with role employee
        _mockUserService.Setup(u => u.EmployeeId).Returns(5);
        _mockUserService.Setup(u => u.RoleId).Returns(1); // Employee
        _mockUserService.Setup(u => u.DepartmentId).Returns(1);

        // Act & Assert
        Assert.ThrowsAsync<ForbiddenException>(() =>
            _serviceSut.GetAttachmentFileByPathAsync(filePath));
    }

    [Test]
    public async Task GetAttachmentFileByPathAsync_FileNotFoundOnDisk_ThrowsNotFoundException()
    {
        // Arrange
        var filePath = "nonexistent_file.pdf";
        var attachment = new ComplaintAttachment
        {
            AttachmentId = 123,
            ComplaintId = 10,
            FilePath = filePath
        };
        var complaint = new Complaint
        {
            ComplaintId = 10,
            RaisedByEmployeeId = 5,
            Category = new Category { DepartmentId = 1 }
        };

        _mockAttachmentRepo.Setup(r => r.GetByFilePathAsync(filePath)).ReturnsAsync(attachment);
        _mockComplaintRepo.Setup(r => r.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        _mockUserService.Setup(u => u.EmployeeId).Returns(5);
        _mockUserService.Setup(u => u.RoleId).Returns(1);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _serviceSut.GetAttachmentFileByPathAsync(filePath));
        Assert.That(ex!.Message, Is.EqualTo("Attachment file not found on disk."));
    }

    [Test]
    public async Task GetAttachmentFileByPathAsync_AuthorizedAndFileExists_ReturnsFileDetails()
    {
        // Arrange
        var relativePath = Path.Combine(TestUploadDir, "temp_doc.pdf");
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);

        // Create the file
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), TestUploadDir);
        Directory.CreateDirectory(uploadDir);
        await File.WriteAllTextAsync(fullPath, "pdf content");

        var attachment = new ComplaintAttachment
        {
            AttachmentId = 123,
            ComplaintId = 10,
            FilePath = relativePath,
            MimeType = "application/pdf",
            OriginalFileName = "test.pdf"
        };
        var complaint = new Complaint
        {
            ComplaintId = 10,
            RaisedByEmployeeId = 5,
            Category = new Category { DepartmentId = 1 }
        };

        _mockAttachmentRepo.Setup(r => r.GetByFilePathAsync(relativePath)).ReturnsAsync(attachment);
        _mockComplaintRepo.Setup(r => r.GetDetailByIdAsync(10)).ReturnsAsync(complaint);

        _mockUserService.Setup(u => u.EmployeeId).Returns(5);
        _mockUserService.Setup(u => u.RoleId).Returns(1);

        try
        {
            // Act
            var result = await _serviceSut.GetAttachmentFileByPathAsync(relativePath);

            // Assert
            Assert.That(result.FilePath, Is.EqualTo(fullPath));
            Assert.That(result.MimeType, Is.EqualTo("application/pdf"));
            Assert.That(result.OriginalFileName, Is.EqualTo("test.pdf"));
        }
        finally
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }

    #endregion
}
