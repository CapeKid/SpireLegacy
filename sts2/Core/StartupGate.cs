namespace SpireLegacy;

/// <summary>Initialization is one attempt, with no duplicate hooks or implicit retry after failure.</summary>
public sealed class StartupGate
{
    private bool entering,ready;
    private Exception? failed;
    private readonly object sync=new();
    public bool Run(Action initialize)
    {
        lock(sync){
        if(ready)return false;
        if(failed!=null)throw new InvalidOperationException("Spire Legacy initialization previously failed; restart after correcting the reported dependency/API.",failed);
        if(entering)throw new InvalidOperationException("Spire Legacy initialization was called recursively.");
        entering=true;
        try{initialize();ready=true;return true;}
        catch(Exception error){failed=error;throw;}
        finally{entering=false;}
        }
    }
}
