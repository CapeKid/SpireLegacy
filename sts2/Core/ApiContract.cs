using System.Reflection;
using System.Runtime.ExceptionServices;
namespace SpireLegacy;

public static class ApiContract
{
    public static MethodInfo Require(Type type,string name,Type returnBase,params Type[][] variants)
    {
        var candidates=type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);
        foreach(var signature in variants){
            var matches=candidates.Where(m=>m.Name==name&&!m.ContainsGenericParameters&&m.GetParameters().Select(p=>p.ParameterType).SequenceEqual(signature)).ToArray();
            if(matches.Length>1)throw new AmbiguousMatchException("Ambiguous supported API: "+type.FullName+"."+name);
            if(matches.Length==1&&returnBase.IsAssignableFrom(matches[0].ReturnType))return matches[0];
        }
        var available=string.Join("; ",candidates.Where(m=>m.Name==name).Select(m=>m.ToString()));
        var expected=string.Join(" or ",variants.Select(v=>"("+string.Join(", ",v.Select(t=>t.FullName))+") -> "+returnBase.FullName));
        throw new MissingMethodException($"Unsupported API {type.FullName}.{name}. Expected {expected}. Available: {available}");
    }
    public static object? Invoke(MethodInfo method,object? instance,object?[] args)
    {
        try{return method.Invoke(instance,args);}
        catch(TargetInvocationException error) when(error.InnerException!=null){ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
    }
}
