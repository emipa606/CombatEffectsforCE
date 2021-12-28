using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatEffectsCE;

public class MyGraphicData : GraphicData
{
    [NoTranslate]
    //public string texPath;

    //public Type graphicClass;

    //public ShaderTypeDef shaderType;

    //public List<ShaderParameter> shaderParameters;

    //public Color color = Color.white;

    //public Color colorTwo = Color.white;

    //public Vector2 drawSize = Vector2.one;

    //public float onGroundRandomRotateAngle;

    //public bool drawRotated = true;

    //public bool allowFlip = true;

    //public float flipExtraRotation;

    //public ShadowData shadowData;

    //public DamageGraphicData damageData;

    //public LinkDrawerType linkType;

    //public LinkFlags linkFlags;
    [Unsaved]
    private Graphic cachedGraphic;

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

        // If this is a Graphic_Cluster we mustnt change it's this.color or else we get an error
        color = newColor;

        //Log.Message("Correct function called");
        //if (this.cachedGraphic != null)
        //{
        //    string m = this.cachedGraphic.ToString();
        //    Log.Message(m);
        //}            
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

    //public bool Linked
    //{
    //    get
    //    {
    //        return this.linkType != LinkDrawerType.None;
    //    }
    //}

    //public Graphic Graphic
    //{
    //    get
    //    {
    //        if (this.cachedGraphic == null)
    //        {
    //            this.Init();
    //        }
    //        return this.cachedGraphic;
    //    }
    //}

    //public void CopyFrom(GraphicData other)
    //{
    //    this.texPath = other.texPath;
    //    this.graphicClass = other.graphicClass;
    //    this.shaderType = other.shaderType;
    //    this.color = other.color;
    //    this.colorTwo = other.colorTwo;
    //    this.drawSize = other.drawSize;
    //    this.onGroundRandomRotateAngle = other.onGroundRandomRotateAngle;
    //    this.drawRotated = other.drawRotated;
    //    this.allowFlip = other.allowFlip;
    //    this.flipExtraRotation = other.flipExtraRotation;
    //    this.shadowData = other.shadowData;
    //    this.damageData = other.damageData;
    //    this.linkType = other.linkType;
    //    this.linkFlags = other.linkFlags;
    //}

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

    //public void ResolveReferencesSpecial()
    //{
    //    if (this.damageData != null)
    //    {
    //        this.damageData.ResolveReferencesSpecial();
    //    }
    //}

    //public Graphic GraphicColoredFor(Thing t)
    //{
    //    if (t.DrawColor.IndistinguishableFrom(this.Graphic.Color) && t.DrawColorTwo.IndistinguishableFrom(this.Graphic.ColorTwo))
    //    {
    //        return this.Graphic;
    //    }
    //    return this.Graphic.GetColoredVersion(this.Graphic.Shader, t.DrawColor, t.DrawColorTwo);
    //}

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