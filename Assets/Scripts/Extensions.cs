using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;
using Input = UnityEngine.Input;

public static class Extensions
{

    #region GameObject

    public static Component AddComponent(this GameObject go, Component toAdd)
    {
        return go.AddComponent(toAdd.GetType()).GetCopyOf(toAdd);
    }

    /// <summary>
    /// Находит дочерний объект с указанным именем. Ищет лишь первый уровень
    /// </summary>
    public static GameObject FindChild(this GameObject _go, string _name)
    {
        if (_go == null)
            return null;
        foreach (Transform trans in _go.transform)
            if (trans.name == _name)
                return trans.gameObject;
        return null;
    }

    public static GameObject FindChildGameObject(this GameObject go, string withName)
    {
        Transform[] ts = go.transform.GetComponentsInChildren<Transform>();
        foreach (Transform t in ts) if (t.gameObject.name == withName) return t.gameObject;
        return null;
    }

    public static T GetComponentInChildren<T>(this GameObject _obj, bool _includeInactive)
        where T : class
    {
        var list = _obj.GetComponentsInChildren<T>(_includeInactive);
        return list.Length > 0 ? list[0] : null;
    }


    public static T GetComponentForced<T>(this GameObject _object)
        where T : Component
    {
        T cmp = _object.GetComponent<T>();
        if (cmp != null)
            return cmp;
        return _object.AddComponent<T>();
    }

    #endregion

    #region Component

    public static Component GetCopyOf(this Component comp, Component other)
    {
        Type type = comp.GetType();
        if (type != other.GetType()) return null;
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos)
        {
            if (!pinfo.CanWrite) continue;
            try { pinfo.SetValue(comp, pinfo.GetValue(other, null), null); }
            catch { }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos)
        {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp;
    }

    #endregion

    #region MonoBehaviour

    public static T GetComponentInChildren<T>(this MonoBehaviour _mono, bool _includeInactive)
        where T : class
    {
        return _mono.gameObject.GetComponentInChildren<T>(_includeInactive);
    }

    /// <summary>
    /// Вызывает указанную функцию через указанное время в секундах.
    /// Функция использует корутину, так что объект _owner должен быть активен.
    /// </summary>
    /// <returns></returns>
    public static Coroutine CallTimeout(this MonoBehaviour _owner, Action _method, float _time = 0f)
    {
        return _owner.StartCoroutineSafe(Caller(_method, _time));
    }


    private static IEnumerator Caller(Action _call, float _timer)
    {
        if (_timer > 0f)
            yield return new WaitForSeconds(_timer);
        else
            yield return null;
        _call.Execute();
    }

    public static Coroutine StartCoroutineSafe(this MonoBehaviour _component, IEnumerator _routine)
    {
        if (_component == null || !_component.isActiveAndEnabled)
            return null;
        return _component.StartCoroutine(_routine);
    }

    #endregion

    #region BoxCollider

    /// <summary>
    /// Возвращает случайную точку внутри коллайдера в глобальной системе координат
    /// </summary>
    /// <param name="_box"></param>
    /// <param name="_yAsZero"></param>
    /// <returns></returns>
    public static Vector3 GetRandomPoint(this BoxCollider _box, bool _yAsZero = true, bool _local = false, Transform _target = null)
    {
        if (_box == null)
            return Vector3.zero;
        var pnt = _box.center + (_yAsZero ? _box.size.RandomXZ(-0.5f, 0.5f) : _box.size.RandomXYZ(-0.5f, 0.5f));
        if (_local && _target == null)
            return pnt;

        pnt = _box.transform.TransformPoint(pnt);
        if (!_local)
            return pnt;
        return _target.InverseTransformPoint(pnt);

        /*pnt = _box.transform.TransformPoint( pnt );
        if (!_local)
            return pnt;
        return _target.InverseTransformPoint( pnt );*/

        /*var xx = Random.Range( -_box.size.x, _box.size.x ) * 0.5f;
        var yy = _yAsZero ? 0f : Random.Range( -_box.size.y, _box.size.y ) * 0.5f;
        var zz = Random.Range( -_box.size.z, _box.size.z ) * 0.5f;
        return (_local?_box.transform.localPosition:_box.transform.position) + _box.center + new Vector3( xx, yy, zz );*/
    }

    /// <summary>
    /// Возвращает центр коллайдера в глобальной системе координат
    /// </summary>
    /// <param name="_box"></param>
    /// <returns></returns>
    public static Vector3 GetPoint(this BoxCollider _box, bool _local = false)
    {
        return (_local ? _box.transform.localPosition : _box.transform.position) + _box.center;
    }

    public static Bounds? GetIntersect(this BoxCollider _box, BoxCollider _other)
    {
        var r1 = new Bounds(_box.GetPoint(), _box.size);
        var r2 = new Bounds(_other.GetPoint(), _other.size);

        return r1.GetIntersect(r2);
    }

