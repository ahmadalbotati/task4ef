using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace NationalBank;

public class BankContext : DbContext
{
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Manager> Managers => Set<Manager>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            "Server=.;Database=NationalBankDb;Trusted_Connection=True;TrustServerCertificate=True");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Manager>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.FullName).IsRequired().HasMaxLength(100);
            e.Property(m => m.Email).IsRequired().HasMaxLength(100);
            e.Property(m => m.PhoneNumber).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<Branch>(e =>
        {
            e.HasKey(b => b.Code);
            e.Property(b => b.Code).HasMaxLength(20);
            e.Property(b => b.Name).IsRequired().HasMaxLength(100);
            e.Property(b => b.Address).IsRequired().HasMaxLength(200);
            e.Property(b => b.PhoneNumber).IsRequired().HasMaxLength(20);
            e.HasOne(b => b.Manager)
                .WithOne(m => m.Branch)
                .HasForeignKey<Branch>(b => b.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.FullName).IsRequired().HasMaxLength(100);
            e.Property(c => c.NationalId).IsRequired().HasMaxLength(14);
            e.Property(c => c.Email).IsRequired().HasMaxLength(100);
            e.Property(c => c.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(c => c.Address).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.AccountNumber);
            e.Property(a => a.AccountNumber).HasMaxLength(30);
            e.Property(a => a.CurrentBalance).HasPrecision(18, 2);
            e.HasOne(a => a.Branch)
                .WithMany(b => b.Accounts)
                .HasForeignKey(a => a.BranchCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerAccount>(e =>
        {
            e.HasKey(ca => new { ca.CustomerId, ca.AccountNumber });
            e.HasOne(ca => ca.Customer)
                .WithMany(c => c.CustomerAccounts)
                .HasForeignKey(ca => ca.CustomerId);
            e.HasOne(ca => ca.Account)
                .WithMany(a => a.CustomerAccounts)
                .HasForeignKey(ca => ca.AccountNumber);
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.TransactionNumber);
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Note).HasMaxLength(250);
            e.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountNumber);
        });

        modelBuilder.Entity<Manager>().HasData(
            new Manager { Id = 1, FullName = "Khaled Mahmoud", Email = "khaled@nationalbank.com", PhoneNumber = "01011111111", HireDate = new DateTime(2015, 3, 1) },
            new Manager { Id = 2, FullName = "Sara Hassan", Email = "sara@nationalbank.com", PhoneNumber = "01022222222", HireDate = new DateTime(2018, 9, 15) });

        modelBuilder.Entity<Branch>().HasData(
            new Branch { Code = "CAI-01", Name = "Cairo Main Branch", Address = "Downtown, Cairo", PhoneNumber = "0225550001", ManagerId = 1 },
            new Branch { Code = "ALX-01", Name = "Alexandria Branch", Address = "Corniche, Alexandria", PhoneNumber = "0345550002", ManagerId = 2 });
    }
}