package heir;
import java.util.*;
/** Immutable, visible starter-card adaptations derived from the active heir. */
public final class StarterGenes {
    public final int damage,block,draw,heal,poison;
    public final boolean mercy;
    public final List<String> traits=new ArrayList<>();
    public StarterGenes(Data.Row card,Collection<String> inherited){
        boolean basic=card.s("rarity").equals("BASIC"),attack=card.s("type").equals("ATTACK"),skill=card.s("type").equals("SKILL");int d=card.i("damage"),b=card.i("block"),drawBonus=0,h=0,p=0,mercyBlock=0;boolean convert=false;
        if(basic)for(Data.Row gene:Data.rows("starter_genes"))if(inherited.contains(gene.s("id"))){
            boolean applies=attack&&(gene.i("damage")!=0||gene.i("heal")!=0||gene.b("mercy"))||skill&&(gene.i("block")!=0||gene.i("draw")!=0);
            if(!applies)continue;traits.add(gene.s("id"));
            if(attack){d+=gene.i("damage");h+=gene.i("heal");if(gene.b("mercy")){convert=true;mercyBlock+=gene.i("block");p+=gene.i("poison");}}
            if(skill){b+=gene.i("block");drawBonus+=gene.i("draw");}
        }
        damage=convert?0:Math.max(0,d);block=convert?mercyBlock:Math.max(0,b);draw=drawBonus;heal=h;poison=p;mercy=convert;
    }
}
