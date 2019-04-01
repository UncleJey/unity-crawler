using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

using OpenQA.Selenium;
using OpenQA;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Support;

// xpath:  http://citforum.ru/internet/xpath/xpath04.shtml

public class Main : MonoBehaviour 
{
	[SerializeField]
	private Transform curLabel;

	public Button prepareBTN;
	public Toggle tAuto;
	public Button saveBTN;
	public Button openBtn;

	public string defData;
	IWebDriver driver;
	public static bool auto = false;

	string fileData;

	string[] fields = new string[64];
	List<string> pagesParced = new List<string> ();

	public static Main instance;
	public GroupLayoutPool pool;

	public static System.Action ActionDone;

	Modifier _curModifier = null;
	Modifier curModifier
	{
		get
		{
			return _curModifier;
		}
		set
		{
			_curModifier = value;
			if (_curModifier != null)
			{
				curLabel.parent = _curModifier.transform;
				curLabel.localPosition = Vector3.zero;
				curLabel.gameObject.SetActive (true);
				curLabel.localScale = Vector3.one;
			}
			else
			{
				curLabel.SetParent(this.transform);
				curLabel.gameObject.SetActive (false);
			}
		}
	}

	public static string data;
	public static string startURL;
	public static string siteName="";

	/// <summary>
	/// Имя файла с результатом
	/// </summary>
	public static string resFileName = "result.txt";
	/// <summary>
	/// Результат
	/// </summary>
	System.IO.FileStream resfile;
	StreamWriter resWriter;

	void Awake () 
	{
		instance = this;
		prepareBTN.onClick.AddListener (doPrepare);
		saveBTN.onClick.AddListener (doSave);

		if (!string.IsNullOrEmpty (data))
			Serialized = JsonObject.FromJson (data);

		openBtn.onClick.AddListener (()=>{OpenFile(); });
	}

	/// <summary>
	/// Загрузка конфига вручную с диска
	/// </summary>
	void OpenFile()
	{
		string[] paths = SFB.StandaloneFileBrowser.OpenFilePanel("Open File", "", "", false);
		if (paths != null && paths.Length > 0) 
		{
			FileStream fs = new FileStream (paths [0], FileMode.Open);
			StreamReader sr = new StreamReader (fs);
			data = sr.ReadToEnd();
			sr.Close ();
			fs.Close ();
			auto = false;
			UnityEngine.SceneManagement.SceneManager.LoadScene ("main");
		}
	}

	void Start()
	{
		if (auto)
			StartCoroutine (AutoStart ());
	}

	IEnumerator AutoStart()
	{
		yield return null;
		doPrepare ();
		bool done = false;
		while (!done)
		{
			yield return new WaitForSeconds (1f);
			try
			{
				driver.Manage ().Window.Maximize ();
				done = true;
			}
			catch
			{
				done = false;
			}
		}
		curModifier = GetComponentsInChildren<Modifier> (false)[0];
		StepLogic ();
	}

	void doPrepare () 
	{
		driver = new EdgeDriver ();//"C:\\Program Files (x86)\\Microsoft Web Driver");
		//driver = new OpenQA.Selenium.Chrome.ChromeDriver();
		pagesParced.Clear ();
		resfile = new FileStream (resFileName, FileMode.Create);
		resWriter = new StreamWriter (resfile);
	}

	void OnDisable()
	{
		if (driver != null) 
		{
			driver.Quit ();
			driver = null;
		}

		if (resWriter != null)
			resWriter.Flush ();

		if (resfile != null)
			resfile.Close ();
	}

	/// <summary>
	/// Gos the button click.
	/// </summary>
	public static void BtnClick(Modifier pModifier, ModAction pAction)
	{
		auto = instance.tAuto.isOn;
		switch (pAction)
		{
			case  ModAction.Go:
				instance.curModifier = pModifier;
				instance.StepLogic ();
			break;
			case ModAction.Add:
				Modifier m = instance.pool.InstantiateElement ().GetComponent<Modifier> ();
				m.gameObject.transform.SetSiblingIndex (pModifier.transform.GetSiblingIndex () + 1);
				m.Serialized = pModifier.Serialized;
			break;
			case ModAction.Del:
				instance.pool.DestroyElement(pModifier.GetComponent<LayoutElement>());
			break;
			default:
				Debug.LogError ("Unknown action " + pAction.ToString ());
			break;
		}
	}

