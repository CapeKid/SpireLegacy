package heir;
import com.megacrit.cardcrawl.core.AbstractCreature;
import com.megacrit.cardcrawl.powers.*;

/** Original cards compose the host's tested combat powers. */
public final class CardPowers {
    public static AbstractPower create(String effect,AbstractCreature owner,int n){
        switch(effect){
            case "exhaust_damage":case "skill_block":case "skill_energy":case "discard_block":case "discard_energy":case "attack_vigor":case "power_draw":case "scry_block":return new BuildPower(effect,owner,n);
            case "evolve":return new EvolvePower(owner,n);
            case "fire_breathing":return new FireBreathingPower(owner,n);
            case "combust":return new CombustPower(owner,1,n);
            case "establishment":return new com.megacrit.cardcrawl.powers.watcher.EstablishmentPower(owner,n);
            case "burst":return new BurstPower(owner,n);
            case "noxious":return new NoxiousFumesPower(owner,n);
            case "foresight":return new com.megacrit.cardcrawl.powers.watcher.ForesightPower(owner,n);
            case "reservoir":case "quiver":return new HeirEnginePower(effect,n);
            case "metallicize":return new MetallicizePower(owner,n);
            case "juggernaut":return new JuggernautPower(owner,n);
            case "rupture":return new RupturePower(owner,n);
            case "feel_no_pain":return new FeelNoPainPower(owner,n);
            case "thorns":return new ThornsPower(owner,n);
            case "brutality":return new BrutalityPower(owner,n);
            case "barricade":return new BarricadePower(owner);
            case "demon_form":return new DemonFormPower(owner,n);
            case "dark_embrace":return new DarkEmbracePower(owner,n);
            case "berserk":return new BerserkPower(owner,n);
            case "buffer":return new BufferPower(owner,n);
            case "double_tap":return new DoubleTapPower(owner,n);
            case "equilibrium":return new EquilibriumPower(owner,n);
            case "panache":return new PanachePower(owner,n);
            case "thousand_cuts":return new ThousandCutsPower(owner,n);
            case "tools":return new ToolsOfTheTradePower(owner,n);
            case "sadistic":return new SadisticPower(owner,n);
            case "after_image":return new AfterImagePower(owner,n);
            case "well_laid":return new RetainCardPower(owner,n);
            case "envenom":return new EnvenomPower(owner,n);
            default:throw new IllegalArgumentException("Unknown card power: "+effect);
        }
    }
}
