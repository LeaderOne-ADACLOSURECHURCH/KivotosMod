using System;

namespace KivotosMod.Content.Skills;

// Runtime-only, shared by every student weapon. No inventory item owns shield health or cooldown.
public sealed class WeaponShieldState
{
    public const int Capacity = 100, Duration = 600, Cooldown = 1200, FrameTicks = 2;
    public int Health, TimeLeft, CooldownLeft, Age, HitTicks, BreakTicks;
    public byte HitDirection;
    public bool Active => Health > 0 && TimeLeft > 0;
    public bool Activate()
    {
        if (CooldownLeft > 0 || Active) return false;
        Health = Capacity; TimeLeft = Duration; CooldownLeft = Cooldown;
        Age = HitTicks = BreakTicks = 0;
        return true;
    }
    public void Stop()
    {
        if (Active) BreakTicks = 22 * FrameTicks;
        Health = TimeLeft = 0;
    }
    public int Absorb(int damage, byte direction)
    {
        if (!Active || damage <= 0) return damage;
        int absorbed = Math.Min(Health, damage);
        Health -= absorbed; HitDirection = (byte)(direction % 12); HitTicks = 15 * FrameTicks;
        if (Health == 0) { TimeLeft = 0; BreakTicks = 22 * FrameTicks; }
        return damage - absorbed;
    }
    public void Tick()
    {
        if (CooldownLeft > 0) CooldownLeft--;
        if (HitTicks > 0) HitTicks--;
        if (BreakTicks > 0) BreakTicks--;
        if (Active) { Age++; if (--TimeLeft == 0) { Health = 0; BreakTicks = 22 * FrameTicks; } }
    }
}
