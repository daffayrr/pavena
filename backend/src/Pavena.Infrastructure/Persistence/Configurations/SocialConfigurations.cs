using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Social;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        builder.ToTable("connections", t =>
        {
            t.HasCheckConstraint("chk_conn_no_self", "`RequesterUserId` <> `AddresseeUserId`");
            t.HasCheckConstraint("chk_conn_canonical_order", "`CanonicalUserA` < `CanonicalUserB`");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => new { c.CanonicalUserA, c.CanonicalUserB })
            .IsUnique()
            .HasDatabaseName("uq_connections_canonical_pair");

        builder.HasOne(c => c.RequesterUser)
            .WithMany()
            .HasForeignKey(c => c.RequesterUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.AddresseeUser)
            .WithMany()
            .HasForeignKey(c => c.AddresseeUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("follows");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.TargetType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(f => new { f.FollowerUserId, f.TargetType, f.TargetId })
            .IsUnique()
            .HasDatabaseName("uq_follows_pair");

        builder.HasOne(f => f.FollowerUser)
            .WithMany()
            .HasForeignKey(f => f.FollowerUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        builder.ToTable("user_blocks", t =>
        {
            t.HasCheckConstraint("chk_user_blocks_no_self", "`BlockerUserId` <> `BlockedUserId`");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Reason)
            .HasMaxLength(255);

        builder.HasIndex(b => new { b.BlockerUserId, b.BlockedUserId })
            .IsUnique()
            .HasDatabaseName("uq_user_blocks_pair");

        builder.HasOne(b => b.BlockerUser)
            .WithMany()
            .HasForeignKey(b => b.BlockerUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.BlockedUser)
            .WithMany()
            .HasForeignKey(b => b.BlockedUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Content)
            .HasMaxLength(Post.MaxContentLength)
            .IsRequired();

        builder.Property(p => p.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasIndex(p => p.AuthorUserId)
            .HasDatabaseName("idx_posts_author");

        builder.HasIndex(p => new { p.Status, p.PublishedAtUtc })
            .HasDatabaseName("idx_posts_status_published");

        builder.HasIndex(p => new { p.Visibility, p.PublishedAtUtc })
            .HasDatabaseName("idx_posts_visibility_published");

        builder.HasIndex(p => new { p.PublishedAtUtc, p.Id })
            .HasDatabaseName("idx_posts_feed_composite");

        builder.HasOne(p => p.AuthorUser)
            .WithMany()
            .HasForeignKey(p => p.AuthorUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attachments)
            .WithOne(a => a.Post)
            .HasForeignKey(a => a.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PostAttachmentConfiguration : IEntityTypeConfiguration<PostAttachment>
{
    public void Configure(EntityTypeBuilder<PostAttachment> builder)
    {
        builder.ToTable("post_attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(a => a.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(a => a.PostId)
            .HasDatabaseName("idx_post_attachments_post");

        builder.HasOne(a => a.Post)
            .WithMany(p => p.Attachments)
            .HasForeignKey(a => a.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
