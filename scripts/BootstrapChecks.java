import heir.*;
import com.evacipated.cardcrawl.modthespire.*;
import java.nio.file.*;
import java.util.*;

/** Uses the packaged reader against owned game files, with isolated data only. */
public final class BootstrapChecks {
    static int checks;
    static void check(boolean value,String message){checks++;if(!value)throw new AssertionError(message);}
    public static void main(String[] args)throws Exception {
        Path jar=Paths.get(args[0]).toAbsolutePath(),host=Paths.get(args[1]).toAbsolutePath(),data=Paths.get(args[2]).toAbsolutePath();
        Path runtime=ContentBootstrap.bundle(jar);check(runtime!=null,"Workshop sibling runtime discovery");
        ModInfo info=ModInfo.ReadModInfo(jar.toFile());info.jarURL=jar.toUri().toURL();Loader.MODINFOS=new ModInfo[]{info};Loader.STS_JAR=host.resolve("desktop-1.0.jar").toString();
        List<String> windows=ContentBootstrap.command(runtime,host,data,"Windows 11",Collections.singletonMap("HEIR_RL2_DIR","C:/a path with spaces/Rogue Legacy 2"));
        check(windows.get(0).endsWith("ReadRogueLegacy.exe"),"Windows reader selection");check(windows.contains("C:/a path with spaces/Rogue Legacy 2"),"Explicit game stays one process argument");check(windows.contains(data.resolve("cache").toString()),"Isolated cache argument");
        List<String> linux=ContentBootstrap.command(runtime,host,data,"Linux",Collections.<String,String>emptyMap());
        check(linux.get(0).endsWith("python3.12")&&linux.get(1).endsWith("load_rl.py"),"Native Linux reader selection");
        try{ContentBootstrap.command(runtime,host,data,"Mac OS X",Collections.<String,String>emptyMap());throw new AssertionError("Unsupported platform accepted");}catch(IllegalStateException expected){check(expected.getMessage().contains("supports Windows"),"Unsupported platform explanation");}
        if(args.length>3&&args[3].equals("failure")){
            java.io.ByteArrayOutputStream output=new java.io.ByteArrayOutputStream();java.io.PrintStream previous=System.err;
            try{
                System.setErr(new java.io.PrintStream(output,true,"UTF-8"));
                try{ContentBootstrap.prepare(data);throw new AssertionError("Missing owned installation accepted");}catch(IllegalStateException expected){
                    String fullLog=new String(Files.readAllBytes(data.resolve("content-preparation.log")),java.nio.charset.StandardCharsets.UTF_8);
                    check(expected.getMessage().contains(fullLog),"Error details include the entire current log");
                    check(output.toString("UTF-8").contains(fullLog),"Debug output includes the entire current log");
                    check(expected.getMessage().contains("content-preparation.log"),"Reader failure points to diagnostics");
                }
            }finally{System.setErr(previous);}
            check(!ContentBootstrap.complete(data),"Failure cannot create a valid cache");check(!Files.exists(data.resolve("family.json")),"Failure cannot create family state");
            info.jarURL=data.resolve("missing-runtime/mods/HeirOfTheSpire.jar").toUri().toURL();output.reset();
            try{
                System.setErr(new java.io.PrintStream(output,true,"UTF-8"));
                try{ContentBootstrap.prepare(data);throw new AssertionError("Missing runtime accepted");}catch(IllegalStateException expected){
                    check(!expected.getMessage().contains("--- Spire Legacy content preparation log ---"),"Error details do not display an old attempt's log");
                    check(!output.toString("UTF-8").contains("--- Spire Legacy content preparation log ---"),"Debug output does not display stale reader diagnostics");
                }
            }finally{System.setErr(previous);info.jarURL=jar.toUri().toURL();}
        }else{
            check(!ContentBootstrap.complete(data),"Fresh test cache initially absent");ContentBootstrap.prepare(data);check(ContentBootstrap.complete(data),"Fresh cache prepared by packaged reader");
            byte[] signature=Files.readAllBytes(data.resolve("cache/source.json"));ContentBootstrap.prepare(data);check(Arrays.equals(signature,Files.readAllBytes(data.resolve("cache/source.json"))),"Repeat launch verifies existing cache");
            Files.delete(data.resolve("cache/Icons_Classes_BowClass.png"));check(!ContentBootstrap.complete(data),"Missing sprite invalidates completeness");ContentBootstrap.prepare(data);check(ContentBootstrap.complete(data),"Reader repairs missing sprite");
            check(!Files.exists(data.resolve("family.json")),"Preparation preserves family state");
        }
        String log=new String(Files.readAllBytes(data.resolve("content-preparation.log")),java.nio.charset.StandardCharsets.UTF_8);
        check(log.contains("Spire Legacy bootstrap | OS:"),"Bootstrap records its actual platform");
        check(log.contains("Spire Legacy content reader"),"Bootstrap retains reader diagnostics");
        check(log.contains("Checking RL2 installation:"),"Reader records the installation it checked");
        System.out.println("Passed "+checks+" packaged bootstrap checks.");
    }
}
