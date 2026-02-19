using Assets.Scenes.Block;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scenes
{
    /// <summary>
    /// 擺放各Class都可能用到的function
    /// </summary>
    public class CalculateTool
    {
        /// <summary>取目前點擊位置的Block</summary>
        public static BlockObj GetBlockByWorldPoint(Vector2 worldPoint)
        {
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider != null)
            {
                BlockObj clickedBlock = hit.collider.GetComponent<BlockObj>();
                if (clickedBlock != null)
                {
                    //Debug.Log($"hit block:[{clickedBlock.ID}] x:[{clickedBlock.X}] y:[{clickedBlock.Y}]");
                    return clickedBlock;
                }
            }
            return null;
        }

        /// <summary>依權重做隨機，回傳隨機結果為第幾個項目的index</summary>
        public static int GetWeightedRandomIndex(List<int> weights)
        {
            //計算總權重
            int totalWeight = 0;
            foreach (int w in weights)
            {
                totalWeight += w;
            }

            if (totalWeight > 0)
            {
                // 生成 0 到 總權重 的隨機數
                float randomValue = Random.Range(0, totalWeight);   //Random.Range不包含最大值
                float currentWeightSum = 0;

                for (int i = 0; i < weights.Count; i++)
                {
                    currentWeightSum += weights[i];
                    if (randomValue < currentWeightSum)
                    {
                        return i; //返回選中的索引
                    }
                }
            }
            return 0; //正常不應執行到這段
        }

        /// <summary>依權重做隨機，回傳隨機結果為第幾個項目</summary>
        public static object GetWeightedRandomIndex(List<object> items, List<int> weights)
        {
            int index = GetWeightedRandomIndex(weights);
            if (items.Count > index)
            {
                return items[index];
            }
            else
            {//預設回傳第一個項目
                return items.FirstOrDefault();
            }
        }

        public static bool IsMatch(List<HashSet<BlockID>> canMatchSetList, HashSet<BlockID> currHashSet)
        {
            foreach (HashSet<BlockID> canMatchSet in canMatchSetList)
            {
                if (canMatchSet.SetEquals(currHashSet))
                {//目前的HashSet與任意一組可配對的HashSet相等，代表配對成功
                    return true;
                }
            }
            return false;
        }

        /// <summary>判斷相鄰Grid是否match，用於減少重複的code</summary>
        public static bool CheckNearGridMatch(GridObj[,] gridMap, List<List<BlockObj>> matchListHis, HashSet<BlockObj> matchBlockSet
            , int x, int y, int gridCount, bool isHorizontal, List<HashSet<BlockID>> canMatchSetList)
        {
            bool isMatch = false;

            int width = gridMap.GetLength(0);
            int height = gridMap.GetLength(1);

            HashSet<BlockID> currSet = new HashSet<BlockID>();
            for (int i = 0; i < gridCount; i++)
            {
                int checkX = isHorizontal ? x + i : x;
                int checkY = isHorizontal ? y : y + i;
                if (checkX >= width || checkY >= height || gridMap[checkX, checkY].Block == null)
                {//超過邊界，或中間有grid裡面沒block的情況，無法滿足match條件
                    return false;
                }
                currSet.Add(gridMap[checkX, checkY].Block.ID);
            }

            if (CalculateTool.IsMatch(canMatchSetList, currSet))
            {//配對成功的情況
                List<BlockObj> list = new List<BlockObj>();
                for (int i = 0; i < gridCount; i++)
                {
                    int checkX = isHorizontal ? x + i : x;
                    int checkY = isHorizontal ? y : y + i;
                    list.Add(gridMap[checkX, checkY].Block);
                }
                matchListHis.Add(list);
                matchBlockSet.UnionWith(list);
                isMatch = true;
            }
            return isMatch;
        }

        /// <summary>往右/上方向判斷相鄰BlockID是否match，用於減少重複的code</summary>
        public static bool IsNearBlockIDExistMatch(BlockID[,] blockIDMap, int x, int y, int gridCount, bool isHorizontal, List<HashSet<BlockID>> canMatchSetList)
        {
            int width = blockIDMap.GetLength(0);
            int height = blockIDMap.GetLength(1);

            HashSet<BlockID> currSet = new HashSet<BlockID>();
            for (int i = 0; i < gridCount; i++)
            {
                int checkX = isHorizontal ? x + i : x;
                int checkY = isHorizontal ? y : y + i;
                if (checkX >= width || checkY >= height || blockIDMap[checkX, checkY] == BlockID.None)
                {//超過邊界，或中間有grid裡面沒block的情況，無法滿足match條件
                    return false;
                }
                currSet.Add(blockIDMap[checkX, checkY]);
            }

            if (CalculateTool.IsMatch(canMatchSetList, currSet))
            {//配對成功的情況
                return true;
            }
            return false;
        }
    }
}
