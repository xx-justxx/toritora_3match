using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scenes
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;
        public static bool isRestarted = false; //確認是否為第一次進入，static於場景重建後會保留

        [Header("Start Menu")]
        public GameObject StartCanvas;
        public Button StartButton;

        [Header("End Menu")]
        public GameObject EndCanvas;
        public Button RestartButton;

        public GameStatus Status { get; private set; } = GameStatus.Pause;  //開始時先不處理輸入，等點選Start按鈕才開始

        private void Awake()
        {//場景清空時會一起清空場景上的GameObject，需要重設Instance
            Instance = this;
        }

        private void Start()
        {
            if (StartCanvas != null)
            {
                if (!isRestarted) 
                { 
                    Status = GameStatus.Pause;
                    StartCanvas.SetActive(true);    //開始時顯示開始選單
                    StartButton.onClick.AddListener(StartGame);
                }
                else
                {//Restart 後不顯示 StartCanvas
                    Status = GameStatus.CanInput;
                    StartCanvas.SetActive(false);
                    AudioManager.Instance.PlayBGM();
                }
            }

            if (EndCanvas != null)
            {
                EndCanvas.SetActive(false);
                RestartButton.onClick.AddListener(RestartGame);
            }
        }

        public void StartGame()
        {//開始遊戲
            Status = GameStatus.CanInput;
            AudioManager.Instance.PlayBGM();    //BGM一開始會是靜音，等按開始才做播放(避免有人不想放聲音)
            if (StartCanvas != null) StartCanvas.SetActive(false);
        }

        /// <summary>遊戲結束，顯示結算</summary>
        public void GameOver(string typeMsg)
        {
            Status = GameStatus.GameOver;
            if (EndCanvas != null)
            {
                StartCoroutine(ShowResultAfterBlocksStop());
            }
            Debug.Log($"遊戲結束！[{typeMsg}]");
        }

        private IEnumerator ShowResultAfterBlocksStop()
        {//等待移動中的物件數量為0才顯示結算，避免結算時還在配對
            yield return new WaitUntil(() => GridManager.Instance.MovingBlock == 0);
            if (EndCanvas != null)
            {
                UIManager.Instance.ShowResult();
                EndCanvas.SetActive(true);
            }
        }

        public void RestartGame()
        {
            isRestarted = true; // 標記為 Restart

            string currentSceneName = SceneManager.GetActiveScene().name;
            SceneManager.LoadScene(currentSceneName);               //重新載入場景

            //重載後BGM會重播，有需要可以用BGMSource.time紀錄時間讓BGM接續播放。

            Debug.Log($"遊戲Restart！");
        }

        public void SetStatus_Processing()
        {
            Status = GameStatus.Processing;
        }

        public void SetStatus_CanInput()
        {
            Status = GameStatus.CanInput;
        }
    }
}
