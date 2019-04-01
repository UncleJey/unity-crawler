using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

	/// <summary>
	/// Используется совместно с одним из лейаутов. Инстанцирует и создаёт элементы лейаута через пул.
	/// </summary>
	public class GroupLayoutPool : MonoBehaviour 
	{
		[SerializeField]
		private LayoutElement elementPrefab;
		private Stack<LayoutElement> m_stack;
		private List<LayoutElement> activeElements;
		private Stack<LayoutElement> ElementsStack 
		{
			get 
			{
				if (m_stack == null) 
				{
					m_stack = new Stack<LayoutElement>();
				}
				return m_stack;
			}
		}

	    public int Count 
		{
	        get 
			{
				return activeElements.Count; 
			}
	    }

	    void Awake()
		{
			if (activeElements == null) 
				activeElements = new List<LayoutElement>();

			if (elementPrefab!= null && elementPrefab.transform.parent == transform)	// Если префаб лежит внутри
				DestroyElement(elementPrefab);
		}

		/// <summary>
		/// Инстанцирует элемент с префаба или достаёт из пула.
		/// </summary>
		/// <returns></returns>
		public LayoutElement InstantiateElement(bool gameObjActive = true) 
		{
			LayoutElement element;
			if (ElementsStack.Count > 0) 
			{
				element = ElementsStack.Pop();
			}
			else 
			{
				element = Instantiate<LayoutElement>(elementPrefab);
				element.transform.SetParent(this.transform);
			}
			element.gameObject.SetActive(gameObjActive);
			if (activeElements == null) 
			{
				activeElements = new List<LayoutElement>();
			}
			activeElements.Add(element);
			element.transform.SetAsLastSibling();
			element.transform.localScale = Vector3.one;
			return element;
		}

		/// <summary>
		/// Деактивирует элемент и помещает его в пул доступных объектов.
		/// </summary>
		public void DestroyElement(LayoutElement element) 
		{
			ElementsStack.Push(element);
			activeElements.Remove(element);
			element.gameObject.SetActive(false);
		}

		/// <summary>
		/// Получить активный элемент с порядковым номером.
		/// </summary>
		public LayoutElement getElement(int pNo, bool recreate=false, bool gameObjActive = true)
		{
			if (activeElements.Count > pNo)
				return activeElements[pNo];
			else if (recreate)
				return InstantiateElement(gameObjActive);
			else 
				return null;
		}

		/// <summary>
		/// Деактивирует все элементы
		/// </summary>
		public void Clear() 
		{
			if (activeElements != null)
			{
				while(activeElements.Count > 0)
					DestroyElement(activeElements[0]);
			}
		}
	}