    public static bool GetIntersectFact(this BoxCollider _box, BoxCollider _other)
    {
        var r1 = new Bounds(_box.GetPoint(), _box.size);
        var r2 = new Bounds(_other.GetPoint(), _other.size);

        return r1.Intersects(r2);
    }

    public static Bounds? GetIntersect(this Bounds r1, Bounds r2)
    {
        if (!r1.Intersects(r2))
            return null;

        float x1 = Mathf.Min(r1.max.x, r2.max.x);
        float x2 = Mathf.Max(r1.min.x, r2.min.x);
        float y1 = Mathf.Min(r1.max.y, r2.max.y);
        float y2 = Mathf.Max(r1.min.y, r2.min.y);
        float z1 = Mathf.Min(r1.max.z, r2.max.z);
        float z2 = Mathf.Max(r1.min.z, r2.min.z);

        Bounds res = new Bounds();
        res.size = new Vector3(Mathf.Max(0.0f, x1 - x2), Mathf.Max(0.0f, y1 - y2), Mathf.Max(0.0f, z1 - z2));
        res.center = new Vector3(Mathf.Min(x1, x2), Mathf.Min(y1, y2), Mathf.Min(z1, z2)) + res.size * 0.5f;
        return res;
    }

    public static bool IsInside(this Bounds r1, Bounds r2)
    {
        return r1.min.x >= r2.min.x && r1.min.z >= r2.min.z &&
               r1.max.x <= r2.max.x && r1.max.z <= r2.max.z;
    }

    public static bool IsInside(this Vector3 r1, Bounds r2)
    {
        return r1.x >= r2.min.x && r1.z >= r2.min.z &&
               r1.x <= r2.max.x && r1.z <= r2.max.z;
    }

    #endregion

    #region Bounds

    /// <summary>
    /// Возвращает случайную точку внутри коллайдера
    /// </summary>
    public static Vector3 GetRandomPoint(this Bounds _box, bool _yAsZero = true)
    {
        return _box.center + (_yAsZero ? _box.size.RandomXZ(-0.5f, 0.5f) : _box.size.RandomXYZ(-0.5f, 0.5f));
    }

    #endregion

    #region IList

    public static void AddOnce<T>(this IList<T> _list, T _value)
    {
        if (!_list.Contains(_value))
            _list.Add(_value);
    }

    public static void AddOnce(this IList _list, object _value)
    {
        if (!_list.Contains(_value))
            _list.Add(_value);
    }

    public static void AddNotNull(this IList _list, object _value)
    {
        if (_value != null)
            _list.Add(_value);
    }

    public static void AddCast<T>(this IList<T> _list, object _value)
    {
        _list.Add((T)_value);
    }

    public static T GetRandom<T>(this IList<T> _list, T _onEmpty = default(T))
    {
        if (_list.Count == 0)
            return _onEmpty;
        var index = Random.Range(0, _list.Count);
        return _list[index];
    }

    public static object GetRandom(this IList _list, object _onEmpty)
    {
        if (_list.Count == 0)
            return _onEmpty;
        var index = Random.Range(0, _list.Count);
        return _list[index];
    }

    public static T GetOrInsert<T>(this IList<T> _list, int _index, T _def = default(T))
    {
        if ((uint)_index >= (uint)_list.Count)
            _list.Insert(_index, _def);
        return _list[_index];
    }

    public static T Get<T>(this IList<T> _list, int _index, T _def = default(T))
    {
        if ((uint)_index >= (uint)_list.Count)
            return _def;
        return _list[_index];
    }

    public static T GetClamped<T>(this IList<T> _list, int _index, T _def = default(T))
    {
        return Get(_list, Mathf.Clamp(_index, 0, _list.Count - 1), _def);
    }

    public static T Last<T>(this IList<T> _list, T _def = default(T))
    {
        return _list.Get(_list.Count - 1, _def);
    }

    public static T First<T>(this IList<T> _list, T _def = default(T))
    {
        return _list.Get(0, _def);
    }

    /// <summary>
    /// Возвращает первое значение, отвечающее условию сортировки
    /// </summary>
    public static T First<T>(this IList<T> _list, Func<T, T, bool> _condition, T _default = default(T))
    {
        if (_list == null || _list.Count == 0)
            return _default;
        List<T> lst = new List<T>(_list);
        lst.Sort((_x, _y) => _condition(_x, _y) ? -1 : 1);
        return lst[0];
    }

    public static void Set<T>(this IList<T> _list, int _index, T _value)
    {
        if ((uint)_index >= (uint)_list.Count)
            _list.Insert(_index, _value);
        else
            _list[_index] = _value;
    }

    public static void Replace<T>( this IList<T> _list, T _valueSource, T _valueNew )
    {
        int index = _list.IndexOf(_valueSource);
        if (index >= 0)
            _list[index] = _valueNew;
    }


    /// <summary>
    /// Меняет два элемента из позициями в списке
    /// </summary>
    public static void Exchange<T>(this IList<T> _list, T _first, T _second)
    {
        if (_list == null)
            return;
        int index1 = _list.IndexOf(_first);
        int index2 = _list.IndexOf(_second);

        if (index1 < 0 || index2 < 0)
            return;

        _list[index1] = _second;
        _list[index2] = _first;
    }

