using System;

namespace Assets.Scenes
{
    public enum GameStatus
    {//遊戲狀態
        Pause = 0,      //暫停，用於選單操作時避免觸發換位
        CanInput = 1,   //可進行輸入(點擊或觸控)操作
        Processing = 2, //處理中，用於交換或落下進行中時，不處理輸入
        GameOver = 3    //遊戲結束
    }
}
