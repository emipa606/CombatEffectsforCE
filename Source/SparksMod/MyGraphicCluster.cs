using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatEffectsCE
{
    class MyGraphicCluster : Graphic_Cluster
    {
        public void ChangeGraphicColor(Color newColor)
        {
            if (prevColor == newColor)
            {
                return;
            }

            prevColor = newColor;

            //Log.Message("New Cluster color called");
            if(cachedTextures == null)
            {
                Log.Error("Tried to change the color on MyGraphicCluster before initialization");
            }

            subGraphics = new Graphic[cachedTextures.Count];
            for (int i=0; i < cachedTextures.Count; i++)
            {
                string path = cachedReq.path + "/" + cachedTextures[i].name;
                subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), path, cachedReq.shader, drawSize, prevColor, ColorTwo, null, cachedReq.shaderParameters);
            }
        }

        public override void Init(GraphicRequest req)
        {
            cachedReq = req;
            data = req.graphicData;
            if (req.path.NullOrEmpty())
            {
                throw new ArgumentNullException("folderPath");
            }
            if (req.shader == null)
            {
                throw new ArgumentNullException("shader");
            }
            path = req.path;
            color = req.color;
            colorTwo = req.colorTwo;
            drawSize = req.drawSize;
            List<Texture2D> list = (from x in ContentFinder<Texture2D>.GetAllInFolder(req.path)
                                    where !x.name.EndsWith(Graphic_Single.MaskSuffix)
                                    orderby x.name
                                    select x).ToList<Texture2D>();
            cachedTextures = list;

            if (list.NullOrEmpty<Texture2D>())
            {
                Log.Error("Collection cannot init: No textures found at path " + req.path, false);
                subGraphics = new Graphic[]
                {
            BaseContent.BadGraphic
                };
                return;
            }
            subGraphics = new Graphic[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                string path = req.path + "/" + list[i].name;
                subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), path, req.shader, drawSize, color, colorTwo, null, req.shaderParameters);
            }
        }

        List<Texture2D> cachedTextures;
        GraphicRequest cachedReq;
        Color prevColor;

    }
}
