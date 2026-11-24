/// <summary>What one robot did in the current episode. Written to TensorBoard at the end of each episode.</summary>
public class FightStats {
    /// <summary>Damage dealt to the enemy's zones (1 = one zone's whole HP).</summary>
    public float DamageDealt { get; private set; }
    /// <summary>Damage taken from the enemy.</summary>
    public float DamageTaken { get; private set; }
    /// <summary>Falls out of the arena and flips.</summary>
    public int Respawns { get; private set; }

    public void AddHit(float dealt, float taken) {
        DamageDealt += dealt;
        DamageTaken += taken;
    }

    public void AddRespawn() {
        Respawns++;
    }

    public void Reset() {
        DamageDealt = 0f;
        DamageTaken = 0f;
        Respawns = 0;
    }
}