    /// <summary>
    /// Меняет два элемента из позициями в списке
    /// </summary>
    public static void Exchange<T>(this IList<T> _list, int _index1, int _index2)
    {
        if (_list == null)
            return;
        if (_index1 < 0 || _index2 < 0)
            return;

        T e1 = _list[_index1];
        _list[_index1] = _list[_index2];
        _list[_index2] = e1;
    }

    /// <summary>
    /// Перемешивает элементы списка в случайном порядке. Возвращает тот же список.
    /// </summary>
    public static IList<T> Shuffle<T>(this IList<T> _list)
    {
        List<T> tlist = new List<T>(_list);
        _list.Clear();
        while (tlist.Count > 0)
        {
            int pos = Random.Range(0, tlist.Count);
            _list.Add(tlist[pos]);
            tlist.RemoveAt(pos);
        }
        return _list;
    }

    /*public static int CountSafe<T>( this IList<T> _list )
    {
        return _list != null ? _list.Count : 0;
    }*/

    public static int CountSafe(this IList _list)
    {
        return _list != null ? _list.Count : 0;
    }

    public static bool IsInRange(this IList _list, int _index)
    {
        return _list != null && ((uint)_index) < _list.Count;
    }

    public static TResult[] Select<TSource, TResult>(this IList<TSource> source, Func<TSource, TResult> selector)
    {
        TResult[] res = new TResult[source.Count];
        for (int i = source.Count - 1; i >= 0; i--)
            res[i] = selector(source[i]);
        return res;
    }

    public static TSource Aggregate<TSource>(this IList<TSource> _source, Func<TSource, TSource, TSource> _func)
    {
        if (_source.Count == 0)
            return default(TSource);

        TSource source1 = _source[0];
        for (int i = 1; i < _source.Count; i++)
            source1 = _func(source1, _source[i]);

        return source1;
    }

    /// <summary>
    /// Выполнить функция с объектом. Просто для удобства - чтобы можно было в одну строчку всё вписать.
    /// </summary>
    public static TResult DoWith<TSource, TResult>(this TSource _source, Func<TSource, TResult> _operator)
    {
        return _operator(_source);
    }

    #endregion

    #region IEnumerable

    public static List<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
    {
        List<TResult> res = new List<TResult>();
        foreach (TSource src in source)
            res.Add(selector(src));
        return res;
    }

    public static List<T> ToList<T>(this IEnumerable<T> source)
    {
        return new List<T>(source);
    }

    public static T[] ToArray<T>(this IEnumerable<T> source)
    {
        return ToList(source).ToArray();
    }
    #endregion

    #region Dictionary
    /// <summary>
    /// Добавляет значение в словарь только если ключ отсутствует
    /// </summary>
    public static void AddOnce<TKey, TValue>(this Dictionary<TKey, TValue> _dict, TKey _key, TValue _value)
    {
        if (!_dict.ContainsKey(_key))
            _dict.Add(_key, _value);
    }

    /// <summary>
    /// Записывает значение в словарь под указанным ключом. Наличие или отсутствие ключа не играет роли.
    /// </summary>
    public static void Replace<TKey, TValue>(this Dictionary<TKey, TValue> _dict, TKey _key, TValue _value)
    {
        if (!_dict.ContainsKey(_key))
            _dict.Add(_key, _value);
        else
            _dict[_key] = _value;
    }

    /// <summary>
    /// Возвращает значение из словаря. Если оно небыло найдено - добавляет его туда.
    /// </summary>
    public static TValue Request<TKey, TValue>(this Dictionary<TKey, TValue> _dict, TKey _key, TValue _new = default(TValue))
    {
        if (_dict.ContainsKey(_key))
            return _dict[_key];

        _dict.Add(_key, _new);
        return _new;
    }

