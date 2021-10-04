using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TokenGainer
{
    public partial class MainDB : DbContext
    {
        public MainDB() : base("name=MainDB") { }
        public virtual DbSet<SETTINGS> SETTINGS { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder) { }
    }
    [Table("SETTINGS")]
    public partial class SETTINGS
    {
        [Key]
        [StringLength(255)]
        public string FNAME { get; set; }
        public string FVALUE { get; set; }
        public DateTime FMODIFIEDON { get; set; }
    }
}
