using System.Linq.Expressions;
using System.Reflection;

namespace SimpleNetAuth.Tools;

public static class PropertyCopy
{
    public static void Copy<TSource, TTarget>(TSource source, TTarget target, bool copyObjects = false)
        where TSource : class
        where TTarget : class
    {
        PropertyCopier<TSource, TTarget>.Copy(source, target, copyObjects);
    }
}

public static class PropertyCopy<TTarget> where TTarget : class, new()
{
    public static TTarget CopyFrom<TSource>(TSource source, bool copyObjects = false) where TSource : class
    {
        return PropertyCopier<TSource, TTarget>.Copy(source, copyObjects);
    }
}

internal static class PropertyCopier<TSource, TTarget>
{
    private static readonly Func<TSource, bool, TTarget> creator;
    private static readonly List<PropertyInfo> sourceProperties = new();
    private static readonly List<PropertyInfo> targetProperties = new();
    private static readonly Exception initializationException;

    internal static TTarget Copy(TSource source, bool copyObjects = false)
    {
        if (initializationException != null)
            throw initializationException;

        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return creator(source, copyObjects);
    }

    internal static void Copy(TSource source, TTarget target, bool copyObjects = false)
    {
        if (initializationException != null)
            throw initializationException;

        if (source == null)
            throw new ArgumentNullException(nameof(source));

        for (var i = 0; i < sourceProperties.Count; i++)
        {
            var sourceValue = sourceProperties[i].GetValue(source);
            var targetProperty = targetProperties[i];

            // ZOMBIE: Debug code - Uncomment if the property in question isn't copying:
            //if (targetProperty.Name == "SqFt")
            //{
            //    var j = 0;
            //}

            // Check if the property is a simple type
            if (IsSimpleType(targetProperty.PropertyType) || copyObjects)
            {
                targetProperty.SetValue(target, sourceValue, null);
            }
        }
    }

    static PropertyCopier()
    {
        try
        {
            creator = BuildCreator();
            initializationException = null;
        }
        catch (Exception e)
        {
            creator = null;
            initializationException = e;
        }
    }

    private static Func<TSource, bool, TTarget> BuildCreator()
    {
        var sourceParameter = Expression.Parameter(typeof(TSource), "source");
        var copyObjectsParameter = Expression.Parameter(typeof(bool), "copyObjects");

        var bindings = new List<MemberBinding>();

        foreach (var sourceProperty in typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!sourceProperty.CanRead) continue;

            var targetProperty = typeof(TTarget).GetProperty(sourceProperty.Name);
            if (targetProperty == null || !targetProperty.CanWrite ||
                (targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0 ||
                !targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                continue;

            // Add logic to only copy simple types or all properties based on the flag
            var isSimple = IsSimpleType(targetProperty.PropertyType);
            var condition = Expression.OrElse(copyObjectsParameter, Expression.Constant(isSimple));

            var bindExpression = Expression.Bind(targetProperty,
                Expression.Condition(condition,
                    Expression.Property(sourceParameter, sourceProperty),
                    Expression.Default(targetProperty.PropertyType)));

            bindings.Add(bindExpression);

            sourceProperties.Add(sourceProperty);
            targetProperties.Add(targetProperty);
        }

        var initializer = Expression.MemberInit(Expression.New(typeof(TTarget)), bindings);
        return Expression.Lambda<Func<TSource, bool, TTarget>>(initializer, sourceParameter, copyObjectsParameter).Compile();
    }

    private static bool IsSimpleType(Type type)
    {
        // Check if the type is nullable and get the underlying type
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType.IsPrimitive ||
               underlyingType.IsEnum ||
               underlyingType == typeof(string) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(bool) ||
               underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(TimeSpan) ||
               underlyingType == typeof(Guid);
    }
}