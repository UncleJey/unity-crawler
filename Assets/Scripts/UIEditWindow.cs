using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIEditWindow : MonoBehaviour 
{
	static UIEditWindow _instance;
	static Modifier myModifier;

	[SerializeField]
	private Button closeBtn, SaveBtn, RestoreBtn;


	/// <summary>
	/// caption
	/// </summary>
	[SerializeField]
	private InputField caption;

	/// <summary>
	/// xPath
	/// </summary>
	[SerializeField]
	private InputField text;
	/// <summary>
	/// Какое действие делать
	/// </summary>
	[SerializeField]
	private Dropdown actions;
	/// <summary>
	/// Значение для действия
	/// </summary>
	[SerializeField]
	private InputField actionVal;
	/// <summary>
	/// номера с
	/// </summary>
	[SerializeField]
	private InputField valFrom;
	/// <summary>
	/// номера по
	/// </summary>
	[SerializeField]
	private InputField valTo;
	/// <summary>
	/// Сливать в одну колонку
	/// </summary>
	[SerializeField]
	private Toggle splitTog;


	/// <summary>
	/// The property.
	/// </summary>
	[SerializeField]
	private InputField prop;
	/// <summary>
	/// Модификация текста
	/// </summary>
	[SerializeField]
	private InputField modify;

	private List<string> av = new List<string>();

	static UIEditWindow instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Main.instance.GetComponentsInChildren<UIEditWindow> (true)[0];
			}
			return _instance;
		}
	}

	public static void Show(Modifier pModifier)
	{
		myModifier = pModifier;
		instance.gameObject.SetActive (true);
	}

	void Start()
	{
		closeBtn.onClick.AddListener (() =>{Close();});
		SaveBtn.onClick.AddListener (()=>{Save(); Close(); });
		RestoreBtn.onClick.AddListener (()=>{Load();});
	}

	void OnEnable()
	{
		if (myModifier == null)
		{
			gameObject.SetActive (false);
		}
		else
		{
			if (av.Count < 1) 
			{
				foreach (string mn in System.Enum.GetNames(typeof(SMAction))) 
					av.Add (mn);
				actions.options.Clear ();
				actions.AddOptions (av);
			}

			Load ();
		}
	}

	void Close()
	{
		gameObject.SetActive (false);
	}

	void Load()
	{
		text.text = myModifier.xPath;
		actions.value = (int)myModifier.action;
		actionVal.text = myModifier.actionVal;
		caption.text = myModifier.caption;
		prop.text = myModifier.actionProp;
		modify.text = myModifier.actionModify;
		splitTog.isOn = myModifier.splitRes;
		valTo.text = myModifier.valTo.ToString ();
		valFrom.text = myModifier.valFrom.ToString ();
	}

	void Save()
	{
		myModifier.xPath = text.text;
		myModifier.SetAction (actions.value);
		myModifier.actionVal = actionVal.text;
		myModifier.caption = caption.text;
		myModifier.actionProp = prop.text;
		myModifier.actionModify = modify.text;
		myModifier.splitRes = splitTog.isOn;

		int.TryParse (valFrom.text, out myModifier.valFrom);
		int.TryParse (valTo.text, out myModifier.valTo);
		myModifier.Refresh ();
	}
}