	public JsonObject Serialized
	{
		get 
		{
			JsonObject jo = new JsonObject ();

			JsonArray ja = new JsonArray ();
			Modifier[] modifiers = GetComponentsInChildren<Modifier> (false);
			foreach (Modifier m in modifiers)
				ja.Add (m.Serialized);

			jo.Add ("m", ja);
			return jo;
		}
		set 
		{
			JsonArray ja = value.Get<JsonArray> ("m", null);
			foreach (JsonObject o in ja) 
			{
				Modifier m = pool.InstantiateElement ().GetComponent<Modifier>();
				m.Serialized = o;
				FindParents (m);
			}
		}
	}

	void doSave()
	{
		string data = Serialized.ToJson ();
		using (System.IO.FileStream fs = new FileStream(fileData, FileMode.Create))
		{
			using (StreamWriter writer = new StreamWriter(fs))
			{
				writer.Write(data);
			}
		}
		#if UNITY_EDITOR
			UnityEditor.AssetDatabase.Refresh ();
		#endif
		Debug.Log (data);
	}

#region logic

	public static void FindParents(Modifier pModifier)
	{
		Modifier[] ms = instance.GetComponentsInChildren<Modifier> ();

		int index = ms.IndexOf (pModifier);
		if (pModifier.parentModifier != null)
		{
			pModifier.parentModifier.clhildModifiers.Remove (pModifier);
			pModifier.parentModifier = null;
		}

		if (pModifier.clhildModifiers == null)
			pModifier.clhildModifiers = new List<Modifier> ();
		else if (pModifier.clhildModifiers.Count > 0)
		{
			foreach (Modifier m in pModifier.clhildModifiers)
				m.parentModifier = null;

			pModifier.clhildModifiers.Clear ();
		}

		for (int i = index - 1; i >= 0; i--)
		{
			if (ms[i].level < pModifier.level)
			{
				ms [i].clhildModifiers.Add (pModifier);
				pModifier.parentModifier = ms [i];
				break;
			}
		}

		int maxLevel = 10;

		if (ms.Length > 1)
		{
			Modifier[] modLevels = new Modifier[maxLevel];
			modLevels [0] = ms [0];

			for (int i = 1; i < ms.Length; i++)
			{
				if (ms [i].level == ms[i-1].level)
				{
					ms[i-1].nextModifier = ms [i];
				}
				else if (ms [i].level > ms[i-1].level)
				{
					modLevels [ms[i-1].level] = ms[i-1];
					ms[i-1].nextModifier = null;
				}
				else
				{
					if (modLevels [ms [i].level] != null)
						modLevels [ms [i].level].nextModifier = ms [i];
					modLevels [ms [i].level] = ms[i];
					for (int j = ms [i].level + 1; j < maxLevel; j++)
						modLevels [j] = null;
				}
			}
		}
		pModifier.Refresh ();
	}

	/// <summary>
	/// Запомнить поле
	/// </summary>
	void AddField(string pData, int pColumn)
	{
		if (string.IsNullOrEmpty (pData))
			return;
		
		pData = pData.Trim ().Replace('"','\'').Replace(';',',').Replace("\\r","\r").Replace("\\n","\n");

		Debug.Log ("Add Field "+pData+" to col "+pColumn.ToString());
		if (!string.IsNullOrEmpty (fields [pColumn]))
			fields [pColumn] += "; ";
		fields [pColumn] += pData;
	}

	/// <summary>
	/// Получить аттрибут поля
	/// </summary>
	string GetFieldAttrs(IWebElement el, string pField)
	{
		string[] flds = pField.Split ('\\');

		string res = "";
		foreach (string f in flds)
		{
			if (flds.Length > 1 && string.IsNullOrEmpty (f))
				continue;

			if (f.Equals ("n"))
				res += "\\n";
			else if (f.Equals ("r"))
				res += "\\r";
			else
				res += GetFieldAttr(el, f);
		}
		return res;
	}

