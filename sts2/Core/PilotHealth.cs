namespace SpireLegacy;

// Conservative fixture policy: reserve one HP before immediate unblockable self-damage.
public static class PilotHealth
{
    public static bool Affordable(int hp,int loss)=>loss<=0||hp>loss;
    public static double Penalty(int hp,int loss)=>loss<=0?0:loss*2d+loss*12d/Math.Max(1,hp-loss);
}
