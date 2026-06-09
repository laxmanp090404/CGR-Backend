using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class SlaEscalationJob : ISlaEscalationJob
{
    #region Status Ids

    private const short STATUS_ESCALATED = 4;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;

    #endregion

    #region Notification Types

    private const short NOTIF_COMPLAINT_ESCALATED = 4;
    private const short NOTIF_EXTERNALLY_ESCALATED = 9;

    #endregion

    private readonly IComplaintRepository _complaintRepository;
    private readonly IComplaintAssignmentEngine _assignmentEngine;
    private readonly IComplaintAssignmentRepository _assignmentRepository;
    private readonly IComplaintHistoryRepository _historyRepository;
    private readonly IRepository<int, ComplaintEscalation> _escalationRepository;
    private readonly IEscalationRuleRepository _escalationRuleRepository;
    private readonly INotificationService _notificationService;

    public SlaEscalationJob(
        IComplaintRepository complaintRepository,
        IComplaintAssignmentEngine assignmentEngine,
        IComplaintAssignmentRepository assignmentRepository,
        IComplaintHistoryRepository historyRepository,
        IRepository<int, ComplaintEscalation> escalationRepository,
        IEscalationRuleRepository escalationRuleRepository,
        INotificationService notificationService)
    {
        _complaintRepository = complaintRepository;
        _assignmentEngine = assignmentEngine;
        _assignmentRepository = assignmentRepository;
        _historyRepository = historyRepository;
        _escalationRepository = escalationRepository;
        _notificationService = notificationService;
        _escalationRuleRepository = escalationRuleRepository;
    }

    public async Task ProcessEscalationsAsync()
    {
        var escalationDueComplaints = await _complaintRepository.GetEscalationDueComplaintsAsync();

        foreach (var complaint in escalationDueComplaints)
        {
            if (complaint.EscalationDueAt == null || complaint.EscalationDueAt > DateTime.UtcNow)
            {
                continue;
            }
            await ProcessComplaintAsync(complaint);
        }
    }

    private async Task ProcessComplaintAsync(Complaint complaint)
    {
        var oldHandler = complaint.CurrentHandlerEmployeeId;

        var oldLevel = complaint.EscalationLevel;
        var oldStatus = complaint.StatusId;

        var assignment = await _assignmentEngine.DetermineEscalationAssignmentAsync(complaint);

        // L2 breached -> external escalation
        if (assignment.EscalationLevel == 3)
        {
            await HandleExternalEscalationAsync(complaint);

            return;
        }

        complaint.CurrentHandlerEmployeeId = assignment.HandlerId;

        complaint.EscalationLevel = assignment.EscalationLevel;

        complaint.StatusId = STATUS_ESCALATED;

        complaint.EscalationDueAt = await _assignmentEngine.CalculateEscalationDueAtAsync(complaint);

        complaint.UpdatedAt = DateTime.UtcNow;

        await _complaintRepository.Update(complaint, complaint.ComplaintId);

        await CreateEscalationRecordAsync(complaint, assignment.HandlerId,oldLevel);

        await CreateAssignmentHistoryAsync(complaint, oldHandler, assignment.HandlerId);

        await CreateHistoryAsync(
            complaint,
            oldHandler,
            assignment.HandlerId,
            oldLevel,
            oldStatus);

        await _notificationService.SendAsync(
            assignment.HandlerId,
            NOTIF_COMPLAINT_ESCALATED,
            "Complaint Escalated",
            $"Complaint '{complaint.ComplaintTitle}' has been escalated to you.",
            complaint.ComplaintId);
    }

    private async Task HandleExternalEscalationAsync(
        Complaint complaint)
    {
        var oldHandler =
            complaint.CurrentHandlerEmployeeId;

        var oldLevel =
            complaint.EscalationLevel;
        var oldStatus =
            complaint.StatusId;
        complaint.StatusId =
            STATUS_EXTERNALLY_ESCALATED;

        complaint.EscalationLevel = 3;

        complaint.EscalationDueAt = null;

        complaint.UpdatedAt =
            DateTime.UtcNow;

        await _complaintRepository.Update(
            complaint,
            complaint.ComplaintId);

        await _historyRepository.Create(
            new ComplaintHistory
            {
                ComplaintId = complaint.ComplaintId,
                OldStatusId = oldStatus,
                NewStatusId = STATUS_EXTERNALLY_ESCALATED,
                OldHandlerEmployeeId = oldHandler,
                NewHandlerEmployeeId = oldHandler,
                ChangedBy = null,
                RoleIdAtActionTime = null,
                Remarks =
                    "Automatically escalated outside the system.",
                EscalationLevelSnapshot = oldLevel,
                CreatedAt = DateTime.UtcNow
            });

        await _notificationService.SendAsync(
            complaint.RaisedByEmployeeId,
            NOTIF_EXTERNALLY_ESCALATED,
            "Complaint Externally Escalated",
            $"Your complaint '{complaint.ComplaintTitle}' has been escalated to a higher authority out of the system.",
            complaint.ComplaintId);
    }

    private async Task CreateEscalationRecordAsync(Complaint complaint, int newHandlerId,short breachedLevel)
    {
        var rule = await _escalationRuleRepository.GetRuleAsync(complaint.CategoryId, complaint.PriorityId, breachedLevel);

        await _escalationRepository.Create(
            new ComplaintEscalation
            {
                ComplaintId = complaint.ComplaintId,
                EscalatedAt = DateTime.UtcNow,
                EscalatedByEmployeeId = null,
                EscalatedToEmployeeId = newHandlerId,
                EscalationLevel = complaint.EscalationLevel,
                EscalationRuleId = rule?.EscalationRuleId,
                Reason = breachedLevel==0?"Category SLA breached":$"Escalation Level {breachedLevel} breached"
            });
    }

    private async Task CreateAssignmentHistoryAsync(Complaint complaint, int? oldHandler, int newHandler)
    {
        await _assignmentRepository.Create(
            new ComplaintAssignmentHistory
            {
                ComplaintId = complaint.ComplaintId,
                OldHandlerEmployeeId = oldHandler,
                NewHandlerEmployeeId = newHandler,
                AssignedBy = null,
                AssignmentReason = "ESCALATION"
            });
    }

    private async Task CreateHistoryAsync(Complaint complaint, int? oldHandler, int newHandler, short oldLevel, short oldStatus)
    {
        await _historyRepository.Create(
            new ComplaintHistory
            {
                ComplaintId = complaint.ComplaintId,
                OldStatusId = oldStatus,
                NewStatusId = STATUS_ESCALATED,
                OldHandlerEmployeeId = oldHandler,
                NewHandlerEmployeeId = newHandler,
                ChangedBy = null,
                RoleIdAtActionTime = null,
                Remarks = "Automatic SLA escalation.",
                EscalationLevelSnapshot = oldLevel,
                CreatedAt = DateTime.UtcNow
            });
    }
}