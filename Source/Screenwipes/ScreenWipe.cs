namespace Celeste64;

public abstract class ScreenWipe(float duration)
{
	public bool IsFromBlack;
	public bool IsFinished { get; private set; } = false;
	public float Percent => percent;

	private float percent = 0;

	public void Restart(bool isFromBlack)
	{
		percent = 0;
		IsFromBlack = isFromBlack;
		IsFinished = false;
		Start();
	}

	public void Update()
	{
		if (percent < 1)
		{
			percent = Calc.Approach(percent, 1.0f, Time.Delta / duration);
			Step(percent);
			if (percent >= 1.0f)
				IsFinished = true;
		}
	}

	public abstract void Start();
	public abstract void Step(float percent);
	public abstract void Render(Batcher batch, Rect bounds);

}
