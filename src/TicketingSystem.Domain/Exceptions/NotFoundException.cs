namespace TicketingSystem.Domain.Exceptions;

public class NotFoundException(string message) : Exception(message);
