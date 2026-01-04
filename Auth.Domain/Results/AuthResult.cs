namespace Auth.Domain.Results;

public sealed record AuthResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AuthError? Error { get; }

    private AuthResult(T value)
    {
        IsSuccess = true;
        Value = value;
        Error = null;
    }

    private AuthResult(AuthError error)
    {
        IsSuccess = false;
        Value = default;
        Error = error;
    }

    public static AuthResult<T> Success(T value) => new(value);
    public static AuthResult<T> Failure(AuthError error) => new(error);

    public static implicit operator AuthResult<T>(T value) => Success(value);
    public static implicit operator AuthResult<T>(AuthError error) => Failure(error);
}

public sealed record AuthError(string Code, string Message)
{
    public static AuthError InvalidCredentials => new("AUTH.INVALID_CREDENTIALS", "Invalid email or password");
    public static AuthError UserNotFound => new("AUTH.USER_NOT_FOUND", "User not found");
    public static AuthError UserAlreadyExists => new("AUTH.USER_ALREADY_EXISTS", "User with this email already exists");
    public static AuthError UserInactive => new("AUTH.USER_INACTIVE", "User account is inactive");
    public static AuthError UserDeleted => new("AUTH.USER_DELETED", "User account has been deleted");
    public static AuthError InvalidToken => new("AUTH.INVALID_TOKEN", "Invalid or expired token");
    public static AuthError InsufficientPermissions => new("AUTH.INSUFFICIENT_PERMISSIONS", "Insufficient permissions to perform this action");
    public static AuthError InvalidModule => new("AUTH.INVALID_MODULE", "Invalid module specified");
    public static AuthError TenantNotFound => new("AUTH.TENANT_NOT_FOUND", "Tenant not found");
    public static AuthError OAuthError => new("AUTH.OAUTH_ERROR", "OAuth authentication failed");

    public static AuthError Custom(string code, string message) => new(code, message);
}