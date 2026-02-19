using Assets.Scenes.Block;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scenes
{
    public class GridObj
    {//定義每個Grid中內容
        public int GridX;                   //位於Grid的X，Init後不再異動。用於交換位置後同步Block.X/Y
        public int GridY;                   //位於Grid的Y，Init後不再異動
        public Vector2 Position;            //Grid對應的實際座標，Init後不再異動
        public BlockObj Block;              //目前的Block，會隨遊戲進行異動

        public List<GridObj> NearGrids = new List<GridObj>();     //紀錄附近的Grid，Init後不再異動。用於後續換位比較
    }
}
