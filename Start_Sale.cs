using System;
using System.IO;
using System.Collections.Generic;

public class CPHInline
{
    public static string currentReward;
    public bool Execute()
    {
        /*
			Hi there, I'm pwnyy! Thanks for using my RewardSale extension. 
			I would like to say I am by no means a professional programmer, so there will be
			sections that may be questionable in this program! By any means improve it as you wish!

			Idea suggestion for this extension came from Neiluler https://www.twitch.tv/neiluler ,
            for their RPG based stream.

			If you want to contact me, you can do so on https://twitter.com/pwnyy or on the
			Streamer.Bot Discord, just look up pwnyy you should be able to find me.

			To support me https://ko-fi.com/pwnyy , always appreciated!
		
		*/
		
        //Check if action got triggered by a reward so that reward can be excluded
        currentReward = args.ContainsKey("rewardId") ? args["rewardId"].ToString() : String.Empty;
        //Read Arguments for discount, usageSetting
        string discountString = args.ContainsKey("discount") ? args["discount"].ToString() : String.Empty;
        int discount = -1;
        //Check if discount is a valid numeric
        bool isNumeric = int.TryParse(discountString, out discount);
        if (isNumeric)
        {
			if(discount < 0){
				CPH.LogInfo($"[pwnRewardSale] - Discount Value was {discount}, which is below 0. Please use only positiv values.");
				return false;
			}
			
			
        }else if (!isNumeric)
        {
			CPH.LogInfo("[pwnRewardSale] - Discount needs to be of numeric value. Currently your discount value can not be parsed into an int.");
			return false;
        }
        
        
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
                return false;
            }
            
