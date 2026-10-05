namespace TaskBoard.Api;

public sealed class TaskStore
{
    private readonly object gate = new();
    private readonly Dictionary<Guid, TaskItem> tasks = new();
    public TaskItem[] List() { lock(gate) return tasks.Values.ToArray(); }
    public TaskItem? Get(Guid id) { lock(gate) return tasks.GetValueOrDefault(id); }
    public (TaskItem? Item, string? Error) Create(TaskInput input)
    {
        lock(gate)
        {
            if(tasks.Values.Any(t => t.Title.Equals(input.Title,StringComparison.OrdinalIgnoreCase))) return (null,"duplicate_title");
            var item = new TaskItem(Guid.NewGuid(),input.Title,input.Description,input.Priority,input.IsCompleted);
            tasks.Add(item.Id,item);return (item,null);
        }
    }
    public (TaskItem? Item, string? Error) Update(Guid id, TaskInput input)
    {
        lock(gate)
        {
            if(!tasks.ContainsKey(id)) return (null,"not_found");
            if(tasks.Values.Any(t => t.Id != id && t.Title.Equals(input.Title,StringComparison.OrdinalIgnoreCase))) return (null,"duplicate_title");
            var item = new TaskItem(id,input.Title,input.Description,input.Priority,input.IsCompleted);
            tasks[id]=item;return (item,null);
        }
    }
    public bool Delete(Guid id) { lock(gate) return tasks.Remove(id); }
}
