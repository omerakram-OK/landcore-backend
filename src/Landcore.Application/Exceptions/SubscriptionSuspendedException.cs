namespace Landcore.Application.Exceptions;

public sealed class SubscriptionSuspendedException : AppException
{
    public SubscriptionSuspendedException(string message)
        : base("SUBSCRIPTION_SUSPENDED", message, 403)
    {
    }
}
