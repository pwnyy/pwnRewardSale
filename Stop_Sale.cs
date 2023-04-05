using System;
using System.Collections.Generic;

public class CPHInline
{
	public bool Execute()
	{
		//Get old saved costs from rewards
		Dictionary<string,int> oldRewards = CPH.GetGlobalVar<Dictionary<string,int>>("pwnRewardSale_oldCost",true);
		
		//Go through dictionary
		foreach(KeyValuePair<string,int> reward in oldRewards)
		{
			//Update Rewards to old costs
			CPH.UpdateRewardCost(reward.Key,reward.Value);
		}
		
		//Unset global var
		CPH.UnsetGlobalVar("pwnRewardSale_oldCost",true);
		return true;
	}
}
