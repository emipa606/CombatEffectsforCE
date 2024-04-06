using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatEffectsCE;

internal class MyGraphicCluster : Graphic_Cluster
{
    private GraphicRequest cachedReq;

    private List<Texture2D> cachedTextures;
    private Color prevColor;

    public void ChangeGraphicColor(Color newColor)
    {
        if (prevColor == newColor)
        {
            return;
        }

        prevColor = newColor;

        CombatEffectsCEMod.LogMessage("New Cluster color called");
        if (cachedTextures == null)
        {
            Log.Error("Tried to change the color on MyGraphicCluster before initialization");
        }

        if (cachedTextures == null)
        {
            return;
        }

        subGraphics = new Graphic[cachedTextures.Count];
        for (var i = 0; i < cachedTextures.Count; i++)
        {
            var cachedReqPath = $"{cachedReq.path}/{cachedTextures[i].name}";
            subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), cachedReqPath, cachedReq.shader, drawSize,
                prevColor, ColorTwo, null, cachedReq.shaderParameters);
        }
    }

    public override void Init(GraphicRequest graphicRequest)
    {
        cachedReq = graphicRequest;
        data = graphicRequest.graphicData;
        if (graphicRequest.path.NullOrEmpty())
        {
            throw new ArgumentNullException(nameof(graphicRequest.path));
        }

        if (graphicRequest.shader == null)
        {
            throw new ArgumentNullException(nameof(graphicRequest.shader));
        }

        path = graphicRequest.path;
        color = graphicRequest.color;
        colorTwo = graphicRequest.colorTwo;
        drawSize = graphicRequest.drawSize;
        var list = (from x in ContentFinder<Texture2D>.GetAllInFolder(graphicRequest.path)
            where !x.name.EndsWith(Graphic_Single.MaskSuffix)
            orderby x.name
            select x).ToList();
        cachedTextures = list;

        if (list.NullOrEmpty())
        {
            Log.Error($"Collection cannot init: No textures found at path {graphicRequest.path}");
            subGraphics =
            [
                BaseContent.BadGraphic
            ];
            return;
        }

        subGraphics = new Graphic[list.Count];
        for (var i = 0; i < list.Count; i++)
        {
            var graphicRequestPath = $"{graphicRequest.path}/{list[i].name}";
            subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), graphicRequestPath, graphicRequest.shader,
                drawSize, color, colorTwo, null, graphicRequest.shaderParameters);
        }
    }
}