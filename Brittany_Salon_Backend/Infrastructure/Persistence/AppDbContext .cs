using Brittany_Salon_Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Brittany_Salon_Backend.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Service> Services { get; set; } = null!;
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<Clients> Clients { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<AppointmentService> AppointmentServices { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<AppointmentProduct> AppointmentProducts { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Review> Reviews { get; set; } = null!;
        public DbSet<Inventory> Inventory { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Service>(entity =>
            {
                entity.ToTable("Service");
                entity.HasKey(x => x.ServiceId);

                entity.Property(x => x.ServiceName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.ServiceDescription)
                      .HasMaxLength(255);

                entity.Property(x => x.Price)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();

                entity.Property(x => x.DurationMinutes)
                      .IsRequired();

                entity.Property(x => x.ImageUrl)
                      .HasMaxLength(255);

                entity.Property(x => x.ServiceType)
                      .HasMaxLength(50);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);
            });

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.ToTable("Employee");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.Email)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.Password)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(x => x.Image)
                      .HasMaxLength(255);

                entity.Property(x => x.Specialty)
                      .HasMaxLength(100);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);
            });

            modelBuilder.Entity<Clients>(entity =>
            {
                entity.ToTable("Client");
                entity.HasKey(x => x.ClientId);

                entity.Property(x => x.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.Email)
                      .HasMaxLength(150)
                      .IsRequired();

                entity.Property(x => x.Phone)
                      .HasMaxLength(20);

                entity.Property(x => x.Password)
                      .HasMaxLength(255)
                      .IsRequired();

                entity.Property(x => x.PendingBalance)
                      .HasColumnType("decimal(10,2)")
                      .HasDefaultValue(0);

                entity.Property(x => x.ImageUrl)
                      .HasMaxLength(255);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.Property(x => x.CreatedAt)
                      .HasColumnType("timestamp without time zone")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointment");
                entity.HasKey(x => x.AppointmentId);

                entity.Property(x => x.AppointmentDate)
                      .HasColumnType("date")
                      .IsRequired();

                entity.Property(x => x.StartTime)
                      .HasColumnType("timestamp without time zone")
                      .IsRequired();

                entity.Property(x => x.EndTime)
                      .HasColumnType("timestamp without time zone")
                      .IsRequired();

                entity.Property(x => x.AppointmentStatus)
                      .HasMaxLength(50);

                entity.Property(x => x.TotalCost)
                      .HasColumnType("decimal(10,2)");

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.Property(x => x.HairLengthOption)
                        .HasColumnName("hairLengthOption");

                entity.Property(x => x.ClientId)
                      .IsRequired();

                entity.HasOne(x => x.Client)
                      .WithMany()
                      .HasForeignKey(x => x.ClientId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AppointmentService>(entity =>
            {
                entity.ToTable("AppointmentService");
                entity.HasKey(x => x.AppointmentServiceId);

                entity.Property(x => x.ServicePrice)
                      .HasColumnType("decimal(10,2)");

                entity.HasOne(x => x.Appointment)
                      .WithMany(a => a.AppointmentServices)
                      .HasForeignKey(x => x.AppointmentId);

                entity.HasOne(x => x.Service)
                      .WithMany(s => s.AppointmentServices)
                      .HasForeignKey(x => x.ServiceId);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Product");
                entity.HasKey(x => x.ProductId);

                entity.Property(x => x.ProductName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(x => x.ProductDescription)
                      .HasMaxLength(255);

                entity.Property(x => x.Price)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();

                entity.Property(x => x.ImageUrl)
                      .HasMaxLength(255);

                entity.Property(x => x.ExpirationDate)
                      .HasColumnType("date");

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.Property(x => x.CategoryId)
                      .IsRequired();

                entity.HasOne(x => x.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(x => x.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AppointmentProduct>(entity =>
            {
                entity.ToTable("AppointmentProduct");
                entity.HasKey(x => x.AppointmentProductId);

                entity.Property(x => x.Quantity)
                      .IsRequired()
                      .HasDefaultValue(1);

                entity.HasIndex(x => new { x.AppointmentId, x.ProductId })
                      .IsUnique();

                entity.HasOne(x => x.Appointment)
                      .WithMany(a => a.AppointmentProducts)
                      .HasForeignKey(x => x.AppointmentId);

                entity.HasOne(x => x.Product)
                      .WithMany(p => p.AppointmentProducts)
                      .HasForeignKey(x => x.ProductId);
            });


            modelBuilder.Entity<Payment>(entity =>
            {
                entity.ToTable("Payment");
                entity.HasKey(x => x.PaymentId);

                entity.Property(x => x.Amount)
                      .HasColumnType("decimal(10,2)")
                      .IsRequired();

                entity.Property(x => x.PaymentDate)
                      .HasColumnType("timestamp without time zone")
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(x => x.PaymentMethod)
                      .HasMaxLength(50);

                entity.Property(x => x.PaymentStatus)
                      .HasMaxLength(50);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

            entity.HasOne(x => x.Appointment)
                      .WithMany(a => a.Payments)
                      .HasForeignKey(x => x.AppointmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Category");
                entity.HasKey(x => x.CategoryId);

                entity.Property(x => x.CategoryName)
                      .HasMaxLength(50)
                      .IsRequired();

                entity.Property(x => x.CategoryDescription)
                      .HasMaxLength(255);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.HasIndex(x => x.CategoryName)
                      .IsUnique();
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                entity.ToTable("Inventory");
                entity.HasKey(x => x.InventoryId);

                entity.Property(x => x.ProductId)
                      .IsRequired();

                entity.Property(x => x.Quantity)
                      .IsRequired();

                entity.Property(x => x.MinimumStock)
                      .IsRequired();

                entity.Property(x => x.MaximumStock)
                      .IsRequired();

                entity.Property(x => x.Location)
                      .HasMaxLength(100);

                entity.Property(x => x.Notes)
                      .HasMaxLength(255);

                entity.Property(x => x.IsActive)
                      .HasDefaultValue(true);

                entity.Property(x => x.LastUpdatedAt)
                      .HasColumnType("timestamp(6)");

                entity.HasOne(x => x.Product)
                      .WithMany()
                      .HasForeignKey(x => x.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshToken");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Token)
                      .IsRequired()
                      .HasMaxLength(500);

                entity.Property(x => x.UserId)
                      .IsRequired();

                entity.Property(x => x.UserType)
                      .IsRequired()
                      .HasMaxLength(50);

                entity.Property(x => x.ExpirationDate)
                      .IsRequired();

                entity.Property(x => x.IsRevoked)
                      .HasDefaultValue(false);

                entity.Property(x => x.CreatedAt)
                      .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(x => x.RevocationReason)
                      .HasMaxLength(255);

                // Índice para búsquedas rápidas por token
                entity.HasIndex(x => x.Token)
                      .IsUnique();

                // Índice para búsquedas por UserId y UserType
                entity.HasIndex(x => new { x.UserId, x.UserType });
            });

        }
    }
}

