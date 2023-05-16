namespace back_end;

//  projects.name,
//  componentsName
//  components.quantity,
//  reservation.quantity

public class MissingComponent
{
    public String ProjectName {get;set;} = String.Empty;
    
    public String Name {get;set;} = String.Empty;
    
    public int ComponentQuantity {get;set;}
    
    public int Reserved {get;set;}
}