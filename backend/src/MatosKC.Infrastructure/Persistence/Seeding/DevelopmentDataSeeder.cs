using Microsoft.EntityFrameworkCore;
namespace MatosKC.Infrastructure.Persistence.Seeding;

using MatosKC.Application.Files.Ports;
using MatosKC.Application.Accounts.Ports;
using MatosKC.Domain.Entities.Accounts;
using MatosKC.Domain.Entities.Agencies;
using MatosKC.Domain.Equipments;

public sealed class DevelopmentDataSeeder
{
    private readonly MatosKCDbContext DbContext;
    private readonly IObjectStorage ObjectStorage;
    private readonly IPasswordHasher PasswordHasher;


    public DevelopmentDataSeeder(
        MatosKCDbContext dbContext,
        IObjectStorage objectStorage,
        IPasswordHasher passwordHasher
    )
    {
        DbContext = dbContext;
        ObjectStorage = objectStorage;
        PasswordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAgenciesAndAccountsAsync(cancellationToken);

        await DevelopmentAgencyData.SeedAsync(DbContext, ObjectStorage, cancellationToken);
    }

    private async Task SeedAgenciesAndAccountsAsync(
        CancellationToken cancellationToken
    )
    {
        // SeedAsync is called only in Development by Program.cs.
        const string developmentPassword = "Dev123!";

        (int Code, string Name)[] agencyDefinitions =
        [
            (83, "83 - Epinal"),
            (33, "33 - Poussay"),
            (12, "12 - Laneuveville")
        ];

        foreach (var (Code, Name) in agencyDefinitions)
        {
            Agency? agency = await DbContext.Agencies.SingleOrDefaultAsync(
                candidate => candidate.Code == Code,
                cancellationToken
            );

            if (agency is null)
            {
                agency = new Agency(Name, Code);
                DbContext.Agencies.Add(agency);
            }

            string administratorEmail = $"admin{Code}@example.test";

            Account? administrator = await DbContext.Accounts.SingleOrDefaultAsync(
                account => account.Email == administratorEmail,
                cancellationToken
            );

            if (administrator is null)
            {
                DbContext.Accounts.Add(new Account(
                    displayName: $"Administrateur - {Name}",
                    email: administratorEmail,
                    hashedPassword: PasswordHasher.HashPassword(developmentPassword),
                    isActive: true,
                    role: Role.Admin,
                    agencyId: agency.Id
                ));
            }
            else if (administrator.Role != Role.Admin || administrator.AgencyId != agency.Id)
            {
                throw new InvalidOperationException(
                    $"Cannot seed {administratorEmail}: the existing account has a different role or agency."
                );
            }

            bool hasAgencyAccount = await DbContext.Accounts.AnyAsync(
                account => account.Role == Role.Agency && account.AgencyId == agency.Id,
                cancellationToken
            );

            if (!hasAgencyAccount)
            {
                DbContext.Accounts.Add(new Account(
                    displayName: $"Accès partagé - {Name}",
                    email: null,
                    hashedPassword: PasswordHasher.HashPassword(developmentPassword),
                    isActive: true,
                    role: Role.Agency,
                    agencyId: agency.Id
                ));
            }
        }

        // Save agencies and their accounts together. Existing records are preserved.
        await DbContext.SaveChangesAsync(cancellationToken);
    }

}
