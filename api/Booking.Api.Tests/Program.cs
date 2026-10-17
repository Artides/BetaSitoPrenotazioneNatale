// Optional in-process runner for restricted environments that prohibit VSTest's local sockets.
// The same Fact/Theory methods remain compatible with normal `dotnet test`.
using System.Reflection;
using Xunit;
int passed=0,failed=0;
foreach(var type in Assembly.GetExecutingAssembly().GetTypes().Where(t=>t.Namespace=="Booking.Api.Tests" && t.IsPublic)) {
    foreach(var method in type.GetMethods().Where(m=>m.GetCustomAttribute<FactAttribute>()!=null)) {
        var rows=method.GetCustomAttributesData().Where(a=>a.AttributeType==typeof(InlineDataAttribute)).Select(a=>((IEnumerable<CustomAttributeTypedArgument>)a.ConstructorArguments[0].Value!).Select(v=>v.Value).ToArray()).ToArray();
        if(rows.Length==0) rows=[Array.Empty<object>()];
        foreach(var row in rows) {
            try { var instance=Activator.CreateInstance(type);var result=method.Invoke(instance,row);if(result is Task task) await task;passed++;Console.WriteLine($"PASS {type.Name}.{method.Name}({string.Join(",",row)})"); }
            catch(Exception ex) {failed++;Console.Error.WriteLine($"FAIL {type.Name}.{method.Name}: {(ex.InnerException??ex)}");}
        }
    }
}
Console.WriteLine($"{passed} passed, {failed} failed");
return failed==0?0:1;
