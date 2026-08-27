namespace Roguelite.Combat
{
    public interface IDamageable
    {
        void TakeDamage(DamageInfo damageInfo);
        bool IsDead { get; }
    }
}
