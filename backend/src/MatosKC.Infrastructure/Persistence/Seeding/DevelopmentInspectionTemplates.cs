namespace MatosKC.Infrastructure.Persistence.Seeding;

using MatosKC.Domain.Entities.Inspection;
using Microsoft.EntityFrameworkCore;

public static class DevelopmentInspectionTemplates
{
    public static IReadOnlyList<InspectionTemplate> Create()
    {
        (string Title, string Detail)[][] definitions =
        [
            [
                ("Souffleuse", "Éléments à vérifier :\n\t• Bloc de soufflage présent et fixé\n\t• Boîtier d’alimentation présent\n\t• Trémie propre et sans fissure\n\t• Grille de protection en place"),
                ("Tuyaux et accessoires", "Éléments à vérifier :\n\t• Tuyaux de soufflage présents\n\t• Raccords et colliers complets\n\t• Télécommande et câble présents\n\t• Absence de déchirure et d’obstruction"),
                ("Alimentation et fonctionnement", "Éléments à vérifier :\n\t• Câble et prise sans détérioration\n\t• Commandes et arrêt d’urgence fonctionnels\n\t• Mise en marche sans bruit anormal")
            ],
            [
                ("Fourches et mât", "Éléments à vérifier :\n\t• Fourches sans déformation\n\t• Verrouillage des fourches\n\t• Chaînes et galets en état\n\t• Absence de fuite hydraulique"),
                ("Poste de conduite", "Éléments à vérifier :\n\t• Ceinture et siège en état\n\t• Commandes et avertisseur fonctionnels\n\t• Rétroviseurs présents et propres"),
                ("Roulement et énergie", "Éléments à vérifier :\n\t• Pneumatiques sans détérioration\n\t• Freinage fonctionnel\n\t• Niveau de carburant ou charge relevé")
            ],
            [
                ("Fourches et châssis", "Éléments à vérifier :\n\t• Fourches sans déformation\n\t• Châssis sans fissure\n\t• Plaque de charge lisible"),
                ("Roues et timon", "Éléments à vérifier :\n\t• Galets et roues en état\n\t• Timon et poignée fixés\n\t• Direction libre"),
                ("Levage", "Éléments à vérifier :\n\t• Montée et descente fonctionnelles\n\t• Absence de fuite\n\t• Maintien de la charge")
            ],
            [
                ("Cabine et carrosserie", "Éléments à vérifier :\n\t• Carrosserie et pare-chocs\n\t• Pare-brise et rétroviseurs\n\t• Sièges et ceintures\n\t• Clés et documents présents"),
                ("Plateau ou benne", "Éléments à vérifier :\n\t• Ridelles et verrouillages\n\t• Plancher et points d’arrimage\n\t• Commandes de benne, si équipée"),
                ("Sécurité et fonctionnement", "Éléments à vérifier :\n\t• Pneumatiques et éclairage\n\t• Témoins du tableau de bord\n\t• Niveau de carburant et kilométrage relevés")
            ],
            [
                ("Carrosserie et vitrage", "Éléments à vérifier :\n\t• Panneaux et pare-chocs\n\t• Pare-brise et rétroviseurs\n\t• Portes et serrures"),
                ("Habitacle et chargement", "Éléments à vérifier :\n\t• Sièges et ceintures\n\t• Propreté de l’habitacle\n\t• Plancher et points d’arrimage\n\t• Clés et documents présents"),
                ("Sécurité et fonctionnement", "Éléments à vérifier :\n\t• Pneumatiques et éclairage\n\t• Témoins du tableau de bord\n\t• Niveau de carburant et kilométrage relevés")
            ]
        ];

        return definitions.Select((points, index) =>
        {
            Guid templateId = Guid.Parse($"40000000-0000-0000-0000-{index + 1:D12}");
            Guid categoryId = Guid.Parse($"10000000-0000-0000-0000-{index + 1:D12}");

            return new InspectionTemplate(templateId, points.Select((point, position) =>
                new InspectionTemplatePoint(
                    Guid.Parse($"41000000-0000-0000-0000-{(index + 1) * 100 + position:D12}"),
                    templateId, point.Title, point.Detail, null, position)), categoryId);
        }).ToArray();
    }

    public static async Task SeedAsync(MatosKCDbContext db, CancellationToken ct)
    {
        var categoryIds = await db.EquipmentCategories.Select(category => category.Id).ToListAsync(ct);
        var configured = await db.InspectionTemplates.Select(template => template.EquipmentCategoryId)
            .Distinct().ToListAsync(ct);

        // Never make a demonstration template supersede an existing administrator template.
        db.InspectionTemplates.AddRange(Create().Where(template =>
            categoryIds.Contains(template.EquipmentCategoryId)
            && !configured.Contains(template.EquipmentCategoryId)));

        await db.SaveChangesAsync(ct);
    }
}
