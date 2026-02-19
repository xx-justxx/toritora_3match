using TMPro;
using UnityEngine;

namespace Assets.Scenes
{
    /// <summary>用於顯示浮在右側的Combo</summary>
    public class FloatingCombo : MonoBehaviour
    {
        public float MoveSpeed = 1.5f;   //向上飄的速度
        public float Duration = 2f;      //存續時間

        private TextMeshProUGUI _comboText;
        private float _timer;

        public void Init(int combo, bool isToritoraCombo)
        {
            _comboText = GetComponentInChildren<TextMeshProUGUI>();
            if(!isToritoraCombo)
            {
                _comboText.text = $"{combo}Combo!";
            }
            else
            {//第二行標示為とりとらCombo
                _comboText.text = $"{combo}Combo!\n<size=80%><color=#FFFFFF>(<color=#eb6ea0>と</color><color=#00b379>り</color><color=#b19cd9>と</color><color=#bbe2f1>ら</color>)</color></size>";
            }
            _timer = 0;
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
            }
        }
    }
}
