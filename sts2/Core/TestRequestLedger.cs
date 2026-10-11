using System.Text;
namespace SpireLegacy;
public sealed class TestRequestLedger
{
    private readonly HashSet<string> claimed=new(StringComparer.Ordinal);
    public void Claim(string id,string path)
    {
        if(!Guid.TryParseExact(id,"N",out _))throw new InvalidDataException("Oracle requestId must be a UUID without separators.");
        if(File.Exists(path))foreach(var prior in File.ReadLines(path).Take(10001))claimed.Add(prior);
        if(claimed.Count>=10000)throw new InvalidDataException("Oracle receipt limit reached; use a fresh disposable profile.");
        if(claimed.Contains(id))throw new InvalidDataException("Oracle request was already claimed; do not retry a possibly applied mutation.");
        using(var file=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.Read))
        {file.Write(Encoding.UTF8.GetBytes(id+"\n"));file.Flush(true);}
        claimed.Add(id);
    }
}
