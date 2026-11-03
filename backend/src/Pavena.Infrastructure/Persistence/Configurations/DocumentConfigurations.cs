using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Documents;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(d => d.Mode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasOne(d => d.OwnerUser)
            .WithMany()
            .HasForeignKey(d => d.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Project)
            .WithMany()
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class DocumentCollaboratorConfiguration : IEntityTypeConfiguration<DocumentCollaborator>
{
    public void Configure(EntityTypeBuilder<DocumentCollaborator> builder)
    {
        builder.ToTable("document_collaborators");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.DocumentId, c.UserId })
            .IsUnique()
            .HasDatabaseName("uq_doc_collaborators_pair");

        builder.HasOne(c => c.Document)
            .WithMany(d => d.Collaborators)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.GrantedByUser)
            .WithMany()
            .HasForeignKey(c => c.GrantedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
