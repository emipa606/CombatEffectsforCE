using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace CombatEffectsCE
{
    class MyGraphicCluster : Graphic_Cluster
    {
        public void changeGraphicColor(Color newColor)
        {
            if (this.prevColor == newColor)
            {
                return;
            }

            this.prevColor = newColor;

            //Log.Message("New Cluster color called");
            if(this.cachedTextures == null)
            {
                Log.Error("Tried to change the color on MyGraphicCluster before initialization");
            }

            this.subGraphics = new Graphic[this.cachedTextures.Count];
            for (int i=0; i < cachedTextures.Count; i++)
            {
                string path = this.cachedReq.path + "/" + this.cachedTextures[i].name;
                this.subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), path, this.cachedReq.shader, this.drawSize, this.prevColor, this.ColorTwo, null, this.cachedReq.shaderParameters);
            }
        }

        public override void Init(GraphicRequest req)
        {
            this.cachedReq = req;
            this.data = req.graphicData;
            if (req.path.NullOrEmpty())
            {
                throw new ArgumentNullException("folderPath");
            }
            if (req.shader == null)
            {
                throw new ArgumentNullException("shader");
            }
            this.path = req.path;
            this.color = req.color;
            this.colorTwo = req.colorTwo;
            this.drawSize = req.drawSize;
            List<Texture2D> list = (from x in ContentFinder<Texture2D>.GetAllInFolder(req.path)
                                    where !x.name.EndsWith(Graphic_Single.MaskSuffix)
                                    orderby x.name
                                    select x).ToList<Texture2D>();
            this.cachedTextures = list;

            if (list.NullOrEmpty<Texture2D>())
            {
                Log.Error("Collection cannot init: No textures found at path " + req.path, false);
                this.subGraphics = new Graphic[]
                {
            BaseContent.BadGraphic
                };
                return;
            }
            this.subGraphics = new Graphic[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                string path = req.path + "/" + list[i].name;
                this.subGraphics[i] = GraphicDatabase.Get(typeof(Graphic_Single), path, req.shader, this.drawSize, this.color, this.colorTwo, null, req.shaderParameters);
            }
        }

        List<Texture2D> cachedTextures;
        GraphicRequest cachedReq;
        Color prevColor;

    }
}
