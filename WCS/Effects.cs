using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Timers;

namespace WCS
{
    class Effects
    {
        public static CBeam DrawLaserBetween(CCSPlayerController player, Vector startPos, Vector endPos, Color color, float life, float width)
        {
            CBeam beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam == null)
            {
                return null;
            }
            beam.Render = color;
            beam.Width = width;

            beam.Teleport(startPos, player.PlayerPawn.Value.AbsRotation, player.PlayerPawn.Value.AbsVelocity);
            beam.EndPos.X = endPos.X;
            beam.EndPos.Y = endPos.Y;
            beam.EndPos.Z = endPos.Z;
            beam.DispatchSpawn();
            Timer t = new Timer(life, beam.Remove);

            return beam;
        }

        private static List<Vector> CalculateCircleEdgeCoords(Vector center, float radius, int parts)
        {
            float x = center.X;
            float y = center.Y;

            List<Vector> returnValue = new List<Vector>();

            double part = 2 * Math.PI / parts;

            for (int i = 0; i < parts; i++)
            {
                returnValue.Add(
                    new Vector(
                        x + ((float)(radius * Math.Sin(part * i))),
                        y + ((float)(radius * Math.Cos(part * i))),
                        center.Z
                    )
                );
            }

            return returnValue;
        }
    }
}