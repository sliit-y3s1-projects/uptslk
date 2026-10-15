using api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace api.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(user => user.Centre)
            .WithMany()
            .HasForeignKey(user => user.CentreId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
