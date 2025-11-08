namespace HealthCalendar.Shared
{
    // enum used to communicate Status from Repos to Controllers
    public enum OperationStatus
    {
        Ok,
        Error,
        NotFound,
        NotAcceptable
    }
}