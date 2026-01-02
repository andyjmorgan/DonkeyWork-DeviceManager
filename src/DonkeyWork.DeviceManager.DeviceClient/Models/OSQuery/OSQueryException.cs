namespace DonkeyWork.DeviceManager.DeviceClient.Models.OSQuery;

/// <summary>
/// Base exception for OSQuery-related errors.
/// </summary>
public class OSQueryException : Exception
{
    public OSQueryException(string message) : base(message)
    {
    }

    public OSQueryException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when OSQuery is not available on the system.
/// </summary>
public class OSQueryNotAvailableException : OSQueryException
{
    public OSQueryNotAvailableException(string message) : base(message)
    {
    }
}

/// <summary>
/// Exception thrown when an OSQuery query execution exceeds the timeout.
/// </summary>
public class OSQueryTimeoutException : OSQueryException
{
    public OSQueryTimeoutException(string message) : base(message)
    {
    }
}
