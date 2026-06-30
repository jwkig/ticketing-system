using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.ValueObjects;

namespace TicketingSystem.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");

        var nameConverter = new ValueConverter<TeamName, string>(
            t => t.Value,
            v => new TeamName(v));

        var nameComparer = new ValueComparer<TeamName>(
            (a, b) => a != null && b != null && a.Value == b.Value,
            v => v.Value.GetHashCode(),
            v => new TeamName(v.Value));

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasConversion(nameConverter, nameComparer)
            .HasMaxLength(200)
            .IsRequired();

        // Case-insensitive uniqueness enforced via a PostgreSQL expression index in migration.
        // The index below provides a standard uniqueness guard for the exact stored value.
        builder.HasIndex(t => t.Name)
            .HasDatabaseName("idx_teams_name");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at");
        builder.Property(t => t.ModifiedAt).HasColumnName("modified_at");
    }
}
