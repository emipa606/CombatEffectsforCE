using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatEffectsCE;

public class MyGraphicData : GraphicData
{
    [NoTranslate] [Unsaved] private Graphic cachedGraphic;

    private void ChangeClusterGraphicsColor(Color newColor)
    {
        ((MyGraphicCluster)Graphic).ChangeGraphicColor(newColor);
    }

    public void ChangeGraphicColor(Color newColor)
    {
        if (color == newColor)
        {
            return;
        }

        if (graphicClass == typeof(MyGraphicCluster))
        {
            ChangeClusterGraphicsColor(newColor);
            return;
        }

        // If this is a Graphic_Cluster we mustn't change it's this.color or else we get an error
        color = newColor;

        CombatEffectsCEMod.LogMessage("Correct function called");
        var cutout = shaderType;
        if (cutout == null)
        {
            cutout = ShaderTypeDefOf.Cutout;
        }

        var shader = cutout.Shader;

        cachedGraphic = GraphicDatabase.Get(graphicClass, texPath, shader, drawSize, color, color, this,
            shaderParameters);

        if (onGroundRandomRotateAngle > 0.01f)
        {
            cachedGraphic = new Graphic_RandomRotated(cachedGraphic, onGroundRandomRotateAngle);
        }

        if (Linked)
        {
            cachedGraphic = GraphicUtility.WrapLinked(cachedGraphic, linkType);
        }
    }

    private void Init()
    {
        if (graphicClass == null)
        {
            cachedGraphic = null;
            return;
        }

        var cutout = shaderType;
        if (cutout == null)
        {
            cutout = ShaderTypeDefOf.Cutout;
        }

        var shader = cutout.Shader;
        cachedGraphic = GraphicDatabase.Get(graphicClass, texPath, shader, drawSize, color, colorTwo, this,
            shaderParameters);
        if (onGroundRandomRotateAngle > 0.01f)
        {
            cachedGraphic = new Graphic_RandomRotated(cachedGraphic, onGroundRandomRotateAngle);
        }

        if (Linked)
        {
            cachedGraphic = GraphicUtility.WrapLinked(cachedGraphic, linkType);
        }
    }

    internal IEnumerable<string> ConfigErrors(ThingDef thingDef)
    {
        if (graphicClass == null)
        {
            yield return "graphicClass is null";
        }

        if (texPath.NullOrEmpty())
        {
            yield return "texPath is null or empty";
        }

        if (thingDef is { drawerType: DrawerType.RealtimeOnly } && Linked)
        {
            yield return
                "does not add to map mesh but has a link drawer. Link drawers can only work on the map mesh.";
        }

        if (thingDef != null &&
            (shaderType == ShaderTypeDefOf.Cutout || shaderType == ShaderTypeDefOf.CutoutComplex) &&
            thingDef.mote != null && (thingDef.mote.fadeInTime > 0f || thingDef.mote.fadeOutTime > 0f))
        {
            yield return
                "mote fades but uses cutout shader type. It will abruptly disappear when opacity falls under the cutout threshold.";
        }
    }
}