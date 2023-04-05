using System;
using System.IO;
using System.Collections.Generic;

public class CPHInline
{
    public static string currentReward;
    public bool Execute()
    {
        //Check if action got triggered by a reward so that reward can be excluded
        currentReward = args.ContainsKey("rewardId") ? args["rewardId"].ToString() : String.Empty;
        //Read Arguments for discount, usageSetting
        int discount = args.ContainsKey("discount") ? Convert.ToInt32(args["discount"]) : -1;
        int usageSetting = args.ContainsKey("usageSetting") ? Convert.ToInt32(args["usageSetting"]) : -1;
        if (usageSetting == 0)
        {
            //Set standard filepath
            string filePath = @"data\pwnRewardSaleList.txt";
            if (!File.Exists(filePath))
            {
                CPH.LogInfo("[pwnRewardSale] - File pwnRewardSaleList.txt, in data folder, has been created as it did not exist before.");
                using (FileStream fs = File.Create(filePath))
                    ;
                CPH.SendMessage("pwnRewardSale.txt in your SB data folder has been created. Please write each reward title, that should have discounts applied, in their separate line.");
                return false;
            }

            //Read file to array and convert to list
            string[] rewardArray = File.ReadAllLines(filePath);
            List<string> rewardList = new List<string>(rewardArray);
            //Update Rewards from File
            DiscountFileRewards(rewardList, discount);
        //Check if option 1, which is to use a custom file by the user
        }
        else if (usageSetting == 1)
        {
            //Check setFile argument is not disabled
            string customFile = args.ContainsKey("setFile") ? args["setFile"].ToString() : String.Empty;
            if (String.IsNullOrEmpty(customFile))
            {
                CPH.LogInfo("[pwnRewardSale] - Settings are set to 1. Custom file path required. Please enable the 'Set argument %setFile%' sub-action.");
                return false;
            }

            //Check if file exists if not, break.
            if (!File.Exists(customFile))
            {
                CPH.LogInfo("[pwnRewardSale] - File did not exist, please make sure you have set the right path to the file you want to use.");
                return false;
            }

            //Read file to array and convert to list
            string[] rewardArray = File.ReadAllLines(customFile);
            List<string> rewardList = new List<string>(rewardArray);
            //Update Rewards from File
            DiscountFileRewards(rewardList, discount);
        //Check if user wants to use markers to identify reward
        }
        else if (usageSetting == 2)
        {
            string marker = args.ContainsKey("setMarker") ? args["setMarker"].ToString() : String.Empty;
            //Check if marker is empty or not
            if (String.IsNullOrEmpty(marker))
            {
                CPH.LogInfo("[pwnRewardSale] - Set Argument %setMarker% has to be enabled. Also setMarker must have at least one character.");
            }
        }
        else if (usageSetting == 3)
        {
        }
        else
        {
            CPH.LogDebug("[pwnRewardSale] - Set Argument of usageSetting was not set as 0,1,2 or 3. Please make sure to only use valid options.");
            return false;
        }

        return true;
    }

    public bool DiscountFileRewards(List<string> discountList, int discount)
    {
        Dictionary<string, int> oldCostList = CPH.GetGlobalVar<Dictionary<string, int>>("pwnRewardSale_oldCost", true);
        //if there currently is a list then go through it and update the rewards
        if (oldCostList != null)
        {
            //Go through dictionary
            foreach (KeyValuePair<string, int> reward in oldCostList)
            {
                int oldCost = reward.Value;
                int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                //Update Rewards to old costs
                CPH.UpdateRewardCost(reward.Key, newCost);
            }
        }

        //Get all rewards
        var rewardList = CPH.TwitchGetRewards();
        int rewardCount = rewardList.Count;
        if (discount == -1)
        {
            CPH.LogDebug("[pwnRewardSale] - Discount argument was not found");
            return false;
        }

        for (int i = 0; i < rewardCount; i++)
        {
            if (!oldCostList.ContainsKey(rewardList[i].Id))
            {
                if (rewardList[i].IsOurs && discountList.Contains(rewardList[i].Title) && !rewardList[i].Id.Equals(currentReward))
                {
                    //Get current cost
                    int oldCost = rewardList[i].Cost;
                    //Add Reward ID and Cost to Dictionary
                    oldCostList.Add(rewardList[i].Id, oldCost);
                    //Calculate New Cost with Discount
                    int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                    //Update Rewards Costs
                    CPH.UpdateRewardCost(rewardList[i].Id, newCost);
                    //Debug Info
                    CPH.LogDebug($"[pwnRewardSale] - {rewardList[i].Title} cost was updated from {rewardList[i].Cost} to {newCost} (Discount of {discount}%)");
                }
            }
        }

        //Save Dictionary to Global Variable
        CPH.SetGlobalVar("pwnRewardSale_oldCost", oldCostList, true);
        return true;
    }

