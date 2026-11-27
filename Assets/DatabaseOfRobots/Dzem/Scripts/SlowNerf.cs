using UnityEngine;

public class SlowNerf : SpeedEffect
{
    protected override Reward Reward
    {
        get
        {
            return Reward.Negative;
        }
    }

    protected override float getDragAmount()
    {
        return dragAmount;
    }

    protected override float getAngularDragAmount()
    {
        return angularDragAmount;
    }


}
