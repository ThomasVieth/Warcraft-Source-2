using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System.Drawing;
using CounterStrikeSharp.API.Modules.Utils;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace WCS.API
{
    public class Effects
    {
        public static void DrawLaserBetween(CCSPlayerController player, Vector startPos, Vector endPos, Color color, float life, float width)
        {
            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null)
            {
                return;
            }
            beam.Render = color;
            beam.Width = width;

            beam.Teleport(startPos, player.PlayerPawn.Value.AbsRotation, player.PlayerPawn.Value.AbsVelocity);
            beam.EndPos.X = endPos.X;
            beam.EndPos.Y = endPos.Y;
            beam.EndPos.Z = endPos.Z;
            beam.DispatchSpawn();
            Timer t = new Timer(life, beam.Remove);
        }
    }
}