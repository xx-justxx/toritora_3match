using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;

namespace Assets.Scenes.Block
{
    public enum BlockID
    {
        None = 0,
        Chieri = 1,
        Iori = 2,
        Pino = 3,
        Suzu = 4,
    }

    public enum BlockStatus
    {
        None = 0,           //一般
        Selected = 1,       //被選中
        MatchSuccess = 2,   //配對成功
        MatchFail = 3       //配對失敗
    }


    public class BlockObj : MonoBehaviour
    {
        public GridObj Grid;                                                //紀錄此Block位於哪個Grid
        public virtual BlockID ID { get; } = BlockID.None;                  //由override覆寫
        public virtual int Score { get; set; } = 100;
        public virtual Color ScoreColor { get; } = Color.white;             //顯示分數時顯示的顏色
        public BlockStatus Status { get; private set; } = BlockStatus.None; //0:一般狀態

        [Header("服裝1_None表情設定")]
        public Sprite[] Images_None_Apparel1;

        [Header("服裝1_Selected表情設定")]
        public Sprite[] Images_Selected_Apparel1;

        [Header("服裝1_Match表情設定")]
        public Sprite[] Images_Match_Apparel1;

        [Header("服裝1_MatchFail表情設定")]
        public Sprite[] Images_MatchFail_Apparel1;

        [Header("服裝2_None表情設定")]
        public Sprite[] Images_None_Apparel2;

        [Header("服裝2_Selected表情設定")]
        public Sprite[] Images_Selected_Apparel2;

        [Header("服裝2_Match表情設定")]
        public Sprite[] Images_Match_Apparel2;

        [Header("服裝2_MatchFail表情設定")]
        public Sprite[] Images_MatchFail_Apparel2;

        protected Sprite[] _images_none;          //目前可選的none圖示
        protected Sprite[] _images_selected;      //目前可選的selected圖示
        protected Sprite[] _images_match;
        protected Sprite[] _images_matchFail;

        protected List<int> _weights_none;        //顯示權重
        protected List<int> _weights_selected;
        protected List<int> _weights_match;
        protected List<int> _weights_matchFail;

        private int _apparelType = 1;
        private SpriteRenderer _spriteRenderer;

        void Awake()
        {
            _spriteRenderer = this.GetComponent<SpriteRenderer>();
        }

        public void Init(GridObj gridObj, bool isGridInit)
        {
            this.Grid = gridObj;
            _images_none = Images_None_Apparel1;            //Default
            _images_selected = Images_Selected_Apparel1;
            _images_match = Images_Match_Apparel1;
            _images_matchFail = Images_MatchFail_Apparel1;

            SetWeights();
            if (isGridInit)
            {
                _spriteRenderer.sprite = _images_none[0];    //開始時先固定都用第1張，讓全部表情相同
                this.transform.position = this.Grid.Position;
            }
            else
            {//遊戲中新建的block會由上往下掉，不另外設position
                ChangeSprite();
            }
        }

        public void SetStatus_Selected()
        {//被選中
            Status = BlockStatus.Selected;
            _spriteRenderer.sortingOrder = 1;   //被選中的block顯示在前方
            ChangeSprite();
        }

        public void SetStatus_Moving()
        {//移動中，目前不需特別處理
            //ChangeSprite();
        }

        public void SetStatus_Match3Success()
        {//配對成功
            Status = BlockStatus.MatchSuccess;
            this.transform.localScale *= 1.1f;  //稍微放大方便玩家確認match的位置
            ChangeSprite();
        }

        public void SetStatus_Match4Success()
        {//配對成功
            Status = BlockStatus.MatchSuccess;
            this.transform.localScale *= 1.1f;  //稍微放大方便玩家確認match的位置
            ChangeSprite();
        }

        public void SetStatus_MatchFail()
        {//配對失敗
            Status = BlockStatus.MatchFail;
            _spriteRenderer.sortingOrder = 1;   //被選中的block顯示在前方(第一次交換位置MoveEnd時會設回0，所以再設一次)
            ChangeSprite();
        }

        public void SetStatus_MoveEnd(GridObj gridObj)
        {//移動結束，已確定結束後座標
            if (Status != BlockStatus.None)
            {
                Status = BlockStatus.None;
                ChangeSprite();
                if (_spriteRenderer.sortingOrder != 0)
                {
                    _spriteRenderer.sortingOrder = 0;   //取消選擇時調整回原本的order順序
                }
            }
            this.Grid = gridObj;
            this.transform.position = this.Grid.Position;   //確保最終定位不會偏差
        }

        public void SetStatus_FallSwap(GridObj gridObj)
        {//落下時設定落下目的地的grid位置
            this.Grid = gridObj;
        }

        protected virtual void ChangeSprite()
        {//virtual用於後續可override，不同人可設不同顯示機率
            int randomIndex;
            switch (this.Status)
            {
                case BlockStatus.Selected://被選中
                    randomIndex = CalculateTool.GetWeightedRandomIndex(_weights_selected);
                    _spriteRenderer.sprite = _images_selected[randomIndex];
                    break;
                case BlockStatus.MatchSuccess:
                    randomIndex = CalculateTool.GetWeightedRandomIndex(_weights_match);
                    _spriteRenderer.sprite = _images_match[randomIndex];
                    break;
                case BlockStatus.MatchFail:
                    randomIndex = CalculateTool.GetWeightedRandomIndex(_weights_matchFail);
                    _spriteRenderer.sprite = _images_matchFail[randomIndex];
                    break;
                case BlockStatus.None://一般
                default:
                    randomIndex = CalculateTool.GetWeightedRandomIndex(_weights_none);
                    _spriteRenderer.sprite = _images_none[randomIndex];
                    break;
            }
        }

        public virtual void ApparelChange()
        {//衣裝Change
            if(_apparelType == 1)
            {//已變更過不再變更
                _images_none = Images_None_Apparel2;
                _images_selected = Images_Selected_Apparel2;
                _images_match = Images_Match_Apparel2;
                _images_matchFail = Images_MatchFail_Apparel2;

                this.Score = 200;
                SetWeights();   //重設權重
                ChangeSprite();
                _apparelType = 2;
            }
        }

        protected virtual void SetWeights()
        {
            _weights_none = Enumerable.Repeat(1, _images_none.Length).ToList();         //全部權重設1，顯示機率相同
            _weights_selected = Enumerable.Repeat(1, _images_selected.Length).ToList();
            _weights_match = Enumerable.Repeat(1, _images_match.Length).ToList();
            _weights_matchFail = Enumerable.Repeat(1, _images_matchFail.Length).ToList();
        }

        /*        
        public void OnMouseDown()
        {//測試用
            Debug.Log(this.ToLogStr());
        }
        */

        public string ToLogStr()
        {//取於測試時取Log用字串
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Class:[{0}] ", this.GetType());
            sb.AppendFormat("ID:[{0}] ", this.ID);
            sb.AppendFormat("X:[{0}] ", this.Grid.GridX);
            sb.AppendFormat("Y:[{0}] ", this.Grid.GridY);
            sb.AppendFormat("Score:[{0}] ", this.Score);
            sb.AppendFormat("Pos_X:[{0}] ", this.transform.position.x);
            sb.AppendFormat("Pos_Y:[{0}] ", this.transform.position.y);
            return sb.ToString();
        }
    }
}
