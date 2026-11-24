using System;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaExState
{
    internal const int MaxCharge = 100;
    internal const int StoreInterval = 30 * 60;
    internal const int MaxDomainTime = 3 * StoreInterval;
    internal const int ScatterTick = 154;
    internal const int ImpactTick = 194;
    internal const int ResonanceDuration = 600;
    internal const int PulseInterval = 6;
    internal const int LastPulseTick = 306;
    internal const int StarfallTick = 320;
    internal const int FinalDuration = 360;
    internal const int ManualCooldown = 7*60;
    internal const int ManualCutoff = 82*60;
    internal const int OpeningDuration = 240;
    internal const int OpeningCapture = 234;
    internal const int OpeningEnd = 345;

    internal int Charge;
    internal int DomainTime;
    internal int ResonanceStart = -1;
    internal int Power;
    internal bool Active;
    internal int Reserve;
    internal bool Final;
    internal int CrystalReadyAt;
    internal int ResonanceTime => ResonanceStart < 0 ? -1 : DomainTime - ResonanceStart;
    internal bool CanCast => Active && !Final && ResonanceTime < 0;
    internal bool CanCastManually => Active && !Final && DomainTime < ManualCutoff &&
        (ResonanceTime < 0 || ResonanceTime >= ManualCooldown);

    internal void Hit(int amount)
    {
        if (!Active)
            Charge = Math.Min(MaxCharge, Charge + amount);
    }

    internal bool Press()
    {
        if (!Active && Charge == MaxCharge)
        {
            Charge = 0;
            Active = true;
            return true;
        }
        if (!CanCastManually || Reserve == 0)
            return false;

        StartResonance(Reserve);
        Reserve = 0;
        return true;
    }

    internal bool UseCrystal()
    {
        if (!CanCast || DomainTime < CrystalReadyAt) return false;
        CrystalReadyAt = DomainTime + ResonanceDuration;
        StartResonance(1);
        return true;
    }

    private void StartResonance(int power)
    {
        Power = power;
        ResonanceStart = DomainTime;
    }

    internal void Advance()
    {
        if (!Active) return;
        DomainTime++;
        if (!Final && DomainTime <= MaxDomainTime && DomainTime % StoreInterval == 0)
        {
            Reserve++;
            if (DomainTime == MaxDomainTime)
            {
                Final = true;
                StartResonance(Reserve);
                Reserve = 0;
            }
        }
        if (ResonanceStart < 0) return;
        if (Final && ResonanceTime >= FinalDuration)
            Clear();
        else if (!Final && ResonanceTime >= ResonanceDuration)
            ResonanceStart = -1;
    }

    internal int DamageAt(int tick)
    {
        int multiplierTwice = 2 + 3 * (Power - 1);
        int elapsed = tick - ImpactTick;
        if (elapsed == 0)
            return 3500 * multiplierTwice;
        if (elapsed > 0 && tick <= LastPulseTick && elapsed % PulseInterval == 0)
            return 1000 * multiplierTwice;
        return 0;
    }

    internal void Clear()
    {
        Charge = DomainTime = Power = 0;
        ResonanceStart = -1;
        Reserve = CrystalReadyAt = 0;
        Active = Final = false;
    }
}
