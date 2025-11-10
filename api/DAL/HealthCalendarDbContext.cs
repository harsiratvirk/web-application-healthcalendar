using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthCalendar.Models;

namespace HealthCalendar.DAL;

public class HealthCalendarDbContext : DbContext
{
    public HealthCalendarDbContext(DbContextOptions<HealthCalendarDbContext> options) : base(options) { }

    public DbSet<Availability> Availability { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Schedule> Schedule { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseLazyLoadingProxies();
    }

}
