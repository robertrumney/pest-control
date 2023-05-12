#region NAMESPACES
using UnityEngine;
using UnityEngine.UI;

using System.IO;
using System.Collections;
using System.Collections.Generic;

using MaterialUI;
using UIWidgets;
using UIWidgetsSamples;
#endregion

public class App : MonoBehaviour 
{	
	public BatchNOEXP batchNOEXP; 

	public TaskRecClosePanel taskRecPanel;
	public GetPestSightingLog pestActionPanel;

	public string version="";
	public GameObject loadingBox;
	public Text loadingText;
	public GameObject alertBox;
	public Text alertText;



	public InstallationToUploadRoot nonUploadedInstalls;
	public TreatmentReport currentTreatmentReport;
	public IncompleteTreatmentReports incompleteTreatmentReports;

	public bool shittyInternet=false;

	#region VARIABLES
	public string selectedTreat;
	public Text uploadButtonText;
	//SINGLETON REFERENCE
	public static App instance ;
	public const string baseUrl = "{baseurl};

	public List<BranchDetails> branches;
	public List<string> pcoBranches;

	public SiteCode currentUnServiced;

	public PrimitiveBaseClass app;
	[Space(5)]
	public Session session;
	[Space(5)]
	public PageObjects pages;
	[Space(5)]
	public AppUIObjects ui;
	[Space(5)]
	public FancyDropDowns dropDowns;
	[Space(10)]

	public NotUploadedTreatments not;
	[Space(5)]

	//CACHEABLE WEB DATA
	public Clients clientList;
	public SiteCodes siteCodeCache;
	public SiteCodes siteCodes;
	public SiteMaps siteMaps;
	public SubClientJSON subClients; 
	public SubClientJSON subClientCache;
	private List<string> seqNos = new List<string>();
	public List<UnitInstallation> availableUnitCodes;

	[Space(10)]
	//SCAN/INSTALL DATA
	private Visit currentScan = new Visit();
	public PestScanData currentPestScan;
	public UnitScanData currentUnitScan;
	public Visits visitData;
	public Products productList;
	public List<UnitScanData> pestScans;

	[Space(10)]
	//PRIMITIVE DATA LISTS AND ARRAYS
	private string[] array;
	public List<string> selectedServices = new List<string>();
	private string[] catArray;
	public List<string> selectedCategories= new List<string>();
	private string[] productArray;
	public List<string> selectedproducts= new List<string>();
	public List<string> availableProducts= new List<string>();
	private string[] prepArray;
	public List<string> selectedPreps= new List<string>();
	public List<string> availablePreps= new List<string>();
	private string[] consumableArray;
	public List<string> selectedConsumables= new List<string>();
	public List<string> availableConsumables= new List<string>();
	private string[] speciesArray;
	public List<string> selectedSpecies= new List<string>();
	public List<string> availableSpecies= new List<string>();
	private string[] infest_array = new string[4];

	public List<string> currentQTY = new List<string>();

	public List<string> currentBatchNO = new List<string>();
	public List<string> currentEXPDate = new List<string>();

	private string[] appMethodArray;
	private List<int>_preps;

	[Space(20)]
	//PRESET DATA LISTS POPULATED IN UNITY INSPECTOR
	public List<Service> services;
	public List<Preparations> preparations;

	[System.Serializable]
	public class PrepJSON
	{
		public List<Preparations> preparations;
	}

	public List<Pest> pestList;
	public List<Pest> reccomendations;
	public List<string> callTypes;
	public List<string> tasks;
	public List<string> applicationMethods;
	#endregion 

	#region SIMPLE UTILITY METHODS
	//SIMPLE UTILITY METHODS
	public void Print(string x)
	{
		ui.debugText.text=x; 
	}

	public void CapitaliseInputField(InputField x)
	{
		x.text=x.text.ToUpper();
	}
	public void DebugText(string x)
	{
		ui.debugText.text=x;
		//print(x);
	}
	public string CurrentTime()
	{
		return System.DateTime.Now.ToString();
	}
	public void Alert(string x)
	{
		alertBox.SetActive(true);
		alertBox.transform.parent.gameObject.SetActive(true);
		alertText.text=x;

	}
	public void Quit()
	{
		Application.Quit ();
	}
	public string GetLNumber(string x)
	{
		string y="";
		foreach(Preparations prep in preparations){
			if(prep.name==x)
				y="L"+prep.lRegNumber;
		}
		return y;
	}
	public void Load(string x)
	{
		if(!app.loading)
		{
			loadingBox.SetActive(true);
			loadingBox.transform.parent.gameObject.SetActive(true);
			loadingText.text=x;
			app.loading=true;
			StartCoroutine(HideWindowAfterSeconds());
		}
	}
	public static Texture2D LoadPNG(string filePath) 
	{
		Texture2D tex = null;
		byte[] fileData;

		if (File.Exists(filePath))     {
			fileData = File.ReadAllBytes(filePath);
			tex = new Texture2D(2, 2,TextureFormat.RGB24,false);
			tex.LoadImage(fileData);
		}
		return tex;
	}
	public void Sync()
	{
		UploadAllInstallations();
		StartCoroutine(StartX());
		StartCoroutine(GetProducts());
	}
	#endregion

	#region STARTCOROUTINE() PROXY METHODS
	public void GetSitePlansButton()
	{
		StartCoroutine(GetSitePlans(session.companyID));
	}
	public void GetVisitDataButton()
	{
		StartCoroutine (GetVisitDataPost ());
	}
	public void UploadTreatmentReport(TreatmentReport treat)
	{
		StartCoroutine(UploadTreatmentReportGo(treat));
	}
    public void UploadVisitData(TreatmentReport treat,int selectedData,string treatID)
	{
		StartCoroutine(UploadIncompleteVisitData(treat,selectedData,treatID));
	}
	#endregion

	#region MONOBEHAVIOUR INHERITED CALLBACKS
	private void Awake()
	{
		instance = this;
		PopulateDropDowns ();
		StartCoroutine(SyncPreps());
	}

	IEnumerator SyncPreps()
	{
		//connect to api
		WWW www = new WWW("https://bidvestpestcontrol.co.za/apiv2/preplistsfromportal");
		yield return www;

		if(www.error==null)
		{
			//connection was succesful - decode data into preps
			PrepJSON json = JsonUtility.FromJson<PrepJSON>(www.text);
			preparations=json.preparations;

			//then save latest synced preps to cache
			string _json = JsonUtility.ToJson(json);
			AppLogic.instance.Save(_json,"PrepCache");
		}
		else
		{
			//there was an error connecting - if previous cache exists load that instead
			if(AppLogic.instance.FileExists("PrepCache"))
			{
				string _json = AppLogic.instance.Load("PrepCache");
				PrepJSON json = JsonUtility.FromJson<PrepJSON>(_json);
				preparations=json.preparations;
			}
		}
	}
	
	IEnumerator Steve()
	{
		PrepJSON json = new PrepJSON();
		json.preparations=preparations;
		string prepJSON = JsonUtility.ToJson(json);

		string php = "https://bidvestpestcontrol.co.za/apiv2/preplistsfromdevice";

		WWW www;
		Dictionary<string,string> postHeader = new Dictionary<string,string>();

		postHeader.Add("Content-Type", "application/json");
		var formData = System.Text.Encoding.UTF8.GetBytes(prepJSON);		
		www = new WWW(php, formData, postHeader);
		yield return www;

		print (www.text);
	}

	public void ResumeService(TreatmentReport treat)
	{
		    currentTreatmentReport = treat;
				

			if(!string.IsNullOrEmpty(currentTreatmentReport.treatmentReport.clientName))
			{
				if(currentTreatmentReport.callType=="Installation")
				{
					Alert("Incomplete Installation - " + currentTreatmentReport.treatmentReport.clientName + " Started at : " + currentTreatmentReport.treatmentReport.timeIn + " on: " + currentTreatmentReport.treatmentReport.treatmentDate );

					if(AppLogic.instance.FileExists("IncompleteSiteCodes"))
					{
						string jason = AppLogic.instance.Load("IncompleteSiteCodes");
						siteCodes = JsonUtility.FromJson<SiteCodes>(jason);
					}

					ShowInstallList();

					ui.clientNameText.text=currentTreatmentReport.treatmentReport.clientName;
					session.companyID=currentTreatmentReport.clientID;
					session.clientID=currentTreatmentReport.clientID;
					session.sequenceNumber=currentTreatmentReport.sequenceNumber;
					session.pcoID=currentTreatmentReport.pcoID;
					session.companyName=currentTreatmentReport.name;
					session.pcoName=currentTreatmentReport.pcoName;
					App.instance.session.currentCallType = App.instance.currentTreatmentReport.callType;
					string url = Application.persistentDataPath +"/"+"ClientSignature"+App.instance.currentTreatmentReport.clientID+".png";
					string url2 = Application.persistentDataPath +"/"+"PcoSignature"+App.instance.currentTreatmentReport.clientID+".png";
					currentTreatmentReport.clientSignature = LoadPNG(url);
					currentTreatmentReport.pcoSignature = LoadPNG(url2);
					app.installMode=true;
					pages.loginObject.SetActive(false);
					pages.installObject.SetActive(true);
					not.gameObject.SetActive(false);
					pages.selectClientObject.SetActive(false);
					pages.selectClientObject.SetActive(false);
					App.instance.ui.installClientName.text=session.companyName;

					StartCoroutine(GetSiteCodes(session.sequenceNumber));
				}else
				{
					Alert("Incomplete Treatment Report - " + currentTreatmentReport.treatmentReport.clientName + " Started at : " + currentTreatmentReport.treatmentReport.timeIn + " on: " + currentTreatmentReport.treatmentReport.treatmentDate );
					
					pages.loginObject.SetActive(false);
					pages.menuPage.SetActive(true);
					not.gameObject.SetActive(false);
					pages.selectClientObject.SetActive(false);
					ui.clientNameText.text=currentTreatmentReport.treatmentReport.clientName;
					session.companyID=currentTreatmentReport.clientID;
					session.sequenceNumber=currentTreatmentReport.sequenceNumber;
					session.clientID=currentTreatmentReport.clientID;
					session.pcoID=currentTreatmentReport.pcoID;
					session.companyName=currentTreatmentReport.name;
					session.pcoName=currentTreatmentReport.pcoName;
					App.instance.session.currentCallType = App.instance.currentTreatmentReport.callType;
					string url = Application.persistentDataPath +"/"+"ClientSignature"+App.instance.currentTreatmentReport.clientID+".png";
					string url2 = Application.persistentDataPath +"/"+"PcoSignature"+App.instance.currentTreatmentReport.clientID+".png";
					currentTreatmentReport.clientSignature = LoadPNG(url);
					currentTreatmentReport.pcoSignature = LoadPNG(url2);
					ui.pcoNameText.text=session.pcoName;

					StartCoroutine(GetSiteCodes(session.sequenceNumber));
				}
			}
	}

	private string branchArray;

