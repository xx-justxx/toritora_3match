using Assets.Scenes.Block;
using TMPro;
using UnityEngine;

namespace Assets.Scenes
{
    /// <summary>用於顯示和消除浮在block上的分數</summary>
    public class FloatingScore : MonoBehaviour
    {
        public float MoveSpeed = 1.5f;   //向上飄的速度
        public float Duration = 1f;      //存續時間

        private TextMeshProUGUI _scoreText;
        private Color _startColor;
        private float _timer;

        public void Init(BlockObj block)
        {
            _scoreText = GetComponentInChildren<TextMeshProUGUI>();
            _scoreText.text = $"+{block.Score}";
            _scoreText.color = block.ScoreColor;
            _startColor = _scoreText.color;
            _timer = 0;

            //隨機偏移顯示位置
            //transform.position += new Vector3(Random.Range(-0.3f, 0.3f), 0, 0);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= Duration)
            {//逾期銷毀物件
                Destroy(gameObject);
            }
            else
            {//向上淡出
                this.transform.position += Vector3.up * MoveSpeed * Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, _timer / Duration);
                _scoreText.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
            }
        }
    }
}
