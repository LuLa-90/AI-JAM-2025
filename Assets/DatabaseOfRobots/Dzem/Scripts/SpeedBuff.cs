using UnityEngine;

public class SpeedBuff : SpeedEffect
{
    protected override Reward Reward
    {
        get
        {
            return Reward.Positive;
        }
    }

    protected override float getDragAmount()
    {
        return 1 / dragAmount;
    }

    protected override float getAngularDragAmount()
    {
        return 1 / angularDragAmount;
    }
}