	public void _Start()
	{

		if(AppLogic.instance.FileExists("NonUploadedInstalls"))
		{
			string _json = AppLogic.instance.Load("NonUploadedInstalls");
			nonUploadedInstalls = JsonUtility.FromJson<InstallationToUploadRoot>(_json);
		}

		UploadAllInstallations();

		if(PlayerPrefs.HasKey("pcoBranches"))
		{
			branchArray="";
			string[] y =PlayerPrefsX.GetStringArray("pcoBranches");
			pcoBranches.Clear();
			foreach(string x in y)
			{
				pcoBranches.Add(x);
				branchArray+=x+",";
			
			}

			branchArray=branchArray.Substring(0,branchArray.Length-1);
		}


		StartCoroutine(StartX());
		TrimData();

		#region SYNC INCOMPLETE REPORTS AND INSTALLS / PRODUCTS
		if(AppLogic.instance.FileExists("CurrentTreatmentReportData"))
		{
			string json = AppLogic.instance.Load("CurrentTreatmentReportData");
			currentTreatmentReport = JsonUtility.FromJson<TreatmentReport>(json);

			if(!string.IsNullOrEmpty(currentTreatmentReport.treatmentReport.clientName))
			{
				if(currentTreatmentReport.callType=="Installation")
				{
					Alert("Incomplete Installation - " + currentTreatmentReport.treatmentReport.clientName + " Started at : " + currentTreatmentReport.treatmentReport.timeIn + " on: " + currentTreatmentReport.treatmentReport.treatmentDate );
					print ("incomplete thing");

					App.instance.pages.selectClientObject.SetActive(false);

					if(AppLogic.instance.FileExists("IncompleteSiteCodes"))
					{
						string jason = AppLogic.instance.Load("IncompleteSiteCodes");
						siteCodes = JsonUtility.FromJson<SiteCodes>(jason);
						print ("loading the thing");
					}

					ShowInstallList();

					ui.clientNameText.text=currentTreatmentReport.treatmentReport.clientName;
					session.companyID=currentTreatmentReport.clientID;
					session.clientID=currentTreatmentReport.clientID;
					session.sequenceNumber=currentTreatmentReport.sequenceNumber;
					session.pcoID=currentTreatmentReport.pcoID;
					session.companyName=currentTreatmentReport.name;
					session.pcoName=currentTreatmentReport.pcoName;
					App.instance.session.currentCallType = App.instance.currentTreatmentReport.callType;
					string url = Application.persistentDataPath +"/"+"ClientSignature"+App.instance.currentTreatmentReport.clientID+".png";
					string url2 = Application.persistentDataPath +"/"+"PcoSignature"+App.instance.currentTreatmentReport.clientID+".png";
					currentTreatmentReport.clientSignature = LoadPNG(url);
					currentTreatmentReport.pcoSignature = LoadPNG(url2);
					app.installMode=true;
					pages.loginObject.SetActive(false);
					pages.installObject.SetActive(true);
					pages.selectClientObject.SetActive(false);
					App.instance.ui.installClientName.text=session.companyName;

					StartCoroutine(GetSiteCodes(session.sequenceNumber));
				}
				else
				{
					Alert("Incomplete Treatment Report - " + currentTreatmentReport.treatmentReport.clientName + " Started at : " + currentTreatmentReport.treatmentReport.timeIn + " on: " + currentTreatmentReport.treatmentReport.treatmentDate );

					App.instance.pages.selectClientObject.SetActive(false);

					if(AppLogic.instance.FileExists("IncompleteSiteCodes"))
					{
						string jason = AppLogic.instance.Load("IncompleteSiteCodes");
						siteCodes = JsonUtility.FromJson<SiteCodes>(jason);
					}
					pages.loginObject.SetActive(false);
					pages.menuPage.SetActive(true);
					ui.clientNameText.text=currentTreatmentReport.treatmentReport.clientName;
					session.companyID=currentTreatmentReport.clientID;
					session.sequenceNumber=currentTreatmentReport.sequenceNumber;
					session.clientID=currentTreatmentReport.clientID;
					session.pcoID=currentTreatmentReport.pcoID;
					session.companyName=currentTreatmentReport.name;
					session.pcoName=currentTreatmentReport.pcoName;
					App.instance.session.currentCallType = App.instance.currentTreatmentReport.callType;
					string url = Application.persistentDataPath +"/"+"ClientSignature"+App.instance.currentTreatmentReport.clientID+".png";
					string url2 = Application.persistentDataPath +"/"+"PcoSignature"+App.instance.currentTreatmentReport.clientID+".png";
					currentTreatmentReport.clientSignature = LoadPNG(url);
					currentTreatmentReport.pcoSignature = LoadPNG(url2);
					ui.pcoNameText.text=session.pcoName;

					StartCoroutine(GetSiteCodes(session.sequenceNumber));
				}
			}
		}

		if(AppLogic.instance.FileExists("productList"))
		{
			string jason2 = AppLogic.instance.Load("productList");
			productList = JsonUtility.FromJson<Products>(jason2);
			PopulateCodes();
		}
		else
		{
			StartCoroutine (GetProducts ());
		}

		if(AppLogic.instance.FileExists("IncompleteTreatmentReports"))
		{
			if (!debugMode) 
			{
				string json3 = AppLogic.instance.Load ("IncompleteTreatmentReports");
				incompleteTreatmentReports = JsonUtility.FromJson<IncompleteTreatmentReports> (json3);
			} else 
			{
				string json3 = System.Text.Encoding.UTF8.GetString(debugJson.bytes, 3, debugJson.bytes.Length - 3);
				json3 = json3.Replace ("/", "");
				incompleteTreatmentReports = JsonUtility.FromJson<IncompleteTreatmentReports> (json3);
			}

			foreach(TreatmentReport treat in incompleteTreatmentReports.reports)
			{
				treat.LoadSignature();
			}
		}


		#endregion
	
		if(PlayerPrefs.GetInt("scannerCrashed")==1)
		{
			pages.menuPage.SetActive(false);
			pages.routineScanner.SetActive(true);
			//scann
			PlayerPrefs.SetInt("scannerCrashed",0);
		}

	}
	#endregion

	#region COROUTINES
	//COROUTINES
	private IEnumerator UploadInstallationData(InstallationToUpload unit)
	{
		WWWForm post = new WWWForm();
		post.AddField("company_ID",unit.clientID);
		post.AddField("pcoName",unit.pcoName);
		post.AddField("pcoID",unit.pcoID);
		post.AddField("productCode",unit.productName);
		post.AddField("siteLocation",unit.siteName);
		post.AddField("barcode",unit.barcode);
		post.AddField("service",unit.service);
		post.AddField("seqNO",unit.seqNo);

		string php = baseUrl+"/api/InsertSiteInstallation.php";

		WWW www = new WWW(php,post);
		yield return www;
//		print (www.text);
		if(www.error==null)
		{
			if(www.text.Contains("Added Data Succesfully"))
			{
				unit.barcode="UPLOADED";
				string _json = JsonUtility.ToJson(nonUploadedInstalls);// print (_json);
				AppLogic.instance.Save(_json,"NonUploadedInstalls");// not uploaded
			}else{
				StartCoroutine(UploadInstallationData(unit));
			}
		}
	}
	private IEnumerator StartX()
	{
		if(!shittyInternet)
		{
			Load("Syncing Data");
			DebugText("LOADING CLIENT DATA FROM WEB");
			WWWForm post = new WWWForm();
			post.AddField("table","tbl_QC_Clients_REPLACE");
			post.AddField("name","client");
			post.AddField("branch",branchArray);
			WWW www = new WWW(baseUrl+"/api/branch_clients.php",post);

			yield return www;

			int timeOut = 15; 

			while (!www.isDone) 
			{
				timeOut--;
				if (timeOut <= 0) 
				{
					if(AppLogic.instance.FileExists("clientList"))
					{
						string jason = AppLogic.instance.Load("clientList");
						clientList = JsonUtility.FromJson<Clients>(jason);
						PopulateAutoComplete();
						DebugText("NO CONNECTION - CLIENTS LOADED FROM CACHE");
						
						app.loading=false;

						yield break;
					}
				}
				yield return new WaitForSeconds (1);
			}
				
			app.loading=false;

			if(www.error==null)
			{
				DebugText("CLIENT DATA LOADED FROM WEB");
				string json = www.text.Trim();
				clientList = JsonUtility.FromJson<Clients>(json);
				AppLogic.instance.Save(json,"clientList");

				PopulateAutoComplete();
			}
			else
			{
				if(AppLogic.instance.FileExists("clientList"))
				{
					string jason = AppLogic.instance.Load("clientList");
					clientList = JsonUtility.FromJson<Clients>(jason);
					PopulateAutoComplete();
					DebugText("NO CONNECTION - CLIENTS LOADED FROM CACHE");
				}
			}
		}else
		{
			if(AppLogic.instance.FileExists("clientList"))
			{
				string jason = AppLogic.instance.Load("clientList");
				clientList = JsonUtility.FromJson<Clients>(jason);
				PopulateAutoComplete();
				DebugText("NO CONNECTION - CLIENTS LOADED FROM CACHE");
			}

		}
	}

	private IEnumerator GetProducts()
	{
		Load("Retrieving Product Data");
		WWWForm post = new WWWForm();
		post.AddField("table","PRODUCT_CODES");
		post.AddField("name","products");
		WWW www = new WWW(baseUrl+"/api/clients.php",post);
		yield return www;
		app.loading=false;
		if(www.error==null)
		{
			string json = www.text.Trim();
			productList = JsonUtility.FromJson<Products>(json);
			AppLogic.instance.Save(json,"productList");
			PopulateCodes();
		}
	}

	private IEnumerator WaitForInstallationList()
	{
		while(true)
		{
			if(dropDowns.installList.gameObject.activeInHierarchy)
			{
				dropDowns.installList.Clear();
				foreach(InstallationToUpload unit in nonUploadedInstalls.nonUploadedInstalls)
				{
					if(unit.clientID==session.companyID)
					{
						if(!IsDuplicateSiteCode(unit.siteName))
						{
							string title = unit.siteName + " - " + unit.barcode + " - " + unit.productName;
							dropDowns.installList.Add(title);
						}
					}
				}
				ShowInstallList();
				yield break;
			}
			yield return null;
		}
	}


	private IEnumerator GetSitePlans(string id)
	{
		WWWForm post = new WWWForm();
		post.AddField("table","SITE_MAPS");
		post.AddField("name","SiteMap");
		post.AddField("where","company_ID");
		post.AddField("like",id);
		WWW www = new WWW(baseUrl+"/api/select.php",post);
		yield return www;
		if(www.error==null)
		{
			string json = www.text.Trim();
			if(json.Contains("No results found"))
			{
				Alert("No Site Maps uploaded yet for this client - please contact your branch administrator");
			}else{
				siteMaps = JsonUtility.FromJson<SiteMaps>(json);
				foreach(SiteMap site in siteMaps.SiteMap)
				{
					ListViewImagesItem item = new ListViewImagesItem();
					item.Url=baseUrl+"/site_plans/"+site.filename;
					dropDowns.listViewImages.Add(item);
				}
			}
		}
	}
	private IEnumerator GetAllSeqNOS()
	{
		WWWForm post = new WWWForm();
		post.AddField("table","tbl_QC_Address_REPLACE");
		post.AddField("name","SubClient");
		WWW www = new WWW(baseUrl+"/api/clients.php",post);
		yield return www;

		if(www.error==null)
		{
			string json = www.text.Trim();

			if(json=="[]")
			{
				if(AppLogic.instance.FileExists("tbl_QC_Address_REPLACE"))
				{
					string jason = AppLogic.instance.Load("tbl_QC_Address_REPLACE");
					subClientCache = JsonUtility.FromJson<SubClientJSON>(jason);
				}
			}else{
				subClientCache = JsonUtility.FromJson<SubClientJSON>(json);
				AppLogic.instance.Save(json,"tbl_QC_Address_REPLACE");
			}

		}else{

			if(AppLogic.instance.FileExists("tbl_QC_Address_REPLACE"))
			{
				string json = AppLogic.instance.Load("tbl_QC_Address_REPLACE");
				subClientCache = JsonUtility.FromJson<SubClientJSON>(json);
			}
		}
	}

