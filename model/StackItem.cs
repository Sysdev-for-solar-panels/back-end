namespace back_end;

//stack.id,stack.component_id,components.name,components.quantity,components.max_quantity

public class StackItem
{
    public int StackId { get; set; }

    public int? ComponentId { get; set; }

    public string? ComponentName {get; set;}

    public int? ComponentQuantity {get; set;}

    public int? ComponentMaxQuantity {get; set;}

}
