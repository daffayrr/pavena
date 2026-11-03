using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pavena.Application.Common.Interfaces;
using Pavena.Domain.Entities.Challenges;
using Pavena.Domain.Entities.Documents;
using Pavena.Domain.Entities.Identity;
using Pavena.Domain.Entities.Organizations;
using Pavena.Domain.Entities.Profiles;
using Pavena.Domain.Entities.Proposals;
using Pavena.Domain.Entities.Research;
using Pavena.Domain.Entities.Social;
using Pavena.Domain.Enums;

namespace Pavena.Infrastructure.Seeding;

public class DemoDataSeeder
{
    public const string DefaultDemoPassword = "Demo#Pass2026";

    private readonly UserManager<User> _userManager;
    private readonly RoleManager<PlatformRole> _roleManager;
    private readonly IPavenaDbContext _dbContext;
    private readonly IObjectStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        UserManager<User> userManager,
        RoleManager<PlatformRole> roleManager,
        IPavenaDbContext dbContext,
        IObjectStorageService storageService,
        IConfiguration configuration,
        ILogger<DemoDataSeeder> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _storageService = storageService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // 0. Safety Check: Refuse execution in Production environment
        var env = _configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError("DEMO SEEDING REFUSED: DemoDataSeeder cannot run in a Production environment.");
            throw new InvalidOperationException("DemoDataSeeder cannot run in a Production environment.");
        }

        _logger.LogInformation("Starting deterministic demo data seeding (Environment: {Environment})...", env ?? "Development");

        // 1. Ensure platform roles
        await EnsurePlatformRolesAsync();

        // 2. Seed Organizations
        var orgs = await SeedOrganizationsAsync(ct);

        // 3. Seed Users & Academic Profiles
        var users = await SeedUsersAndProfilesAsync(orgs, ct);

        // 4. Seed Social Connections, Follows, Blocks, and Posts
        await SeedSocialGraphAndFeedAsync(users, ct);

        // 5. Seed HULU Scenario (Researcher-Led Project & LaTeX Studio)
        await SeedHuluResearchProjectAsync(users, ct);

        // 6. Seed HILIR Scenario (Industry Challenge, NDA Gating, Proposals, Reviews, Conversion)
        await SeedHilirIndustryChallengeAndProposalsAsync(users, orgs, ct);

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Demo data seeding completed successfully and idempotently.");
    }

    private async Task EnsurePlatformRolesAsync()
    {
        string[] roles = [PlatformRole.Superadmin, PlatformRole.PlatformAdmin];
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new PlatformRole(roleName, $"Platform Governance Role: {roleName}"));
                _logger.LogInformation("Created platform role: {RoleName}", roleName);
            }
        }
    }

    private async Task<Dictionary<string, Organization>> SeedOrganizationsAsync(CancellationToken ct)
    {
        var orgDefs = new (string Name, string Slug, OrganizationType Type, string Domain, string City, string Country)[]
        {
            ("Universitas Nusantara Digital", "und", OrganizationType.University, "und.ac.id", "Jakarta", "ID"),
            ("Institut Teknologi Cakrawala", "itc", OrganizationType.University, "itc.ac.id", "Bandung", "ID"),
            ("Pusat Riset Inovasi Nusantara", "prin", OrganizationType.ResearchInstitution, "prin.go.id", "Bogor", "ID"),
            ("PT Energi Hijau Nusantara", "energi-hijau", OrganizationType.Company, "energi-hijau.co.id", "Surabaya", "ID"),
            ("PT Inovasi Teknologi Indonesia", "inovasi-tech", OrganizationType.Company, "inovasi-tech.co.id", "Jakarta", "ID"),
            ("PT Agro Digital Solusi", "agro-digital", OrganizationType.Company, "agro-digital.co.id", "Semarang", "ID")
        };

        var result = new Dictionary<string, Organization>();

        foreach (var def in orgDefs)
        {
            var org = await _dbContext.Organizations.FirstOrDefaultAsync(o => o.Slug == def.Slug, ct);
            if (org == null)
            {
                org = new Organization(def.Name, def.Slug, def.Type)
                {
                    VerificationStatus = OrganizationVerificationStatus.Verified,
                    WebsiteUrl = $"https://{def.Domain}",
                    City = def.City,
                    CountryCode = def.Country,
                    Description = $"{def.Name} is a verified research and innovation partner on the Pavena platform.",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _dbContext.Organizations.Add(org);
                await _dbContext.SaveChangesAsync(ct);
                _logger.LogInformation("Seeded organization: {OrgName} ({Slug})", org.Name, org.Slug);
            }
            result[def.Slug] = org;
        }

        return result;
    }

    private async Task<Dictionary<string, User>> SeedUsersAndProfilesAsync(Dictionary<string, Organization> orgs, CancellationToken ct)
    {
        var userDefs = new[]
        {
            // Platform Admin
            new UserSeedDef(
                Email: "admin@pavena.local",
                Username: "platform_admin",
                DisplayName: "Platform Administrator (Operations)",
                Headline: "Pavena Platform Operations & Trust",
                Bio: "Platform operations, security governance, and compliance officer.",
                City: "Jakarta",
                Country: "ID",
                OrgSlug: null,
                OrgRole: OrgMembershipRole.Student,
                IsPlatformAdmin: true,
                Skills: ["Platform Governance", "Information Security", "Compliance"],
                Degree: null,
                University: null
            ),

            // Academic Researchers
            new UserSeedDef(
                Email: "andi.pratama@und.ac.id",
                Username: "andi_pratama",
                DisplayName: "Dr. Andi Pratama",
                Headline: "Associate Professor of AI & Autonomous Robotic Systems",
                Bio: "Specializing in decentralized multi-agent robotics, drone swarms, and environmental telemetry sensor networks for tropical ecosystem monitoring.",
                City: "Jakarta",
                Country: "ID",
                OrgSlug: "und",
                OrgRole: OrgMembershipRole.Lecturer,
                IsPlatformAdmin: false,
                Skills: ["Autonomous Robotics", "Swarm Intelligence", "ROS2", "Decentralized Systems", "Edge AI"],
                Degree: "Ph.D. in Computer Science",
                University: "Nanyang Technological University"
            ),

            new UserSeedDef(
                Email: "siti.rahmawati@itc.ac.id",
                Username: "siti_rahmawati",
                DisplayName: "Dr. Siti Rahmawati",
                Headline: "Lead Researcher in Perovskite Photovoltaics & Advanced Materials",
                Bio: "Conducting applied research on perovskite-silicon tandem solar cell stability, encapsulation composites, and tropical degradation kinetics.",
                City: "Bandung",
                Country: "ID",
                OrgSlug: "itc",
                OrgRole: OrgMembershipRole.Lecturer,
                IsPlatformAdmin: false,
                Skills: ["Perovskite Photovoltaics", "Materials Characterization", "Thin-film Deposition", "Solar Durability", "Nanotechnology"],
                Degree: "Ph.D. in Materials Science & Engineering",
                University: "Tokyo Institute of Technology"
            ),

            new UserSeedDef(
                Email: "budi.santoso@prin.go.id",
                Username: "budi_santoso",
                DisplayName: "Dr. Budi Santoso",
                Headline: "Senior Agronomy & Precision Agriculture Scientist",
                Bio: "Principal Investigator in IoT agricultural telemetry, remote sensing for peatland hydrology, and smart farming systems.",
                City: "Bogor",
                Country: "ID",
                OrgSlug: "prin",
                OrgRole: OrgMembershipRole.Researcher,
                IsPlatformAdmin: false,
                Skills: ["Precision Agriculture", "IoT Sensors", "Remote Sensing", "Agro-ecosystem Modeling", "Data Analytics"],
                Degree: "Ph.D. in Agricultural Engineering",
                University: "Wageningen University"
            ),

            new UserSeedDef(
                Email: "maya.putri@und.ac.id",
                Username: "maya_putri",
                DisplayName: "Dr. Maya Putri",
                Headline: "Assistant Professor of Nanomaterials & Biomedical Sensors",
                Bio: "Researching microfluidic bio-interfaces, flexible nanomaterial sensors, and interdisciplinary environmental monitoring devices.",
                City: "Jakarta",
                Country: "ID",
                OrgSlug: "und",
                OrgRole: OrgMembershipRole.Lecturer,
                IsPlatformAdmin: false,
                Skills: ["Nanomaterials", "Biosensors", "Microfluidics", "Interdisciplinary Research"],
                Degree: "Ph.D. in Bioengineering",
                University: "National University of Singapore"
            ),

            // Peer Reviewers
            new UserSeedDef(
                Email: "eko.wijaya@prin.go.id",
                Username: "eko_wijaya",
                DisplayName: "Dr. Eko Wijaya",
                Headline: "Principal Research Specialist in Clean Energy Technologies",
                Bio: "Senior peer reviewer and expert in energy storage, solar cell qualification, and national industrial R&D standards.",
                City: "Jakarta",
                Country: "ID",
                OrgSlug: "prin",
                OrgRole: OrgMembershipRole.Researcher,
                IsPlatformAdmin: false,
                Skills: ["Photovoltaic Standards", "Energy Storage", "Technical Peer Review", "R&D Grant Assessment"],
                Degree: "Ph.D. in Electrical Engineering",
                University: "Universitas Indonesia"
            ),

            new UserSeedDef(
                Email: "rina.dewi@itc.ac.id",
                Username: "rina_dewi",
                DisplayName: "Dr. Rina Dewi",
                Headline: "Associate Professor in Renewable Power Systems & Materials",
                Bio: "Faculty researcher and reviewer specializing in solar energy integration, polymer encapsulation, and accelerated durability testing.",
                City: "Bandung",
                Country: "ID",
                OrgSlug: "itc",
                OrgRole: OrgMembershipRole.Lecturer,
                IsPlatformAdmin: false,
                Skills: ["Solar Cell Degradation", "Polymer Composites", "Accelerated Weathering", "Peer Review"],
                Degree: "Ph.D. in Physics",
                University: "Institut Teknologi Bandung"
            ),

            new UserSeedDef(
                Email: "hendra.gunawan@und.ac.id",
                Username: "hendra_gunawan",
                DisplayName: "Dr. Hendra Gunawan",
                Headline: "Professor of Informatics & Autonomous Control",
                Bio: "Senior academic and research director focusing on cyber-physical systems, autonomous vehicles, and flight control algorithms.",
                City: "Jakarta",
                Country: "ID",
                OrgSlug: "und",
                OrgRole: OrgMembershipRole.Lecturer,
                IsPlatformAdmin: false,
                Skills: ["Control Systems", "Cyber-Physical Systems", "Academic Evaluation", "Scientific Writing"],
                Degree: "Ph.D. in Robotics",
                University: "Kyoto University"
            ),

            // Industry R&D Leaders
            new UserSeedDef(
                Email: "diana.kusuma@energi-hijau.co.id",
                Username: "diana_kusuma",
                DisplayName: "Diana Kusuma, M.B.A.",
                Headline: "VP of Research & Development at PT Energi Hijau Nusantara",
                Bio: "Directing industrial renewable energy R&D initiatives, joint university-industry research partnerships, and clean technology commercialization.",
                City: "Surabaya",
                Country: "ID",
                OrgSlug: "energi-hijau",
                OrgRole: OrgMembershipRole.OrganizationAdmin,
                IsPlatformAdmin: false,
                Skills: ["R&D Management", "Technology Transfer", "Photovoltaic Manufacturing", "Industrial Partnerships"],
                Degree: "M.B.A. in Technology Management",
                University: "INSEAD"
            ),

            new UserSeedDef(
                Email: "reza.gunadi@inovasi-tech.co.id",
                Username: "reza_gunadi",
                DisplayName: "Ir. Reza Gunadi",
                Headline: "Chief Technology Officer at PT Inovasi Teknologi Indonesia",
                Bio: "Industrial technology strategist, sponsor of autonomous systems challenges, and commercialization mentor for deep-tech spin-offs.",
                City: "Jakarta",
                Country: "ID",
                OrgSlug: "inovasi-tech",
                OrgRole: OrgMembershipRole.OrganizationAdmin,
                IsPlatformAdmin: false,
                Skills: ["Autonomous Systems", "Technology Strategy", "Venture Commercialization"],
                Degree: "M.Sc. in Systems Engineering",
                University: "TU Delft"
            ),

            new UserSeedDef(
                Email: "bambang@agro-digital.co.id",
                Username: "bambang_sutrisno",
                DisplayName: "Bambang Sutrisno",
                Headline: "Head of Product Innovation at PT Agro Digital Solusi",
                Bio: "Leading smart agriculture technology deployments and industry-academia collaborative research projects across Indonesia.",
                City: "Semarang",
                Country: "ID",
                OrgSlug: "agro-digital",
                OrgRole: OrgMembershipRole.OrganizationAdmin,
                IsPlatformAdmin: false,
                Skills: ["Agro-tech Commercialization", "IoT Hardware", "Precision Agriculture"],
                Degree: "B.Eng. in Electronics",
                University: "Universitas Gadjah Mada"
            )
        };

        var result = new Dictionary<string, User>();

        foreach (var def in userDefs)
        {
            var user = await _userManager.FindByEmailAsync(def.Email);
            if (user == null)
            {
                user = new User(def.Email, def.Username)
                {
                    EmailConfirmed = true,
                    Status = UserStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                var createRes = await _userManager.CreateAsync(user, DefaultDemoPassword);
                if (!createRes.Succeeded)
                {
                    var err = string.Join("; ", createRes.Errors.Select(e => e.Description));
                    _logger.LogError("Failed to create demo user {Email}: {Errors}", def.Email, err);
                    continue;
                }

                _logger.LogInformation("Created demo user: {Email}", def.Email);
            }

            if (def.IsPlatformAdmin)
            {
                if (!await _userManager.IsInRoleAsync(user, PlatformRole.PlatformAdmin))
                {
                    await _userManager.AddToRoleAsync(user, PlatformRole.PlatformAdmin);
                }
            }

            // Profile
            var profile = await _dbContext.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
            if (profile == null)
            {
                profile = new UserProfile(user.Id, def.DisplayName)
                {
                    Headline = def.Headline,
                    Bio = def.Bio,
                    City = def.City,
                    CountryCode = def.Country,
                    PreferredLanguage = "en",
                    Visibility = ProfileVisibility.Public,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                _dbContext.UserProfiles.Add(profile);
            }
            else
            {
                profile.DisplayName = def.DisplayName;
                profile.Headline = def.Headline;
                profile.Bio = def.Bio;
                profile.City = def.City;
                profile.CountryCode = def.Country;
            }

            // Membership
            if (def.OrgSlug != null && orgs.TryGetValue(def.OrgSlug, out var org))
            {
                var membership = await _dbContext.OrganizationMemberships
                    .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == org.Id, ct);

                if (membership == null)
                {
                    membership = new OrganizationMembership(org.Id, user.Id, def.OrgRole)
                    {
                        Title = def.Headline,
                        Department = "R&D / Academic Department",
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    _dbContext.OrganizationMemberships.Add(membership);
                }
            }

            // Skills
            foreach (var skillName in def.Skills)
            {
                var hasSkill = await _dbContext.UserSkills.AnyAsync(s => s.UserId == user.Id && s.SkillName == skillName, ct);
                if (!hasSkill)
                {
                    _dbContext.UserSkills.Add(new UserSkill
                    {
                        UserId = user.Id,
                        SkillName = skillName,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            // Education
            if (def.Degree != null && def.University != null)
            {
                var hasEdu = await _dbContext.UserEducations.AnyAsync(e => e.UserId == user.Id && e.InstitutionName == def.University, ct);
                if (!hasEdu)
                {
                    _dbContext.UserEducations.Add(new UserEducation
                    {
                        UserId = user.Id,
                        InstitutionName = def.University,
                        Degree = def.Degree,
                        FieldOfStudy = "Applied Sciences & Engineering",
                        StartYear = 2012,
                        EndYear = 2016,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            await _dbContext.SaveChangesAsync(ct);
            result[def.Username] = user;
        }

        return result;
    }

    private async Task SeedSocialGraphAndFeedAsync(Dictionary<string, User> users, CancellationToken ct)
    {
        if (!users.TryGetValue("andi_pratama", out var andi) ||
            !users.TryGetValue("siti_rahmawati", out var siti) ||
            !users.TryGetValue("budi_santoso", out var budi) ||
            !users.TryGetValue("maya_putri", out var maya) ||
            !users.TryGetValue("eko_wijaya", out var eko) ||
            !users.TryGetValue("hendra_gunawan", out var hendra) ||
            !users.TryGetValue("reza_gunadi", out var reza) ||
            !users.TryGetValue("diana_kusuma", out var diana))
        {
            return;
        }

        // 1. Accepted Connections
        await EnsureConnectionAsync(andi.Id, siti.Id, ConnectionStatus.Accepted, ct);
        await EnsureConnectionAsync(siti.Id, maya.Id, ConnectionStatus.Accepted, ct);
        await EnsureConnectionAsync(andi.Id, eko.Id, ConnectionStatus.Accepted, ct);

        // 2. Pending Connection (Budi -> Siti)
        await EnsureConnectionAsync(budi.Id, siti.Id, ConnectionStatus.Pending, ct);

        // 3. Follows
        await EnsureFollowAsync(maya.Id, andi.Id, ct);
        await EnsureFollowAsync(reza.Id, andi.Id, ct);
        await EnsureFollowAsync(diana.Id, siti.Id, ct);

        // 4. Interpersonal Block (Budi blocks Hendra for COI scenario)
        var existingBlock = await _dbContext.UserBlocks
            .FirstOrDefaultAsync(b => b.BlockerUserId == budi.Id && b.BlockedUserId == hendra.Id, ct);

        if (existingBlock == null)
        {
            _dbContext.UserBlocks.Add(UserBlock.CreateBlock(budi.Id, hendra.Id, "Conflict of interest and prior intellectual property dispute."));
            _logger.LogInformation("Seeded interpersonal block: Dr. Budi Santoso blocks Dr. Hendra Gunawan.");
        }

        // 5. Posts: Public, Connections-only, OnlyMe
        await EnsurePostAsync(
            andi.Id,
            "Thrilled to announce our preliminary telemetry data on autonomous UAV swarm communication across dense tropical peatland canopies! Over a 4.2 km mesh radius, our low-power payload sustained 98.2% packet delivery with real-time methane and PM2.5 mapping. Collaborating with @siti_rahmawati on sensor casing field trials in Kalimantan next month! #AutonomousSystems #PeatlandConservation #SwarmRobotics #PavenaResearch",
            PostVisibility.Public,
            PostCategory.ResearchUpdate,
            ct);

        await EnsurePostAsync(
            siti.Id,
            "Our laboratory at Institut Teknologi Cakrawala has just completed atomic layer deposition (ALD) encapsulation runs for high-humidity perovskite solar cells. Cross-sectional SEM imaging demonstrates sub-nanometer barrier conformity. We are actively seeking joint modeling collaborations with materials science researchers.",
            PostVisibility.Connections,
            PostCategory.CollaborationOpportunity,
            ct);

        await EnsurePostAsync(
            budi.Id,
            "Confidential notes for Q4 peatland agronomy sensors: Calibration curves must be validated against wet sapric peat samples prior to the consortium grant review on Friday.",
            PostVisibility.OnlyMe,
            PostCategory.ResearchFinding,
            ct);

        await _dbContext.SaveChangesAsync(ct);
    }

    private async Task EnsureConnectionAsync(Guid u1, Guid u2, ConnectionStatus status, CancellationToken ct)
    {
        var existing = await _dbContext.Connections
            .FirstOrDefaultAsync(c => (c.RequesterUserId == u1 && c.AddresseeUserId == u2) ||
                                      (c.RequesterUserId == u2 && c.AddresseeUserId == u1), ct);

        if (existing == null)
        {
            var conn = Connection.CreateRequest(u1, u2);
            if (status == ConnectionStatus.Accepted)
            {
                conn.Accept(u2);
            }
            _dbContext.Connections.Add(conn);
        }
    }

    private async Task EnsureFollowAsync(Guid followerId, Guid targetUserId, CancellationToken ct)
    {
        var existing = await _dbContext.Follows
            .FirstOrDefaultAsync(f => f.FollowerUserId == followerId && f.TargetId == targetUserId && f.TargetType == FollowTargetType.User, ct);

        if (existing == null)
        {
            _dbContext.Follows.Add(Follow.FollowUser(followerId, targetUserId));
        }
    }

    private async Task EnsurePostAsync(Guid authorId, string content, PostVisibility visibility, PostCategory category, CancellationToken ct)
    {
        var existing = await _dbContext.Posts
            .FirstOrDefaultAsync(p => p.AuthorUserId == authorId && p.Content == content, ct);

        if (existing == null)
        {
            var post = Post.CreatePublished(authorId, content, visibility, category);
            _dbContext.Posts.Add(post);
        }
    }

    private async Task SeedHuluResearchProjectAsync(Dictionary<string, User> users, CancellationToken ct)
    {
        if (!users.TryGetValue("andi_pratama", out var andi) ||
            !users.TryGetValue("siti_rahmawati", out var siti))
        {
            return;
        }

        const string slug = "eco-drone-swarm";
        var project = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .Include(p => p.Milestones)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

        if (project == null)
        {
            project = ResearchProject.CreatePersonal(
                "Autonomous Eco-Drone Swarm for Tropical Peatland Reforestation and Wildfire Monitoring",
                slug,
                andi.Id,
                "This project develops decentralized drone swarm topologies adapted to dense equatorial canopy environments, utilizing low-power mesh radios and sensor fusion for early subsurface peat fire detection.",
                "Autonomous Systems & Remote Sensing",
                "Develop resilient UAV swarm algorithms for multi-point methane detection and canopy monitoring over tropical peatlands.");

            project.Visibility = ProjectVisibility.PublicDiscovery;
            project.Stage = ResearchStage.DataCollection;
            project.StartDate = DateTime.UtcNow.AddMonths(-3);
            project.EndDate = DateTime.UtcNow.AddMonths(9);

            _dbContext.ResearchProjects.Add(project);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded HULU ResearchProject: {Title} ({Slug})", project.Title, project.Slug);
        }

        // Add Dr. Siti Rahmawati as CoResearcher
        if (!project.Members.Any(m => m.UserId == siti.Id))
        {
            project.Members.Add(new ResearchMember(project.Id, siti.Id, ProjectMemberRole.CoResearcher, andi.Id));
            project.LogActivity(andi.Id, "MemberAdded", $"Dr. Siti Rahmawati joined as {ProjectMemberRole.CoResearcher}.");
        }

        // Milestones
        var m1 = project.Milestones.FirstOrDefault(m => m.Title == "Sensor Array Architecture & Peatland Calibration");
        if (m1 == null)
        {
            m1 = new ResearchMilestone(project.Id, "Sensor Array Architecture & Peatland Calibration", "Benchmarking multi-gas sensors for high humidity.", DateTime.UtcNow.AddMonths(-1), andi.Id)
            {
                Status = MilestoneStatus.Achieved,
                AchievedAtUtc = DateTime.UtcNow.AddDays(-15)
            };
            project.Milestones.Add(m1);
        }

        var m2 = project.Milestones.FirstOrDefault(m => m.Title == "Autonomous Pathfinding in Dense Canopy");
        if (m2 == null)
        {
            m2 = new ResearchMilestone(project.Id, "Autonomous Pathfinding in Dense Canopy", "Mesh routing protocols and dynamic tree obstacle avoidance.", DateTime.UtcNow.AddMonths(3), andi.Id)
            {
                Status = MilestoneStatus.InProgress
            };
            project.Milestones.Add(m2);
        }

        var m3 = project.Milestones.FirstOrDefault(m => m.Title == "Field Trial in Central Kalimantan Peatland");
        if (m3 == null)
        {
            m3 = new ResearchMilestone(project.Id, "Field Trial in Central Kalimantan Peatland", "Full-scale deployment with ground telemetry base stations.", DateTime.UtcNow.AddMonths(7), andi.Id)
            {
                Status = MilestoneStatus.Planned
            };
            project.Milestones.Add(m3);
        }

        await _dbContext.SaveChangesAsync(ct);

        // Tasks
        if (!project.Tasks.Any(t => t.Title == "Calibrate methane and PM2.5 multi-gas payload"))
        {
            project.Tasks.Add(new ResearchTask(project.Id, "Calibrate methane and PM2.5 multi-gas payload", andi.Id,
                "Laboratory chamber verification under 90% relative humidity.", andi.Id, TaskPriority.High, DateTime.UtcNow.AddDays(-20), m1.Id)
            {
                Status = ResearchTaskStatus.Done,
                CompletedAtUtc = DateTime.UtcNow.AddDays(-18)
            });
        }

        if (!project.Tasks.Any(t => t.Title == "Develop localized decentralized mesh routing"))
        {
            project.Tasks.Add(new ResearchTask(project.Id, "Develop localized decentralized mesh routing", andi.Id,
                "Implement ad-hoc OLSR routing daemon on embedded onboard Raspberry Pi compute nodes.", andi.Id, TaskPriority.Urgent, DateTime.UtcNow.AddDays(45), m2.Id)
            {
                Status = ResearchTaskStatus.InProgress
            });
        }

        if (!project.Tasks.Any(t => t.Title == "Design weather-resistant casing prototype"))
        {
            project.Tasks.Add(new ResearchTask(project.Id, "Design weather-resistant casing prototype", andi.Id,
                "IP67 compliant casing for sensor telemetry node with silicone gaskets.", siti.Id, TaskPriority.Medium, DateTime.UtcNow.AddDays(30), m2.Id)
            {
                Status = ResearchTaskStatus.InProgress
            });
        }

        await _dbContext.SaveChangesAsync(ct);

        // Collaborative LaTeX Document
        var doc = await _dbContext.Documents
            .Include(d => d.Files)
            .Include(d => d.Collaborators)
            .FirstOrDefaultAsync(d => d.ProjectId == project.Id && d.Title == "Peatland-Swarm-Monitoring-Framework", ct);

        if (doc == null)
        {
            doc = Document.Create(
                "Peatland-Swarm-Monitoring-Framework",
                EditorMode.Latex,
                andi.Id,
                project.Id,
                "Collaborative manuscript for IEEE Transactions on Geoscience and Remote Sensing.",
                "Manuscript");

            doc.Visibility = DocumentVisibility.Shared;
            doc.Collaborators.Add(new DocumentCollaborator(doc.Id, siti.Id, DocumentPermissionRole.Editor, andi.Id));

            _dbContext.Documents.Add(doc);
            await _dbContext.SaveChangesAsync(ct);

            // Seed sample LaTeX file in Object Storage
            const string latexContent = @"\documentclass[11pt,a4paper]{article}
\usepackage[utf8]{inputenc}
\usepackage{amsmath,amsfonts,amssymb}
\usepackage{graphicx}
\usepackage{hyperref}

\title{Decentralized Autonomous Drone Swarm Architecture for High-Humidity Tropical Peatland Telemetry}
\author{Dr. Andi Pratama\textsuperscript{1}, Dr. Siti Rahmawati\textsuperscript{2}\\
\small\textsuperscript{1}Universitas Nusantara Digital, Jakarta, Indonesia\\
\small\textsuperscript{2}Institut Teknologi Cakrawala, Bandung, Indonesia}
\date{\today}

\begin{document}
\maketitle

\begin{abstract}
Tropical peatlands sequester substantial global carbon stocks yet remain vulnerable to underground smoldering fires. This paper presents an autonomous UAV swarm architecture utilizing mesh networking and multi-gas sensor payloads for real-time peatland monitoring.
\end{abstract}

\section{Introduction}
Early detection of subsurface peat fires requires dense spatial-temporal sensor coverage inaccessible to conventional ground patrols.

\section{System Methodology}
Our swarm model implements decentralized flocking rules with dynamic obstacle avoidance in dense tropical rainforest canopies.

\section{Preliminary Field Observations}
Telemetry packet loss remained under 1.8\% over a 4.2 km ad-hoc mesh radius.

\end{document}";

            var storageKey = $"storage/documents/{doc.Id}/main.tex";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(latexContent)))
            {
                await _storageService.SaveObjectAsync(storageKey, stream, "text/x-tex", ct);
            }

            var docFile = DocumentFile.Create(
                doc.Id,
                "main.tex",
                "main.tex",
                ".tex",
                "text/x-tex",
                Encoding.UTF8.GetByteCount(latexContent),
                storageKey,
                isMain: true,
                isBinary: false,
                andi.Id);

            doc.Files.Add(docFile);
            doc.MainFileId = docFile.Id;
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded collaborative LaTeX document and saved physical main.tex into object storage.");
        }
    }

    private async Task SeedHilirIndustryChallengeAndProposalsAsync(
        Dictionary<string, User> users,
        Dictionary<string, Organization> orgs,
        CancellationToken ct)
    {
        if (!users.TryGetValue("diana_kusuma", out var diana) ||
            !users.TryGetValue("siti_rahmawati", out var siti) ||
            !users.TryGetValue("maya_putri", out var maya) ||
            !users.TryGetValue("budi_santoso", out var budi) ||
            !users.TryGetValue("eko_wijaya", out var eko) ||
            !users.TryGetValue("rina_dewi", out var rina) ||
            !orgs.TryGetValue("energi-hijau", out var sponsorOrg) ||
            !orgs.TryGetValue("itc", out var itcOrg) ||
            !orgs.TryGetValue("prin", out var prinOrg))
        {
            return;
        }

        const string challengeSlug = "tropical-tandem-pv-encapsulation";
        var challenge = await _dbContext.IndustryChallenges
            .Include(c => c.Domains)
            .Include(c => c.Attachments)
            .Include(c => c.NdaAgreements)
            .FirstOrDefaultAsync(c => c.Slug == challengeSlug, ct);

        if (challenge == null)
        {
            challenge = IndustryChallenge.CreateDraft(
                sponsorOrg.Id,
                diana.Id,
                "Advanced Perovskite-Silicon Tandem Photovoltaic Encapsulation for High-Humidity Tropical Climates",
                challengeSlug,
                "Seeking breakthrough polymer-composite encapsulation techniques to prevent moisture-induced degradation in tandem perovskite-silicon solar cells under tropical climate conditions.",
                "Perovskite-silicon tandem cells demonstrate laboratory power conversion efficiencies exceeding 30%, but rapidly degrade within 6 months when exposed to RH > 85% and temperatures > 40°C in equatorial regions. We require industrial-grade encapsulation solutions achieving < 5% efficiency loss after 1000h damp-heat testing.",
                "Development and validation of moisture-barrier multi-layer composites, low-temperature lamination protocols, and accelerated environmental durability testing.",
                "Encapsulation prototype specifications, damp-heat degradation kinetics report, and pilot manufacturing readiness assessment.",
                DateTime.UtcNow.AddMonths(6),
                ChallengeVisibility.Public,
                InstitutionalEligibility.AccreditedResearchInstitutesOnly,
                "IDR",
                minBudget: 500_000_000,
                maxBudget: 1_000_000_000,
                estimatedDurationMonths: 18,
                requiresManualNdaApproval: false);

            challenge.NdaTermsVersion = "v1.0";
            challenge.NdaTermsText = "Standard Pavena Bilateral Non-Disclosure Agreement for Industrial R&D Challenges: All proprietary manufacturing telemetry and test data shared hereunder is strictly confidential.";

            // Domains
            challenge.Domains.Add(new ChallengeResearchDomain
            {
                ChallengeId = challenge.Id,
                DomainName = "Renewable Energy & Photovoltaics",
                SubDomain = "Perovskite-Silicon Tandem",
                IsPrimary = true
            });
            challenge.Domains.Add(new ChallengeResearchDomain
            {
                ChallengeId = challenge.Id,
                DomainName = "Materials Science & Nanotechnology",
                SubDomain = "Polymer Barriers",
                IsPrimary = false
            });

            // Transition: Draft -> Submit -> Approve -> Publish -> OpenForProposals
            challenge.SubmitForApproval(diana.Id, "Ready for organizational review.");
            challenge.Approve(diana.Id, "Approved by R&D Director.");
            challenge.Publish(diana.Id);
            challenge.OpenForProposals(diana.Id);

            _dbContext.IndustryChallenges.Add(challenge);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded IndustryChallenge: {Title} ({Slug})", challenge.Title, challenge.Slug);

            // Challenge Attachments
            // 1. Tier 1: Public Briefing
            const string publicPdfContent = "%PDF-1.4 Public Briefing: Tropical PV Degradation Benchmark Summary";
            var publicKey = $"storage/challenges/{challenge.Id}/Tropical_PV_Degradation_Benchmark_Summary.pdf";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(publicPdfContent)))
            {
                await _storageService.SaveObjectAsync(publicKey, stream, "application/pdf", ct);
            }

            challenge.Attachments.Add(ChallengeAttachment.Create(
                challenge.Id,
                "Tropical_PV_Degradation_Benchmark_Summary.pdf",
                ".pdf",
                "application/pdf",
                Encoding.UTF8.GetByteCount(publicPdfContent),
                publicKey,
                AttachmentConfidentialityLevel.PublicBriefing,
                diana.Id,
                "Public technical briefing on tropical PV degradation benchmarks."));

            // 2. Tier 2: NDA-gated Confidential Attachment
            const string confidentialPdfContent = "%PDF-1.4 Confidential Industrial Autoclave Damp-Heat Test Data v2";
            var confidentialKey = $"storage/challenges/{challenge.Id}/Confidential_Industrial_Autoclave_Test_Data_v2.pdf";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(confidentialPdfContent)))
            {
                await _storageService.SaveObjectAsync(confidentialKey, stream, "application/pdf", ct);
            }

            challenge.Attachments.Add(ChallengeAttachment.Create(
                challenge.Id,
                "Confidential_Industrial_Autoclave_Test_Data_v2.pdf",
                ".pdf",
                "application/pdf",
                Encoding.UTF8.GetByteCount(confidentialPdfContent),
                confidentialKey,
                AttachmentConfidentialityLevel.ConfidentialNdaRequired,
                diana.Id,
                "Confidential industrial damp-heat test results (NDA required)."));

            await _dbContext.SaveChangesAsync(ct);
        }

        // Digital NDA: Dr. Siti signs NDA (Dr. Andi does NOT sign, demonstrating access gating)
        var ndaHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(challenge.NdaTermsText ?? "v1.0")));
        var sitiNda = await _dbContext.ChallengeNdaAgreements
            .FirstOrDefaultAsync(n => n.ChallengeId == challenge.Id && n.ResearcherUserId == siti.Id, ct);

        if (sitiNda == null)
        {
            sitiNda = ChallengeNdaAgreement.Create(
                challenge.Id,
                siti.Id,
                challenge.NdaTermsVersion,
                ndaHash,
                "192.168.1.100",
                requiresManualApproval: false,
                researcherOrgId: itcOrg.Id);

            _dbContext.ChallengeNdaAgreements.Add(sitiNda);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded signed NDA for Dr. Siti Rahmawati on challenge {Slug}", challenge.Slug);
        }

        // ==========================================
        // PROPOSAL A: Accepted & Converted to Project
        // ==========================================
        const string proposalASlug = "fluoropolymer-tandem-pv-encapsulation";
        var proposalA = await _dbContext.ResearchProposals
            .Include(p => p.Investigators)
            .Include(p => p.Domains)
            .FirstOrDefaultAsync(p => p.Slug == proposalASlug, ct);

        if (proposalA == null)
        {
            proposalA = ResearchProposal.CreateDraft(
                challenge.Id,
                siti.Id,
                itcOrg.Id,
                "Hydrophobic Fluoropolymer Multi-Layer Barrier Encapsulation for Perovskite Tandem Solar Cells",
                proposalASlug,
                "This proposal develops an ALD-deposited fluoropolymer multi-layer composite barrier that prevents water vapor transmission down to 10^-5 g/m^2/day under tropical conditions.",
                "Moisture ingress rapidly degrades metal-halide perovskite crystals into lead halides under high relative humidity, causing sudden catastrophic cell failure.",
                "Formulate low-temperature ALD fluoropolymer barrier films and validate module performance through 1000-hour damp heat testing at 85C/85% RH.",
                "Atomic layer deposition of fluoropolymer barrier followed by cross-linking UV curing and environmental chamber degradation monitoring.",
                "Encapsulated perovskite-silicon tandem modules demonstrating < 4% power conversion efficiency loss after 1000h accelerated tropical chamber exposure.",
                proposedTimelineMonths: 12,
                proposedBudget: 780_000_000,
                currency: "IDR",
                fundingJustification: "Covers precursor chemical synthesis, specialized ALD tool time, and accelerated environmental chamber utility costs.",
                confidentiality: ProposalConfidentialityLevel.Confidential);

            // Add Co-investigator: Dr. Maya Putri
            proposalA.AddInvestigator(maya.Id, InvestigatorRole.CoInvestigator, "Nanomaterial surface interface analysis and microfluidic barrier testing.");

            // Domains
            proposalA.Domains.Add(new ProposalResearchDomain { ProposalId = proposalA.Id, Domain = "Renewable Energy & Photovoltaics" });
            proposalA.Domains.Add(new ProposalResearchDomain { ProposalId = proposalA.Id, Domain = "Materials Science & Nanotechnology" });

            // Lifecycle: Draft -> Submit -> PassEligibility -> Accept
            proposalA.Submit(siti.Id);
            proposalA.PassEligibility(diana.Id, "Meets institutional accreditation and researcher profile requirements.");
            proposalA.Accept(diana.Id, "Exceptional technical methodology, comprehensive ALD barrier design, and clear industrial scalability.");

            _dbContext.ResearchProposals.Add(proposalA);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded Proposal A: {Title} ({Slug})", proposalA.Title, proposalA.Slug);
        }

        // Convert Proposal A to ResearchProject (HILIR Pathway Traceability)
        const string convertedProjectSlug = "converted-fluoropolymer-tandem-pv";
        var convertedProject = await _dbContext.ResearchProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Slug == convertedProjectSlug, ct);

        if (convertedProject == null)
        {
            convertedProject = ResearchProject.CreateFromProposal(
                proposalA.Title,
                convertedProjectSlug,
                siti.Id,
                challenge.Id,
                proposalA.Id,
                proposalA.Abstract,
                "Renewable Energy & Photovoltaics",
                proposalA.ResearchObjectives);

            convertedProject.OriginType = ProjectOriginType.IndustryChallenge;
            convertedProject.Visibility = ProjectVisibility.Private;
            convertedProject.Stage = ResearchStage.MethodologyDesign;
            convertedProject.StartDate = DateTime.UtcNow;
            convertedProject.EndDate = DateTime.UtcNow.AddMonths(12);

            // Add Diana Kusuma as Industry Partner
            convertedProject.Members.Add(new ResearchMember(convertedProject.Id, diana.Id, ProjectMemberRole.IndustryPartner, siti.Id));

            // Add Dr. Maya Putri as CoResearcher
            convertedProject.Members.Add(new ResearchMember(convertedProject.Id, maya.Id, ProjectMemberRole.CoResearcher, siti.Id));

            _dbContext.ResearchProjects.Add(convertedProject);
            await _dbContext.SaveChangesAsync(ct);

            proposalA.MarkConverted(convertedProject.Id, diana.Id);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Converted Proposal A to ResearchProject ID {ProjectId} with full traceability.", convertedProject.Id);
        }

        // ==========================================
        // PROPOSAL B: Under Review with Rubrics & COI
        // ==========================================
        const string proposalBSlug = "graphene-nanocomposite-pv-barrier";
        var proposalB = await _dbContext.ResearchProposals
            .Include(p => p.Investigators)
            .Include(p => p.Domains)
            .Include(p => p.ReviewAssignments).ThenInclude(a => a.Assessment)
            .FirstOrDefaultAsync(p => p.Slug == proposalBSlug, ct);

        if (proposalB == null)
        {
            proposalB = ResearchProposal.CreateDraft(
                challenge.Id,
                budi.Id,
                prinOrg.Id,
                "Graphene-Enhanced Dual-Layer Nanocomposite Barrier for Tropical Photovoltaic Durability",
                proposalBSlug,
                "This project investigates graphene nanoplatelet-embedded polyurethane composites to formulate low-cost, ultra-flexible moisture impermeable encapsulants.",
                "Existing EVA encapsulants fail rapidly in tropical conditions due to acetic acid release under high temperature and UV radiation.",
                "Formulate stable graphene-polymer dispersions and establish automated roll-to-roll spray lamination parameters for pilot solar cell fabrication.",
                "Direct liquid-phase exfoliation of graphene, integration into thermoplastic polyurethane, and environmental damp-heat testing.",
                "Commercial-ready nanocomposite encapsulation sheet exhibiting zero acetic acid generation and water vapor transmission rate < 10^-4 g/m^2/day.",
                proposedTimelineMonths: 14,
                proposedBudget: 820_000_000,
                currency: "IDR",
                fundingJustification: "Raw materials (high-purity graphite), ultrasonic dispersion equipment, and prototype module lamination line trials.",
                confidentiality: ProposalConfidentialityLevel.Confidential);

            // Domains
            proposalB.Domains.Add(new ProposalResearchDomain { ProposalId = proposalB.Id, Domain = "Materials Science & Nanotechnology" });
            proposalB.Domains.Add(new ProposalResearchDomain { ProposalId = proposalB.Id, Domain = "Renewable Energy & Photovoltaics" });

            // Lifecycle: Draft -> Submit -> PassEligibility
            proposalB.Submit(budi.Id);
            proposalB.PassEligibility(diana.Id, "Submitting institution PRIN is verified; lead researcher profile is complete.");

            _dbContext.ResearchProposals.Add(proposalB);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded Proposal B: {Title} ({Slug})", proposalB.Title, proposalB.Slug);
        }

        // Reviewer 1: Dr. Eko Wijaya (Score: 84.6%, Minor Revision)
        var rev1 = proposalB.ReviewAssignments.FirstOrDefault(a => a.ReviewerUserId == eko.Id);
        if (rev1 == null)
        {
            rev1 = new ProposalReviewAssignment
            {
                Id = Guid.NewGuid(),
                ProposalId = proposalB.Id,
                ReviewerUserId = eko.Id,
                AssignedByUserId = diana.Id,
                Status = ReviewAssignmentStatus.Completed,
                DueAtUtc = DateTime.UtcNow.AddDays(14),
                AssignedAtUtc = DateTime.UtcNow.AddDays(-6),
                AcceptedAtUtc = DateTime.UtcNow.AddDays(-5),
                HasCoiDeclaration = true,
                CoiType = CoiDeclarationType.None,
                CoiDescription = "Reviewer explicitly declared absence of conflict of interest.",
                CoiDeclaredAtUtc = DateTime.UtcNow.AddDays(-5)
            };
            proposalB.ReviewAssignments.Add(rev1);
            await _dbContext.SaveChangesAsync(ct);
        }

        var assessment1 = await _dbContext.ProposalAssessments.FirstOrDefaultAsync(a => a.AssignmentId == rev1.Id, ct);
        if (assessment1 == null)
        {
            assessment1 = new ProposalAssessment
            {
                Id = Guid.NewGuid(),
                AssignmentId = rev1.Id,
                Assignment = rev1,
                ProposalId = proposalB.Id,
                ReviewerUserId = eko.Id,
                Status = AssessmentStatus.Draft,
                SubmittedAtUtc = DateTime.UtcNow.AddDays(-2),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
            };

            assessment1.UpdateRubricScores(
                technicalMerit: 8.5m,
                methodology: 8.0m,
                feasibility: 8.2m,
                innovation: 9.0m,
                budgetAdequacy: 8.0m,
                teamQualification: 8.5m,
                impactPotential: 8.7m,
                alignment: 8.8m,
                strengths: "Innovative graphene integration with promising vapor transmission reduction rates and strong national lab facilities at PRIN.",
                weaknesses: "Lamination temperature tolerance needs deeper validation on temperature-sensitive perovskite substrates.",
                recommendations: "Provide thermal tolerance curves for perovskite crystal structures during composite hot-lamination.",
                confidentialRemarks: "Strong team; recommended for funding subject to thermal lamination revision.",
                recommendation: AssessmentRecommendation.RecommendRevision);

            assessment1.SubmitAssessment();
            _dbContext.ProposalAssessments.Add(assessment1);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded Reviewer 1 assessment by Dr. Eko Wijaya (Score: {Score}%)", assessment1.OverallScore);
        }

        // Reviewer 2: Dr. Rina Dewi (Score: 88.8%, Accept)
        var rev2 = proposalB.ReviewAssignments.FirstOrDefault(a => a.ReviewerUserId == rina.Id);
        if (rev2 == null)
        {
            rev2 = new ProposalReviewAssignment
            {
                Id = Guid.NewGuid(),
                ProposalId = proposalB.Id,
                ReviewerUserId = rina.Id,
                AssignedByUserId = diana.Id,
                Status = ReviewAssignmentStatus.Completed,
                DueAtUtc = DateTime.UtcNow.AddDays(14),
                AssignedAtUtc = DateTime.UtcNow.AddDays(-6),
                AcceptedAtUtc = DateTime.UtcNow.AddDays(-5),
                HasCoiDeclaration = true,
                CoiType = CoiDeclarationType.None,
                CoiDescription = "Reviewer explicitly declared absence of conflict of interest.",
                CoiDeclaredAtUtc = DateTime.UtcNow.AddDays(-5)
            };
            proposalB.ReviewAssignments.Add(rev2);
            await _dbContext.SaveChangesAsync(ct);
        }

        var assessment2 = await _dbContext.ProposalAssessments.FirstOrDefaultAsync(a => a.AssignmentId == rev2.Id, ct);
        if (assessment2 == null)
        {
            assessment2 = new ProposalAssessment
            {
                Id = Guid.NewGuid(),
                AssignmentId = rev2.Id,
                Assignment = rev2,
                ProposalId = proposalB.Id,
                ReviewerUserId = rina.Id,
                Status = AssessmentStatus.Draft,
                SubmittedAtUtc = DateTime.UtcNow.AddDays(-1),
                UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
            };

            assessment2.UpdateRubricScores(
                technicalMerit: 9.0m,
                methodology: 8.8m,
                feasibility: 8.5m,
                innovation: 9.0m,
                budgetAdequacy: 8.5m,
                teamQualification: 9.0m,
                impactPotential: 9.0m,
                alignment: 9.2m,
                strengths: "Well-crafted methodology eliminating corrosive acetic acid degradation completely while improving mechanical flexural strength.",
                weaknesses: "Batch-to-batch exfoliation consistency needs standard operating procedures.",
                recommendations: "Include optical transmittance loss measurements for the front encapsulation layer.",
                confidentialRemarks: "High impact project that directly addresses tropical deployment requirements.",
                recommendation: AssessmentRecommendation.RecommendAccept);

            assessment2.SubmitAssessment();
            _dbContext.ProposalAssessments.Add(assessment2);
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded Reviewer 2 assessment by Dr. Rina Dewi (Score: {Score}%)", assessment2.OverallScore);
        }
    }

    private record UserSeedDef(
        string Email,
        string Username,
        string DisplayName,
        string Headline,
        string Bio,
        string City,
        string Country,
        string? OrgSlug,
        OrgMembershipRole OrgRole,
        bool IsPlatformAdmin,
        string[] Skills,
        string? Degree,
        string? University);
}