	private IEnumerator GetAllSiteCodes()
	{
		WWWForm post = new WWWForm();
		post.AddField("table","SITE_INSTALLATIONS");
		post.AddField("name","SiteCode");
		WWW www = new WWW(baseUrl+"/api/clients.php",post);
		yield return www;
		if(www.error==null)
		{
			string json = www.text.Trim();
			if(json!="[]")
			{
				siteCodeCache = JsonUtility.FromJson<SiteCodes>(json);
				AppLogic.instance.Save(json,"SITE_INSTALLATIONS");
			}

		}
			else
		{

			if(AppLogic.instance.FileExists("SITE_INSTALLATIONS"))
			{
				string json = AppLogic.instance.Load("SITE_INSTALLATIONS");
				siteCodeCache = JsonUtility.FromJson<SiteCodes>(json);
			}
		}
	}

	//GET SITE CODES - IE - INSTALLED UNITS FOR A SPECIFIC SEQUENCE NUMBER
	private IEnumerator GetSiteCodes(string id)
	{

		//print ("getting the thing " + id);

		App.instance.Load("FETCHING ALL BARCODES AND UNIT DATA FROM SERVER");
		App.instance.alertBox.SetActive(false);

		siteCodes.SiteCode = new List<SiteCode>();

		foreach(SiteCode sc in siteCodeCache.SiteCode)
		{
			if(sc.SequenceNo==id)
			{
				siteCodes.SiteCode.Add(sc);
			}
		}

		WWWForm post = new WWWForm();
		post.AddField("table","SITE_INSTALLATIONS");
		post.AddField("name","SiteCode");
		post.AddField("where","SequenceNo");
		post.AddField("like",id);

		WWW www = new WWW(baseUrl+"/api/select.php",post);
		yield return www;

		App.instance.loadingBox.SetActive(false);
		App.instance.loadingBox.transform.parent.gameObject.SetActive(false);

		if(www.error==null)
		{
			string json = www.text.Trim();

			//print (json);

			if(json.Contains("No results found"))
			{
			}else{
				siteCodes = JsonUtility.FromJson<SiteCodes>(json);
				DebugText("RETRIEVED BARCODE DATA");
			}
			//dialog.Hide();
		}else{
			//dialog.Hide();
		}

		if(dropDowns.scannedList.gameObject.activeInHierarchy)
		{
			ViewScannedUnits();
		}
	}



	private IEnumerator GetTreatmentData()
	{
		WWWForm post = new WWWForm();
		post.AddField("table","VISIT_DATA");
		WWW www = new WWW(baseUrl+"/api/clients.php",post);
		yield return www;
		if(www.error==null)
		{
			string json = www.text.Trim();
			AppLogic.instance.Save(json,"VISIT_DATA");

		}
	}
	private IEnumerator GetVisitData()
	{
		WWWForm post = new WWWForm();
		post.AddField("table","VISIT_DATA");
		WWW www = new WWW(baseUrl+"/api/clients.php",post);
		yield return www;
		if(www.error==null)
		{
			string json = www.text.Trim();
			AppLogic.instance.Save(json,"VISIT_DATA");
		}
	}

	private IEnumerator GetVisitDataPost()
	{
		WWWForm post = new WWWForm ();
		post.AddField ("table", "VISIT_DATA");
		post.AddField ("where", "company_ID");
		post.AddField ("name", "visit");
		post.AddField ("like", session.clientID);
		string php = baseUrl+"/api/select.php";
		WWW www = new WWW (php, post);
		yield return www;
		
		if (www.error == null) 
		{
			string json = www.text.Trim ();
			if(!json.Contains("No results found"))
			{
				visitData = JsonUtility.FromJson<Visits> (json);
				dropDowns.visitDataList.Clear ();
				dropDowns.outstandingTasks.Clear ();

				foreach (Visit visit in visitData.visit) 
				{
					if (!string.IsNullOrEmpty (visit.recommendations) && visit.recommendations!="None") 
					{
						if (visit.reccomendationStatus=="open") 
						{
							string concat = visit.id + "~" + visit.recommendations + " - " + visit.siteLocation + " : " + visit.detector;

							if(!IsDuplicateRecommendation(concat))
								dropDowns.visitDataList.Add (concat);
						}
					}
					if (!string.IsNullOrEmpty (visit.tasks) && visit.tasks != "None") 
					{
						if (visit.taskStatus=="open") 
						{
							string concat =visit.id + "~" + visit.tasks + " - " + visit.siteLocation + " : " + visit.detector;

							if(!IsDuplicateTask(concat))
								dropDowns.outstandingTasks.Add (concat);
						}
					}
				}
			}
		}
	}

	private bool IsDuplicateRecommendation(string x)
	{
		bool dupe=false;
		foreach(string visit in dropDowns.visitDataList.DataSource)
		{
			if(x==visit)
				dupe=true;
		}
		return dupe;
	}

	private bool IsDuplicateTask(string x)
	{
		bool dupe=false;
		foreach(string visit in dropDowns.outstandingTasks.DataSource)
		{
			if(x==visit)
				dupe=true;
		}
		return dupe;
	}

	private IEnumerator GetSequenceNo(string x)
	{
		subClients = new SubClientJSON();
		dropDowns.sequenceNumberSelect.ClearData();
		dropDowns.sequenceNumberSelect.buttonTextContent.text="Select Sequence No#";
		seqNos.Clear();

		WWWForm post = new WWWForm();
		post.AddField("table","tbl_QC_Address_REPLACE");
		post.AddField("where","CustomerID");
		post.AddField("like", x );
		post.AddField("name","SubClient");

		WWW www = new WWW(baseUrl+"/api/select.php",post);

		yield return www;

		if(www.error==null)
		{
			string jason = www.text.Trim();

			if(jason=="No results found")
			{
				GetSequenceNoFromCache(x);
			}else{

				subClients = JsonUtility.FromJson<SubClientJSON>(www.text.Trim());
				if(subClients.SubClient.Count>0)
				{
					foreach(SubClient s_n in subClients.SubClient)
					{
						dropDowns.sequenceNumberSelect.AddData(new OptionData(s_n.SequenceNo+" : "+s_n.DeliveryDescription,null,null));
						seqNos.Add(s_n.SequenceNo);
					}
				}
			}

		}else{
			GetSequenceNoFromCache(x);
		}
	}
	private IEnumerator HideWindowAfterSeconds()
	{
		while(app.loading)
		{
			yield return null;
		}
		
		loadingBox.SetActive(false);
		loadingBox.transform.parent.gameObject.SetActive(false);
	}
	#endregion

	#region UPLOADING TREATMENT PREP DATA COROUTINES
	private IEnumerator UploadTreatmentReportGo(TreatmentReport treat)
	{
		treat.appVersion=version;

		if(LocationServices.ready)
		{
			treat.lat=LocationServices.lat.ToString();
			treat.lon=LocationServices.lon.ToString();
		}

		DebugText("uploading treatment report");
		WWWForm post = new WWWForm();

		string jason = JsonUtility.ToJson(treat);
		post.AddField("pcoID",session.pcoID);
		post.AddField("json",jason);
		string php = baseUrl+"/api/DecodeJson2.php";

		Print("loaded treatment report packet");
		WWW www = new WWW(php,post);
		yield return www;

		string trimmed = www.text.Trim();

		DebugText("Uploaded Treatment");
		Print("SUCCESS :) UPLOADED Treatment report and the id is: " + trimmed);
		string x = trimmed;
		yield return new WaitForSeconds(0.5f);
		Print("uploading signature 1");
		StartCoroutine(UploadSignature(treat.clientSignature as Texture2D,x,"client"));
		yield return new WaitForSeconds(0.5f);
		Print("uploading signature 2");
		StartCoroutine(UploadSignature(treat.pcoSignature as Texture2D,x,"pco"));
	}

	private IEnumerator UploadTreatmentPrep(string x,TreatmentPrep prep)
	{
		Print ("uploading prep " + prep.product);
		WWWForm post = new WWWForm();
		post.AddField("treatmentReportID",x);
		post.AddField("product",prep.product);
		post.AddField("productQuantity",prep.productQuantity);
		post.AddField("prepUsed",prep.prepUsed);
		post.AddField("prepLNumber",prep.prepLNumber);
		post.AddField("prepQuantity",prep.prepQuantity);
		post.AddField("appMethod",prep.appMethod);
		post.AddField("rootNode",prep.rootNode);
		string php = baseUrl+"/api/InsertTreatmentPrep.php";
		DebugText("Uploading Treatment Preparation");
		WWW www = new WWW(php,post);
		
		yield return www;
		
		if(www.text.Contains("Added Data Succesfully"))
		{
			app.prepsToGo--;
		}else{
			Print("treat prep failed");
			StartCoroutine(UploadTreatmentPrep(x,prep));
		}
	}

    private IEnumerator UploadIncompleteVisitData(TreatmentReport treat,int selectedData,string treatID)
	{
		Print ("adding visit data");
		WWWForm post = new WWWForm();
        post.AddField("treatID", treatID);
		post.AddField("company_ID",treat.currentVisitData[selectedData].company_ID);
		post.AddField("seqNO",treat.currentVisitData[selectedData].SequenceNo);
		post.AddField("pcoName",session.pcoName);
		post.AddField("pcoID",session.pcoID);
		post.AddField("service",treat.currentVisitData[selectedData].service);
		post.AddField("siteLocation",treat.currentVisitData[selectedData].siteLocation);
		post.AddField("productCode",treat.currentVisitData[selectedData].productCode);
		post.AddField("lat",treat.currentVisitData[selectedData].lat);
		post.AddField("lon",treat.currentVisitData[selectedData].lon);
		post.AddField("comments",treat.currentVisitData[selectedData].comments);
		post.AddField("recommendations",treat.currentVisitData[selectedData].recommendations);
		post.AddField("reccomendationStatus",treat.currentVisitData[selectedData].reccomendationStatus);
		post.AddField("tasks",treat.currentVisitData[selectedData].tasks);
		post.AddField("taskStatus",treat.currentVisitData[selectedData].taskStatus);
		post.AddField("activity",treat.currentVisitData[selectedData].activity);
		post.AddField("activity_type",treat.currentVisitData[selectedData].activity_type);
		post.AddField("species",treat.currentVisitData[selectedData].species);
		post.AddField("visitType",treat.currentVisitData[selectedData].visitType);
		post.AddField("nonServiceNO",treat.currentVisitData[selectedData].nonServiceNO);
		post.AddField("nonServiceReason",treat.currentVisitData[selectedData].nonServiceReason);
		string php = baseUrl+"/api/InsertVisitData.php";
		WWW www = new WWW(php,post);
		yield return www;

		if(www.text.Contains("Added Data Succesfully"))
		{
			app.dataToGo--;
		}else{
            StartCoroutine(UploadIncompleteVisitData(treat,selectedData,treatID));
		}
	}

