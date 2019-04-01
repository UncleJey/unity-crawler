using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Networking;

public enum wState : byte
{
	 SITE_STATE_IDLE = 0 // запрос нового задания
	,SITE_STATE_WAIT= 1 // закончил и передаёт файл
	,SITE_STATE_PARCING= 2 // начат парсинг
	,SITE_STATE_SETUP= 3
	,SITE_STATE_PREGRESS = 4
}

public class Preloader : MonoBehaviour 
{
	public string url = "http://localhost:65111/xschudeler55.php";
	public wState state;
	bool waiting = false;

	public static string siteUrl;
	public static string siteSettings;
	public static string siteName;
	public static string siteID;
	static Preloader instance;

	void Start()
	{
		if (instance == null) 
		{
			instance = this;
			DontDestroyOnLoad (gameObject);
			waiting = false;
			state = wState.SITE_STATE_IDLE;
			Main.ActionDone += ActionDone;
		}
		else
			Destroy (gameObject);
	}

	IEnumerator GetTask()
	{
		Debug.Log ("get new task");
		yield return new WaitForSeconds(1f);
		waiting = true;
		state = wState.SITE_STATE_IDLE;
		WWW www = new WWW (string.Format("{0}?sid={1}",url,(byte)state));
		Debug.Log ("request: " + www.url);
		yield return null;
		yield return www;

		if (!string.IsNullOrEmpty (www.error)) 
		{
			Debug.Log ("get task error: " + www.error);
			StartCoroutine (LateStart (5f));
		}
		else if (string.IsNullOrEmpty(www.text))
		{
			Debug.Log ("have no task");
			StartCoroutine (LateStart (15f));
		}
		else 
		{
			Debug.Log ("answer: " + www.text);
			StartCoroutine(ParceSite (www.text));
		}
	}

	string siteData = "";
	IEnumerator ParceSite (string pData)
	{
		siteData = pData;
		yield return null;
		Debug.Log ("Start parcing");
		JsonObject jo = JsonObject.FromJson (pData);
		if (jo == null || !jo.ContainsKey("url"))
		{
			Debug.Log ("wrong answer");
			StartCoroutine (LateStart (10f));
		}
		else
		{
			siteID = jo.Get<string> ("id");
			siteUrl = jo.Get<string> ("url");

			Debug.Log (string.Format ("id: {0} url: {1}", siteID, siteUrl));
			state = wState.SITE_STATE_PARCING;
			waiting = true;
			WWW www = new WWW (string.Format("{0}?sid={1}&tid={2}",url,(byte)state,siteID));
			Debug.Log ("request: " + www.url);

			yield return null;
			yield return www;
			if (!string.IsNullOrEmpty (www.error)) 
			{
				Debug.Log ("can not start task. Error "+www.error);
				StartCoroutine (LateStart (15f));
			}
			else if (string.IsNullOrEmpty(www.text))
			{
				Debug.Log ("can not start task. Empty answer.");
				StartCoroutine (LateStart (15f));
			}
			else
			{
				siteSettings = www.text;
				Debug.Log ("Settings: " + siteSettings);
				state = wState.SITE_STATE_PREGRESS;
				waiting = false;
			}
		}
	}

	void StartProgress()
	{
		waiting = true;
		Main.data = siteSettings;
		Main.auto = true;
		Main.startURL = siteUrl;
		SceneManager.LoadScene ("main");
	}

	string resfile;
	IEnumerator EndProgress()
	{
		resfile = Main.resFileName;
		Destroy (Main.instance.gameObject);
		yield return new WaitForSeconds (0.2f);

		SceneManager.UnloadScene ("main");


		FileStream fs = new FileStream (resfile, FileMode.Open);
		StreamReader sr = new StreamReader (fs);
		string data = sr.ReadToEnd();
		sr.Close ();
		fs.Close ();

		byte[] myData = System.Text.Encoding.UTF8.GetBytes(data);
		state = wState.SITE_STATE_WAIT;
		UnityWebRequest www = UnityWebRequest.Put(string.Format("{0}?sid={1}&tid={2}",url,(byte)state, siteID), myData);
		Debug.Log ("request: " + www.url);
		yield return www.Send (); // SendWebRequest();

		if (!string.IsNullOrEmpty (www.error))
			Debug.LogError ("cant send result! " + www.error);
		else
			Debug.Log ("sent ");

		yield return new WaitForSeconds (1f);
		SceneManager.LoadScene (0);
		yield return new WaitForSeconds (2f);
		state = wState.SITE_STATE_IDLE;
		waiting = false;
	}

	void ActionDone()
	{
		Debug.Log ("Action Done!");
		StartCoroutine(EndProgress ());
	}

	IEnumerator LateStart(float tm)
	{
		yield return new WaitForSeconds (tm);
		waiting = false;
	}

	void Update()
	{
		if (waiting)
			return;

		switch (state) 
		{
			case wState.SITE_STATE_IDLE:
				waiting = true;
				StartCoroutine (GetTask ());
			break;
			case wState.SITE_STATE_PARCING:
				waiting = true;
				StartCoroutine (ParceSite (siteData));
			break;
			case wState.SITE_STATE_PREGRESS:
				waiting = true;
				StartProgress ();
			break;
		}
	}
}