    public bool DiscountMarkerRewards(string marker, int discount)
    {
        Dictionary<string, int> oldCostList = CPH.GetGlobalVar<Dictionary<string, int>>("pwnRewardSale_oldCost", true);
        //if there currently is a list then go through it and update the rewards
        if (oldCostList != null)
        {
            //Go through dictionary
            foreach (KeyValuePair<string, int> reward in oldCostList)
            {
                int oldCost = reward.Value;
                int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                //Update Rewards to old costs
                CPH.UpdateRewardCost(reward.Key, newCost);
            }
        }

        //Get marker length
        int markerLen = marker.Length;
        //Get all rewards
        var rewardList = CPH.TwitchGetRewards();
        int rewardCount = rewardList.Count;
        if (discount == -1)
        {
            CPH.LogDebug("[pwnRewardSale] - Discount argument was not found");
            return false;
        }

        for (int i = 0; i < rewardCount; i++)
        {
            if (!oldCostList.ContainsKey(rewardList[i].Id))
            {
                string title = rewardList[i].Title;
                int titleLen = title.Length;
                string markerCheck = title.Substring(titleLen - markerLen, titleLen);
                CPH.LogInfo($"[pwnRewardSale] - CHECK Reward {markerCheck}");
                if (rewardList[i].IsOurs && markerCheck.Equals(marker) && !rewardList[i].Id.Equals(currentReward))
                {
                    //Get current cost
                    int oldCost = rewardList[i].Cost;
                    //Add Reward ID and Cost to Dictionary
                    oldCostList.Add(rewardList[i].Id, oldCost);
                    //Calculate New Cost with Discount
                    int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                    //Update Rewards Costs
                    CPH.UpdateRewardCost(rewardList[i].Id, newCost);
                    //Debug Info
                    CPH.LogDebug($"[pwnRewardSale] - {title} cost was updated from {rewardList[i].Cost} to {newCost} (Discount of {discount}%)");
                }
            }
        }

        //Save Dictionary to Global Variable
        CPH.SetGlobalVar("pwnRewardSale_oldCost", oldCostList, true);
        return true;
    }

    public bool DiscountAllRewards(int discount)
    {
        Dictionary<string, int> oldCostList = CPH.GetGlobalVar<Dictionary<string, int>>("pwnRewardSale_oldCost", true);
        //if there currently is a list then go through it and update the rewards
        if (oldCostList != null)
        {
            //Go through dictionary
            foreach (KeyValuePair<string, int> reward in oldCostList)
            {
                int oldCost = reward.Value;
                int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                //Update Rewards to old costs
                CPH.UpdateRewardCost(reward.Key, newCost);
            }
        }

        //Get all rewards
        var rewardList = CPH.TwitchGetRewards();
        int rewardCount = rewardList.Count;
        if (discount == -1)
        {
            CPH.LogDebug("[pwnRewardSale] - Discount argument was not found");
            return false;
        }

        for (int i = 0; i < rewardCount; i++)
        {
            if (!oldCostList.ContainsKey(rewardList[i].Id))
            {
                string title = rewardList[i].Title;
                if (rewardList[i].IsOurs && !rewardList[i].Id.Equals(currentReward))
                {
                    //Get current cost
                    int oldCost = rewardList[i].Cost;
                    //Add Reward ID and Cost to Dictionary
                    oldCostList.Add(rewardList[i].Id, oldCost);
                    //Calculate New Cost with Discount
                    int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                    //Update Rewards Costs
                    CPH.UpdateRewardCost(rewardList[i].Id, newCost);
                    //Debug Info
                    CPH.LogDebug($"[pwnRewardSale] - {title} cost was updated from {rewardList[i].Cost} to {newCost} (Discount of {discount}%)");
                }
            }
        }

        //Save Dictionary to Global Variable
        CPH.SetGlobalVar("pwnRewardSale_oldCost", oldCostList, true);
        return true;
    }

    public class TwitchReward
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Prompt { get; set; }
        public int Cost { get; set; }
        public bool InputRequired { get; set; }
        public string BackgroundColor { get; set; }
        public bool Paused { get; set; }
        public bool Enabled { get; set; }
        public bool IsOurs { get; set; }
    }
}