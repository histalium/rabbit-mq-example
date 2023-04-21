namespace RabbitMQExample;

public class Event<T>
{
    public Guid StreamId { get; set; }
    public string? EventName { get; set; }
    public T? Message { get; set; }
}

public class EventWithOnlyName
{
    public string? EventName { get; set; }
}

public class DescriptionChanged
{
    public string? Description { get; set; }
}

public class AmountIncremented
{
    public int Increment { get; set; }
}

public class AmountDecremented
{
    public int Decrement { get; set; }
}
