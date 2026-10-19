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

    private const string PlaceholderPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

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

        IReadOnlyCollection<EquipmentCategory> categories =
            DevelopmentData.CreateCategories();

        Guid[] categoryIds = categories
            .Select(category => category.Id)
            .ToArray();

        HashSet<Guid> existingCategoryIds = await DbContext.EquipmentCategories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .Select(category => category.Id)
            .ToHashSetAsync(cancellationToken);

        await DbContext.EquipmentCategories.AddRangeAsync(
            categories.Where(category => !existingCategoryIds.Contains(category.Id)),
            cancellationToken
        );

        await DbContext.SaveChangesAsync(cancellationToken);

        IReadOnlyCollection<Equipment> equipments =
            DevelopmentData.CreateEquipments();

        Guid[] equipmentIds = equipments
            .Select(equipment => equipment.Id)
            .ToArray();

        HashSet<Guid> existingEquipmentIds = await DbContext.Equipments
            .AsNoTracking()
            .Where(equipment => equipmentIds.Contains(equipment.Id))
            .Select(equipment => equipment.Id)
            .ToHashSetAsync(cancellationToken);

        await DbContext.Equipments.AddRangeAsync(
            equipments.Where(equipment => !existingEquipmentIds.Contains(equipment.Id)),
            cancellationToken
        );

        await DbContext.SaveChangesAsync(cancellationToken);

        await DevelopmentInspectionTemplates.SeedAsync(DbContext, cancellationToken);
        await SeedPhotosAsync(cancellationToken);
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

    private async Task SeedPhotosAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<EquipmentPhoto> photos =
            DevelopmentData.CreateEquipmentPhotos();
        Guid[] photoIds = photos.Select(photo => photo.Id).ToArray();

        HashSet<Guid> existingPhotoIds = await DbContext.EquipmentPhotos
            .AsNoTracking()
            .Where(photo => photoIds.Contains(photo.Id))
            .Select(photo => photo.Id)
            .ToHashSetAsync(cancellationToken);

        byte[] content = Convert.FromBase64String(PlaceholderPngBase64);
        await ObjectStorage.EnsureReadyAsync(cancellationToken);

        foreach (EquipmentPhoto photo in photos.Where(
                     photo => !existingPhotoIds.Contains(photo.Id)))
        {
            await using var stream = new MemoryStream(content, writable: false);
            await ObjectStorage.StoreAsync(
                photo.ObjectKey,
                stream,
                photo.ContentType,
                cancellationToken
            );

            await DbContext.EquipmentPhotos.AddAsync(photo, cancellationToken);
        }

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
