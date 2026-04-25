namespace ApiApp.Helpers;

public static class ModelTouch
{
    public static void Touch(object entity)
    {
        var now = DateTime.UtcNow;
        var t = entity.GetType();
        var p1 = t.GetProperty("LastUpdate");
        if (p1 is not null && p1.PropertyType == typeof(DateTime)) p1.SetValue(entity, now);
        var p2 = t.GetProperty("last_update");
        if (p2 is not null && p2.PropertyType == typeof(DateTime)) p2.SetValue(entity, now);
    }
}
