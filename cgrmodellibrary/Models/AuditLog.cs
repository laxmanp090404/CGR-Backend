using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class AuditLog
{
    public long AuditId { get; set; }

    public string TableName { get; set; } = null!;

    public string RecordId { get; set; } = null!;

    public char Operation { get; set; }

    public string ColumnName { get; set; } = null!;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public int? ChangedByEmployeeId { get; set; }

    public DateTime ChangedAt { get; set; }

    public string? ApplicationContext { get; set; }

    public virtual Employee? ChangedByEmployee { get; set; }
}
