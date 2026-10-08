package heir;
import java.nio.file.*;
import java.util.Map;
/** Stable platform-native family/cache location, shared with both launchers. */
public final class PlatformPaths {
    public static Path data(Map<String,String> env,String home){
        String override=env.get("HEIR_DATA_DIR");if(override!=null&&!override.trim().isEmpty())return Paths.get(override);
        String local=env.get("LOCALAPPDATA");if(local!=null&&!local.isEmpty())return Paths.get(local,"HeirOfTheSpire");
        String xdg=env.get("XDG_DATA_HOME");return xdg!=null&&!xdg.isEmpty()?Paths.get(xdg,"HeirOfTheSpire"):Paths.get(home,".local","share","HeirOfTheSpire");
    }
}
