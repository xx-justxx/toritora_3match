using System.Linq;
using UnityEngine;

namespace Assets.Scenes.Block
{
    public class Block_Suzu : BlockObj
    {
        public override BlockID ID { get; } = BlockID.Suzu;
        public override Color ScoreColor { get; } = new Color32(0x00, 0xb3, 0x79, 0xff);   //Emerald Green

        protected override void SetWeights()
        {
            _weights_none = Enumerable.Repeat(1, _images_none.Length).ToList();         //全部權重設1，顯示機率相同
            _weights_matchFail = Enumerable.Repeat(1, _images_matchFail.Length).ToList();

            _weights_selected = Enumerable.Repeat(1, _images_selected.Length).ToList(); //越後面權重越大(1,2,3,4...)
            _weights_match = Enumerable.Range(1, _images_match.Length).ToList();        //越後面權重越大(1,2,3,4...)
        }
    }
}
