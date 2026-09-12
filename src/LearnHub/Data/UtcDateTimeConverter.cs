using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LearnHub.Data;

/// <summary>
/// LearnHub stores every timestamp in UTC. SQLite returns DateTime values without a kind, so values
/// read from the database are marked as UTC; otherwise the browser would treat them as local time.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
