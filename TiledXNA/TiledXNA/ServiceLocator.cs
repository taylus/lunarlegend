using System;
using System.Collections.Generic;

/// <summary>
/// This class serves as a service locator for anything in the project.
/// Registering anything here effectively makes it a global singleton.
/// </summary>
public static class ServiceLocator
{
    private static IDictionary<Type, object> services = new Dictionary<Type, object>();

    public static void Register<T>(T t)
    {
        if (!services.ContainsKey(typeof(T)))
            services.Add(typeof(T), t);
        else
            throw new ArgumentException("ServiceLocator already has a registered service of type " + typeof(T).Name + ". Unregister it first.");
    }

    public static void Unregister<T>(T t)
    {
        services.Remove(typeof(T));
    }

    public static T Get<T>()
    {
        if (services.ContainsKey(typeof(T)))
            return (T)services[typeof(T)];
        else
            throw new ArgumentException("ServiceLocator does not have a registered service of type " + typeof(T).Name + ".");
    }
}
