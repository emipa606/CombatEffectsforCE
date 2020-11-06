using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace CombatEffectsCE
{
    public class MyGraphicData : GraphicData
    {

        private void ChangeClusterGraphicsColor(Color newColor)
        {
            ((MyGraphicCluster)Graphic).ChangeGraphicColor(newColor);
            return;
        }

        public void ChangeGraphicColor(Color newColor)
        {
            if(color == newColor)
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
            ShaderTypeDef cutout = shaderType;
            if (cutout == null)
            {
                cutout = ShaderTypeDefOf.Cutout;
            }
            Shader shader = cutout.Shader;
                        
            cachedGraphic = GraphicDatabase.Get(graphicClass, texPath, shader, drawSize, color, color, this, shaderParameters);
            
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

        // Token: 0x170009AC RID: 2476
        // (get) Token: 0x06004029 RID: 16425 RVA: 0x001E1282 File Offset: 0x001DF682
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

        // Token: 0x0600402A RID: 16426 RVA: 0x001E129C File Offset: 0x001DF69C
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

        // Token: 0x0600402B RID: 16427 RVA: 0x001E1354 File Offset: 0x001DF754
        private void Init()
        {
            if (graphicClass == null)
            {
                cachedGraphic = null;
                return;
            }
            ShaderTypeDef cutout = shaderType;
            if (cutout == null)
            {
                cutout = ShaderTypeDefOf.Cutout;
            }
            Shader shader = cutout.Shader;
            cachedGraphic = GraphicDatabase.Get(graphicClass, texPath, shader, drawSize, color, colorTwo, this, shaderParameters);
            if (onGroundRandomRotateAngle > 0.01f)
            {
                cachedGraphic = new Graphic_RandomRotated(cachedGraphic, onGroundRandomRotateAngle);
            }
            if (Linked)
            {
                cachedGraphic = GraphicUtility.WrapLinked(cachedGraphic, linkType);
            }
        }

        //// Token: 0x0600402C RID: 16428 RVA: 0x001E1408 File Offset: 0x001DF808
        //public void ResolveReferencesSpecial()
        //{
        //    if (this.damageData != null)
        //    {
        //        this.damageData.ResolveReferencesSpecial();
        //    }
        //}

        // Token: 0x0600402D RID: 16429 RVA: 0x001E1420 File Offset: 0x001DF820
        //public Graphic GraphicColoredFor(Thing t)
        //{
        //    if (t.DrawColor.IndistinguishableFrom(this.Graphic.Color) && t.DrawColorTwo.IndistinguishableFrom(this.Graphic.ColorTwo))
        //    {
        //        return this.Graphic;
        //    }
        //    return this.Graphic.GetColoredVersion(this.Graphic.Shader, t.DrawColor, t.DrawColorTwo);
        //}

        // Token: 0x0600402E RID: 16430 RVA: 0x001E148C File Offset: 0x001DF88C
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
            if (thingDef != null && thingDef.drawerType == DrawerType.RealtimeOnly && Linked)
            {
                yield return "does not add to map mesh but has a link drawer. Link drawers can only work on the map mesh.";
            }
            if ((shaderType == ShaderTypeDefOf.Cutout || shaderType == ShaderTypeDefOf.CutoutComplex) && thingDef.mote != null && (thingDef.mote.fadeInTime > 0f || thingDef.mote.fadeOutTime > 0f))
            {
                yield return "mote fades but uses cutout shader type. It will abruptly disappear when opacity falls under the cutout threshold.";
            }
            yield break;
        }

        // Token: 0x04002975 RID: 10613
        [NoTranslate]
        //public string texPath;

        // Token: 0x04002976 RID: 10614
        //public Type graphicClass;

        // Token: 0x04002977 RID: 10615
        //public ShaderTypeDef shaderType;

        // Token: 0x04002978 RID: 10616
        //public List<ShaderParameter> shaderParameters;

        // Token: 0x04002979 RID: 10617
        //public Color color = Color.white;

        // Token: 0x0400297A RID: 10618
        //public Color colorTwo = Color.white;

        // Token: 0x0400297B RID: 10619
        //public Vector2 drawSize = Vector2.one;

        //// Token: 0x0400297C RID: 10620
        //public float onGroundRandomRotateAngle;

        //// Token: 0x0400297D RID: 10621
        //public bool drawRotated = true;

        //// Token: 0x0400297E RID: 10622
        //public bool allowFlip = true;

        //// Token: 0x0400297F RID: 10623
        //public float flipExtraRotation;

        //// Token: 0x04002980 RID: 10624
        //public ShadowData shadowData;

        //// Token: 0x04002981 RID: 10625
        //public DamageGraphicData damageData;

        //// Token: 0x04002982 RID: 10626
        //public LinkDrawerType linkType;

        //// Token: 0x04002983 RID: 10627
        //public LinkFlags linkFlags;

        // Token: 0x04002984 RID: 10628
        [Unsaved]
        private Graphic cachedGraphic;

    }
}
