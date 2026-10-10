namespace SpireLegacy;

/// <summary>Original victory dialogue, shared by native rendering and content checks.</summary>
public static class ArchitectConversation
{
    public sealed record Line(bool Architect, string Text);
    public static IReadOnlyList<Line> ForClass(string classId, bool firstVictory) => (classId, firstVictory) switch
    {
        ("knight", true) => [
            new(true, "You carried a house's worth of oaths up my stairs."),
            new(false, "And I intend to carry them home."),
            new(true, "Steel breaks. Stone crumbles. What will your family inherit?"),
            new(false, "A way forward. That is enough to build on.")],
        ("knight", false) => [
            new(true, "Another heir beneath the same banner. Does your house never tire?"),
            new(false, "We take turns carrying the weight."),
            new(true, "Then the foundation holds."),
            new(false, "It was laid by everyone who did not make it this far.")],
        ("mage", true) => [
            new(true, "Most climbers search for an exit. You studied the walls."),
            new(false, "Someone in my family will want to know how they stand."),
            new(true, "And if your answers bring them back here?"),
            new(false, "Then I will leave better notes.")],
        ("mage", false) => [
            new(true, "Your bloodline returns with another theory."),
            new(false, "The last one needed a few corrections."),
            new(true, "You call all this suffering research?"),
            new(false, "I call it a lesson the next heir should not have to repeat.")],
        ("ranger", true) => [
            new(true, "You followed a trail no one was meant to finish."),
            new(false, "My family left tracks. I kept looking."),
            new(true, "There is nothing left to hunt here."),
            new(false, "Good. Now I can find the way home.")],
        ("ranger", false) => [
            new(true, "I have seen your banner at the end of this trail before."),
            new(false, "Then you know we do not give up easily."),
            new(true, "Will you send another heir after you?"),
            new(false, "I will leave them a clear path. The choice is theirs.")],
        _ => throw new ArgumentOutOfRangeException(nameof(classId), classId, "Unknown heir class")
    };
}