            DiscountMarkerRewards(marker,discount);
        }
        else if (usageSetting == 3)
        {
            DiscountAllRewards(discount);
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
        if(oldCostList == null)
        {
            //If non existent set as empty dictionary
            oldCostList = new Dictionary<string,int>();
        }

        //Create new Dictionary
        Dictionary<string,int> updatedCostList = new Dictionary<string,int>();
        
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
            //Get Reward Data
            string rewardId = rewardList[i].Id;
            string title = rewardList[i].Title;
            bool isOurs = rewardList[i].IsOurs;
            int oldCost = rewardList[i].Cost;


            if(isOurs && discountList.Contains(title) && !rewardId.Equals(currentReward))
            {
                //If reward is in the stored global list
                if(oldCostList.ContainsKey(rewardId))
                {
                    //If yes get cost from list, as that's the original and remove keyvaluepair from list
                    oldCost = oldCostList[rewardId];
                    oldCostList.Remove(rewardId);
                }
                //Add Reward ID and Cost to updated cost dictionary
                updatedCostList.Add(rewardId,oldCost);
                //Calculate New Cost with Discount
                int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                //Update Reward Cost
                CPH.UpdateRewardCost(rewardId, newCost);
                //Debug Info
                CPH.LogDebug($"[pwnRewardSale] - {title} cost was updated from {oldCost} to {newCost} (Discount of {discount}%)");

            }
        }
        //Go through oldCost Dictionary and set those rewards back to normal cost
        if(oldCostList.Count > 0)
        {
            foreach(KeyValuePair<string,int> reward in oldCostList)
            {
                CPH.UpdateRewardCost(reward.Key,reward.Value);
            }
            CPH.LogDebug($"[pwnRewardSale] - {oldCostList.Count} reward(s) have been set back to their original cost as they no longer fulfill the criteria.");
        }

        //Save Dictionary to Global Variable
        CPH.SetGlobalVar("pwnRewardSale_oldCost", updatedCostList, true);
        return true;
    }

    public bool DiscountMarkerRewards(string marker, int discount)
    {
        Dictionary<string, int> oldCostList = CPH.GetGlobalVar<Dictionary<string, int>>("pwnRewardSale_oldCost", true);
        if(oldCostList == null)
        {
            //If non existent set as empty dictionary
            oldCostList = new Dictionary<string,int>();
        }


        //Create new Dictionary
        Dictionary<string,int> updatedCostList = new Dictionary<string,int>();

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
            //Get Reward Data
            string rewardId = rewardList[i].Id;
            string title = rewardList[i].Title;
            bool isOurs = rewardList[i].IsOurs;
            int oldCost = rewardList[i].Cost;
            //Other Info
            int titleLen = title.Length;
            string markerCheck = title.Substring(titleLen - markerLen, markerLen);



            if(isOurs && markerCheck.Equals(marker) && !rewardId.Equals(currentReward))
            {
                //If reward is in the stored global list
                if(oldCostList.ContainsKey(rewardId))
                {
                //If yes get cost from list, as that's the original and remove keyvaluepair from list
                oldCost = oldCostList[rewardId];
                oldCostList.Remove(rewardId);
                }
                //Add Reward ID and Cost to updated cost dictionary
                updatedCostList.Add(rewardId,oldCost);
                //Calculate New Cost with Discount
                int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                //Update Reward Cost
                CPH.UpdateRewardCost(rewardId, newCost);
                //Debug Info
                CPH.LogDebug($"[pwnRewardSale] - {title} cost was updated from {oldCost} to {newCost} (Discount of {discount}%)");

            }
        }
        //Go through oldCost Dictionary and set those rewards back to normal cost
        if(oldCostList.Count > 0)
        {
            foreach(KeyValuePair<string,int> reward in oldCostList)
            {
                CPH.UpdateRewardCost(reward.Key,reward.Value);
            }
            CPH.LogDebug($"[pwnRewardSale] - {oldCostList.Count} reward(s) have been set back to their original cost as they no longer fulfill the criteria.");
        }

        //Save Dictionary to Global Variable
        CPH.SetGlobalVar("pwnRewardSale_oldCost", updatedCostList, true);
        return true;
    }

    public bool DiscountAllRewards(int discount)
    {
        Dictionary<string, int> oldCostList = CPH.GetGlobalVar<Dictionary<string, int>>("pwnRewardSale_oldCost", true);
        if(oldCostList == null)
        {
            //If non existent set as empty dictionary
            oldCostList = new Dictionary<string,int>();
        }


        //Create new Dictionary
        Dictionary<string,int> updatedCostList = new Dictionary<string,int>();

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
            //Get Reward Data
            string rewardId = rewardList[i].Id;
            string title = rewardList[i].Title;
            bool isOurs = rewardList[i].IsOurs;
            int oldCost = rewardList[i].Cost;

            if(isOurs && !rewardId.Equals(currentReward))
            {
                //If reward is in the stored global list
                if(oldCostList.ContainsKey(rewardId))
                {
                    //If yes get cost from list, as that's the original and remove keyvaluepair from list
                    oldCost = oldCostList[rewardId];
                    oldCostList.Remove(rewardId);
                }
                //Add Reward ID and Cost to updated cost dictionary
                updatedCostList.Add(rewardId,oldCost);
                //Calculate New Cost with Discount
                int newCost = (int)Math.Floor(oldCost - (oldCost * (Math.Round((double)discount / 100, 2))));
                //Update Reward Cost
                CPH.UpdateRewardCost(rewardId, newCost);
                //Debug Info
                CPH.LogDebug($"[pwnRewardSale] - {title} cost was updated from {oldCost} to {newCost} (Discount of {discount}%)");

            }
        }
        //Go through oldCost Dictionary and set those rewards back to normal cost
        if(oldCostList.Count > 0)
        {
            foreach(KeyValuePair<string,int> reward in oldCostList)
            {
                CPH.UpdateRewardCost(reward.Key,reward.Value);
            }
            CPH.LogDebug($"[pwnRewardSale] - {oldCostList.Count} reward(s) have been set back to their original cost as they no longer fulfill the criteria.");
        }

        //Save Dictionary to Global Variable
        CPH.SetGlobalVar("pwnRewardSale_oldCost", updatedCostList, true);
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