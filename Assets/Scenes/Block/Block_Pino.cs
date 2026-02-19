using System.Linq;
using UnityEngine;

namespace Assets.Scenes.Block
{
    public class Block_Pino : BlockObj
    {
        public override BlockID ID { get; } = BlockID.Pino;
        public override Color ScoreColor { get; } = new Color32(0xb1, 0x9c, 0xd9, 0xff);   //Light Purple

        protected override void SetWeights()
        {
            _weights_none = Enumerable.Repeat(1, _images_none.Length).ToList();         //全部權重設1，顯示機率相同
            _weights_matchFail = Enumerable.Repeat(1, _images_matchFail.Length).ToList();
            _weights_match = Enumerable.Repeat(1, _images_match.Length).ToList();

            _weights_selected = Enumerable.Repeat(1, _images_selected.Length).ToList(); //越後面權重越大(1,2,3,4...)
        }
    }
}
