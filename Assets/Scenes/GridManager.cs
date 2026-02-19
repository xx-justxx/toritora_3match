using Assets;
using Assets.Scenes;
using Assets.Scenes.Block;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;     //供其他Manager腳本呼叫

    //------------矩陣生成------------
    private GridObj[,] _gridMap = null;

    [Header("Grid設定")]
    public int Width = 7;                   //寬度
    public int Height = 7;                  //高度
    public float BlockSpacing = 0.1f;       //方塊間距
    public float CenterWidth = 0f;          //Grid的中心點，預設(width * spacing) / 2
    public float CenterHeight = -1f;        //Grid的中心點，預設(height * spacing) / 2;
    public float BlockSize = 2;             //目前Block的寬高固定為2單位
    public float SwapDuration = 0.5f;       //交換動畫的移動花費(秒)
    public float BlockFallSpeed = 1f;       //Block下落速度

    [Header("block種類設定")]
    public GameObject Block_Chieri;
    public GameObject Block_Iori;
    public GameObject Block_Pino;
    public GameObject Block_Suzu;

    [Header("effect設定")]
    public GameObject FloatingScore;        //顯示分數用的Prefab
    public GameObject FloatingCombo;        //顯示Combo用的Prefab
    public GameObject MatchEffectPrefab;    //4match觸發時的特效

    //------------配對成功類型------------
    private readonly List<HashSet<BlockID>> _3matchCheckList = new List<HashSet<BlockID>>()
    {//設定能湊成3match的情況
     //目前僅有全部同類(3個block放入hashset後僅有1種blockid)才能觸發3match。未來有機會擴充的話可以添加團體Tag
        new HashSet<BlockID>(){ BlockID.Chieri},
        new HashSet<BlockID>(){ BlockID.Iori},
        new HashSet<BlockID>(){ BlockID.Suzu},
        new HashSet<BlockID>(){ BlockID.Pino},
    };

    private readonly List<HashSet<BlockID>> _4matchCheckList = new List<HashSet<BlockID>>()
    {//設定能湊成4match的情況。透過SetEquals比較時無順序差異。
        new HashSet<BlockID>(){ BlockID.Chieri, BlockID.Iori, BlockID.Suzu, BlockID.Pino}
    };

    private readonly List<BlockID> _canRandomIDList = new List<BlockID>()
    {//能隨機出現的block類型
        BlockID.Chieri,
        BlockID.Iori,
        BlockID.Pino,
        BlockID.Suzu,
    };

    //------------操控狀態控制------------
    private Camera _mainCamera = null;          //用於座標轉換
    private BlockObj _selectedBlock = null;     //目前選擇中的Block
    private int _movingBlocks = 0;              //移動中的Block，用於確認執行狀況

    public int MovingBlock { get { return _movingBlocks; } }

    private void Awake()
    {
        Instance = this;
        _mainCamera = Camera.main;//內部會有查詢消耗，只做一次就好
    }

    private void Start()
    {
        InitGridMap();
        Debug.Log($"GridManager InitOK！");
    }

    private void InitGridMap()
    {//初始化GridMap
        _gridMap = new GridObj[Width, Height];

        //取Grid的畫面座標
        float totalWidth = Width * (BlockSize) + (Width - 1) * BlockSpacing;
        float totalHeight = Height * (BlockSize) + (Height - 1) * BlockSpacing;
        float startX = (-totalWidth / 2) + (BlockSize / 2) + CenterWidth;
        float startY = (-totalHeight / 2) + (BlockSize / 2) + CenterHeight;

        //------------初始化Grid內容------------
        for (int x = 0; x < Width; x++)
        {//col
            for (int y = 0; y < Height; y++)
            {//row
                GridObj gridObj = new GridObj();
                Vector2 pos = new Vector2(startX + x * (BlockSize + BlockSpacing), startY + y * (BlockSize + BlockSpacing));
                gridObj.Position = pos;
                gridObj.GridX = x;
                gridObj.GridY = y;
                _gridMap[x, y] = gridObj;
            }
        }

        foreach (GridObj gridObj in _gridMap)
        {//設定相鄰的Grid
            if (gridObj.GridX - 1 >= 0)
            {//左
                gridObj.NearGrids.Add(_gridMap[gridObj.GridX - 1, gridObj.GridY]);
            }
            if (gridObj.GridX + 1 < Width)
            {//右
                gridObj.NearGrids.Add(_gridMap[gridObj.GridX + 1, gridObj.GridY]);
            }
            if (gridObj.GridY - 1 >= 0)
            {//下
                gridObj.NearGrids.Add(_gridMap[gridObj.GridX, gridObj.GridY - 1]);
            }
            if (gridObj.GridY + 1 < Height)
            {//上
                gridObj.NearGrids.Add(_gridMap[gridObj.GridX, gridObj.GridY + 1]);
            }
        }

        //------------填入隨機block------------
        for (int x = 0; x < Width; x++)
        {//col
            for (int y = 0; y < Height; y++)
            {//row
                List<BlockID> canRandomIDList = new List<BlockID>(_canRandomIDList);    //先取得全部可選種類，之後再篩減
                if (x >= 2 && _gridMap[x - 1, y].Block.ID == _gridMap[x - 2, y].Block.ID)
                {//避免一開始就觸發match，如果左方已有2個一樣的block，下一個就不能再出現相同block
                    BlockID tmpBlockID = _gridMap[x - 1, y].Block.ID;   //再出現1個就會match的blockID
                    if (canRandomIDList.Contains(tmpBlockID))
                    {
                        canRandomIDList.Remove(tmpBlockID);
                    }
                }

                if (y >= 2 && _gridMap[x, y - 1].Block.ID == _gridMap[x, y - 2].Block.ID)
                {//避免一開始就觸發match，如果下方已有2個一樣的block，下一個就不能再出現相同block
                    BlockID tmpBlockID = _gridMap[x, y - 1].Block.ID;   //再出現1個就會match的blockID
                    if (canRandomIDList.Contains(tmpBlockID))
                    {
                        canRandomIDList.Remove(tmpBlockID);
                    }
                }

                if (x >= 3 && canRandomIDList.Count > 1)
                {//若可選數量>1，則盡量避免一開始就觸發4match
                    HashSet<BlockID> currSet = new HashSet<BlockID>() {
                        _gridMap[x-1, y].Block.ID,
                        _gridMap[x-2, y].Block.ID,
                        _gridMap[x-3, y].Block.ID
                    };
                    foreach (HashSet<BlockID> match4Set in _4matchCheckList)
                    {
                        HashSet<BlockID> tmp4MatchSet = new HashSet<BlockID>(match4Set);//避免影響到原本的HashSet
                        tmp4MatchSet.ExceptWith(currSet);
                        if (tmp4MatchSet.Count == 1 && canRandomIDList.Count > 1)
                        {//如果只差某種就觸發4match，避免選到該種類
                            canRandomIDList.Remove(tmp4MatchSet.First());
                        }
                    }
                }

                int randomIndex = UnityEngine.Random.Range(0, canRandomIDList.Count); //包含最小值，不包含最大值
                BlockID randomID = canRandomIDList[randomIndex];
                GameObject prefabBlock = GetBlockGameObject(randomID);  //取要建立的Block物件

                GameObject newBlock = Instantiate(prefabBlock, _gridMap[x, y].Position, Quaternion.identity);
                newBlock.transform.parent = this.transform;

                BlockObj newBlockObj = newBlock.GetComponent<BlockObj>();
                newBlockObj.Init(_gridMap[x, y], true);
                _gridMap[x, y].Block = newBlockObj;
            }
        }
    }

    private GameObject GetBlockGameObject(BlockID blockID)
    {
        switch (blockID)
        {
            case BlockID.Chieri:
                return Block_Chieri;
            case BlockID.Iori:
                return Block_Iori;
            case BlockID.Pino:
                return Block_Pino;
            case BlockID.Suzu:
                return Block_Suzu;
            default:
                return null;    //正常不應執行到這段
        }
    }

    void Update()
    {//Update全部執行完後，才會換下一Frame
        if (GameManager.Instance.Status == GameStatus.CanInput)
        {
            CheckInput();   //檢查是否有Input動作
        }
    }

    private void CheckInput()
    {
        int inputStatus = 0;    //0:none,1:開始按壓,2:按壓中,3:放開
        Vector2 inputPos = Vector2.zero;
        if (Mouse.current != null)
        {//PC滑鼠判斷
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {//滑鼠點擊
                if (_selectedBlock == null)
                {
                    inputStatus = 1;
                    inputPos = Mouse.current.position.ReadValue();
                }
            }
            else if (Mouse.current.leftButton.isPressed)
            {//持續按壓中
                if (_selectedBlock != null)
                {
                    inputStatus = 2;
                    inputPos = Mouse.current.position.ReadValue();
                }
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame || !Mouse.current.leftButton.isPressed)
            {//只要非處於按壓狀態，就當作放開滑鼠(wasReleasedThisFrame如果剛好放開時不在視窗內有可能不會觸發到)
                inputStatus = 3;
            }
        }
        else if (Touchscreen.current != null && Touchscreen.current.touches.Count > 0)
        {//手機觸控判斷
            var touch = Touchscreen.current.touches[0]; //僅判斷第一個觸控點(手機有可能多指觸控)
            var phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {//觸控點擊
                if (_selectedBlock == null)
                {
                    inputStatus = 1;
                    inputPos = touch.position.ReadValue();
                }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Moved || phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {//持續按壓中
                if (_selectedBlock != null)
                {
                    inputStatus = 2;
                    inputPos = Mouse.current.position.ReadValue();
                }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {//放開
                if (_selectedBlock != null)
                {
                    inputStatus = 3;
                }
            }
        }

        switch (inputStatus)
        {
            case 1://開始按壓
                {
                    Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(inputPos);
                    _selectedBlock = CalculateTool.GetBlockByWorldPoint(worldPoint);
                    if (_selectedBlock != null)
                    {
                        _selectedBlock.SetStatus_Selected();
                    }
                    break;
                }
            case 2://按壓中
                {
                    Vector2 worldPoint = _mainCamera.ScreenToWorldPoint(inputPos);
                    Vector2 oriPos = _selectedBlock.Grid.Position;
                    Vector2 delta = worldPoint - oriPos;            //以偏移量判斷移動方向

                    if (Mathf.Abs(delta.x) > (BlockSize + BlockSpacing) || Mathf.Abs(delta.y) > (BlockSize + BlockSpacing))
                    {//若移動距離過長，就將block移動回原本方塊位置。避免判斷出問題
                        _selectedBlock.SetStatus_MoveEnd(_selectedBlock.Grid);
                        _selectedBlock = null;
                    }
                    else
                    {
                        //-------------移動selectedBlock-------------
                        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                        {//水平移動，只更新 X
                            _selectedBlock.transform.position = new Vector2(worldPoint.x, oriPos.y);
                        }
                        else
                        {//垂直移動，只更新 Y
                            _selectedBlock.transform.position = new Vector2(oriPos.x, worldPoint.y);
                        }

                        //-------------確認是否已移至相鄰Block上方-------------
                        GridObj targetGrid = null;
                        foreach (GridObj nearGrid in _selectedBlock.Grid.NearGrids)
                        {
                            float checkOffset = 0.7f;   //移動一定程度之後才判斷，避免稍微移動一點就觸發交換
                            if (Mathf.Abs(worldPoint.x - nearGrid.Position.x) <= BlockSize * checkOffset
                                && Mathf.Abs(worldPoint.y - nearGrid.Position.y) <= BlockSize * checkOffset)
                            {
                                if (nearGrid.Block != null)
                                {
                                    Debug.Log($"Block[{_selectedBlock.ID}] Move into Grid[{nearGrid.GridX},{nearGrid.GridY}] ID:[{nearGrid.Block.ID}]");
                                    targetGrid = nearGrid;
                                }
                            }
                        }

                        if (targetGrid != null)
                        {//已移動至相鄰grid上時，進行交換判斷
                            GameManager.Instance.SetStatus_Processing();    //交換中不處理輸入操作。執行SwapAndCheckMatch前就先更新狀態，避免執行前又觸發update
                            StartCoroutine(SwapAndCheckMatch(_selectedBlock.Grid, targetGrid));
                            _selectedBlock = null;
                        }
                    }
                    break;
                }
            case 3://放開
            default://滑鼠移出遊戲視窗等情況有可能偵測不出放開，非按壓的情況就當放開處理
                {
                    if (_selectedBlock != null)
                    {//將block移動回原本方塊位置
                        _selectedBlock.SetStatus_MoveEnd(_selectedBlock.Grid);
                        _selectedBlock = null;
                    }
                    break;
                }
        }
    }

    /// <summary>交換處理</summary>
    private IEnumerator SwapAndCheckMatch(GridObj selectedGrid, GridObj targetGrid)
    {
        GameManager.Instance.SetStatus_Processing();    //交換中不處理輸入操作

        //交換位置
        yield return StartCoroutine(SwapBlock(selectedGrid, targetGrid));
        yield return new WaitUntil(() => _movingBlocks == 0);   //等待直到所有Block移動結束

        bool hasMatch = false;
        yield return StartCoroutine(CheckMatchs(result =>
        {
            hasMatch = result;
        }));

        if (!hasMatch)
        {//沒消除的情況，把位置換回原位
            yield return new WaitForSeconds(0.1f);              //停頓一小段時間。因為還有換位回去的動畫，所以不用停太久
            targetGrid.Block.SetStatus_MatchFail();             //已交換位置，所以狀態修改改為對target做
            yield return StartCoroutine(SwapBlock(selectedGrid, targetGrid));
            yield return new WaitUntil(() => _movingBlocks == 0);   //等待直到所有Block移動結束
        }
        else
        {//有消除的情況，進入MatchLoop
            yield return StartCoroutine(MatchLoop());
        }

        GameManager.Instance.SetStatus_CanInput();  //交換結束

        if (!HasMoveAvailable())
        {//確認如果已無可交換的位置，結束遊戲
            GameManager.Instance.GameOver("CanMachBlockIs0");
        }
    }

    /// <summary>有消除的情況，落下新Block並重新確認是否Match</summary>
    private IEnumerator MatchLoop()
    {
        UIManager.Instance.SetCombo(1); //進入MatchLoop前已經有觸發過一次match
        bool hasMatch = false;
        do
        {
            yield return StartCoroutine(FillNewBlock());            //落下處理

            if(UIManager.Instance.Combo < 99)
            {
                if (GameManager.Instance.Status != GameStatus.GameOver && GameManager.Instance.Status != GameStatus.Pause)
                {//TimeOut等情況導致已進入GameOver的話就不做Match
                    yield return StartCoroutine(CheckMatchs(result => hasMatch = result));
                }
            }
            else
            {//Combo超過99就不再做Match，避免無限循環
                hasMatch = false;
            }            
        } while (hasMatch);

        UIManager.Instance.SetCombo(0);//combo結束後歸0
    }

    private IEnumerator SwapBlock(GridObj gridA, GridObj gridB)
    {
        Debug.Log($"SwapBlock Begin A:[{gridA.Block.ID}] B:[{gridB.Block.ID}]");
        _movingBlocks += 2;
        try
        {
            Vector2 beginPosA = gridA.Block.transform.position;  //gridA目前實際座標
            Vector2 beginPosB = gridB.Block.transform.position;
            Vector2 endPosA = gridB.Position;                    //gridA移動後的目標座標
            Vector2 endPosB = gridA.Position;
            gridA.Block.SetStatus_Moving();
            gridB.Block.SetStatus_Moving();
            float elapsed = 0f;
            while (elapsed < SwapDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / SwapDuration;
                gridA.Block.transform.position = Vector3.Lerp(beginPosA, endPosA, t);
                gridB.Block.transform.position = Vector3.Lerp(beginPosB, endPosB, t);
                yield return null;  // 等待下一frame
            }


            Debug.Log($"SwapBlock Begin A:[{gridA.Block.ID}][{gridA.Position}][{gridA.Block.transform.position}]  B:[{gridB.Block.ID}][{gridB.Position}][{gridB.Block.transform.position}]");

            //移動動畫結束後，實際交換Grid中的內容
            BlockObj tmpBlock = gridA.Block;
            gridA.Block = gridB.Block;
            gridB.Block = tmpBlock;
            gridA.Block.SetStatus_MoveEnd(gridA);
            gridB.Block.SetStatus_MoveEnd(gridB);

            Debug.Log($"SwapBlock End A:[{gridA.Block.ID}][{gridA.Position}][{gridA.Block.transform.position}]  B:[{gridB.Block.ID}][{gridB.Position}][{gridB.Block.transform.position}]");
        }
        finally
        {//避免有exception導致movingBlocks算錯遊戲卡住
            _movingBlocks -= 2;
        }
        Debug.Log($"SwapBlock End A:[{gridA.Block.ID}] B:[{gridB.Block.ID}]");
    }

    /// <summary>Match相關處理，回傳是否觸發Match(是否需換回原位)</summary>
    private IEnumerator CheckMatchs(System.Action<bool> callback)
    {
        List<List<BlockObj>> matchListHis = new List<List<BlockObj>>(); //用於有多個位置觸發match時，儲存每一次的match資訊
        HashSet<BlockObj> match4BlockSet = new HashSet<BlockObj>();     //用於之後特效處理
        HashSet<BlockObj> match3BlockSet = new HashSet<BlockObj>();
        int match4Count = Check4Matches(matchListHis, match4BlockSet);  //檢查是否有4Match

        if (match4Count > 0)
        {//match4的情況多停一下，讓玩家可確認狀況
            yield return new WaitForSeconds(0.05f);
        }

        int match3Count = Check3Matches(matchListHis, match3BlockSet);  //檢查是否有3Match

        HashSet<BlockObj> matchBlockSet = new HashSet<BlockObj>();      //用於之後清除Block，HashSet避免重複清除
        matchBlockSet.UnionWith(match4BlockSet);
        matchBlockSet.UnionWith(match3BlockSet);

        bool hasMatch = (match4Count + match3Count > 0);
        if (hasMatch)
        {//有消除的情況，做後續處理
            UIManager.Instance.AddCombo();
            if (FloatingCombo != null)
            {
                GameObject comboObject = Instantiate(FloatingCombo);
                FloatingCombo floatingCombo = comboObject.GetComponent<FloatingCombo>();
                floatingCombo.Init(UIManager.Instance.Combo, (match4Count > 0));
            }

            if (AudioManager.Instance != null)
            {//一次check只播放一次
                AudioManager.Instance.PlaySE_Match();
            }

            yield return new WaitForSeconds(0.2f);             //停頓一小段時間，讓玩家可確認狀況
            if (match4Count > 0)
            {
                UIManager.Instance.AddToritoraCount();
                foreach (BlockObj block in match4BlockSet)
                {//變裝特效
                    GameObject effect = Instantiate(MatchEffectPrefab, block.transform.position, Quaternion.identity);
                    ParticleSystem particleSystem = effect.GetComponent<ParticleSystem>();
                    if (particleSystem != null)
                    {
                        var main = particleSystem.main;
                        main.startColor = block.ScoreColor;
                    }
                }
                foreach (GridObj grid in _gridMap)
                {//觸發match4時，對目前畫面上的全部block做衣裝change
                    if (grid.Block != null)
                    {
                        grid.Block.ApparelChange();
                    }
                }
            }

            foreach (BlockObj block in matchBlockSet)
            {//消除Block
                EarnScore(block);
                block.Grid.Block = null;
                Destroy(block.gameObject);
            }
        }
        callback?.Invoke(hasMatch); //callback回傳結果
    }

    /// <summary>檢查是否觸發4Match</summary>
    private int Check4Matches(List<List<BlockObj>> matchListHis, HashSet<BlockObj> matchBlockSet)
    {
        HashSet<BlockObj> match4BlockSet = new HashSet<BlockObj>();
        //4Match只判斷四人並列的情況，所以只做橫向判斷
        for (int y = 0; y < Height; y++)
        {//row
            for (int x = 0; x < Width; x++)
            {//col
                if (x + 4 > Width)
                {//橫向右方不存在4個grid
                    break;
                }
                bool ismatch = CalculateTool.CheckNearGridMatch(_gridMap, matchListHis, match4BlockSet, x, y, 4, true, _4matchCheckList);
                if (ismatch)
                {//4match的情況，一次match最多4人(C,I,P,S,C的情況，只match前面4個)
                    x += 3;
                }
            }
        }

        foreach (BlockObj block in match4BlockSet)
        {
            block.SetStatus_Match4Success();
            Debug.Log($"4MatchOK,[{block.ToLogStr()}]");
        }
        matchBlockSet.UnionWith(match4BlockSet);
        return match4BlockSet.Count;
    }

    /// <summary>檢查是否觸發3Match</summary>
    private int Check3Matches(List<List<BlockObj>> matchListHis, HashSet<BlockObj> matchBlockSet)
    {
        HashSet<BlockObj> match3BlockSet = new HashSet<BlockObj>();
        //先判斷橫向
        for (int y = 0; y < Height; y++)
        {//row
            for (int x = 0; x < Width; x++)
            {//col
                if (x + 3 > Width)
                {//橫向右方不存在3個grid
                    break;
                }
                CalculateTool.CheckNearGridMatch(_gridMap, matchListHis, match3BlockSet, x, y, 3, true, _3matchCheckList);
            }
        }

        //再判斷直向
        for (int x = 0; x < Width; x++)
        {//col
            for (int y = 0; y < Height; y++)
            {//row
                if (y + 3 > Height)
                {//直向上方不存在3個grid
                    break;
                }
                CalculateTool.CheckNearGridMatch(_gridMap, matchListHis, match3BlockSet, x, y, 3, false, _3matchCheckList);
            }
        }

        foreach (BlockObj block in match3BlockSet)
        {
            block.SetStatus_Match3Success();
            Debug.Log($"3MatchOK,[{block.ToLogStr()}]");
        }
        matchBlockSet.UnionWith(match3BlockSet);
        return match3BlockSet.Count;

    }

    /// <summary>檢查是否還有可移動觸發match的block</summary>
    public bool HasMoveAvailable()
    {
        //先建立一個暫存gridMap快照。這邊不能用Clone(淺拷貝)，且MonoBehaviour不允許new，所以直接用BlockID
        BlockID[,] blockIDMap = new BlockID[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_gridMap[x, y].Block != null)
                {
                    blockIDMap[x, y] = _gridMap[x, y].Block.ID;
                }
                else
                {
                    blockIDMap[x, y] = BlockID.None;
                }
            }
        }

        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (blockIDMap[x, y] == BlockID.None)
                {
                    continue;
                }

                //嘗試與右方交換
                int checkX = x + 1;
                int checkY = y;
                if (checkX < Width && blockIDMap[checkX, checkY] != BlockID.None)
                {
                    BlockID tempBlockID = blockIDMap[x, y];        //交換做確認
                    blockIDMap[x, y] = blockIDMap[checkX, checkY];
                    blockIDMap[checkX, checkY] = tempBlockID;

                    if (HasMoveAvailable_CheckAllMatch(blockIDMap))
                    {
                        return true;
                    }

                    blockIDMap[checkX, checkY] = blockIDMap[x, y];  //確認後交換回去
                    blockIDMap[x, y] = tempBlockID;
                }

                //嘗試與下方交換
                checkX = x;
                checkY = y + 1;
                if (checkY < Height && blockIDMap[checkX, checkY] != BlockID.None)
                {
                    BlockID tempBlockID = blockIDMap[x, y];        //交換做確認
                    blockIDMap[x, y] = blockIDMap[checkX, checkY];
                    blockIDMap[checkX, checkY] = tempBlockID;

                    if (HasMoveAvailable_CheckAllMatch(blockIDMap))
                    {
                        return true;
                    }
                    blockIDMap[checkX, checkY] = blockIDMap[x, y];  //確認後交換回去
                    blockIDMap[x, y] = tempBlockID;
                }
            }
        }
        return false;
    }

    private bool HasMoveAvailable_CheckAllMatch(BlockID[,] blockIDMap)
    {
        //比對全部grid是否有可能觸發match
        //這邊可以改成只針對交換的兩個grid的上下左右+最長match長度(影響範圍)做局部比對。以目前規模來說感覺不需要，所以先全部比對
        for (int x = 0; x < blockIDMap.GetLength(0); x++)
        {
            for (int y = 0; y < blockIDMap.GetLength(1); y++)
            {
                if (CalculateTool.IsNearBlockIDExistMatch(blockIDMap, x, y, 3, true, _3matchCheckList))
                {//橫向3格判斷
                    Debug.Log($"IsNearBlockIDExistMatch,x:[{x}] y:[{y}] gridCount:[{3}] horizontal:[true]");
                    return true;
                }

                if (CalculateTool.IsNearBlockIDExistMatch(blockIDMap, x, y, 4, true, _4matchCheckList))
                {//橫向4格判斷
                    Debug.Log($"IsNearBlockIDExistMatch,x:[{x}] y:[{y}] gridCount:[{4}] horizontal:[true]");
                    return true;
                }

                if (CalculateTool.IsNearBlockIDExistMatch(blockIDMap, x, y, 3, false, _3matchCheckList))
                {//直向3格判斷
                    Debug.Log($"IsNearBlockIDExistMatch,x:[{x}] y:[{y}] gridCount:[{3}] horizontal:[false]");
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>補充新的Block的處理</summary>
    IEnumerator FillNewBlock()
    {
        yield return StartCoroutine(CurrentBlockFall()); //等待現有Block落下
        yield return StartCoroutine(NewBlockFall());     //等待新的Block落下
    }

    IEnumerator CurrentBlockFall()
    {//讓現有Block往下方落下
        for (int x = 0; x < Width; x++)
        {//col
            for (int y = 0; y < Height; y++)
            {//row
                if (_gridMap[x, y].Block == null)
                {//往上找最近的一個方塊
                    for (int nextY = y + 1; nextY < Height; nextY++)
                    {
                        if (_gridMap[x, nextY].Block != null)
                        {
                            //把方塊交換至下方空位
                            _gridMap[x, y].Block = _gridMap[x, nextY].Block;
                            _gridMap[x, nextY].Block = null;
                            _gridMap[x, y].Block.SetStatus_FallSwap(_gridMap[x, y]); //重新設定Block對應的grid

                            //執行下落動畫
                            StartCoroutine(AnimateBlockFall(_gridMap[x, y]));
                            break; //只把上方最近的往下移，更上面的等下一輪y再異動
                        }
                    }
                }
            }
        }
        yield return new WaitUntil(() => _movingBlocks == 0);   //等待直到落下處理結束
    }

    IEnumerator NewBlockFall()
    {
        for (int x = 0; x < Width; x++)
        {//col
            int nullBlockCount = 0; //確認這是第幾個block(同col有多個時，讓後面的從比較高的地方落下)
            for (int y = 0; y < Height; y++)
            {//row
                if (_gridMap[x, y].Block == null)
                {
                    nullBlockCount++;
                    GridObj topGridObj = _gridMap[x, Height - 1];   //從最上面的grid上方往下掉落
                    float newPosY = topGridObj.Position.y + nullBlockCount * (BlockSize + BlockSpacing);
                    Vector2 pos = new Vector2(topGridObj.Position.x, newPosY);

                    int randomIndex = Random.Range(0, _canRandomIDList.Count);
                    BlockID randomID = _canRandomIDList[randomIndex];
                    GameObject prefabBlock = GetBlockGameObject(randomID);  //取要建立的Block物件

                    GameObject newBlock = Instantiate(prefabBlock, pos, Quaternion.identity);
                    newBlock.transform.parent = this.transform;

                    BlockObj newBlockObj = newBlock.GetComponent<BlockObj>();
                    newBlockObj.Init(_gridMap[x, y], false);
                    _gridMap[x, y].Block = newBlockObj;

                    //執行下落動畫
                    StartCoroutine(AnimateBlockFall(_gridMap[x, y]));
                }
            }
        }
        yield return new WaitUntil(() => _movingBlocks == 0);   //等待直到落下處理結束
    }

    IEnumerator AnimateBlockFall(GridObj grid)
    {
        _movingBlocks++;
        try
        {
            while (Vector3.Distance(grid.Block.transform.position, grid.Position) > 0.01f)
            {//隨frame觸發向下移動
                grid.Block.transform.position = Vector3.MoveTowards(grid.Block.transform.position
                    , grid.Position, BlockFallSpeed * Time.deltaTime);
                yield return null;
            }
            grid.Block.transform.position = grid.Position;  //移動結束時效準座標
        }
        finally
        {
            _movingBlocks--;//用於確認是否仍有block在移動中
        }
    }

    private void EarnScore(BlockObj block)
    {
        UIManager.Instance.AddScore(block.Score);
        if (FloatingScore != null)
        {
            GameObject scoreObject = Instantiate(FloatingScore, block.transform.position, Quaternion.identity);
            FloatingScore floatingScore = scoreObject.GetComponent<FloatingScore>();
            if (floatingScore != null)
            {
                floatingScore.Init(block);
            }
        }
    }
}
