using Microsoft.EntityFrameworkCore;
using Quantia.Models;
using Quantia.Models.ViewModels;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserModel> Users => Set<UserModel>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<TradeModel> Trades => Set<TradeModel>();


}
