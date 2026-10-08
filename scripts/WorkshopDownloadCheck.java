import com.codedisaster.steamworks.*;
import java.lang.reflect.Proxy;
import java.util.Collection;
/** Creator-side verification with the official uploader's Steam SDK. */
public final class WorkshopDownloadCheck {
    public static void main(String[] args)throws Exception {
        if(!SteamAPI.init())throw new IllegalStateException("Start Steam in the creator account first");
        try{
            SteamUGCCallback callback=(SteamUGCCallback)Proxy.newProxyInstance(SteamUGCCallback.class.getClassLoader(),new Class<?>[]{SteamUGCCallback.class},(proxy,method,values)->null);
            SteamUGC ugc=new SteamUGC(callback);
            for(String id:args){
                SteamPublishedFileID item=new SteamPublishedFileID(Long.parseLong(id));
                if(!ugc.downloadItem(item,true))throw new IllegalStateException("Steam rejected download "+id);
                long deadline=System.currentTimeMillis()+120000;
                while(System.currentTimeMillis()<deadline){
                    SteamAPI.runCallbacks();Collection<SteamUGC.ItemState> state=ugc.getItemState(item);
                    if(state.contains(SteamUGC.ItemState.Installed)&&!state.contains(SteamUGC.ItemState.NeedsUpdate)&&!state.contains(SteamUGC.ItemState.Downloading)&&!state.contains(SteamUGC.ItemState.DownloadPending)){
                        SteamUGC.ItemInstallInfo info=new SteamUGC.ItemInstallInfo();if(!ugc.getItemInstallInfo(item,info))throw new IllegalStateException("No install folder "+id);
                        System.out.println(id+" installed at "+info.getFolder());break;
                    }
                    Thread.sleep(250);
                }
                if(System.currentTimeMillis()>=deadline)throw new IllegalStateException("Steam download timed out: "+id);
            }
            ugc.dispose();
        }finally{SteamAPI.shutdown();}
    }
}
