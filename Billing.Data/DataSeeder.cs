using Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Billing.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(BillingDbContext context, ILogger logger, CancellationToken ct = default)
    {
        await SeedModulesAsync(context, logger, ct);
    }

    private static async Task SeedModulesAsync(BillingDbContext context, ILogger logger, CancellationToken ct)
    {
        if (await context.Modules.AnyAsync(ct))
        {
            logger.LogInformation("Módulos já existem, pulando seed");
            return;
        }

        logger.LogInformation("Criando módulos iniciais...");

        // Módulo CRM - R$ 50,00 por usuário/mês
        var crmModule = new Module("CRM", "Gestão de Clientes", 50.00m, "CRM completo, funil de vendas, gestão de leads");
        context.Modules.Add(crmModule);

        await context.SaveChangesAsync(ct);

        // Adiciona descontos por quantidade para cada módulo
        var modules = await context.Modules.ToListAsync(ct);

        foreach (var module in modules)
        {
            // 10+ usuários = 5% desconto
            context.ModuleQuantityDiscounts.Add(new ModuleQuantityDiscount(module.Id, 10, 0.05m));

            // 25+ usuários = 10% desconto
            context.ModuleQuantityDiscounts.Add(new ModuleQuantityDiscount(module.Id, 25, 0.10m));

            // 50+ usuários = 15% desconto
            context.ModuleQuantityDiscounts.Add(new ModuleQuantityDiscount(module.Id, 50, 0.15m));

            // 100+ usuários = 20% desconto
            context.ModuleQuantityDiscounts.Add(new ModuleQuantityDiscount(module.Id, 100, 0.20m));
        }

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Módulos criados com sucesso: CRM (R$50) por usuário/mês");
        logger.LogInformation("Descontos por quantidade configurados: 10+ (5%), 25+ (10%), 50+ (15%), 100+ (20%)");
    }
}
