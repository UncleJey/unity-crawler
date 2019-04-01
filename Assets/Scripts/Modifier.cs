using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using OpenQA.Selenium;
using OpenQA;

/// <summary>
/// Действия над элементами
/// </summary>
public enum SMAction : byte
{
	 None			= 0
	,GoToURL		= 1 // Перейти к адресу
	,CheckURL		= 2 // Проверить адрес на повторы
	,ClickAndWait	= 3 // Сликнуть и подождать загрузки
	,SaveToColumn	= 4 // Сохранить в колонку
	,SaveURL		= 5 // Сохранить URL в списке просмотренных
	,NewItem		= 6 // Создать новый товар
	,SaveItem		= 7 // Сохранить товар
	,ComeBack		= 8 // Вернуться на предыдущую страницу
	,Exit			= 9 // Завершение
}

public enum ModAction : byte
{
	 None	= 0
	,Add	= 1
	,Del	= 2
	,Up		= 3
	,Down	= 4
	,Go		= 5
}

public enum ModifierType : byte
{
	 None		= 0
	,Address	= 1
	,Category	= 2
	,Elements	= 3
	,Item		= 4
}

public class Modifier : MonoBehaviour 
{
#region InfoAndAction
	[SerializeField]
	private Button btnGo, btnPlus, btnMinus, btnLeft, btnRight, btnUp, btnDown, btnEdit;
	public Text orederNum;
#endregion InfoAndAction

#region settings
	/// <summary>
	/// Комментарии
	/// </summary>
	public string caption;
	/// <summary>
	/// xPath для действия
	/// </summary>
	public string xPath;
	/// <summary>
	/// Поля с
	/// </summary>
	public int valFrom;
	/// <summary>
	/// Поля по
	/// </summary>
	public int valTo;

	/// <summary>
	/// Действие
	/// </summary>
	public SMAction action;
	/// <summary>
	/// уточнение для действия
	/// </summary>
	public string actionVal;
	/// <summary>
	/// Свойство над которым производится действие
	/// </summary>
	public string actionProp;
	/// <summary>
	/// Модификация текста
	/// </summary>
	public string actionModify;
	/// <summary>
	/// Сливать в одну колонку
	/// </summary>
	public bool splitRes;
#endregion settings

#region processing
	/// <summary>
	/// Предок, если в дереве
	/// </summary>
	public Modifier parentModifier;
	/// <summary>
	/// Дети, если в дереве
	/// </summary>
	public List<Modifier> clhildModifiers;
	/// <summary>
	/// Следующий, если линейно
	/// </summary>
	public Modifier nextModifier;
	public bool clicked = false;

	/// <summary>
	/// Новер итема, который в процессе
	/// </summary>
	public int curItemNum = -1;
	/// <summary>
	/// Список итемов
	/// </summary>
	public string[]  items;
	/// <summary>
	/// Адрес текущей страницы
	/// </summary>
	public string pageAddress;
#endregion processing
	[SerializeField]
	private RectTransform _vlg;
	int _level = 0;
	/// <summary>
	/// Уровень вложенности
	/// </summary>
	public int level
	{
		get
		{
			return _level;
		}
		set
		{
			_level = value;
			_vlg.anchoredPosition = new Vector2(_level * 20,0);
			Main.FindParents (this);
		}
	}

	void Awake()
	{
		parentModifier = null;
		if (clhildModifiers != null)
			clhildModifiers.Clear ();
		else
			clhildModifiers = new List<Modifier> ();
	}

	public bool multiple
	{
		get 
		{
			return valTo > 1;
		}
	}

	void Start ()
	{
		btnGo.onClick.RemoveAllListeners ();
		btnGo.onClick.AddListener (()=>{Main.BtnClick(this, ModAction.Go);});

		btnPlus.onClick.RemoveAllListeners ();
		btnPlus.onClick.AddListener (()=>{Main.BtnClick(this, ModAction.Add);});

		btnMinus.onClick.RemoveAllListeners ();
		btnMinus.onClick.AddListener (()=>{Main.BtnClick(this, ModAction.Del);});

		btnRight.onClick.RemoveAllListeners ();
		btnRight.onClick.AddListener (() =>{if (level<10) level++;} );

		btnLeft.onClick.RemoveAllListeners ();
		btnLeft.onClick.AddListener (() =>{if (level>0) level--;} );

		btnEdit.onClick.RemoveAllListeners ();
		btnEdit.onClick.AddListener (() =>{ UIEditWindow.Show(this);});

		clicked = false;
	}

	public JsonObject Serialized
	{
		get 
		{
			JsonObject val = new JsonObject ();
			val.Add ("i", xPath);
			val.Add ("l", _level);
			val.Add ("a", (int)action);
			val.Add ("v", actionVal);
			val.Add ("c", caption);
			val.Add ("p", actionProp);
			val.Add ("y", actionModify);
			val.Add ("s", splitRes ? 1 : 0);
			val.Add ("f", valFrom);
			val.Add ("t", valTo);

			return val;
		}
		set 
		{
			xPath = value.Get<string> ("i", "");
			level = value.Get<int> ("l", 0);
			SetAction(value.Get<int> ("a", 0));
			actionVal = value.Get<string> ("v", "");
			caption = value.Get<string> ("c", "");
			actionProp = value.Get<string> ("p", "");
			actionModify = value.Get<string> ("y", "");
			splitRes = value.Get<int> ("s", 0) == 1;
			valFrom = value.Get<int> ("f", 0);
			valTo = value.Get<int> ("t", 0);

			if (value.Get ("m", 0) == 1) 
			{
				valFrom = 0;
				valTo = 9999;
			}
			Refresh ();
		}
	}

	public void SetAction(int pAction)
	{
		action = (SMAction) System.Enum.GetValues(typeof(SMAction)).GetValue(pAction);
	}

	public void Refresh ()
	{
		name = "p_"+CalcNum ();
		orederNum.text = string.Format ("{0} : {4} [ {1} ({2}) ]     {3}", name, action.ToString(), actionVal, xPath, caption);
	}

	/// <summary>
	/// Визуализация дерева номеров
	/// </summary>
	public string CalcNum()
	{
		string nr = transform.GetSiblingIndex ().ToString();

		Modifier m = parentModifier;
		while (m != null)
		{
			nr = m.transform.GetSiblingIndex ().ToString () + "." + nr;
			m = m.parentModifier;
		}

		return nr;
	}

	/// <summary>
	/// Значение
	/// </summary>
	public float floatVal
	{
		get
		{
			if (string.IsNullOrEmpty (actionVal))
				return 0;
			float f = 0;
			if (float.TryParse(actionVal.Trim(), out f))
				return f;
			return 0;
		}
	}
}
