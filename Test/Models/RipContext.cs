namespace Test.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class RipContext : DbContext
    {
        public RipContext()
            : base("name=TestDB")
        {
        }

        public virtual DbSet<Company> Company { get; set; }
        public virtual DbSet<Users> Users { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Company>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Company>()
                .Property(e => e.ContractStatus)
                .IsUnicode(false);

            modelBuilder.Entity<Company>()
                .HasMany(e => e.Users)
                .WithRequired(e => e.Company1)
                .HasForeignKey(e => e.Company)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Users>()
                .Property(e => e.Name)
                .IsUnicode(false);

            modelBuilder.Entity<Users>()
                .Property(e => e.Password)
                .IsUnicode(false);

            modelBuilder.Entity<Users>()
                .Property(e => e.Login)
                .IsUnicode(false);
        }
    }
}
