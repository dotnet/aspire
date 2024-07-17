using Microsoft.EntityFrameworkCore;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
        Options = options;
    }

    public DbContextOptions<TestDbContext> Options { get; }

    public DbSet<Car> Cars => Set<Car>();

    public class Car
    {
        public int Id { get; set; }
        public string Brand { get; set; } = default!;
    }
}
