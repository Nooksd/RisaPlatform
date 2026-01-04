namespace Auth.Domain.Enums;

/// <summary>
/// Tipo de conta no sistema
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Dono do tenant - Acesso total a todos os módulos
    /// </summary>
    TenantOwner = 0,

    /// <summary>
    /// Usuário interno do tenant - Acesso conforme permissões
    /// </summary>
    TenantUser = 1,

    /// <summary>
    /// Usuário público de um módulo específico
    /// </summary>
    PublicUser = 2
}
