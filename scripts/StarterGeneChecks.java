import heir.*;
import java.util.*;
public final class StarterGeneChecks {
    private static int checks=0;
    private static void check(boolean yes,String message){checks++;if(!yes)throw new AssertionError(message);}
    private static StarterGenes genes(String card,String...traits){return new StarterGenes(Data.row("cards",card),Arrays.asList(traits));}
    public static void main(String[] args){
        StarterGenes heavy=genes("knight_strike","large","weapon");check(heavy.damage==11&&heavy.block==2,"offensive traits stack without lowering attack-card Block");
        check(genes("knight_guard","large","weapon").block==3,"defensive tradeoffs stack");
        check(genes("mage_guard","magic").draw==1,"Bookish modifies starter skill draw");check(genes("mage_strike","magic").damage==4,"Bookish attack tradeoff");
        check(genes("ranger_strike","vampire").heal==1,"starter lifesteal");
        StarterGenes mercy=genes("shield","cantattack","weapon");check(mercy.mercy&&mercy.damage==0&&mercy.block==4&&mercy.poison==2,"Pacifist conversion overrides offensive genes");
        check(genes("ranger_guard","smallhitbox","lowergravity").draw==2,"two draw traits stack");check(genes("knight_guard","small").block==7,"Dwarfism starter defense");
        for(Data.Row card:Data.rows("cards"))if(!card.s("rarity").equals("BASIC")){StarterGenes unchanged=genes(card.s("id"),"cantattack","weapon","magic","vampire");check(unchanged.damage==card.i("damage")&&unchanged.block==card.i("block")&&unchanged.draw==0&&unchanged.heal==0&&!unchanged.mercy,"reward cards remain independent of starter genetics");}
        System.out.println("Passed "+checks+" starter-genetics checks.");
    }
}