	private IEnumerator WaitForTheEnd()
	{
		DialogProgress dialog = DialogManager.ShowProgressCircular("Uploading", "Loading", MaterialIconHelper.GetIcon(MaterialIconEnum.HOURGLASS_EMPTY));
		while(true)
		{
			if(app.uploadsToGo==0 && app.prepsToGo==0 && app.dataToGo ==0)
			{
				DebugText("Succesfully Uploaded All Treatment Data");
				foreach(TreatmentReport treat in incompleteTreatmentReports.reports)
				{
					string compare = treat.name + " - " + treat.treatmentReport.treatmentDate;
					if (!treat.name.Contains ("SUCCESSFULLY UPLOADED") && compare == selectedTreat) 
					{
						treat.name = treat.name + " -SUCCESSFULLY UPLOADED";
						RefreshIncomplete ();
					}
				}

				string json3 = JsonUtility.ToJson(App.instance.incompleteTreatmentReports); 
				AppLogic.instance.Save(json3,"IncompleteTreatmentReports");
				dialog.Hide();
				yield break;
			}else{
				yield return null;
			}
		}
	}

	private IEnumerator UploadSignature(Texture2D tex, string id,string type)
	{
		string php = baseUrl+"/api/uploadSignature.php";
		var form = new WWWForm();
		form.AddField("frameCount", Time.frameCount.ToString());
		form.AddField("type",type);
		form.AddField("id",id);
		if(tex)
		{
			tex.Compress(false);
			Texture2D tax = new Texture2D(tex.width,tex.height,TextureFormat.ARGB32,false);
			tax.SetPixels(tex.GetPixels());
			tax.Apply();
			var bytes = tax.EncodeToPNG();
			form.AddBinaryData("file", bytes, id+".png", "image/png");
		}
		var w = new WWW(php, form);
		yield return w;
		
		app.uploadsToGo=0;
		Print ("Done");
	}
	#endregion

	#region CHECKBOXES
	bool setProductOnce=false;
	//Selecting Service
	public void SelectServiceCheckBox()
	{
		List<string> serv = new List<string>();

		foreach(Service service in services)
		{
			serv.Add(service.name);
		}
		array = serv.ToArray();

		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
			(
				array, 
				OnServiceCheckboxValidateClicked, 
				"OK", 	
				"Select Service", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
				}, "CANCEL"
			);

		dialog.multiSelect=false;

