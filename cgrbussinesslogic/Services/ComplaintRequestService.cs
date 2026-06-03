using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.ComplaintRequest;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

/// <summary>
/// Request type IDs: REJECTION_REQUEST=1, REOPEN_REQUEST=2, ROLE_UPGRADE_REQUEST=3
/// Request status IDs: PENDING=1, APPROVED=2, REJECTED=3
/// </summary>
public class ComplaintRequestService : IComplaintRequestService
{
    private const short REQUEST_TYPE_REJECTION = 1;
    private const short REQUEST_TYPE_REOPEN = 2;
    private const short REQUEST_STATUS_PENDING = 1;
    private const short REQUEST_STATUS_APPROVED = 2;
    private const short REQUEST_STATUS_REJECTED = 3;

    private const short STATUS_REJECTED = 7;
    private const short NOTIF_COMPLAINT_REJECTED = 6;

    private readonly IComplaintRequestRepository _requestRepository;
    private readonly IComplaintRepository _complaintRepository;
    private readonly INotificationService _notificationService;

    public ComplaintRequestService(
        IComplaintRequestRepository requestRepository,
        IComplaintRepository complaintRepository,
        INotificationService notificationService)
    {
        _requestRepository = requestRepository;
        _complaintRepository = complaintRepository;
        _notificationService = notificationService;
    }

    public async Task<ComplaintRequestDto> CreateAsync(int complaintId, CreateComplaintRequestDto dto, int employeeId)
    {
        var complaint = await _complaintRepository.GetDetailByIdAsync(complaintId)
            ?? throw new NotFoundException($"Complaint {complaintId}");

        // Check for duplicate pending request of same type
        var existing = await _requestRepository.GetPendingByComplaintAndTypeAsync(complaintId, dto.RequestTypeId);
        if (existing != null)
            throw new ConflictException("A pending request of this type already exists for this complaint.");

        var request = new ComplaintRequest
        {
            ComplaintId = complaintId,
            RequestTypeId = dto.RequestTypeId,
            RequestedBy = employeeId,
            RequestStatusId = REQUEST_STATUS_PENDING,
            Remarks = dto.Remarks,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _requestRepository.Create(request);
        return await ReloadDto(created.RequestId);
    }

    public async Task<PagedResultDto<ComplaintRequestDto>> GetPagedAsync(
        int page, int pageSize, short? requestTypeId, short? statusId)
    {
        var (items, total) = await _requestRepository.GetPagedAsync(page, pageSize, requestTypeId, statusId);
        return new PagedResultDto<ComplaintRequestDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ComplaintRequestDto> ReviewAsync(int requestId, ReviewComplaintRequestDto dto, int reviewerId, string reviewerRole)
    {
        var request = await _requestRepository.Get(requestId);
        if (request.RequestStatusId != REQUEST_STATUS_PENDING)
            throw new BusinessRuleException("This request has already been reviewed.");

        request.ReviewedBy = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RequestStatusId = dto.Approve ? REQUEST_STATUS_APPROVED : REQUEST_STATUS_REJECTED;
        request.Remarks = dto.Remarks ?? request.Remarks;

        await _requestRepository.Update(request, requestId);

        // If rejection request approved → mark complaint as REJECTED
        if (dto.Approve && request.RequestTypeId == REQUEST_TYPE_REJECTION)
        {
            var complaint = await _complaintRepository.GetDetailByIdAsync(request.ComplaintId)!;
            if (complaint != null)
            {
                complaint.StatusId = STATUS_REJECTED;
                complaint.UpdatedAt = DateTime.UtcNow;
                await _complaintRepository.Update(complaint, complaint.ComplaintId);

                await _notificationService.SendAsync(
                    complaint.RaisedByEmployeeId, NOTIF_COMPLAINT_REJECTED,
                    "Complaint Rejected",
                    $"Your complaint '{complaint.ComplaintTitle}' has been rejected. Remarks: {dto.Remarks}",
                    complaint.ComplaintId);
            }
        }

        return await ReloadDto(requestId);
    }

    private async Task<ComplaintRequestDto> ReloadDto(int requestId)
    {
        var (items, _) = await _requestRepository.GetPagedAsync(1, 1, null, null);
        var reloaded = items.FirstOrDefault(r => r.RequestId == requestId);
        if (reloaded == null)
        {
            var raw = await _requestRepository.Get(requestId);
            return MapToDto(raw);
        }
        return MapToDto(reloaded);
    }

    private static ComplaintRequestDto MapToDto(ComplaintRequest r) => new()
    {
        RequestId = r.RequestId,
        ComplaintId = r.ComplaintId,
        ComplaintTitle = r.Complaint?.ComplaintTitle,
        RequestTypeId = r.RequestTypeId,
        RequestTypeName = r.RequestType?.RequestTypeName ?? string.Empty,
        RequestedBy = r.RequestedBy,
        RequestedByName = r.RequestedByNavigation?.EmployeeName ?? string.Empty,
        ReviewedBy = r.ReviewedBy,
        ReviewedByName = r.ReviewedByNavigation?.EmployeeName,
        RequestStatusId = r.RequestStatusId,
        RequestStatusName = r.RequestStatus?.StatusName ?? string.Empty,
        Remarks = r.Remarks,
        CreatedAt = r.CreatedAt,
        ReviewedAt = r.ReviewedAt
    };
}
