using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pavena.Domain.Entities.Profiles;

namespace Pavena.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");

        builder.HasKey(p => p.UserId);

        builder.Property(p => p.DisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(p => p.Headline)
            .HasMaxLength(200);

        builder.Property(p => p.Bio);

        builder.Property(p => p.CountryCode)
            .HasMaxLength(2);

        builder.Property(p => p.City)
            .HasMaxLength(100);

        builder.Property(p => p.PreferredLanguage)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(p => p.BannerUrl)
            .HasMaxLength(500);

        builder.Property(p => p.Visibility)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.ConcurrencyStamp)
            .HasMaxLength(36)
            .IsConcurrencyToken();

        builder.HasIndex(p => p.DisplayName)
            .HasDatabaseName("idx_user_profiles_display_name");
    }
}

public class UserEducationConfiguration : IEntityTypeConfiguration<UserEducation>
{
    public void Configure(EntityTypeBuilder<UserEducation> builder)
    {
        builder.ToTable("user_educations");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.InstitutionName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Degree)
            .HasMaxLength(100);

        builder.Property(e => e.FieldOfStudy)
            .HasMaxLength(150);

        builder.Property(e => e.Grade)
            .HasMaxLength(50);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Educations)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserExperienceConfiguration : IEntityTypeConfiguration<UserExperience>
{
    public void Configure(EntityTypeBuilder<UserExperience> builder)
    {
        builder.ToTable("user_experiences");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.OrganizationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.RoleTitle)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.EmploymentType)
            .HasMaxLength(50);

        builder.HasOne(e => e.User)
            .WithMany(u => u.Experiences)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> builder)
    {
        builder.ToTable("user_skills");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SkillName)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(s => s.User)
            .WithMany(u => u.Skills)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserInterestConfiguration : IEntityTypeConfiguration<UserInterest>
{
    public void Configure(EntityTypeBuilder<UserInterest> builder)
    {
        builder.ToTable("user_interests");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Topic)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(i => i.User)
            .WithMany(u => u.Interests)
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