		for(int i=0;i<selectedServices.Count;i++)
		{
			for(int x =0;x<dialog.optionList.Length;x++)
			{
				if(selectedServices[i]==dialog.optionList[x])
				{
					dialog.selectedIndexes[x]=true;
					dialog.selectionItems[x].itemCheckbox.Toggle(true);
					dialog.selectionItems[x].itemCheckbox.toggle.isOn=true;
					dialog.Show();
				}
			}
		}
	}
	private void OnServiceCheckboxValidateClicked(bool[] resultArray)
	{
		selectedServices.Clear();
		for(int x=0;x<array.Length;x++)
		{
			if(resultArray[x])
			{
				selectedServices.Add(array[x]);
			}
		}
		SelectService();
	}
	//Selecting product
	public void SelectProductCheckBox()
	{
		List<string> serv = new List<string>();
		foreach(string strong in availableProducts)
		{
			serv.Add(strong);
		}
		productArray = serv.ToArray();
		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
			(
				productArray, 
				OnProductCheckboxValidateClicked, 
				"OK", 	
				"Select Product", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
				}, "CANCEL"
			);
		for(int i=0;i<selectedproducts.Count;i++)
		{
			for(int x =0;x<dialog.optionList.Length;x++)
			{
				if(selectedproducts[i]==dialog.optionList[x])
				{
					dialog.selectedIndexes[x]=true;
					dialog.selectionItems[x].itemCheckbox.Toggle(true);
					dialog.selectionItems[x].itemCheckbox.toggle.isOn=true;
					dialog.Show();
				}
			}
		}
	}
	private void OnProductCheckboxValidateClicked(bool[] resultArray)
	{
		bool isOther=false;
		selectedproducts.Clear();
		for(int x=0;x<productArray.Length;x++)
		{
			if(resultArray[x])
			{
				if(!productArray[x].ToUpper().Contains("OTHER"))
				{
					selectedproducts.Add(productArray[x]);
				}else{
					isOther=true;
				}
			}
		}

		if(isOther)
		{
			ProductFreeText();
		}
	}
	public void ProductFreeText()
	{
		session.freeTextType="Product";
		pages.freeTextPanel.SetActive(true);
		ui.freeText.text="";
		ui.freeText.GetComponent<MaterialInputField>().hintText="Please enter product type";
	}
	public void EnterProductFreeText()
	{
		pages.freeTextPanel.SetActive(false);
		string x = ui.freeText.text;
		selectedproducts.Clear();
		selectedproducts.Add(x);
		ui.installProductCode.text=x;
	}
	//Selecting Pest Type
	public void SelectCategoryCheckBox()
	{
		List<string> serv = new List<string>();
		foreach(Pest data in pestList)
			serv.Add(data.name);

		catArray = serv.ToArray();
		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
		(
				catArray, 
				OnCategoryCheckboxValidateClicked, 
				"OK", 	
				"Select Pest Type", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
				   CancelFirstMenu();
				}, "BACK"
		);
		dialog.multiSelect=false;
		dialog.Show();
	}

	private void CancelFirstMenu()
	{

		if(currentPestScan.species.Count>0)
		{
			AddAnotherPestMaybe();
		}else{
			if(!app.installMode)
			{
				pages.routineScanner.SetActive(true);
				pages.visitDataObject.SetActive(false);
			}
			else
	        {
				pages.installScanner.SetActive(true);
				pages.visitDataObject.SetActive(false);
			}
		}
			
	}

	private void OnCategoryCheckboxValidateClicked(bool[] resultArray)
	{
		availableSpecies.Clear();
		selectedSpecies.Clear();
		selectedConsumables.Clear();
		availablePreps.Clear();
		selectedPreps.Clear();
		selectedCategories.Clear();
		currentPestScan = new PestScanData();
		bool isNone=false;
		for(int x=0;x<catArray.Length;x++)
		{
			if(resultArray[x])
			{
				selectedCategories.Add(catArray[x]);
				currentPestScan.type=catArray[x];
				foreach(Pest pest in pestList)
				{
					if(pest.name==catArray[x])
					{
						if(pest.type.Length>0)
						{
							foreach(string type in pest.type)
							{
								if (!type.ToUpper ().Contains ("NONE")) {
									availableSpecies.Add (type);
								}else{
									isNone=true;
									if (!type.ToUpper ().Contains ("OTHER")) {
										SelectConsumableCheckBox();
										return;
									}else{
										PestFreeText();
										SelectPrepCheckBox();
										return;
									}
								}
							}
						}else{
							isNone=true;
							SelectConsumableCheckBox();
							return;
						}
					}
				}
			}
		}
		if(isNone)
		{
			availableSpecies.Clear();
		}
		string result = "";
		for (int i = 0; i < selectedCategories.Count; i++)
		{
			result += selectedCategories[i] + ((i < selectedCategories.Count - 1) ? " ," : "");
		}
		if(selectedCategories.Count > 0)
		{
			if(!result.ToUpper().Contains("OTHER"))
			{
				SelectPestCats();

				if(!app.installMode)
				{
					SelectService();
				}
				if(availableSpecies.Count>0)
				{
					SelectSpeciesCheckBox();
				}else{
					SelectInfestationCheckBox();
				}

			}else{
				PestFreeText();
			}
		}else{
			SelectCategoryCheckBox();
			Alert("You must select a category!!");
		}
	}
	public void PestFreeText()
	{
		session.freeTextType="Pest";
		pages.freeTextPanel.SetActive(true);
		ui.freeText.text="";
		ui.freeText.GetComponent<MaterialInputField>().hintText="Please enter pest type";
	}
	public void EnterPestFreeText()
	{
		pages.freeTextPanel.SetActive(false);
		string x = ui.freeText.text;
		selectedCategories.Add(x);
		availablePreps.Clear();
		availableConsumables.Clear();
		currentPestScan.type=x;
	
		foreach(Preparations prep in preparations)
		{
			availablePreps.Add(prep.name);
		}
		foreach(Service serv in services)
		{
			foreach(string consumable in serv.consumables)
			{
				availablePreps.Add(consumable);
			}
		}
		SelectInfestationCheckBox();
	}
	
	//Selecting Pest Species
	public void SelectSpeciesCheckBox()
	{
		List<string> serv = new List<string>();
		foreach(string strong in availableSpecies)
		{
			serv.Add(strong);
		}
		serv.Add("Other");
		serv.Add("None");
		speciesArray = serv.ToArray();

		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
		(
				speciesArray, 
				OnSpeciesCheckboxValidateClicked, 
				"OK", 	
				"Select Species", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
					SelectCategoryCheckBox();
				}, "BACK"
		);
		for(int i=0;i<selectedSpecies.Count;i++)
		{
			for(int x =0;x<dialog.optionList.Length;x++)
			{
				if(selectedSpecies[i]==dialog.optionList[x])
				{
					dialog.selectedIndexes[x]=true;
					dialog.selectionItems[x].itemCheckbox.Toggle(true);
					dialog.selectionItems[x].itemCheckbox.toggle.isOn=true;
					dialog.Show();
				}
			}
		}
	}
	private void OnSpeciesCheckboxValidateClicked(bool[] resultArray)
	{
		bool isOther=false;
		for(int x=0;x<speciesArray.Length;x++)
		{
			if(resultArray[x])
			{
				if(!speciesArray[x].ToUpper().Contains("OTHER"))
				{
					selectedSpecies.Add(speciesArray[x]);
					currentPestScan.species.Add(speciesArray[x]);
				}else{
					isOther=true;
				}
			}
		}
		if(selectedSpecies.Count<=0)
		{
			if(!isOther)
			{
				SelectSpeciesCheckBox();
			}else{
				SpeciesFreeText();
			}
		}else{
			
			if(!isOther)
			{
				SelectInfestationCheckBox();
			}else{
				SpeciesFreeText();
			}
		}
	}

	int currentInfest;
	public List<string> infestList;
	//Selecting pest infestation level
	public void SelectInfestationCheckBox()
	{
		currentInfest=0;
		currentPestScan.infestationLevels.Clear();
		infestList.Clear();

		foreach(string strung in selectedSpecies)
		{
			infestList.Add(strung);
			currentPestScan.infestationLevels.Add("");
		}

		bool flying = false;
		if(selectedCategories[0]=="Flying Insects")
			flying=true;
		if(selectedCategories[0]=="Moths")
			flying=true;
		if(selectedCategories[0]=="Bees")
			flying=true;
		if(selectedCategories[0]=="Wasps")
			flying=true;

		if(flying)
		{
			pages.numericalPestData.SetActive(true);
			ui.numericalInputField.text=selectedSpecies[0];
			ui.numericalInputField.Select();
			currentInfest=infestList.Count;

		}else{
			currentInfest=infestList.Count;
			if(infestList[0].ToUpper()!="NONE")
			{
				DoSelectInfestationCheckBox(infestList[0]);
			}else{
				if(availablePreps.Count > 0)
				{
					SelectPrepCheckBox();
				}else{
					AddAnotherPestMaybe();
				}
			}
		}
	}

	public void EndSelectNumericalInput()
	{
		if(string.IsNullOrEmpty(ui.numericalInputField.text) || ui.numericalInputField.text.Length > 4)
		{

		}
		else
		{
			if(currentInfest > 0)
			{
				int current = (selectedSpecies.Count)-currentInfest;
				currentPestScan.infestationLevels[current]=ui.numericalInputField.text;
				currentInfest--;

				if(currentInfest==0)
				{
					pages.numericalPestData.SetActive(false);
					if(availablePreps.Count > 0)
					{
						SelectPrepCheckBox();
					}else{
						AddAnotherPestMaybe();
					}
				}else{
					int opposite = (selectedSpecies.Count-1)-currentInfest;
					pages.numericalPestData.SetActive(true);
					ui.numericalInputField.text=selectedSpecies[opposite];
					ui.numericalInputField.Select();
					currentPestScan.intensity=ui.numericalInputField.text;
				}

			}else
			{
				pages.numericalPestData.SetActive(false);
				if(availablePreps.Count > 0)
				{
					SelectPrepCheckBox();
				}else{
					AddAnotherPestMaybe();
				}
			}
		}
	}

	public void DoSelectInfestationCheckBox(string species)
	{
		infest_array = new string[4];
		infest_array[0]="None";
		infest_array[1]="Low";
		infest_array[2]="Medium";
		infest_array[3]="High";
		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
		(
				infest_array, 
				OnInfestationCheckboxValidateClicked, 
				"OK", 	
				"Select " +species+ " Infestation Type", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
					SelectSpeciesCheckBox();
				}, "BACK"
		);
		dialog.multiSelect=false;
		dialog.selectedIndexes[0]=true;
		dialog.selectionItems[0].itemCheckbox.Toggle(true);
		dialog.selectionItems[0].itemCheckbox.toggle.isOn=true;
		dialog.Show();
	}
	private void OnInfestationCheckboxValidateClicked(bool[] resultArray)
	{
		if(currentInfest > 0)
		{
			for(int x=0;x<resultArray.Length;x++)
			{
				if(resultArray[x])
				{
					int current = (selectedSpecies.Count)-currentInfest;
					currentPestScan.infestationLevels[current]=ui.numericalInputField.text;
					currentInfest--;
					int opposite = (selectedSpecies.Count-1)-currentInfest;
					currentPestScan.infestationLevels[current]=infest_array[x];
					current++;
					if(currentInfest==0)
					{
						if(availablePreps.Count > 0)
						{
							SelectPrepCheckBox();
						}else{
							AddAnotherPestMaybe();
						}
					}else{
						DoSelectInfestationCheckBox(selectedSpecies[current]);
					}

				}
			}
		}else
		{
			for(int x=0;x<resultArray.Length;x++)
			{
				if(resultArray[x])
				{
					currentPestScan.intensity=infest_array[x];
				}
			}
			if(availablePreps.Count > 0)
			{
				SelectPrepCheckBox();
			}else{
				AddAnotherPestMaybe();
			}
		}
	}

	//Selecting preparations
	public void SelectPrepCheckBox()
	{
		List<string> serv = new List<string>();
		foreach(string strong in availablePreps)
		{
			serv.Add(strong);
		}
		serv.Add("Other");
		prepArray = serv.ToArray();

		DialogCheckboxList dialog = DialogManager.ShowCheckboxListQTY
		(
				prepArray, 
				OnPrepCheckboxValidateClicked, 
				"OK", 	
				"Select Preparations", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
					SelectInfestationCheckBox();
				}, "BACK"
		);
		
		for(int i=0;i<selectedPreps.Count;i++)
		{
			for(int x =0;x<dialog.optionList.Length;x++)
			{
				if(selectedPreps[i]==dialog.optionList[x])
				{
					dialog.selectedIndexes[x]=true;
					dialog.selectionItems[x].itemCheckbox.Toggle(true);
					dialog.selectionItems[x].itemCheckbox.toggle.isOn=true;
					dialog.Show();
				}
			}
		}
	}

	public bool checkingBatches=false;

	IEnumerator CheckAllBatches(bool[] resultArray)
	{
		for(int x=0;x<prepArray.Length;x++)
		{
			if(resultArray[x])
			{
				checkingBatches = true;
				batchNOEXP.gameObject.SetActive(true);
				batchNOEXP.indexNO = x;
				batchNOEXP.textName.text = prepArray[x];

				while(checkingBatches)
				{
					yield return null;
				}

			}

		}

		OnPrepCheckboxValidateClickedX(resultArray);

	}

	private void OnPrepCheckboxValidateClicked(bool[] resultArray)
	{

		StartCoroutine(CheckAllBatches(resultArray));

		return;

		for(int x=0;x<prepArray.Length;x++)
		{
			if(resultArray[x])
			{

			}
		}
	}


	private void OnPrepCheckboxValidateClickedX(bool[] resultArray)
	{

		_preps=new List<int>();
		selectedPreps.Clear();
		for(int x=0;x<prepArray.Length;x++)
		{
			if(resultArray[x])
			{
				selectedPreps.Add(prepArray[x]);
				PrepData prep = new PrepData();
				prep.name=prepArray[x];

				prep.quantity=currentQTY[x]; //fudge
				prep.batchNO = currentBatchNO[x];
				prep.expDate = currentEXPDate[x];

				//prep.

				if(!setProductOnce)
				{
					prep.rootNode="1";
					setProductOnce=true;
				}
				currentPestScan.preps.Add(prep);
				_preps.Add(currentPestScan.preps.Count-1);
			}
		}
		if(selectedPreps.Count>0)
		{
				SelectApplicationTypeInfestationCheckBox();
		}else{
			    SelectPrepCheckBox();
		}
	}
	//Selecting consumables
	public void SelectConsumableCheckBox()
	{
		List<string> serv = new List<string>();
		foreach(string strong in availableConsumables)
		{
			serv.Add(strong);
		}
		serv.Add("Other");
		serv.Add("None");
		consumableArray = serv.ToArray();

		DialogCheckboxList dialog = DialogManager.ShowCheckboxListQTY
			(
				consumableArray, 
				OnConsumableCheckboxValidateClicked, 
				"OK", 	
				"Select Consumables", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
					SelectCategoryCheckBox();
				}, "BACK"
			);

		for(int i=0;i<selectedConsumables.Count;i++)
		{
			for(int x =0;x<dialog.optionList.Length;x++)
			{
				if(selectedConsumables[i]==dialog.optionList[x])
				{
					dialog.selectedIndexes[x]=true;
					dialog.selectionItems[x].itemCheckbox.Toggle(true);
					dialog.selectionItems[x].itemCheckbox.toggle.isOn=true;
					dialog.Show();
				}
			}
		}
	}
	private void OnConsumableCheckboxValidateClicked(bool[] resultArray)
	{
		selectedConsumables.Clear();
		for(int x=0;x<consumableArray.Length;x++)
		{
			if(resultArray[x])
			{
				selectedConsumables.Add(consumableArray[x]);
				PrepData prep = new PrepData();
				prep.name=consumableArray[x];
				prep.quantity=currentQTY[x];  //fudge
				if(!setProductOnce)
				{
					prep.rootNode="1";
					setProductOnce=true;
				}
				currentUnitScan.consumables.Add(prep);
			}
		}
		if(selectedConsumables.Count==0)
		{
			SelectConsumableCheckBox();
		}else{
			
			//Alert("All Pest Findings Reported, proceed to detector level tasks and recommendations");
			DialogManager.ShowAlert
			("All Pest Findings Reported, proceed to detector level tasks and recommendations",
				() => { 
					//ToastManager.Show("Unit Serviced"); 
				}, 
				"Complete", 
				"Complete Unit Service?",
				MaterialIconHelper.GetRandomIcon(), () => {
					//ToastManager.Show("You clicked the dismissive button");
					SelectConsumableCheckBox();
				}, "Back");
		
		}
	}
	//Selecting Site-Level infestation level
	public void SelectSiteLevelInfestationCheckBox()
	{
		infest_array = new string[4];
		infest_array[0]="None";
		infest_array[1]="Low";
		infest_array[2]="Medium";
		infest_array[3]="High";
		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
			(
				infest_array, 
				OnSelectSiteLevelInfestationValidateClicked, 
				"OK", 	
				"Select Infestation Type", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
				}, "BACK"
			);
		dialog.multiSelect=false;
		dialog.selectedIndexes[0]=true;
		dialog.selectionItems[0].itemCheckbox.Toggle(true);
		dialog.selectionItems[0].itemCheckbox.toggle.isOn=true;
		dialog.Show();
	}
	private void OnSelectSiteLevelInfestationValidateClicked(bool[] resultArray)
	{
		for(int x=0;x<resultArray.Length;x++)
		{
			if(resultArray[x])
			{
				currentTreatmentReport.infestationLevel=infest_array[x];
				if(app.installMode)
				{
					CompleteInstallService();
				}else{
					CompleteService ();
				}
			}
		}
	}
	//Select application type
	public void SelectApplicationTypeInfestationCheckBox()
	{
		appMethodArray = applicationMethods.ToArray();
		DialogCheckboxList dialog = DialogManager.ShowCheckboxList
			(
				appMethodArray, 
				OnSelectApplicationTypeValidateClicked, 
				"OK", 	
				"Select Application Method", 	
				MaterialIconHelper.GetRandomIcon(), 
				() => 
				{ 
				}, "BACK"
			);
		dialog.multiSelect=false;
		dialog.selectedIndexes[0]=true;
		dialog.selectionItems[0].itemCheckbox.Toggle(true);
		dialog.selectionItems[0].itemCheckbox.toggle.isOn=true;
		dialog.Show();
	}
	private void OnSelectApplicationTypeValidateClicked(bool[] resultArray)
	{
		for(int x=0;x<resultArray.Length;x++)
		{
			if(resultArray[x])
			{
				foreach(int i in _preps)
				{
					currentPestScan.preps[i].appMethod=appMethodArray[x];
				}
				AddAnotherPestMaybe();
			}
		}
	}
	//Add another pest dialog
	public void AddAnotherPestMaybe()
	{
		currentUnitScan.pestData.Add(currentPestScan);
		currentPestScan = new PestScanData();
		DialogManager.ShowAlert("Would you like to add another pest?", () => 
			{ 
				SelectCategoryCheckBox();
			}, 
			"YES",
			"More Pests Found?",
			MaterialIconHelper.GetRandomIcon(),
			() => 
			{ 
				if(availableConsumables.Count>0)
				{
					SelectConsumableCheckBox();
				}
			}, "NO");
	}
	//Free Text input
	public void EnterFreeText()
	{
		if(session.freeTextType=="Pest")
		{
			EnterPestFreeText();
		}
		if(session.freeTextType=="Species")
		{
			EnterSpeciesFreeText();
		}
		if(session.freeTextType=="Product")
		{
			EnterProductFreeText();
		}

	}
	public void SpeciesFreeText()
	{
		session.freeTextType="Species";
		pages.freeTextPanel.SetActive(true);
		ui.freeText.text="";
		ui.freeText.GetComponent<MaterialInputField>().hintText="Please enter species type";
	}
	public void EnterSpeciesFreeText()
	{
		pages.freeTextPanel.SetActive(false);
		string x = ui.freeText.text;
		selectedSpecies.Add(x);
		currentPestScan.species.Add(x);
		SelectInfestationCheckBox();
	}
	#endregion

	#region INSTALLATIONS
	public void ShowCachedInstallList()
	{
		if(dropDowns.installList.gameObject.activeInHierarchy)
		{
			dropDowns.installList.Clear();
			foreach(SiteCode siteCode in siteCodeCache.SiteCode)
			{
				if(siteCode.clientID==session.companyID)
				{
					if(!IsDuplicateSiteCode(siteCode.siteCode))
					{
						dropDowns.installList.Add(siteCode.siteCode + " : " + siteCode.productCode );
					}
				}
			}
		}else{
			StartCoroutine(WaitForInstallationList());
		}
	}

	private bool IsDuplicateSiteCode(string x)
	{
		bool dupe = false;
		foreach(string strong in dropDowns.installList.DataSource)
		{
			if(strong.Contains(x))
			{
				dupe=true;
			}
		}
		return dupe;
	}

	public void ShowInstallList()
	{
		if(dropDowns.installList.gameObject.activeInHierarchy)
		{
			dropDowns.installList.Clear();
			foreach(InstallationToUpload unit in nonUploadedInstalls.nonUploadedInstalls)
			{
				if(unit.clientID==session.companyID)
				{
					//dropDowns.installList.Add(unit.barcode);
					string title = unit.siteName + " - " + unit.barcode + " - " + unit.productName;
					dropDowns.installList.Add(title);
				}
			}
		}else
		{
			StartCoroutine(WaitForInstallationList());
		}
	}

	public void StartUnitInstall()
	{
		ui.installSiteLocation.text="";
		ui.installProductCode.text="";
		ui.installBarcode.text="";
	}
		
	public void CompleteInstall()
	{
		bool __valid=true;
		if(string.IsNullOrEmpty(ui.installService.text))__valid=false;
		if(string.IsNullOrEmpty(ui.installProductCode.text))__valid=false;
		if(string.IsNullOrEmpty(ui.installSiteLocation.text))__valid=false;
		
		if(!__valid)
		{
			Alert("Please Fill In All Units");
			return;
		}
		
		bool valid=true;
		bool used = false;

		InstallationToUpload _siteLocation = new InstallationToUpload();


		foreach(Visit visit in currentTreatmentReport.currentVisitData)
		{
			if(ui.installBarcode.text == visit.detector)
			{
				used=true;
			}
		}

		if (!used) 
		{
			//_barcode = ui.installBarcode.text;

		} 
		else 
		{
			Alert ("Barcode has been used already! - Please use another");
			valid=false;
		}

		bool site_used = false;

		foreach(Visit visit in currentTreatmentReport.currentVisitData)
		{
			if(ui.installSiteLocation.text == visit.siteLocation)
			{
				site_used=true;
			}
		}

		if (!site_used) 
		{
			InstallationToUpload install = new InstallationToUpload();
			install.siteName = ui.installSiteLocation.text;
			install.seqNo = App.instance.currentTreatmentReport.sequenceNumber;
			//previouslyUsedSiteLocations.Add (install);
			_siteLocation = install;
			//previouslyUsedSiteLocations.Add (ui.installSiteLocation.text);
		} else 
		{
			Alert ("Site Location has been used already! - Please use another");
			valid=false;
		}

		if(valid)
		{
			CompleteInstallX ();
			//previouslyUsedBarcodes.Add (ui.installBarcode.text);
			//previouslyUsedSiteLocations.Add (_siteLocation);
		}
	}
		
	public void CompleteInstallX()
	{
		//}else{
			currentUnitScan = new UnitScanData();
			setProductOnce=false;

			InstallationToUpload install = new InstallationToUpload();

			install.barcode = ui.installBarcode.text;
			install.productName = ui.installProductCode.text;
			install.service = ui.installService.text;
			install.siteName = ui.installSiteLocation.text;

			install.pcoID = session.pcoID;
			install.pcoName = session.pcoName;
			install.clientID = session.clientID;
			install.seqNo = session.sequenceNumber;

			currentScan.productCode = ui.installProductCode.text;
			currentScan.siteLocation = ui.installSiteLocation.text;
			currentScan.detector = ui.installBarcode.text;
			currentScan.service = ui.installService.text;

			nonUploadedInstalls.nonUploadedInstalls.Add(install);

			string _json = JsonUtility.ToJson(nonUploadedInstalls);// print (_json);
			AppLogic.instance.Save(_json,"NonUploadedInstalls");// not uploaded


			ShowInstallList();
			app.installMode=true;
			pages.visitDataObject.SetActive(true);
			pages.installScanner.SetActive(false);
			dropDowns.reccomendationType.currentlySelected=0;
			dropDowns.taskCategory.currentlySelected=0;
			selectedSpecies.Clear();
			selectedCategories.Clear();
			availableConsumables.Clear();
			selectedServices.Clear();
			selectedServices.Add(ui.installService.text);//moo
	
			//ToastManager.Show("Product Found : " + ui.manualProductCode.text);
			SelectService();
			SelectCategoryCheckBox();

			ui.installService.text="";
			ui.installBarcode.text="";
		//}
	}

	public void UploadAllInstallations()
	{
		foreach(InstallationToUpload unit in nonUploadedInstalls.nonUploadedInstalls)
		{
			//print (unit.barcode);
			if(unit.barcode!="UPLOADED")
			{	
				StartCoroutine(UploadInstallationData(unit));
				DebugText("Uploaded installation");
			}
		}
	}

	public void CancelInstall()
	{
		DialogManager.ShowAlert("Are you sure you want to cancel the installation?", 
			() => { 
				currentTreatmentReport = new TreatmentReport();
				string json2 = JsonUtility.ToJson(currentTreatmentReport); 
				AppLogic.instance.Save(json2,"CurrentTreatmentReportData");
				currentUnitScan=new UnitScanData();
				pestScans = new List<UnitScanData> ();
				//installation = new InstallationSet();
				pages.installObject.SetActive(false);
				pages.selectClientObject.SetActive(true);
			}, 
			"YES", 
			"Cancel Installation?",
			MaterialIconHelper.GetRandomIcon(), 
			() => { 

			}, "NO");
	}
	#endregion

	#region VISIT DATA
	private bool IsDuplicatePrep(string x)
	{
		bool dupe = false;

		foreach(string strong in availablePreps)
		{
			if(strong==x)
			{

				dupe=true;

			}

		}

		return dupe;
	}
	private bool IsSCDouble(string x)
	{
		bool Double =false;

		foreach(string y in dropDowns.scannedList.DataSource)
		{
			if(x==y)
			{
				Double=true;
			}
		}
		return Double;
	}
	public string GetProductName(string x)
	{
		string y="";
		foreach(Product product in productList.products)
		{
			if(product.code==x)
			{
				y=product.name;
			}
		}
		return y;
	}
	private void PopulateDropDowns()
	{
		dropDowns.reccomendationType.ClearData ();
		dropDowns.reccomendationCategory.ClearData ();
		dropDowns.taskCategory.ClearData ();

		foreach(Pest reccomend in reccomendations)
		{
			dropDowns.reccomendationCategory.AddData (new OptionData (reccomend.name,null,null));
		}
		foreach(string task in tasks)
		{
			dropDowns.taskCategory.AddData (new OptionData (task,null,null));
		}
	}

	IEnumerator DeleteProduct(string id)
	{
		WWWForm post = new WWWForm();
		post.AddField("id",id);
		string php = baseUrl+"/api/RemoveSiteCode.php";
		WWW www = new WWW(php,post);
		yield return www;

		if(www.error==null)
		{
			Alert(www.text);
			StartCoroutine(GetSiteCodes(session.sequenceNumber));
		}else{

		}
	}
	

	public void FindSiteCodeByName(string name)
	{
		foreach(SiteCode s_c in siteCodes.SiteCode)
		{
			if(s_c.clientID==session.companyID)
			{
				string x = s_c.productCode + " - " + s_c.siteCode;

				print( "if " + name + " = " + x);

				if(name == x)
				{
					print("got it - if " + name + " = " + x);
					//StartCoroutine(DeleteProduct(s_c.id.ToString()));
				}
			}
		}
	}

	public void ReplaceSiteCodeByName(string name)
	{
		foreach(SiteCode s_c in siteCodes.SiteCode)
		{
			if(s_c.clientID==session.companyID)
			{
				string x = s_c.productCode + " - " + s_c.siteCode;

				if(name == x)
				{
					//StartCoroutine(EditProduct(s_c.id.ToString()));
					replaceSiteCode.text = x;
					replaceID = s_c.id.ToString();
					replacePanel.SetActive(true);
				}
			}
		}
	}

	public GameObject replacePanel;
	public string replaceID;
	public Text replaceSiteCode;

	public void ViewScannedUnits()
	{
		dropDowns.scannedList.Clear();

		foreach(SiteCode s_c in siteCodes.SiteCode)
		{
//			print(s_c.SequenceNo);

			if(s_c.SequenceNo==session.sequenceNumber)
			{

				string suffix="";

				foreach(Visit visit in currentTreatmentReport.currentVisitData)
				{
					if(s_c.siteCode == visit.siteLocation)
					{
						suffix="[SERVICED]";
					}
				}

			    string x = s_c.productCode + " - " + s_c.siteCode+suffix;//print(x);
				//if(!IsSCDouble(x))
				//{
					dropDowns.scannedList.Add(x);
				//}
			}
		}
	}


	public void NonServiceDropDown(int x)
	{

		if(ui.nonServiceDropDown.options[x].text=="Other")
		{
			ui.nonServiceInput.gameObject.SetActive(true);
		}
		else
		{
			ui.nonServiceInput.gameObject.SetActive(false);
		}

	}

	public void AddNonService()
	{

		if(ui.nonServiceDropDown.captionText.text != "Other")
		{
			Visit visit = new Visit();
			visit.company_ID=session.companyID;
			visit.pcoID=session.pcoID;
			visit.SequenceNo=session.sequenceNumber;
			visit.visitType=currentTreatmentReport.callType;
			visit.siteLocation=currentUnServiced.siteCode;
			visit.productCode=currentUnServiced.productCode;
			visit.service=currentUnServiced.service;
			visit.nonServiceReason=ui.nonServiceDropDown.captionText.text;
			string json = JsonUtility.ToJson(siteCodes);
			AppLogic.instance.Save(json,"IncompleteSiteCodes");
			currentTreatmentReport.currentVisitData.Add(visit);
			CheckUnervicedSiteCodes();
			return;
		}
			                             
		                            

		if(ui.nonServiceInput.text.Length > 5)
		{
			Visit visit = new Visit();
			visit.company_ID=session.companyID;
			visit.pcoID=session.pcoID;
			visit.SequenceNo=session.sequenceNumber;
			visit.visitType=currentTreatmentReport.callType;
			visit.siteLocation=currentUnServiced.siteCode;
			visit.productCode=currentUnServiced.productCode;
			visit.service=currentUnServiced.service;
			visit.nonServiceReason=ui.nonServiceInput.text;
			string json = JsonUtility.ToJson(siteCodes);
			AppLogic.instance.Save(json,"IncompleteSiteCodes");
			currentTreatmentReport.currentVisitData.Add(visit);
			CheckUnervicedSiteCodes();
		}
		else
		{
			Alert("Please enter in a valid non-service reason");
		}

	}
	public void CheckUnervicedSiteCodes()
	{
		foreach(SiteCode site in siteCodes.SiteCode)
		{
			//if(!site.siteCode.Contains("SERVICED"))
			//{

			    bool serviced = false;
				
				foreach(Visit visit in currentTreatmentReport.currentVisitData)
				{

					//print (" if sitecode " + site.siteCode + " == " + visit.siteLocation);

					if(site.siteCode == visit.siteLocation)
					{
						serviced=true;
					}
				}

				//poo
				if(!serviced)
				{
					Alert("YOU DID NOT SERVICE " + site.siteCode);
					currentUnServiced=site;
					pages.nonServicePanel.SetActive(true);
					ui.nonServiceTitle.text = site.siteCode + " - " + site.productCode;
					ui.nonServiceInput.text="";
					return;
				}
			//}
		}
		pages.nonServicePanel.SetActive(false);
	}

	public void ValidateSiteLocation()
	{

		foreach(SiteCode site in siteCodes.SiteCode)
		{
			//if(site.siteCode ==ui. manualSiteLocation.text || site.siteCode.Contains(ui.manualSiteLocation.text))

//			print("if " + site.siteCode + " =- " + ui.manualSiteLocation.text);
			if(site.siteCode ==ui.manualSiteLocation.text)
			{
				ui.manualService.text = site.service;
				ui.manualProductCode.text = site.productCode;
				setProductOnce=false;
				ui.productFoundText.text = "Product : " + site.barcode;
				ui.serviceFoundText.text = "Service : " + site.service;
				app.currentBarcode = site.barcode;
				//site.siteCode += " [SERVICED]";
				string json = JsonUtility.ToJson(siteCodes);
				AppLogic.instance.Save(json,"IncompleteSiteCodes");
			}
		}

	}

	public void ScanProduct()
	{
		ui.productFoundText.text ="";
		ui.serviceFoundText.text="";
		app.currentBarcode="";
		if(string.IsNullOrEmpty(ui.manualSiteLocation.text))
		{
			Alert("Please scan a barcode or enter in a site location!");
		}else{

			foreach(SiteCode site in siteCodes.SiteCode)
			{

				//fudge

				bool serviced = false;
				
				foreach(Visit visit in currentTreatmentReport.currentVisitData)
				{

					//print (site.siteCode+" " + visit.siteLocation);

					if(site.siteCode == visit.siteLocation)
					{
						//serviced=true;
					}
				}

				//if(site.siteCode ==ui. manualSiteLocation.text || site.siteCode.Contains(ui.manualSiteLocation.text))

				if(serviced)
				{

					Alert("Already serviced this unit!!");

				}else{

					if(site.siteCode == ui. manualSiteLocation.text)
					{
						ui.manualService.text = site.service;
						ui.manualProductCode.text = site.productCode;
						setProductOnce=false;
						ui.productFoundText.text = "Product : " + site.productCode;
						ui.serviceFoundText.text = "Service : " + site.service;
						app.currentBarcode = site.barcode;
						//site.siteCode += " [SERVICED]";
					}

				}
			}

			string json = JsonUtility.ToJson(siteCodes);
			AppLogic.instance.Save(json,"IncompleteSiteCodes");
		}

		if(string.IsNullOrEmpty(ui.manualService.text))
		{
			Alert("Site Location number not found");
		}else{
			DoScanProduct();
			selectedServices.Clear();
			selectedServices.Add(ui.manualService.text);//moo
			currentUnitScan = new UnitScanData();
			currentUnitScan.product =ui.manualProductCode.text;
			currentUnitScan.siteCode = ui.manualSiteLocation.text;
			currentUnitScan.barCode = app.currentBarcode;
			currentUnitScan.service = ui.manualService.text;
			//ToastManager.Show("Product Found : " + ui.manualProductCode.text);
			SelectService();
		}
	}
	public void DoScanProduct()
	{
		currentScan = new Visit();
		app.installMode=false;
		App.instance.currentScan.productCode=App.instance.ui.manualProductCode.text;
		App.instance.currentScan.siteLocation=App.instance.ui.manualSiteLocation.text;
		App.instance.currentScan.date = System.DateTime.Now.ToString();
		App.instance.currentScan.lat=Input.location.lastData.latitude.ToString();
		App.instance.currentScan.lon=Input.location.lastData.longitude.ToString();
		App.instance.currentScan.visitType=App.instance.session.currentCallType;
		dropDowns.reccomendationType.currentlySelected=0;
		dropDowns.reccomendationType.buttonTextContent.text="Please Select";
		dropDowns.taskCategory.currentlySelected=0;
		dropDowns.taskCategory.buttonTextContent.text="Please Select";
		dropDowns.reccomendationCategory.currentlySelected=0;
		dropDowns.reccomendationCategory.buttonTextContent.text="Please Select";
		ui.recommendationFreeText.text="";
		ui.taskFreeText.text="";
		selectedSpecies.Clear();
		selectedCategories.Clear();
		pages.visitDataObject.SetActive(true);
		pages.routineScanner.SetActive(false);
		SelectCategoryCheckBox();
	}
	public void CompleteScan()
	{
		foreach(PestScanData pest in currentUnitScan.pestData)
		{
			int x=0;
			foreach(string species in pest.species)
			{
				if(pest.infestationLevels.Count>0)
				{
					AddVisitData(species,pest.type,pest.infestationLevels[x],currentUnitScan.service);
					x++;
				}else{
					AddVisitData(species,pest.type,pest.intensity,currentUnitScan.service);
				}
			}

			foreach(PrepData data in pest.preps)
			{
				TreatmentPrep prep = new TreatmentPrep();
				prep.batchNumber = data.batchNO;
				prep.expiryDate = data.expDate;
				prep.rootNode=data.rootNode;
				prep.appMethod = data.appMethod;
				prep.product = currentScan.productCode;
				prep.prepUsed = data.name;
				prep.prepQuantity = data.quantity;
				prep.prepLNumber = GetLNumber(prep.prepUsed);
				prep.siteLocation = currentScan.siteLocation;
				currentTreatmentReport.treatmentPreps.treatmentPreps.Add(prep);
				string json2 = JsonUtility.ToJson(currentTreatmentReport); 
				AppLogic.instance.Save(json2,"CurrentTreatmentReportData");
			}
		}

		if(app.installMode)
		{
			pages.visitDataObject.SetActive(false);
			pages.installObject.SetActive(true);
			UploadAllInstallations();
		}else{
			pages.menuPage.SetActive(true);
			pages.visitDataObject.SetActive(false);
		}
		ui.comments.text="";
		Alert("Succesfully serviced unit! - Added to visit data");
	}
	public void AddVisitData(string pestSpecie,string pestType,string infestationLevel,string service)
	{
		Visit scan = new Visit();
		scan.productCode=currentScan.productCode;
		scan.siteLocation=currentScan.siteLocation;
		scan.detector=currentScan.detector;
		//scan.date=currentScan.date;
		//scan.date  = CurrentTime();
		scan.date=System.DateTime.Now.ToString();
		scan.activity_type = selectedCategories[0];
		scan.species=pestSpecie;
		scan.activity_type=pestType;
		//RECCOMENDATIONS AND TASKS - MUST USE FREETEXT
		if (dropDowns.reccomendationType.currentlySelected > -1) {	
			scan.recommendations = dropDowns.reccomendationType.optionDataList.options [dropDowns.reccomendationType.currentlySelected].text;
		}
		if (dropDowns.taskCategory.currentlySelected > -1) {
			scan.tasks = dropDowns.taskCategory.optionDataList.options [dropDowns.taskCategory.currentlySelected].text;
		}
		//Add To Scan Data
		scan.pcoID=session.pcoID;
		scan.pcoName=session.pcoName;
		scan.company_ID=session.companyID;
		scan.comments = ui.comments.text;
		scan.taskStatus = "open";
		scan.reccomendationStatus = "open";
		scan.activity=infestationLevel;

		if(infestationLevel.ToUpper()=="NONE")
			scan.activity="0";
		if(infestationLevel.ToUpper()=="LOW")
			scan.activity="1";
		if(infestationLevel.ToUpper()=="MEDIUM")
			scan.activity="2";
		if(infestationLevel.ToUpper()=="HIGH")
			scan.activity="3";

		scan.service=service;

		if(app.installMode)
		{
			scan.service=currentScan.service;
		}
		scan.SequenceNo = session.sequenceNumber;
		scan.visitType=session.currentCallType;
		scan.lat=LocationServices.lat.ToString();
		scan.lon=LocationServices.lon.ToString();

		currentTreatmentReport.currentVisitData.Add(scan);
	}

	public void SelectPestCats()
	{
		dropDowns.taskCategory.ClearData();
		availablePreps.Clear();
		foreach(string pest in selectedCategories)
		{
			foreach (Preparations prep in preparations)
			{
				if(AppLogic.IsUsed(pest,prep))
				{
					if(!IsDuplicatePrep(prep.name))
					{ 
						availablePreps.Add(prep.name); 
					}
				}
			}
		}

		foreach(string task in tasks)
		{
			dropDowns.taskCategory.AddData (new OptionData (task,null,null));
		}

	}
	public void SelectPestCat(int x)
	{
		dropDowns.taskCategory.ClearData();
		foreach(string task in tasks)
		{
			dropDowns.taskCategory.AddData (new OptionData (task,null,null));
		}
	}
	public void SelectReccomendsCat(int x)
	{
		dropDowns.reccomendationType.ClearData ();
		dropDowns.reccomendationType.currentlySelected=0;
		dropDowns.reccomendationType.buttonTextContent.text="Select Recommendation";
		foreach (string data in reccomendations[x].type) 
		{
			dropDowns.reccomendationType.AddData (new OptionData (data,null,null));
		}
	}

	public void SelectService()
	{
		availableProducts.Clear();
		availableConsumables.Clear();
		foreach (Service data in services) 
		{
			foreach(string strang in selectedServices)
			{
				if(data.name.ToUpper()==strang.ToUpper())
				{
					foreach(string strung in data.products)
					{
						availableProducts.Add(strung);
					}

					if(data.consumables.Length==1)
					{

					}else
					{
						foreach(string strong in data.consumables)
						{
							if(!strong.ToUpper().Contains("AS PER PREP LIST"))
							{
								//availableConsumables.Add(strong);
								availablePreps.Add(strong);
							}
						}
					}
				}
			}
		}
		availableProducts.Add("Other");
		availableProducts.Add("None");
	}
	public void TrimData()
	{
		for(int i=0;i< currentTreatmentReport.currentVisitData.Count;i++)
		{
			if(string.IsNullOrEmpty(currentTreatmentReport.currentVisitData[i].company_ID))
			{
				currentTreatmentReport.currentVisitData.RemoveAt(i);
			}
		}
	}
	public void CompleteService()
	{
		pages.menuPage.SetActive (false);
		pages.completeServiceObject.SetActive (true);
		if(currentTreatmentReport.callType=="Routine")
		{
			CheckUnervicedSiteCodes();
		}
	}
	public void CompleteInstallService()
	{
		pages.installObject.SetActive (false);
		pages.completeServiceObject.SetActive (true);
	}
	#endregion

	#region TREATMENT REPORTS

	public bool debugMode=false;
	public TextAsset debugJson;

	public void RefreshIncomplete()
	{
		not.list.Clear();
		foreach(TreatmentReport treat in App.instance.incompleteTreatmentReports.reports)
		{
			not.list.Add(treat.name+" - "+ treat.treatmentReport.treatmentDate);
		}

	}	

	IEnumerator GetDebug(string jason)
	{

		WWWForm post = new WWWForm ();
		post.AddField ("debug", jason);
		WWW www = new WWW(baseUrl+"/api/debug.php",post);
		yield return www;

	}

	public void UploadOrResume()
	{
		if(uploadButtonText.text=="RESUME")
		{
			foreach(TreatmentReport treat in incompleteTreatmentReports.reports)
			{
				string compare = treat.name + " - " + treat.treatmentReport.treatmentDate;
					compare=compare.Replace("[SUSPENDED]","<color=''FFFFFF''>[SUSPENDED]</color>");
					compare=compare.Replace("SUCCESSFULLY UPLOADED","<color=''FFFFFF''>SUCCESSFULLY UPLOADED</color>");

				if(compare==selectedTreat)
				{
					ResumeService(treat);
					incompleteTreatmentReports.reports.Remove(treat);
					return;
					//print("resuming");
				}

			}
		}else
		{
			UploadSelectedTreatmentReport();
		}
	}

	public void UploadSelectedTreatmentReport()
	{
		app.uploadsToGo=0;
		app.prepsToGo=0;
		app.dataToGo=0;

		foreach(TreatmentReport treat in incompleteTreatmentReports.reports)
		{
			string compare = treat.name + " - " + treat.treatmentReport.treatmentDate;
			compare=compare.Replace("[SUSPENDED]","<color=''FFFFFF''>[SUSPENDED]</color>");
			compare=compare.Replace("SUCCESSFULLY UPLOADED","<color=''FFFFFF''>SUCCESSFULLY UPLOADED</color>");

			if(!treat.name.Contains("<color=''FFFFFF''>SUCCESSFULLY UPLOADED</color>") && compare==selectedTreat)
			{
				app.uploadsToGo++;
				UploadTreatmentReport(treat);
				Print ("uploading " + treat.name);
			}else{

				//print("if " + compare + " == " 
				if(compare==selectedTreat)
				{
					DialogManager.ShowAlert("Would you like to try upload this treatment report again? Only do so if something went wrong.", () => { 
						//ToastManager.Show("You clicked the affirmative button"); 
						app.uploadsToGo++;
						UploadTreatmentReport(treat);
						Print ("uploading " + treat.name);
					}, "YES", "Re-Upload?", MaterialIconHelper.GetRandomIcon(), () => { 
						//ToastManager.Show("You clicked the dismissive button"); 
					}, "NO");
					return;
				}

			}
		}
		
		StartCoroutine(WaitForTheEnd());
	}
    public IEnumerator UploadAllVisitData(TreatmentReport treat, string treatID)
	{
		yield return new WaitForSeconds(0.05f);
		Print("uploading all visit data");
		for(int i=0;i< treat.currentVisitData.Count;i++)
		{
			Print ("found visit data - uploading");
			app.dataToGo++;
			if(!string.IsNullOrEmpty(treat.currentVisitData[i].company_ID))
			{
				Print("uploading visit data " + i);
				yield return new WaitForSeconds(0.05f);
				UploadVisitData(treat,i,treatID);
			}
		}
	}

	public void Cancel()
	{
		DialogManager.ShowAlert("Are you sure you want to exit the treatment?", 
			() => { 
				currentTreatmentReport.name+="[SUSPENDED]";
				incompleteTreatmentReports.reports.Add(currentTreatmentReport);
				currentTreatmentReport = new TreatmentReport();
				string json2 = JsonUtility.ToJson(currentTreatmentReport); 
				AppLogic.instance.Save(json2,"CurrentTreatmentReportData");
				currentUnitScan=new UnitScanData();
				pestScans = new List<UnitScanData> ();
				pages.menuPage.SetActive(false);
				pages.selectClientObject.SetActive(true);
				string json3 = JsonUtility.ToJson(App.instance.incompleteTreatmentReports); 
				AppLogic.instance.Save(json3,"IncompleteTreatmentReports");
			}, 
			"YES", 
			"Cancel Service?",
			MaterialIconHelper.GetRandomIcon(), 
			() => { 

			}, "NO");
	}
	#endregion

	#region SELECTING CLIENT
	private string potentialClientID;
	public void SelectClientEndEdit()
	{
		ui.selectClient.text=session.selectedClient;
		ui.clientNameText.text=session.selectedClient;
		StartCoroutine(GetSequenceNo( session.companyID ));
	}
	public void SelectClientString(int x, string y)
	{
		session.selectedClient=y;
		int endPos = y.Length;
		int startPos = endPos-6;
		string z=y.Substring(startPos,6);
		session.clientID=z;
		session.companyName=y;
		session.companyID=z;
		//StartCoroutine(GetSequenceNo( z ));
	}
	public void ExitClient()
	{
		App.instance.session.currentCallType = "";
		App.instance.currentTreatmentReport.callType ="";
		App.instance.currentTreatmentReport.treatmentReport.treatmentDate = "";
		App.instance.currentTreatmentReport.treatmentReport.clientName = "";
		App.instance.currentTreatmentReport.treatmentReport.customerID = "";
		App.instance.currentTreatmentReport.treatmentReport.timeIn = "";
		App.instance.currentTreatmentReport.name = "";
		App.instance.currentTreatmentReport.clientID = "";
	}
	public void SelectClient()
	{
		UploadAllInstallations();

		if(dropDowns.callType.currentlySelected<0)
		{
			Alert("Please Select A Call Type!");
			return;
		}

		App.instance.session.currentCallType = dropDowns.callType.optionDataList.options[dropDowns.callType.currentlySelected].text;
		App.instance.currentTreatmentReport.callType = App.instance.session.currentCallType;
		App.instance.currentTreatmentReport.treatmentReport.treatmentDate = System.DateTime.Now.ToString();
		App.instance.currentTreatmentReport.treatmentReport.clientName = session.companyName;
		App.instance.currentTreatmentReport.treatmentReport.customerID = session.companyID;
		App.instance.currentTreatmentReport.treatmentReport.timeIn = CurrentTime();
		App.instance.currentTreatmentReport.name = session.companyName;
		App.instance.currentTreatmentReport.clientID = session.companyID;
		App.instance.currentTreatmentReport.pcoID=session.pcoID;
		App.instance.currentTreatmentReport.pcoName=session.pcoName;
		App.instance.ui.installClientName.text=session.companyName;

		if(string.IsNullOrEmpty(session.clientID))
		{
			Alert("Please Select A Client");
			return;
		}

		if(dropDowns.sequenceNumberSelect.currentlySelected <0 )
		{
			Alert("Please Select A Sequence Number");
			return;
		}else{
//			print("has sequence number");
			//print(dropDowns.sequenceNumberSelect.optionDataList.options[dropDowns.sequenceNumberSelect.currentlySelected].text);
			App.instance.currentTreatmentReport.sequenceNumber=seqNos[dropDowns.sequenceNumberSelect.currentlySelected];
			App.instance.session.sequenceNumber=seqNos[dropDowns.sequenceNumberSelect.currentlySelected];
			//App.instance.installation.SequenceNo=seqNos[dropDowns.sequenceNumberSelect.currentlySelected];
			//App.instance.installation.clientName = App.instance.currentTreatmentReport.name;
			StartCoroutine(GetSiteCodes(session.sequenceNumber));

			if(dropDowns.callType.optionDataList.options[dropDowns.callType.currentlySelected].text=="Installation")
			{
				string json2 = JsonUtility.ToJson(currentTreatmentReport); 
				AppLogic.instance.Save(json2,"CurrentTreatmentReportData");
				pages.installObject.SetActive(true);
				pages.selectClientObject.SetActive(false);
				//ShowCachedInstallList();
				ShowInstallList();
				ui.installService.text="";
				ui.installBarcode.text="";

			}else{
				string json2 = JsonUtility.ToJson(currentTreatmentReport); 
				AppLogic.instance.Save(json2,"CurrentTreatmentReportData");
				pages.menuPage.SetActive(true);
				pages.selectClientObject.SetActive(false);
			}
		}
	}
	public void PopulateAutoComplete()
	{
		dropDowns.auto.DataSource.Clear();

		//print ("populating auto complete");

		foreach(Client client in clientList.client)
		{

			foreach(string strung in pcoBranches)
			{
				//only show pco allowed branches
				if(client.BranchID==strung)
				{
					dropDowns.auto.DataSource.Add(client.Name + " - " + client.CustomerID.ToString());
				}
			}
		}
	}

	private void PopulateCodes()
	{
		dropDowns.codes.DataSource.Clear();
		dropDowns.codes2.DataSource.Clear();

		foreach(Product product in productList.products)
		{
			dropDowns.codes.DataSource.Add(product.code + " - " + product.name);
		}

		foreach(Product product in productList.products)
		{
			dropDowns.codes2.DataSource.Add(product.code + " - " + product.name);
		}
	}
	private void GetSequenceNoFromCache(string x)
	{
		subClients = new SubClientJSON();
		dropDowns.sequenceNumberSelect.ClearData();
		seqNos.Clear();

		if(subClientCache.SubClient.Count>0)
		{
			foreach(SubClient s_n in subClientCache.SubClient)
			{

				if(s_n.CustomerID==x)
				{
					dropDowns.sequenceNumberSelect.AddData(new OptionData(s_n.ContactAddress1,null,null));
					seqNos.Add(s_n.SequenceNo);
					subClients.SubClient.Add(s_n);
				}

			}

		}
	}
	#endregion

	#region INTERNAL DATA MODELS
	[System.Serializable]
	public class IncompleteTreatmentReports
	{
		public List<TreatmentReport> reports = new List<TreatmentReport>();
	}
	[System.Serializable]
	public class TreatmentReport
	{
		public string name = "";
		public string clientID = "";
		public string clientName="";
		public string sequenceNumber="";
		public string pcoID = "";
		public string appVersion="";
		public string lat="";
		public string lon="";
		public string pcoName = "";
		public string callType = "";
		public string infestationLevel = "";
		public Texture2D clientSignature;
		public Texture2D pcoSignature;
		public Treatment treatmentReport = new Treatment();
		public TreatmentPrepJson treatmentPreps= new TreatmentPrepJson();
		public TreatmentReccomendationJson treatmentReccomendations= new TreatmentReccomendationJson();
		public TreatmentTaskJson treatmentTasks= new TreatmentTaskJson();
		public List<Visit> currentVisitData = new List<Visit>();

		public void LoadSignature()
		{
			string url = Application.persistentDataPath +"/"+"ClientSignature"+clientID+".png";
			string url2 = Application.persistentDataPath +"/"+"PcoSignature"+clientID+".png";
			clientSignature = LoadPNG(url);
			pcoSignature = LoadPNG(url2);
		}
	}
	#endregion
}
