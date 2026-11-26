namespace UniEventsAPI.Services;

public class ApiError(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
