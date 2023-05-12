using UnityEngine;
using System.Collections;

using UIWidgets;

public class BarcodeAutoComplete : MonoBehaviour 
{
	void OnEnable()
	{
		Refresh();
	}

	void Refresh()
	{

		Autocomplete auto = GetComponent<Autocomplete>();

		foreach(SiteCode siteCode in App.instance.siteCodes.SiteCode)
		{
			auto.DataSource.Add(siteCode.siteCode + " - " + siteCode.productCode);
		}
	}
}