    /// <summary>
    /// Возвращает значение из словаря. Если оно небыло найдено - возвращает значение по-умолчанию
    /// </summary>
    public static TValue GetOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> _dict, TKey _key, TValue _default = default(TValue))
    {
        if (_dict != null && _key != null && _dict.ContainsKey(_key))
            return _dict[_key];

        return _default;
    }

    /// <summary>
    /// Возвращает значение из словаря. Если оно небыло найдено - создает элемент через дефолтный конструктор, добавляет его в словарь и возвращает как результат
    /// </summary>
    [NotNull]
    public static TValue GetOrNew<TKey, TValue>(this Dictionary<TKey, TValue> _dict, TKey _key)
        where TValue : new()
    {
        if (_dict.ContainsKey(_key))
            return _dict[_key];

        TValue value = new TValue();
        _dict.Add(_key, value);
        return value;
    }
    #endregion

    #region Rect

    public static Rect Slice(this Rect _rect, float _offset, float _width)
    {
        return new Rect(_rect.x + _offset, _rect.y, _width, _rect.height);
    }

    public static Rect Slice(this Rect _rect, float _width)
    {
        return new Rect(_rect.x, _rect.y, _width, _rect.height);
    }

    /// <summary>
    /// Возвращает Rect, граничащий с текущим с правой стороны, и имеющий указанную ширину
    /// </summary>
    /// <returns></returns>
    public static Rect Right(this Rect _rect, float _width)
    {
        return new Rect(_rect.xMax, _rect.y, _width, _rect.height);
    }

    /// <summary>
    /// Возвращает Rect, граничащий с текущим с правой стороны, и имеющий ту же ширину
    /// </summary>
    /// <returns></returns>
    public static Rect Right(this Rect _rect)
    {
        return new Rect(_rect.xMax, _rect.y, _rect.width, _rect.height);
    }

    public static Rect Down(this Rect _rect)
    {
        return new Rect(_rect.x, _rect.yMax, _rect.width, _rect.height);
    }

    public static Rect Down(this Rect _rect, float _height)
    {
        return new Rect(_rect.x, _rect.yMax, _rect.width, _height);
    }

    public static Rect Height(this Rect _rect, float _height)
    {
        return new Rect(_rect.x, _rect.y, _rect.width, _height);
    }

    #endregion

    #region Action

    /// <summary>
    /// Вызывает делегат с предварительной проверкой на null
    /// </summary>
    public static void Execute(this Action _action)
    {
        if (_action != null)
            _action();
    }

    /// <summary>
    /// Вызывает делегат с предварительной проверкой на null
    /// </summary>
    public static void Execute<T>(this Action<T> _action, T _a1)
    {
        if (_action != null)
            _action(_a1);
    }

    /// <summary>
    /// Вызывает делегат с предварительной проверкой на null
    /// </summary>
    public static void Execute<T1, T2>(this Action<T1, T2> _action, T1 _a1, T2 _a2)
    {
        if (_action != null)
            _action(_a1, _a2);
    }

    /// <summary>
    /// Вызывает делегат с предварительной проверкой на null
    /// </summary>
    public static void Execute<T1, T2, T3>(this Action<T1, T2, T3> _action, T1 _a1, T2 _a2, T3 _a3)
    {
        if (_action != null)
            _action(_a1, _a2, _a3);
    }

    /// <summary>
    /// Вызывает делегат с предварительной проверкой на null
    /// </summary>
    public static void Execute<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> _action, T1 _a1, T2 _a2, T3 _a3, T4 _a4)
    {
        if (_action != null)
            _action(_a1, _a2, _a3, _a4);
    }

    #endregion

    #region Func

    public enum FuncMode
    {
        Any,
        All
    }

    public static bool Execute<T1, T2, T3, T4>(this Func<T1, T2, T3, T4, bool> _func, T1 _val1, T2 _val2, T3 _val3, T4 _val4,
        FuncMode _mode = FuncMode.Any, bool _onNull = false)
    {
        if (_func == null)
            return _onNull;

        bool result = false;
        Delegate[] list = _func.GetInvocationList();
        switch (_mode)
        {
            case FuncMode.Any:
                result = false;
                for (int i = 0; i < list.Length; i++)
                    result |= (bool)list[i].DynamicInvoke(_val1, _val2, _val3, _val4);
                break;
            case FuncMode.All:
                result = true;
                for (int i = 0; i < list.Length; i++)
                    result &= (bool)list[i].DynamicInvoke(_val1, _val2, _val3, _val4);
                break;
        }
        return result;
    }

    public static bool Execute<T1, T2, T3>(this Func<T1, T2, T3, bool> _func, T1 _val1, T2 _val2, T3 _val3, FuncMode _mode = FuncMode.Any, bool _onNull = false)
    {
        if (_func == null)
            return _onNull;

        bool result = false;
        Delegate[] list = _func.GetInvocationList();
        switch (_mode)
        {
            case FuncMode.Any:
                result = false;
                for (int i = 0; i < list.Length; i++)
                    result |= (bool)list[i].DynamicInvoke(_val1, _val2, _val3);
                break;
            case FuncMode.All:
                result = true;
                for (int i = 0; i < list.Length; i++)
                    result &= (bool)list[i].DynamicInvoke(_val1, _val2, _val3);
                break;
        }
        return result;
    }

    public static bool Execute<T1, T2>(this Func<T1, T2, bool> _func, T1 _val1, T2 _val2, FuncMode _mode = FuncMode.Any, bool _onNull = false)
    {
        if (_func == null)
            return _onNull;

        bool result = false;
        Delegate[] list = _func.GetInvocationList();
        switch (_mode)
        {
            case FuncMode.Any:
                result = false;
                for (int i = 0; i < list.Length; i++)
                    result |= (bool)list[i].DynamicInvoke(_val1, _val2);
                break;
            case FuncMode.All:
                result = true;
                for (int i = 0; i < list.Length; i++)
                    result &= (bool)list[i].DynamicInvoke(_val1, _val2);
                break;
        }
        return result;
    }

    public static bool Execute<T1>(this Func<T1, bool> _func, T1 _val1, FuncMode _mode = FuncMode.Any, bool _onNull = false)
    {
        if (_func == null)
            return _onNull;

        bool result = false;
        Delegate[] list = _func.GetInvocationList();
        switch (_mode)
        {
            case FuncMode.Any:
                result = false;
                for (int i = 0; i < list.Length; i++)
                    result |= (bool)list[i].DynamicInvoke(_val1);
                break;
            case FuncMode.All:
                result = true;
                for (int i = 0; i < list.Length; i++)
                    result &= (bool)list[i].DynamicInvoke(_val1);
                break;
        }
        return result;
    }

    public static bool Execute(this Func<bool> _func, FuncMode _mode = FuncMode.Any, bool _onNull = false)
    {
        if (_func == null)
            return _onNull;

        bool result = false;
        Delegate[] list = _func.GetInvocationList();
        switch (_mode)
        {
            case FuncMode.Any:
                result = false;
                for (int i = 0; i < list.Length; i++)
                    result |= (bool)list[i].DynamicInvoke();
                break;
            case FuncMode.All:
                result = true;
                for (int i = 0; i < list.Length; i++)
                    result &= (bool)list[i].DynamicInvoke();
                break;
        }
        return result;
    }

    #endregion

    #region Vector

    /// <summary>
    /// Возвращает вектор, состоящих из случайных значений, основанных на данном векторе
    /// Для получения значений от 0 до макс. значения - используются множители 0.0 и 1.0
    /// Для получения от минус максимального до максимального - множители -1.0 и 1.0
    /// </summary>
    public static Vector2 RandomXY(this Vector2 _vec, float _leftMult = 0.0f, float _rightMult = 1.0f)
    {
        return new Vector2(
            Random.Range(_vec.x * _leftMult, _vec.x * _rightMult),
            Random.Range(_vec.y * _leftMult, _vec.y * _rightMult));
    }

    /// <summary>
    /// Возвращает вектор, состоящих из случайных значений, основанных на данном векторе
    /// Преобразует Vector2 в Vector3, где x = xRandom, y = 0, z = yRandom
    /// Для получения значений от 0 до макс. значения - используются множители 0.0 и 1.0
    /// Для получения от минус максимального до максимального - множители -1.0 и 1.0
    /// </summary>
    public static Vector3 RandomXZ(this Vector2 _vec, float _leftMult = 0.0f, float _rightMult = 1.0f)
    {
        return new Vector3(
            Random.Range(_vec.x * _leftMult, _vec.x * _rightMult),
            0f,
            Random.Range(_vec.y * _leftMult, _vec.y * _rightMult));
    }

    /// <summary>
    /// Возвращает вектор, состоящих из случайных значений, основанных на данном векторе
    /// Для получения значений от 0 до макс. значения - используются множители 0.0 и 1.0
    /// Для получения от минус максимального до максимального - множители -1.0 и 1.0
    /// </summary>
    public static Vector3 RandomXYZ(this Vector3 _vec, float _leftMult = 0.0f, float _rightMult = 1.0f)
    {
        return new Vector3(
            Random.Range(_vec.x * _leftMult, _vec.x * _rightMult),
            Random.Range(_vec.y * _leftMult, _vec.y * _rightMult),
            Random.Range(_vec.z * _leftMult, _vec.z * _rightMult));
    }

    /// <summary>
    /// Возвращает вектор, состоящих из случайных значений, основанных на данном векторе
    /// Значение Y будет равно нулю
    /// Для получения значений от 0 до макс. значения - используются множители 0.0 и 1.0
    /// Для получения от минус максимального до максимального - множители -1.0 и 1.0
    /// </summary>
    public static Vector3 RandomXZ(this Vector3 _vec, float _leftMult = 0.0f, float _rightMult = 1.0f)
    {
        return new Vector3(
            Random.Range(_vec.x * _leftMult, _vec.x * _rightMult),
            0f,
            Random.Range(_vec.z * _leftMult, _vec.z * _rightMult));
    }

    public static Vector2 xz(this Vector3 _vec)
    {
        return new Vector2(_vec.x, _vec.z);
    }

    public static Vector3 Round(this Vector3 _vec)
    {
        return new Vector3(Mathf.Round(_vec.x), Mathf.Round(_vec.y), Mathf.Round(_vec.z));
    }

    public static Vector3 Floor(this Vector3 _vec)
    {
        return new Vector3(Mathf.Floor(_vec.x), Mathf.Floor(_vec.y), Mathf.Floor(_vec.z));
    }

    public static Vector3 Ceil(this Vector3 _vec)
    {
        return new Vector3(Mathf.Ceil(_vec.x), Mathf.Ceil(_vec.y), Mathf.Ceil(_vec.z));
    }

    public static Vector3 CeilXZ(this Vector3 _vec)
    {
        return new Vector3(Mathf.Ceil(_vec.x), _vec.y, Mathf.Ceil(_vec.z));
    }

    public static Vector3 SetX(this Vector3 _vec, float _value)
    {
        return new Vector3(_value, _vec.y, _vec.z);
    }

    public static Vector3 SetY(this Vector3 _vec, float _value)
    {
        return new Vector3(_vec.x, _value, _vec.z);
    }

    public static Vector3 SetZ(this Vector3 _vec, float _value)
    {
        return new Vector3(_vec.x, _vec.y, _value);
    }

    public static Vector3 Sign(this Vector3 _vec)
    {
        return new Vector3(Mathf.Sign(_vec.x), Mathf.Sign(_vec.y), Mathf.Sign(_vec.z));
    }

    public static Vector3 Abs(this Vector3 _vec)
    {
        return new Vector3(Mathf.Abs(_vec.x), Mathf.Abs(_vec.y), Mathf.Abs(_vec.z));
    }

    public static Vector3 Lerp(this IList<Vector3> _list, float _position)
    {
        _position = Mathf.Clamp01(_position);
        int left = Mathf.FloorToInt(_position * (_list.Count - 1));
        int right = Math.Min(left + 1, _list.Count - 1);

        _position = (_position * (_list.Count - 1)) % 1;

        return Vector3.Lerp(_list[left], _list[right], _position);
    }

    public static Vector3 FlipXZ(this Vector3 _vec)
    {
        return new Vector3(_vec.z, _vec.y, _vec.x);
    }

    public static Vector3 FlipXZ(this Vector3 _vec, bool _doFlip)
    {
        return _doFlip ? _vec.FlipXZ() : _vec;
    }

    /// <summary>
    /// Сохраняет вектор в JsonObject
    /// </summary>
    public static JsonObject Serialize(this Vector3 _vector)
    {
        var obj = new JsonObject();
        obj["x"] = _vector.x;
        obj["y"] = _vector.y;
        obj["z"] = _vector.z;
        return obj;
    }
    /// <summary>
    /// Сохраняет вектор в JsonObject за исключением y-координаты
    /// </summary>
    /// <param name="_vector"></param>
    /// <returns></returns>
    public static JsonObject SerializeXZ(this Vector3 _vector)
    {
        var obj = new JsonObject();
        obj["x"] = _vector.x;
        obj["z"] = _vector.z;
        return obj;
    }

    /// <summary>
    /// Возвращает вектор на основе JsonObject-а
    /// </summary>
    public static Vector3 DeserializeAsVector3(this JsonObject _data)
    {
        return new Vector3(_data.Get<float>("x"), _data.Get<float>("y"), _data.Get<float>("z"));
    }

    #endregion

 
    #region WeakReference

    public static WeakReference Checked(this WeakReference _ref)
    {
        return Exists(_ref) ? _ref : null;
    }

    public static bool Exists(this WeakReference _ref)
    {
        return _ref != null && _ref.IsAlive;
    }

    public static T To<T>(this WeakReference _ref)
    {
        return (T)_ref.Target;
    }

    #endregion

    #region Array

    public static int IndexOf<T>(this IList<T> _array, T _value)
    {
        if (_array == null)
            return -1;
        for (int i = _array.Count - 1; i >= 0; i--)
        {
            if (Equals(_array[i], _value))
                return i;
        }
        return -1;
    }

    public static int IndexOf<T>(this IList<T> _array, Func<T, bool> _value)
    {
        if (_array == null)
            return -1;
        for (int i = _array.Count - 1; i >= 0; i--)
        {
            if (_value(_array[i]))
                return i;
        }
        return -1;
    }

    public static bool Contains<T>(this IList<T> _array, T _value)
    {
        return IndexOf(_array, _value) >= 0;
    }

    public static bool Contains<T>(this IList<T> _array, IList<T> _values)
    {
        for (int i = _values.Count - 1; i >= 0; i--)
        {
            if (IndexOf(_array, _values[i]) >= 0)
                return true;
        }
        return false;
    }

    public static bool Contains<T>(this IList<T> _array, Func<T, bool> _value)
    {
        if (_array == null)
            return false;
        for (int i = _array.Count - 1; i >= 0; i--)
        {
            if (_value(_array[i]))
                return true;
        }
        return false;
    }

    #endregion

    #region Object

    public static IFormatProvider Format = new NumberFormatInfo();

    public static TResult Cast<TResult>(this object _obj, TResult _default = default(TResult))
    {
        if (_obj == null)
            return _default;
        if (_obj is TResult)
            return (TResult)_obj;

        try
        {
            IConvertible res = _obj as IConvertible;
            if (res != null)
                return (TResult)res.ToType(typeof(TResult), Format);
            throw new Exception("Inconvertable type");
        }
        catch (Exception _ex)
        {
            if (typeof(TResult).IsEnum)
            {
                try
                {
                    string str = _obj as string;
                    if (str != null)
                        return (TResult)Enum.Parse(typeof(TResult), str);
                    return (TResult)Enum.ToObject(typeof(TResult), _obj);
                }
                catch (Exception _ex2)
                {
                    _ex = _ex2;
                }
            }
#if UNITY_EDITOR || DEBUG
            Debug.LogWarning("Unable to convert from " + _obj.GetType().Name + " to " + typeof(TResult).Name + ": " + _ex);
#endif
        }
        return _default;
    }

    public static object Cast(this object _obj, Type _castType, object _default)
    {
        if (_obj == null)
            return _default;
        if (_castType.IsInstanceOfType(_obj))
            return _obj;

        try
        {
            IConvertible res = _obj as IConvertible;
            if (res != null)
                return res.ToType(_castType, Format);
            throw new Exception("Inconvertable type");
        }
        catch (Exception _ex)
        {
            if (_castType.IsEnum)
            {
                try
                {
                    return Enum.ToObject(_castType, _obj);
                }
                catch (Exception _ex2)
                {
                    _ex = _ex2;
                }
            }
#if UNITY_EDITOR || DEBUG
            Debug.LogWarning("Unable to convert from " + _obj.GetType().Name + " to " + _castType.Name + ": " + _ex);
#endif
        }
        return _default;
    }

    public static TResult CastNew<TResult>(this object _obj)
        where TResult : new()
    {
        if (_obj == null)
            return new TResult();
        if (_obj is TResult)
            return (TResult)_obj;

        IConvertible res = _obj as IConvertible;
        if (res != null)
        {
            try
            {
                return (TResult)res.ToType(typeof(TResult), Format);
            }
            catch (Exception _ex)
            {
#if UNITY_EDITOR || DEBUG
                Debug.LogWarning("Unable to convert from " + _obj.GetType().Name + " to " + typeof(TResult).Name + ": " + _ex);
#endif
                return new TResult();
            }
        }

        Debug.LogWarning("Unable to convert from" + _obj.GetType().Name + " to " + typeof(TResult).Name);
        return new TResult();
    }

    public static T DeepClone<T>(this T a)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, a);
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }
    }

    #endregion

    #region Camera

    public static Ray MouseRay(this Camera _camera)
    {
        return _camera.ViewportPointToRay(MouseViewport(_camera));
    }

    public static Vector3 MouseViewport(this Camera _camera)
    {
        return new Vector3(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
    }

    public static Vector3 MousePixels(this Camera _camera)
    {
        return Input.mousePosition;
    }

    #endregion

    #region Items

    [Flags]
    public enum ProducerType
    {
        Entity = 1,
        Clicker = 2,
        SpawnZone = 4,
        NPC = 8,
        CristmasTree = 16,
        Achievement = 32,
        Clearable = 64,
        Everything = Entity | Clicker | SpawnZone | NPC | CristmasTree | Achievement,
        RandomSources = SpawnZone | CristmasTree
    }

    public class Producer
    {
        public GameObject Object;
        public ProducerType Type;

        public Producer(GameObject _object, ProducerType _type)
        {
            Object = _object;
            Type = _type;
        }
    }

  

    #endregion

    #region Enum

    public static bool CheckFlag(this Enum _enum, Enum _flag)
    {
        return (Convert.ToInt32(_enum) & Convert.ToInt32(_flag)) > 0;
    }

    #endregion

    #region Type

    /// <summary>
    /// The binding flags for instance fields and properties.
    /// </summary>
    public const BindingFlags instanceBindingFlags =
        BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// The binding flags for static fields and properties.
    /// </summary>
    public const BindingFlags staticBindingFlags =
        BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// A regular expression to match an instance's prefab name
    /// </summary>
    private static readonly Regex matchPrefabName =
        new Regex(@".+(?=\s*\(Clone\))|.+");

    /// <summary>
    /// Gets the custom attributes.
    /// </summary>
    /// <remarks>
    /// http://msdn.microsoft.com/en-us/library/dwc6ew1d(v=vs.90).aspx
    /// </remarks>
    /// <returns>The custom attributes on the provider.</returns>
    /// <param name='provider'>Provider.</param>
    /// <param name='inherit'>
    /// <c>true</c> if inherited attributes should be included; otherwise, <c>false</c>.
    /// </param>
    /// <typeparam name='T'>The type of the custom attributes desired.</typeparam>
    /// <exception cref='System.ArgumentNullException'>
    /// Is thrown if the provider is <see langword="null" />.
    /// </exception>
    public static T[] GetCustomAttributes<T>(
        this ICustomAttributeProvider provider, bool inherit = false
    ) where T : Attribute
    {
        if (provider == null)
        {
            throw new ArgumentNullException("provider");
        }
        T[] attributes = provider.GetCustomAttributes(typeof(T), inherit) as T[];
        if (attributes == null)
        {   // WORKAROUND: Due to a bug in the code for retrieving attributes from a dynamic generated parameter,
            // GetCustomAttributes can return an instance of an object[] instead of T[], and hence the cast above
            // will return null.
            return new T[0];
        }
        return attributes;
    }

    /// <summary>
    /// Gets the field value on an instance.
    /// </summary>
    /// <returns>The field value on an instance.</returns>
    /// <param name="provider">Provider.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="bindingAttr">Binding flags.</param>
    /// <typeparam name="T">The type of the field.</typeparam>
    public static T GetFieldValue<T>(
        this object provider, string fieldName, BindingFlags bindingAttr = instanceBindingFlags
        )
    {
        return GetFieldValue<T>(provider.GetType(), provider, fieldName, bindingAttr);
    }

    /// <summary>
    /// Gets the field value on a provider
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="provider">Provider.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="bindingAttr">Binding attr.</param>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <exception cref="System.ArgumentNullException">
    /// Is thrown if the type or fieldName is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.ArgumentException">Is thrown if fieldName is empty.</exception>
    private static T GetFieldValue<T>(
        Type type, object provider, string fieldName, BindingFlags bindingAttr
        )
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }
        else if (fieldName == null)
        {
            throw new ArgumentNullException("fieldName");
        }
        else if (fieldName.Length == 0)
        {
            throw new ArgumentException("No field name specified.", "fieldName");
        }
        return (T)type.GetField(fieldName, bindingAttr).GetValue(provider);
    }

    /// <summary>
    /// Gets the name of the prefab associated with the supplied instance.
    /// </summary>
    /// <returns>The prefab name.</returns>
    /// <param name="instance">Instance.</param>
    public static string GetPrefabName(this UnityEngine.Object instance)
    {
        return matchPrefabName.Match(instance.name).Value;
    }

    /// <summary>
    /// Gets the property value on an instance.
    /// </summary>
    /// <returns>The property value on an instance.</returns>
    /// <param name="provider">Provider.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="bindingAttr">Binding flags.</param>
    /// <param name="index">Index.</param>
    /// <typeparam name="T">The type of the property.</typeparam>
    public static T GetPropertyValue<T>(
        this object provider,
        string propertyName,
        BindingFlags bindingAttr = instanceBindingFlags,
        object[] index = null
        )
    {
        return GetPropertyValue<T>(provider.GetType(), provider, propertyName, bindingAttr, index);
    }

    /// <summary>
    /// Gets the property value on a provider.
    /// </summary>
    /// <returns>The property value on an instance.</returns>
    /// <param name="provider">Provider.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="bindingAttr">Binding flags.</param>
    /// <param name="index">Index.</param>
    /// <typeparam name="T">The type of the property.</typeparam>
    /// <exception cref="System.ArgumentNullException">
    /// Is thrown if the provider or propertyName is <see langword="null" />.
    /// </exception>
    /// <exception cref="System.ArgumentException">Is thrown if propertyName is empty.</exception>
    private static T GetPropertyValue<T>(
        Type type, object provider, string propertyName, BindingFlags bindingAttr, object[] index
        )
    {
        if (provider == null)
        {
            throw new ArgumentNullException("provider");
        }
        else if (propertyName == null)
        {
            throw new ArgumentNullException("propertyName");
        }
        else if (propertyName.Length == 0)
        {
            throw new ArgumentException("No property name specified.", "propertyName");
        }
        return (T)provider.GetType().GetProperty(propertyName, bindingAttr).GetValue(provider, index);
    }

    /// <summary>
    /// Gets the field value on a class.
    /// </summary>
    /// <returns>The field value on a class.</returns>
    /// <param name="type">Type.</param>
    /// <param name="fieldName">Field name.</param>
    /// <param name="bindingAttr">Binding flags.</param>
    /// <typeparam name="T">The type of the field.</typeparam>
    public static T GetStaticFieldValue<T>(
        this Type type, string fieldName, BindingFlags bindingAttr = staticBindingFlags
        )
    {
        return GetFieldValue<T>(type, null, fieldName, bindingAttr);
    }

    /// <summary>
    /// Gets the property value on a class.
    /// </summary>
    /// <returns>The property value on a class.</returns>
    /// <param name="provider">Provider.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="bindingAttr">Binding flags.</param>
    /// <param name="index">Index.</param>
    /// <typeparam name="T">The type of the property.</typeparam>
    public static T GetStaticPropertyValue<T>(
        this Type type,
        string propertyName,
        BindingFlags bindingAttr = instanceBindingFlags,
        object[] index = null
        )
    {
        return GetPropertyValue<T>(type, null, propertyName, bindingAttr, index);
    }

    #endregion

    #region Coroutine

    /// <summary>
    /// Останавливает корутину если она существует и присваивает значению null
    /// </summary>
    /// <returns>Истина если корутина работала и была остановлена</returns>
    public static bool StopCoroutine(this MonoBehaviour _behaviour, ref Coroutine _routine)
    {
        if (_routine == null)
            return false;
        _behaviour.StopCoroutine(_routine);
        _routine = null;
        return true;
    }

    public static void StartCoroutine(this MonoBehaviour _behaviour, IEnumerator _enum, ref Coroutine _routine)
    {
        if (_routine != null)
            _behaviour.StopCoroutine(_routine);
        _routine = _behaviour.StartCoroutine(_enum);
    }

    #endregion


}