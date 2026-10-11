using System.Text.RegularExpressions;
namespace SpireLegacy;
public static class DiagnosticText
{
    public static string Describe(Exception error,Func<string> metadata)
    {
        string context;
        try{context=metadata();}catch(Exception failure){context="Diagnostic metadata unavailable ("+failure.GetType().Name+")";}
        return Regex.Replace(context+"\n"+error,@"(?i)(\bBearer\s+|\b(?:api[_-]?key|token|password)\s*[=:]\s*)[^\s,;]+","$1[redacted]");
    }
}
