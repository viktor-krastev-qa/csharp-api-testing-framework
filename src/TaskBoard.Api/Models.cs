namespace TaskBoard.Api;
public record TaskInput(string Title, string Description, string Priority, bool IsCompleted);
public record TaskItem(Guid Id, string Title, string Description, string Priority, bool IsCompleted);
public record ApiError(string Error, string[] Details);
