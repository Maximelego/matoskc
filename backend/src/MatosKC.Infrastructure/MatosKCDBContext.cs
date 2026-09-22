using Microsoft.EntityFrameworkCore;

namespace MatosKC.Infrastructure.Persistence;

public class MatosKCDBContext : DbContext
{
    public MatosKCDBContext(DbContextOptions<MatosKCDbContext> dbContextOptions)
    {

    }
}
