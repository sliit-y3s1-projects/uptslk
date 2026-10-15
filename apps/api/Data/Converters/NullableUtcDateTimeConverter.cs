using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace api.Data.Converters;

// Same as UtcDateTimeConverter, for nullable DateTime columns.
public sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value.HasValue ? UtcDateTimeConverter.ToUtc(value.Value) : value,
    value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
