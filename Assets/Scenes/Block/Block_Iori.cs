using UnityEngine;

namespace Assets.Scenes.Block
{
    public class Block_Iori : BlockObj
    {
        public override BlockID ID { get; } = BlockID.Iori;
        public override Color ScoreColor { get; } = new Color32(0xbb, 0xe2, 0xf1, 0xff);   //Baby Blue
    }
}