	string GetFieldAttr(IWebElement el, string pField)
	{
		if (el == null)
			return string.Empty;

		if (string.IsNullOrEmpty (pField) || pField.Equals("text"))
			return el.Text;

		if (pField.Equals("url"))
			return driver.Url;

		return el.GetAttribute (pField);
	}

	string Modify(string pVal, string pMod)
	{
		if (string.IsNullOrEmpty (pMod))
			return pVal;
		if (string.IsNullOrEmpty (pVal))
			return pVal;

		if (pMod == ".")
			return pVal.Replace (".", "").Trim();

		//TODO: регексы вставить
		//FIXME: на регексы перевести
		int p = pVal.IndexOf (pMod);
		if (p >= 0) 
			return pVal.Replace(pMod,"").Trim(); //  Substring (0, p)
		return pVal;
	}

	void SaveToColumn()
	{
		Debug.Log ("selector: " + curModifier.xPath);
		var els = driver.FindElements (By.XPath (curModifier.xPath));
		Debug.Log ("found elements: " + els.Count);

		int _from = curModifier.valFrom;

		for (int i = 0; i < els.Count; i++)
		{
			if (_from <= i) 
			{
				IWebElement el = els [i];
				string val = GetFieldAttrs (el, curModifier.actionProp);
				val = Modify (val, curModifier.actionModify);
				AddField (val, (curModifier.splitRes ? 0 : i - _from) + (int)curModifier.floatVal);
			}
			if (curModifier.valTo <= i)
				break;
		}
		NextStep ();
	}

	void SaveFields()
	{
		string res = "";
		int maxFld = 0;
		for (int i = 0; i < fields.Length; i++)
			if (!string.IsNullOrEmpty (fields [i])) 
			{
				res += i.ToString () + ". " + fields [i] + "\r\n";
				maxFld = i;
			}
		Debug.Log ("SaveFields: "+res);

		res = "";
		for (int i = 0; i <= maxFld; i++)
			if (string.IsNullOrEmpty(fields [i]))
				res +=  "\t";
			else
				res +=  "\""+fields [i] + "\"\t";

		if (maxFld > 0)
			resWriter.WriteLine (res);
		
		NextStep ();
	}

	void ClearFiends()
	{
		Debug.Log ("Clear Fields");
		for (int i = fields.Length - 1; i >= 0; i--)
			fields [i] = "";
		NextStep ();
	}

	void NextStep(bool goDown=true)
	{
		Debug.Log ("Next Step "+goDown.ToString());

		if (goDown && curModifier.clhildModifiers != null && curModifier.clhildModifiers.Count > 0)
			curModifier = curModifier.clhildModifiers [0];
		else if (curModifier.nextModifier != null)
			curModifier = curModifier.nextModifier;
		else if (curModifier.parentModifier != null)
			curModifier = curModifier.parentModifier;
		else
		{
			Debug.Log("Next step not found");
			return;
		}

		if (!auto)
			return;

		StepLogic ();
	}

#region StepLogic
	bool started = false;
	/// <summary>
	/// Переход по адресу
	/// </summary>
	void GoToURL()
	{
		if (!string.IsNullOrEmpty (startURL) && !started) 
		{
			curModifier.xPath = startURL;
			started = true;
		}
		Debug.Log ("To URL :" + curModifier.xPath);
		try
		{
			driver.Navigate ().GoToUrl (curModifier.xPath);
		}
		catch (System.Exception e)
		{
			Debug.LogError ("Error while navigate " + e.Message);
			GoToURL ();
			return;
		}
		curModifier.pageAddress = driver.Url;
		waitLoad (curModifier.floatVal);
	}

	/// <summary>
	/// Логика поведения в случае если итем не найден
	/// </summary>
	void HaveNoHits()
	{
		if (curModifier.curItemNum > 1 && curModifier.multiple)
		{
			curModifier.curItemNum = 0;
			NextStep (false);
			return;
		}
		else if (curModifier.curItemNum == 1 && !curModifier.multiple)
		{
			curModifier.curItemNum = 0;
			NextStep (false);
			return;
		}
		else
		{
			NextStep();
		}
	}

