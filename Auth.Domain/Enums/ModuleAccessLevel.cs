namespace Auth.Domain.Enums;

/// <summary>
/// Nível de acesso a um módulo
/// </summary>
public enum ModuleAccessLevel
{
    /// <summary>
    /// Sem acesso ao módulo
    /// </summary>
    NoAccess = 0,

    /// <summary>
    /// Pode visualizar dados do módulo
    /// </summary>
    View = 1,

    /// <summary>
    /// Pode editar dados do módulo
    /// </summary>
    Edit = 2,

    /// <summary>
    /// Acesso administrativo ao módulo
    /// </summary>
    Admin = 3
}