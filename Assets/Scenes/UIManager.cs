using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scenes
{
    public class UIManager : MonoBehaviour
    {
        private readonly int MaxScore = 9999999;    //float(unityroomRanking用的型別)的精準度上限只到7位數，超過7位數整數部分有可能會有偏差

        public static UIManager Instance;           //供其他Manager腳本呼叫

        [Header("TopUI")]
        public TMP_Text Top_TimeText;               //上方TimeText
        public TMP_Text Top_TotalScoreText;         //上方ScoreText

        [Header("ResultUI")]
        public TMP_Text Result_BaseScoreText;
        public TMP_Text Result_MaxComboText;
        public TMP_Text Result_ToritoraCountText;
        public TMP_Text Result_TotalScoreText;
        public Button RestartButton;
        public TMP_Text Result_SyncStatusText;

        private float _remainingTimeSec = 180f;
        private int _totalScore = 0;

        private int _combo = 0;         //單次交換的連續Match數，每次結束後歸0
        private int _combo_max = 0;
        private int _toritoraCount = 0; //不歸0，結算時加乘Score

        public int Combo { get { return _combo; } }

        private void Awake()
        {//場景清空時會一起清空場景上的GameObject，需要重設Instance
            Instance = this;
        }

        private void Update()
        {
            if (GameManager.Instance.Status != GameStatus.Pause && GameManager.Instance.Status != GameStatus.GameOver)
            {//不是暫停或結束的話就繼續計時
                _remainingTimeSec -= Time.deltaTime;
                _remainingTimeSec = Math.Max(_remainingTimeSec, 0); //小於0時設0，避免TimeText顯示不正確
            }

            TimeSpan ts = TimeSpan.FromSeconds(_remainingTimeSec);
            Top_TimeText.text = string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
            Top_TotalScoreText.text = $"Score: {_totalScore.ToString().PadLeft(7, '0')}";

            if (_remainingTimeSec <= 0 && GameManager.Instance.Status != GameStatus.Pause && GameManager.Instance.Status != GameStatus.GameOver)
            {//Timeout時通知遊戲結束
                GameManager.Instance.GameOver("Timeout");
            }
        }

        public void AddScore(int score)
        {
            score = Mathf.FloorToInt(score * (1 + _combo * 0.1f));   //combo加成

            if (_totalScore + score > MaxScore)
            {
                _totalScore = MaxScore;//score上限
            }
            else
            {
                _totalScore += score;
            }
        }

        public void AddToritoraCount()
        {
            _toritoraCount++;
        }

        public void SetCombo(int combo)
        {
            _combo = combo;
            _combo_max = Math.Max(_combo, _combo_max);
        }

        public void AddCombo()
        {
            _combo++;
            _combo_max = Math.Max(_combo, _combo_max);
        }

        /// <summary>顯示結算分數</summary>
        public void ShowResult()
        {
            RestartButton.interactable = false;                     //等待分數上傳結束才啟用

            float maxComboBuff = (1 + _combo_max * 0.1f);           //maxCombo加成
            float toritoraCountBuff = (1 + _toritoraCount * 0.1f);  //とりとら加成

            Result_BaseScoreText.text = _totalScore.ToString();
            Result_MaxComboText.text = string.Format("{0}(X {1:F1})", _combo_max.ToString(), maxComboBuff); //只顯示到小數1位，避免float精準度問題導致顯示為.999999
            Result_ToritoraCountText.text = string.Format("{0}(X {1:F1})", _toritoraCount.ToString(), toritoraCountBuff);

            float totalScore = _totalScore;
            totalScore = (totalScore > MaxScore / maxComboBuff) ? MaxScore : totalScore * maxComboBuff; //如果會超過上限，就設為上限
            totalScore = (totalScore > MaxScore / toritoraCountBuff) ? MaxScore : totalScore * toritoraCountBuff;

            int totalScoreInt = Mathf.FloorToInt(totalScore);           //Floor避免最後進位超過10位數
            Result_TotalScoreText.text = totalScoreInt.ToString("N0");  //顯示千分位符號

            RankingTool.SendScoreToServer(totalScoreInt, Result_SyncStatusText, RestartButton);
        }
    }
}
