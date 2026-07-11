namespace Game.Weapons
{
    public interface IWeaponStrategy
    {
        void Fire(WeaponController controller, WeaponData data);
    }
}