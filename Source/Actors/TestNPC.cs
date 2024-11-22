namespace Celeste64;

public class TestNPC : NPC, IHaveModels
{
	public readonly string Conversation;
	public Player? TalkingTo;

	public TestNPC(string conversation) : base(Assets.Models["Theo"])
	{
		Conversation = conversation;
		Model.Transform =
			Matrix.CreateScale(4) *
			Matrix.CreateTranslation(0, 0, -1.5f);
		InteractHoverOffset = new Vec3(0, 0, 16);
		InteractRadius = 16;
		PushoutRadius = 6;
	}

	public override void Interact(Player player)
	{
		TalkingTo = player;
		World.Add(new Cutscene(Talk));
	}

	public virtual CoEnumerator Talk(Cutscene cs)
	{
		yield return Co.Run(cs.Face(TalkingTo, Position));
		yield return Co.Run(cs.Say(Loc.Lines(Conversation)));
	}
}