	int clcNo = 0;

	/// <summary>
	/// Кликнуть и подождать загрузки
	/// </summary>
	void ClickAndWait()
	{
		curModifier.curItemNum++;
		string path = curModifier.xPath.Replace ("{0}", curModifier.curItemNum.ToString());
		Debug.Log ("ClickAndWait selector : " + path);
		try
		{
			var els = driver.FindElements(By.XPath(path));
			Debug.Log ("ClickAndWait: page "+driver.Url+" have "+els.Count.ToString()+" hits. "+path);
			if (els.Count > 0)
			{
				clcNo = 0;
				els [0].Click ();
				waitLoad (curModifier.floatVal);
				return;
			}
			else
			{
				clcNo = 0;
				HaveNoHits();
				return;
			}
		}
		catch
		{
			Debug.Log ("try "+clcNo+" ClickAndWait: page "+driver.Url+" Error in xpath. "+path);
			if (clcNo++ >20) 
			{
				Debug.Log ("Error! Item not found! Skip!");
				ClickAndWait ();
				return;
			}
			StartCoroutine (retryClick ());
			return;
		}
		NextStep ();
	}

	IEnumerator retryClick()
	{
		Debug.Log ("retry!");
		ex.ExecuteScript ("window.scrollBy(0, 20)");
		yield return null;
		string state = "-";
		while (!state.Equals ("complete")) 
		{
			yield return new WaitForSeconds (0.2f);
			state = ex.ExecuteScript ("return document.readyState").ToString ();
		}
		curModifier.curItemNum--;
		ClickAndWait ();
	}

	/// <summary>
	/// Запоминаем URL как просмотренный
	/// </summary>
	void SaveURL ()
	{
		pagesParced.Add (driver.Url);
		NextStep ();
	}

	/// <summary>
	/// Проверить, не дублируется ли адрес
	/// </summary>
	void CheckURL ()
	{
		if (!pagesParced.Contains (driver.Url))
			NextStep ();
		else 
		{
			Debug.Log ("skip as have" + driver.Url);
			NextStep (false);
		}
	}

	/// <summary>
	/// Вернуться на предыдущую страницу и подождать загрузку
	/// </summary>
	void Back()
	{
		Debug.Log ("GoBack");
		driver.Navigate ().Back ();
		waitLoad (curModifier.floatVal);
	}

	/// <summary>
	/// Завершение выполнения
	/// </summary>
	void Exit()
	{
		if (ActionDone != null)
			ActionDone.Execute ();
		Debug.Log ("Complete! Quit!");
	}

	/// <summary>
	/// Сделать ход
	/// </summary>
	void StepLogic()
	{
		switch (curModifier.action)
		{
			case SMAction.GoToURL:
				GoToURL ();
			break;
			case SMAction.ClickAndWait:
				ClickAndWait ();
			break;
			case SMAction.SaveToColumn:
				SaveToColumn ();
			break;
			case SMAction.NewItem:
				ClearFiends ();
			break;
			case SMAction.SaveItem:
				SaveFields ();
			break;
			case SMAction.ComeBack:
				Back ();
			break;
			case SMAction.CheckURL:
				CheckURL ();
			break;
			case SMAction.SaveURL:
				SaveURL ();
			break;
			case SMAction.Exit:
				Exit ();
			break;
			default:
				Debug.LogError ("unknown modifier type "+curModifier.action.ToString());
			break;
		}
	}
#endregion StepLogic

	IJavaScriptExecutor ex;
	/// <summary>
	/// Ожидание загрузки перед следующим шагом
	/// </summary>
	void waitLoad(float pTime = 0.5f)
	{
		if (ex == null)
			ex = (IJavaScriptExecutor)driver;

		StartCoroutine (waitloader (pTime));
	}

	IEnumerator waitloader(float pTime)
	{
		yield return null;
		yield return new WaitForSeconds (pTime);
		string state = "-";
		while (!state.Equals ("complete")) 
		{
			yield return new WaitForSeconds (0.2f);
			state = ex.ExecuteScript ("return document.readyState").ToString ();
		}
		NextStep ();
	}
#endregion logic
}
