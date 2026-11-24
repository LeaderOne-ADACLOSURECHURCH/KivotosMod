using System;
using System.IO;

namespace KivotosMod.Content.Skills.BlackHole;

internal sealed class MikaVisualSettings
{
    internal readonly int[] Apple = { 1850, 1195, 25, 20 };
    internal readonly int[] Flower = { 100, 58, 55, 60, 83, 25, 70, 65, 70, 65 };

    internal void Write(BinaryWriter writer)
    {
        foreach (int value in Apple) writer.Write((short)value);
        foreach (int value in Flower) writer.Write((sbyte)value);
    }

    internal static MikaVisualSettings Read(BinaryReader reader)
    {
        var settings = new MikaVisualSettings();
        for (int i = 0; i < settings.Apple.Length; i++)
            settings.Apple[i] = Math.Clamp((int)reader.ReadInt16(), i < 2 ? 256 : 0,
                i < 2 ? 3072 : i == 2 ? 60 : 100);
        for (int i = 0; i < settings.Flower.Length; i++)
            settings.Flower[i] = Math.Clamp((int)reader.ReadSByte(), i == 5 ? -100 : 0, 100);
        return settings;
    }
}
