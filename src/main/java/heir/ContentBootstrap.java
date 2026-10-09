package heir;

import com.evacipated.cardcrawl.modthespire.Loader;
import com.evacipated.cardcrawl.modthespire.ModInfo;
import java.nio.file.*;
import java.nio.channels.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.TimeUnit;

/** Prepare owned content before any class, color or character texture is registered. */
public final class ContentBootstrap {
    public static Path bundle(Path modJar){
        Path parent=modJar.toAbsolutePath().getParent();
        Path workshop=parent.resolve("SpireLegacyRuntime");
        if(Files.isDirectory(workshop))return workshop;
        Path installed=parent.getParent().resolve("HeirOfTheSpire");
        return Files.isDirectory(installed)?installed:null;
    }
    public static List<String> command(Path runtime,Path host,Path data,String os,Map<String,String> env)throws Exception {
        List<String> args=new ArrayList<>();
        if(os.toLowerCase(Locale.ROOT).contains("windows"))args.add(runtime.resolve("Reader/ReadRogueLegacy.exe").toString());
        else if(os.toLowerCase(Locale.ROOT).contains("linux")){
            Path python=runtime.resolve("ReaderLinux/bin/python3.12");
            if(Files.isRegularFile(python)&&!Files.isExecutable(python)&&!python.toFile().setExecutable(true,true))throw new IllegalStateException("Cannot make the bundled Linux content reader executable: "+python);
            args.add(python.toString());args.add(runtime.resolve("load_rl.py").toString());
        }else throw new IllegalStateException("Automatic content preparation supports Windows x64 and Linux x86_64, including Steam Deck.");
        for(String path:args)if(!Files.isRegularFile(Paths.get(path)))throw new IllegalStateException("The content reader is missing: "+path+". Reinstall or re-subscribe to Spire Legacy.");
        Collections.addAll(args,"--host",host.toString(),"--sheet",runtime.resolve("assets.json").toString(),"--cache",data.resolve("cache").toString(),"--no-dialog");
        String game=env.get("HEIR_RL2_DIR");if(game!=null&&!game.trim().isEmpty())Collections.addAll(args,"--game",game);
        return args;
    }
    public static boolean complete(Path data){
        Path cache=data.resolve("cache");
        if(!Files.isRegularFile(cache.resolve("source.json")))return false;
        for(String sheet:Arrays.asList("assets","host_assets"))for(Data.Row row:Data.rows(sheet))if(!Files.isRegularFile(cache.resolve(row.s("output"))))return false;
        return Files.isRegularFile(cache.resolve("hero.png"));
    }
    private static Path modJar()throws Exception {
        if(Loader.MODINFOS!=null)for(ModInfo info:Loader.MODINFOS)if("heir".equals(info.ID)&&info.jarURL!=null)return Paths.get(info.jarURL.toURI());
        return Paths.get(ContentBootstrap.class.getProtectionDomain().getCodeSource().getLocation().toURI());
    }
    public static void prepare(Path data){
        Path log=data.resolve("content-preparation.log");
        boolean logStarted=false;
        try{
            Path runtime=bundle(modJar());
            if(runtime==null){
                // Older manual JAR installations can keep an already prepared complete cache.
                if(complete(data))return;
                throw new IllegalStateException("The Spire Legacy content reader is missing. Install the complete package or re-subscribe to the Workshop item.");
            }
            Path host=Paths.get(Loader.STS_JAR).toAbsolutePath().getParent();
            if(!Files.isRegularFile(host.resolve("desktop-1.0.jar")))throw new IllegalStateException("Slay the Spire's game files could not be located. Launch with mods through Steam.");
            Files.createDirectories(data);
            try(FileChannel channel=FileChannel.open(data.resolve("content-preparation.lock"),StandardOpenOption.CREATE,StandardOpenOption.WRITE);FileLock lock=channel.lock()){
                List<String> args=command(runtime,host,data,System.getProperty("os.name"),System.getenv());
                Files.write(log,Arrays.asList("Spire Legacy bootstrap | OS: "+System.getProperty("os.name"),"Content runtime: "+runtime,"Reader command: "+args),StandardCharsets.UTF_8);
                logStarted=true;
                ProcessBuilder builder=new ProcessBuilder(args).directory(runtime.toFile()).redirectErrorStream(true).redirectOutput(ProcessBuilder.Redirect.appendTo(log.toFile()));
                builder.environment().put("PYTHONPATH",runtime.resolve("ReaderLinux/site").toString());
                System.out.println("HEIR: preparing installed Rogue Legacy 2 content; log: "+log);
                Process child=builder.start();
                try{
                    if(!child.waitFor(180,TimeUnit.SECONDS)){child.destroyForcibly();throw new IllegalStateException("Content preparation took too long. Check "+log+" and try again.");}
                    if(child.exitValue()!=0)throw new IllegalStateException("Content preparation failed. Check "+log+" for the reader version, detected paths and file-access details.");
                }catch(InterruptedException interrupted){child.destroyForcibly();Thread.currentThread().interrupt();throw interrupted;}
                if(!complete(data))throw new IllegalStateException("Content preparation did not produce all required images. Check "+log+" and reinstall the complete mod package.");
            }
        }catch(Exception failure){
            String diagnostics="";
            if(logStarted)try{
                diagnostics="\n\n--- Spire Legacy content preparation log ---\n"+new String(Files.readAllBytes(log),StandardCharsets.UTF_8)+"\n--- End content preparation log ---";
            }catch(Exception readFailure){diagnostics="\nCould not read content preparation log: "+readFailure.getMessage();}
            String message="Spire Legacy could not prepare your installed game content. "+failure.getMessage()+diagnostics;
            // ModTheSpire captures stderr in debug output and the exception in
            // its error details. Include every line in both places for Deck users.
            System.err.println(message);
            throw new IllegalStateException(message,failure);
        }
    }
}
