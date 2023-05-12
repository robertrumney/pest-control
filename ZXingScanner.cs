using UnityEngine;
using UnityEngine.UI;
using System.Collections;

using CielaSpike.Unity.Barcode;
using System.Threading;

public class ZXingScanner : MonoBehaviour
{
	public Text infoText;
	
	WebCamTexture cameraTexture;
	
	//Material cameraMat;
	//GameObject plane;
	
	WebCamDecoder decoder;
	
	//    IBarcodeEncoder qrEncoder, pdf417Encoder;
	
	//    GUIContent qrImage = new GUIContent();
	//    GUIContent pdf417Image = new GUIContent();
	
	GUIContent resultString = new GUIContent();
	
	//    Vector2 scroll = Vector2.zero;
	
	
	public RawImage scannerView;
	public Text resultText;
	
	
//xs	public LoyaltyProgramEdit loyalty;
	
	IEnumerator Start()
	{
		
		// get a reference to web cam decoder component;
		decoder = GetComponent<WebCamDecoder>();
		/*
        // get encoders;
        qrEncoder = Barcode.GetEncoder(BarcodeType.QrCode, new QrCodeEncodeOptions()
            {
                ECLevel = QrCodeErrorCorrectionLevel.H
            });

        //pdf417Encoder = Barcode.GetEncoder(BarcodeType.Pdf417);
		pdf417Encoder = Barcode.GetEncoder(BarcodeType.Ean13);

        qrEncoder.Options.Margin = 1;
        pdf417Encoder.Options.Margin = 2;
		*/
		
		// init web cam;
		if (Application.platform == RuntimePlatform.OSXWebPlayer ||
		    Application.platform == RuntimePlatform.WindowsWebPlayer)
		{
			yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
		}
		
		var devices = WebCamTexture.devices;
		var deviceName = devices[0].name;
		cameraTexture = new WebCamTexture(deviceName, 512, 512);
		cameraTexture.Play();
		
		// start decoding;
		yield return StartCoroutine(decoder.StartDecoding(cameraTexture));
		

		scannerView.texture = cameraTexture;
	}
	
	public bool gotLoyaltyPoints=false;
	private bool isGrowler = false;
	
	public GameObject manualEntry;
	
	void Update()
	{
		var result = decoder.Result;

		if (result.Success && resultString.text != result.Text)
		{
			if(result.Text=="super_secret_panda")
			{
				if(!gotLoyaltyPoints)
				{
					resultText.text="Congratulations! You have been awarded a loyalty point. Keep up the good work!";
					gotLoyaltyPoints=true;
				}
			}
			
			resultString.text = result.Text;

			Debug.Log(string.Format(
				"Decoded: [{0}]{1}", result.BarcodeType, result.Text));
			
			infoText.text=string.Format(
				"Decoded: [{0}]{1}", result.BarcodeType, result.Text);
			
			if(!isGrowler)
			{
				if(result.Text=="0700083405302")
				{
					infoText.text = "GROWLER BOTTLE DETECTED! - Now Scan the Tag Code!";
					//loyalty.code1.text="0700083405302";
					isGrowler=true;
				}
			}
			else
			{
				bool passed=false;
				string that="";
			}
			
			StartCoroutine(ScanCode(result.Text,isGrowler));
		}
		
	}
	
	IEnumerator  ScanCode(string code, bool isGrowler)
	{
		string type = "";
		
		if (isGrowler) 
		{
			type = "Growler";
		} 
		else 
		{
			type = "Other";
		}
		
		WWWForm mForm = new WWWForm ();
		
		mForm.AddField ("codeType",type);
		mForm.AddField ("code", code);
		mForm.AddField ("user",PlayerPrefs.GetString("UserName"));
		mForm.AddField ("store","Store Goes Here");
		
		//bl_ScanCode
		string php = "http:{API}.php";
		
		WWW www = new WWW (php, mForm);
		yield return www;
	}
}
