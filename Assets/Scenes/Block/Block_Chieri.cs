using UnityEngine;

namespace Assets.Scenes.Block
{
    public class Block_Chieri : BlockObj
    {
        public override BlockID ID { get; } = BlockID.Chieri;
        public override Color ScoreColor { get; } = new Color32(0xeb, 0x6e, 0xa0, 0xff);   //Cherry Pink
    }
}
