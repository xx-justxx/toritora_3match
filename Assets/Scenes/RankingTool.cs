using System;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unityroom.Client;

namespace Assets.Scenes
{
    /// <summary>
    /// 用於處理Ranking相關
    /// </summary>
    public class RankingTool
    {
        /*
            unityroom的ranking處理方式有改過很多版本，以目前(2026/02)來說步驟是
            1.先至https://github.com/unityroom/unityroom-sdk取得client串接程式
              (不想裝Git可以Code->Download zip-> 複製unityroom-sdk-main\src\Unityroom.Client\Assets\Unityroom.Client到Assets底下)
            2.於unityroom的API利用設定，勾選"APIを使用する"
            3.於unityroom的APIキー，產生HMAC認証用キー
            4.於unityroom的スコアランキング，新增ランキング。(需先產生HMAC才有辦法新增)
            5.遊戲中，透過UnityroomClient傳送Score至ランキング
            
            傳送Score時，如果玩家沒有勾選プライバシー設定->ユーザー情報の提供を有効にする，顯示的名稱會是ゲストユーザー
        */
        private static UnityroomClient _client = new()
        {
            HmacKey = ""
            //,MaxRetries = 5, //リトライ回数を設定，預設2回
        };

        private static readonly int _scoreboardId = 1;

        public void Dispose()
        {//瀏覽器的情況，關閉分頁時相關記憶體和資源應該會被強制回收，所以Dispose可能並非必要
            _client?.Dispose();
        }

        public static async void SendScoreToServer(int score, TMP_Text syncStatusText, Button restartButton)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));  //超過10秒沒回應就當失敗

            if (restartButton != null && restartButton.gameObject != null)
            {
                restartButton.interactable = false;
            }

            string text = "Sync Score...";
            if (syncStatusText != null && syncStatusText.gameObject != null)
            {
                syncStatusText.text = text;
            }

            try
            {
                var uploadTask = _client.Scoreboards.SendAsync(new()
                {
                    ScoreboardId = _scoreboardId,
                    Score = (float)score
                });

                var completedTask = await Task.WhenAny(uploadTask, Task.Delay(10000, cts.Token));

                if (completedTask == uploadTask)
                {
                    cts.Cancel();                    //成功時取消超時倒數
                    var response = await uploadTask; //取得真正結果
                    if (response.ScoreUpdated)
                    {
                        text = "Sync OK(New Record)";
                    }
                    else
                    {
                        text = "Sync OK(No New Record)";
                    }
                }
                else
                {
                    text = "Sync Score Timeout";
                }
            }
            catch (OperationCanceledException)
            {//提前做cts.Cancel會導致Task.Delay回應Exception，這是正常現象，不用處理
            }
            catch (UnityroomApiException ex)
            {
                text = "Sync Score Fail";

                Debug.Log("Sync Score Fail,ErrorCode: " + ex.ErrorCode);
                Debug.Log("Sync Score Fail,ErrorType: " + ex.ErrorType);
                Debug.Log("Sync Score Fail,Message: " + ex.Message);
            }
            catch (Exception ex)
            {
                text = "Sync Score Fail";
                Debug.Log("Sync Score Fail,Message: " + ex.Message);
            }

            if (syncStatusText != null && syncStatusText.gameObject != null)
            {
                syncStatusText.text = text;
            }

            if (restartButton != null && restartButton.gameObject != null)
            {
                restartButton.interactable = true;
            }
        }
    }
}
