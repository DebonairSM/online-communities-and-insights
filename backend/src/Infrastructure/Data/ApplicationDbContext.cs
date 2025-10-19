using Microsoft.EntityFrameworkCore;
using OnlineCommunities.Core.Entities.Community;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Entities.Tenants;

namespace OnlineCommunities.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for the Online Communities application.
/// Configured for multi-tenant SaaS with clean separation of concerns.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // ========================================================================
    // Identity Domain
    // ========================================================================
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<TenantMembership> TenantMemberships { get; set; } = null!;

    // ========================================================================
    // Tenant Domain
    // ========================================================================
    
    public DbSet<Tenant> Tenants { get; set; } = null!;

    // ========================================================================
    // Community Domain
    // ========================================================================
    
    public DbSet<ChatRoom> ChatRooms { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    public DbSet<ChatRoomMember> ChatRoomMembers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ====================================================================
        // User Entity Configuration
        // ====================================================================
        
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(256);
                
            entity.Property(e => e.FirstName)
                .HasMaxLength(100);
                
            entity.Property(e => e.LastName)
                .HasMaxLength(100);
                
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);
                
            entity.Property(e => e.ExternalLoginProvider)
                .HasMaxLength(50);
                
            entity.Property(e => e.ExternalUserId)
                .HasMaxLength(256);
                
            entity.Property(e => e.EntraTenantId)
                .HasMaxLength(100);
                
            entity.Property(e => e.EntraIdSubject)
                .HasMaxLength(256);

            // Indexes for performance
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => new { e.ExternalLoginProvider, e.ExternalUserId });
            entity.HasIndex(e => e.EntraIdSubject);
        });

        // ====================================================================
        // Tenant Entity Configuration
        // ====================================================================
        
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Subdomain)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.SubscriptionTier)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Free");

            // Indexes for performance
            entity.HasIndex(e => e.Subdomain).IsUnique();
        });

        // ====================================================================
        // TenantMembership Entity Configuration
        // ====================================================================
        
        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.RoleName)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Member");

            // Foreign key relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.TenantMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tenant)
                .WithMany(t => t.Members)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique constraint - user can only be in a tenant once
            entity.HasIndex(e => new { e.UserId, e.TenantId }).IsUnique();
            
            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TenantId);
        });

        // ====================================================================
        // ChatRoom Entity Configuration
        // ====================================================================
        
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);
                
            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            // Indexes for performance
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.IsPublic);
        });

        // ====================================================================
        // ChatMessage Entity Configuration
        // ====================================================================
        
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Content)
                .IsRequired()
                .HasMaxLength(4000);
                
            entity.Property(e => e.MessageType)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.Metadata)
                .HasMaxLength(4000);

            // Foreign key relationships
            entity.HasOne(e => e.ChatRoom)
                .WithMany(r => r.Messages)
                .HasForeignKey(e => e.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            entity.HasIndex(e => e.ChatRoomId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ====================================================================
        // ChatRoomMember Entity Configuration
        // ====================================================================
        
        modelBuilder.Entity<ChatRoomMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("member");

            // Foreign key relationships
            entity.HasOne(e => e.ChatRoom)
                .WithMany(r => r.Members)
                .HasForeignKey(e => e.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique constraint - user can only be a member once per chat room
            entity.HasIndex(e => new { e.ChatRoomId, e.UserId }).IsUnique();
            
            // Indexes for performance
            entity.HasIndex(e => e.ChatRoomId);
            entity.HasIndex(e => e.UserId);
        });
    }
}
