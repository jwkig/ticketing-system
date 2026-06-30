namespace TicketingSystem.Infrastructure.Options;

public sealed class Argon2HashingSettings
{
    public int MemorySize { get; set; } = 65536;
    public int Iterations { get; set; } = 3;
    public int DegreeOfParallelism { get; set; } = 1;
}
