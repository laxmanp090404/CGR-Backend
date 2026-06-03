using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class AuditLog
{
    public int AuditLogId { get; set; }

    public string TableName { get; set; } = null!;

    public long RecordId { get; set; }

    public string ColumnName { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Employee? ChangedByNavigation { get; set; }
}
