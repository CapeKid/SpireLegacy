namespace SpireLegacy;

public static class AncientConversation
{
    public sealed record Line(bool Ancient,string Text);
    public static readonly string[] Supported=["NEOW","DARV","TEZCATARA","OROBAS","PAEL","NONUPEIPE","TANX","VAKUU"];
    public static IReadOnlyList<Line> For(string ancient,string classId,int visits)
    {
        var cls=classId is "knight" or "mage" or "ranger"?classId:"knight";
        var row=Design.Rows("ancient_dialogue").SingleOrDefault(r=>r.Text("ancient")==ancient&&r.Text("classId")==cls&&r.Text("visit")== (visits<=0?"first":"return"));
        if(row!=null)return [new(true,row.Text("opening")),new(false,row.Text("reply")),new(true,row.Text("followup")),new(false,row.Text("closing"))];
        // A future branch's new Ancient gets original family dialogue, never another hero's lines.
        return [new(true,visits<=0?"An heir has reached my door. What does your family seek?":"Another branch of that family. What will this heir carry onward?"),
            new(false,cls switch {"mage"=>"Something worth teaching the next heir.","ranger"=>"A path that leads us home.",_=>"A chance to protect those who follow."}),
            new(true,"Then choose your inheritance carefully."),new(false,"I will leave more than my name behind.")];
    }
}
